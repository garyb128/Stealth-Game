using UnityEngine;

public class NPCPerception : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform eyes;
    public Transform target;
    private PlayerExposure playerExposure;

    [Header("Vision")]
    private float viewDistance = 35f;
    private float fovDegrees = 120f;
    private float fovStickyTime = 0.15f;
    private float fovStickyTimer;

    [Header("Hearing")]
    private float hearingFallOff = 1.0f;
    private float hearingDetectionCap = 0.5f;
    public Vector3 LastHeardPosition { get; private set; }
    public float LastHeardTime { get; private set; }

    [Header("Detection")]
    private float detectionRateClose = 2.0f;
    private float detectionRateFar = 0.1f;
    private float detectionCapClose = 1.0f;
    private float detectionCapFar = 0.0f;
    private float decayRateLow = 1.0f;
    private float decayRateHigh = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private int coneSegments = 16;

    // -------------------------------------------------------------------------
    // Public state — read by NPCBrain and other systems
    // -------------------------------------------------------------------------
    public float Detection { get; set; }
    public Vector3 LastSeenPosition { get; private set; }
    public bool CanSeeTarget { get; private set; }
    public bool InRange { get; private set; }
    public bool InFOV { get; private set; }
    public bool HasLineOfSight { get; private set; }
    public bool CanPotentiallySee { get; private set; }

    // -------------------------------------------------------------------------
    // Raycast result — written by VisionRaycastBridge, read in Update
    // Replaces the direct Physics.Raycast call
    // -------------------------------------------------------------------------
    bool raycastHitSomething = false;
    bool hasRaycastResult = false; // false until the first result arrives

    // Cached per-frame vision query data — written in Update, read by the bridge
    Vector3 cachedOrigin;
    Vector3 cachedDirection;
    float cachedDistance;
    bool cachedQueryValid;
    Vector3 cachedNPCForward;
    Vector3 cachedTargetPosition;
    Vector3 cachedNPCPosition;

    bool lastCanPotentiallySee;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Awake()
    {
        LastSeenPosition = Vector3.negativeInfinity;
        LastHeardPosition = Vector3.negativeInfinity;

        if (target != null)
            playerExposure = target.GetComponentInChildren<PlayerExposure>();
    }

    private void Update()
    {
        if (target == null)
        {
            cachedQueryValid = false;
            return;
        }

        Transform origin = (eyes != null) ? eyes : transform;

        Vector3 targetPoint = target.position + Vector3.up * 1.0f;
        Vector3 toTarget = targetPoint - origin.position;
        float distToTarget = toTarget.magnitude;

        if (distToTarget < 0.001f)
        {
            InRange = true;
            InFOV = true;
            CanPotentiallySee = true;
            cachedQueryValid = false;
            return;
        }

        Vector3 dirToTarget = toTarget / distToTarget;

        // -------------------------------------------------------------------------
        // Range check
        // -------------------------------------------------------------------------
        float distSqr = toTarget.sqrMagnitude;
        float viewDistSqr = viewDistance * viewDistance;
        InRange = distSqr <= viewDistSqr;

        // -------------------------------------------------------------------------
        // FOV check
        // Use origin.right because this rig's forward faces +X not +Z
        // -------------------------------------------------------------------------
        Vector3 toTargetFlat = toTarget;
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
            lastCanPotentiallySee = CanPotentiallySee;

        // -------------------------------------------------------------------------
        // Cache vision query data for the bridge to read this frame
        // The bridge passes this to VisionRaycastSystem which fires the ray
        // and writes the result back via SetRaycastResult next frame
        // -------------------------------------------------------------------------
        if (CanPotentiallySee || InRange) // cache even if not potentially visible so FOV job can calculate
        {
            cachedOrigin = origin.position;
            cachedDirection = dirToTarget;
            cachedDistance = distToTarget;
            cachedNPCPosition = transform.position;
            cachedNPCForward = origin.right;
            cachedTargetPosition = target.position;
            cachedQueryValid = true;
        }
        else
        {
            cachedQueryValid = false;
        }

        // -------------------------------------------------------------------------
        // Use raycast result from last frame
        // Falls back to false (can't see) until first result arrives
        // -------------------------------------------------------------------------
        bool inSight = CanPotentiallySee && hasRaycastResult && !raycastHitSomething;

        bool seenThisFrame = CanPotentiallySee && inSight;

        HasLineOfSight = seenThisFrame;

        // Always update last seen position when we have raw line of sight
        // regardless of exposure — guard tracks position if they can see you
        if (HasLineOfSight)
            LastSeenPosition = target.position;

        // A player in near-darkness shouldn't be seen at all
        bool effectivelySeen = seenThisFrame &&
            (playerExposure == null || playerExposure.Exposure > 0.05f);

        CanSeeTarget = effectivelySeen;

        if (effectivelySeen)
        {
            float exposure = playerExposure != null ? playerExposure.Exposure : 1f;
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

    // -------------------------------------------------------------------------
    // Bridge API
    // Called by VisionRaycastBridge every frame
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the current vision query parameters for the bridge to pass to ECS.
    /// Returns false if there is no valid query this frame.
    /// </summary>
    public bool HasVisionQuery(
        out Vector3 origin,
        out Vector3 direction,
        out float distance,
        out Vector3 npcPosition,
        out Vector3 npcForward,
        out Vector3 targetPosition,
        out float viewDistance,
        out float fovDegrees)
    {
        origin = cachedOrigin;
        direction = cachedDirection;
        distance = cachedDistance;
        npcPosition = cachedNPCPosition;
        npcForward = cachedNPCForward;
        targetPosition = cachedTargetPosition;
        viewDistance = this.viewDistance;
        fovDegrees = this.fovDegrees;
        return cachedQueryValid;
    }

    public void SetFOVResult(bool inRange, bool inFOV)
    {
        InRange = inRange;
        InFOV = inFOV;
        CanPotentiallySee = inRange && inFOV;
    }

    /// <summary>
    /// Called by VisionRaycastBridge with the result of last frame's raycast.
    /// </summary>
    public void SetRaycastResult(bool hitSomething)
    {
        raycastHitSomething = hitSomething;
        hasRaycastResult = true;
    }

    // -------------------------------------------------------------------------
    // Hearing
    // -------------------------------------------------------------------------
    public void HearNoise(Vector3 pos, float strength01)
    {
        LastHeardPosition = pos;
        LastHeardTime = Time.time;

        float newDetection = Mathf.Clamp01(Detection + strength01 * hearingFallOff);
        Detection = Mathf.Min(newDetection, hearingDetectionCap);
    }

    // -------------------------------------------------------------------------
    // Configuration — called by NPCArchetype
    // -------------------------------------------------------------------------
    public void Configure(
        float viewDistance,
        float fovDegrees,
        float fovStickyTime,
        float hearingFallOff,
        float hearingDetectionCap,
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
        this.hearingDetectionCap = hearingDetectionCap;
        this.detectionRateClose = detectionRateClose;
        this.detectionRateFar = detectionRateFar;
        this.detectionCapClose = detectionCapClose;
        this.detectionCapFar = detectionCapFar;
        this.decayRateLow = decayRateLow;
        this.decayRateHigh = decayRateHigh;
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------
    public void SetLastSeenPosition(Vector3 position)
    {
        LastSeenPosition = position;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------
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
            Gizmos.color = CanPotentiallySee
                ? Color.green
                : (InRange ? Color.yellow : Color.red);
            Gizmos.DrawLine(origin.position, target.position);
        }
    }
}