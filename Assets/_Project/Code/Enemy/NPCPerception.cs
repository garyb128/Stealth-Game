using System;
using UnityEngine;

public class NPCPerception : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform eyes;
    public Transform target;
    [SerializeField] LayerMask obstructionMask;
    PlayerExposure playerExposure;

    [Header("Vision")]
    float viewDistance = 35f;
    float fovDegrees = 120f;
    float fovStickyTime = 0.15f;
    float fovStickyTimer;

    [Header("Hearing")]
    [SerializeField] float hearingFallOff = 1.0f;
    [SerializeField] private float hearingDetectionCap = 0.5f; // can't push past investigate threshold via sound alone
    public Vector3 LastHeardPosition { get; private set; }
    public float LastHeardTime { get; private set; }

    [Header("Detection")]
    float detectionRateClose = 2.0f;   // fill rate at point blank
    float detectionRateFar = 0.1f;   // fill rate at max view distance
    float detectionCapClose = 1.0f;   // max detection up close
    float detectionCapFar = 0.0f;   // max detection at view distance edge
    float decayRateLow = 1.0f;   // decay when detection is near 0
    float decayRateHigh = 0.2f;   // decay when detection is near 1
   
    [Header("Debug")]
    [SerializeField] private bool debugLogStateChanges = true;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private int coneSegments = 16;

    public float Detection { get; set; }
    public Vector3 LastSeenPosition { get; private set; }
    public bool CanSeeTarget { get; private set; }


    public bool InRange { get; private set; }
    public bool InFOV { get; private set; }
    public bool HasLineOfSight { get; private set; }
    public bool CanPotentiallySee { get; private set; }

    bool lastCanPotentiallySee = false;

    private void Awake()
    {
        LastSeenPosition = Vector3.negativeInfinity;
        LastHeardPosition = Vector3.negativeInfinity;

        if (target != null)
            playerExposure = target.GetComponentInChildren<PlayerExposure>();
    }

    void Update()
    {
        if (target == null) return;

        Transform origin = (eyes != null) ? eyes : transform;

        Vector3 targetPoint = target.position + Vector3.up * 1.0f;
        Vector3 toTarget = targetPoint - origin.position;

        float distToTarget = toTarget.magnitude;

        if (distToTarget < 0.001f)
        {
            InRange = true;
            InFOV = true;
            CanPotentiallySee = true;

            Debug.Log("PLAYER IN SIGHT");
            return;
        }

        Vector3 dirToTarget = toTarget / distToTarget;

        float distSqr = toTarget.sqrMagnitude;
        float viewDistSqr = viewDistance * viewDistance;
        InRange = distSqr <= viewDistSqr;

        Vector3 toTargetFlat = toTarget;

        // Use origin.right because this rig's forward faces +X (not Unity's default +Z).
        Vector3 forwardFlat = origin.right;

        toTargetFlat.y = 0f;
        forwardFlat.y = 0f;

        if (toTargetFlat.sqrMagnitude < 0.01f)
        {
            InFOV = true;
        }
        else
        {
            forwardFlat.Normalize();
            toTargetFlat.Normalize();

            float angle = Vector3.Angle(forwardFlat, toTargetFlat);

            float verticalDelta = Mathf.Abs(target.position.y - origin.position.y);
            bool withinVertical = verticalDelta <= 3.0f;

            InFOV = withinVertical && (angle <= (fovDegrees * 0.5f));

            if (InFOV)
                fovStickyTimer = fovStickyTime;
            else
                fovStickyTimer -= Time.deltaTime;

            InFOV = InFOV || fovStickyTimer > 0f;
        }

        CanPotentiallySee = InRange && InFOV;

        if (CanPotentiallySee != lastCanPotentiallySee)
        {
            lastCanPotentiallySee = CanPotentiallySee;
        }

        bool inSight = false;

        if (CanPotentiallySee)
        {
            bool hitSomething = Physics.Raycast(
                origin.position,
                dirToTarget,
                out RaycastHit hitInfo,
                distToTarget,
                obstructionMask
            );

            inSight = !hitSomething;
        }

        bool seenThisFrame = (CanPotentiallySee && inSight);

        HasLineOfSight = seenThisFrame;

        // Always update last seen position when we have raw line of sight
        // regardless of exposure — guard tracks where you are if they can see you
        if (HasLineOfSight)
            LastSeenPosition = target.position;

        // A player in near-darkness shouldn't be seen at all
        bool effectivelySeen = seenThisFrame && (playerExposure == null || playerExposure.Exposure > 0.05f);

        CanSeeTarget = effectivelySeen;

        if (effectivelySeen)
        {
            float exposure = playerExposure.Exposure;

            float t = distToTarget / viewDistance;
            float speed = Mathf.Lerp(detectionRateClose, detectionRateFar, t) * exposure;
            float maxDet = Mathf.Lerp(detectionCapClose, detectionCapFar, t) * exposure;
            Detection = Mathf.MoveTowards(Detection, maxDet, speed * Time.deltaTime);
        }
        else
        {
            float decayRate = Mathf.Lerp(decayRateLow, decayRateHigh, Detection);
            Detection = Mathf.MoveTowards(Detection, 0f, decayRate * Time.deltaTime);
        }
    }

    public void HearNoise(Vector3 pos, float strength01)
    {
        LastHeardPosition = pos;
        LastHeardTime = Time.time;

        float newDetection = Mathf.Clamp01(Detection + strength01 * hearingFallOff);
        Detection = Mathf.Min(newDetection, hearingDetectionCap);
    }

    public void Configure(
    float viewDistance,
    float fovDegrees,
    float fovStickyTime,
    float hearingFallOff,
    float detectionRateClose,
    float detectionRateFar,
    float detectionCapClose,
    float detectionCapFar,
    float decayRateLow,
    float decayRateHigh
)
    {
        this.viewDistance = viewDistance;
        this.fovDegrees = fovDegrees;
        this.fovStickyTime = fovStickyTime;
        this.hearingFallOff = hearingFallOff;

        this.detectionRateClose = 2.0f;   // fill rate at point blank
        this.detectionRateFar = 0.1f;   // fill rate at max view distance
        this.detectionCapClose = 1.0f;   // max detection up close
        this.detectionCapFar = 0.0f;   // max detection at view distance edge
        this.decayRateLow = 1.0f;   // decay when detection is near 0
        this.decayRateHigh = 0.2f;   // decay when detection is near 1
}

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Transform origin = eyes ? eyes : transform;

        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawSphere(origin.position, viewDistance);

        float halfAngle = fovDegrees * 0.5f;
        float radius = Mathf.Tan(halfAngle * Mathf.Deg2Rad) * viewDistance;

        Vector3 fwd = origin.right;
        Vector3 right = origin.forward;
        Vector3 up = origin.up;

        Vector3 center = origin.position + fwd * viewDistance;
        Vector3 prevPoint = center + right * radius;

        Gizmos.color = Color.cyan;

        int seg = Mathf.Max(6, coneSegments);
        for (int i = 1; i <= seg; i++)
        {
            float t = (float)i / seg * Mathf.PI * 2f;

            Vector3 offset = (Mathf.Cos(t) * right + Mathf.Sin(t) * up) * radius;
            Vector3 point = center + offset;

            Gizmos.DrawLine(origin.position, point);
            Gizmos.DrawLine(prevPoint, point);

            prevPoint = point;
        }

        if (target)
        {
            Gizmos.color = CanPotentiallySee ? Color.green : (InRange ? Color.yellow : Color.red);
            Gizmos.DrawLine(origin.position, target.position);
        }
    }

    public void SetLastSeenPosition(Vector3 position)
    {
        LastSeenPosition = position;
    }
}