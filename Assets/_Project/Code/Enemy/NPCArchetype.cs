using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCArchetype", menuName = "NPC/Archetype")]
public class NPCArchetype : ScriptableObject
{
    // State
    [Header("Default State")]
    public NPCBrain.EnemyState defaultState;

    // Patrol
    [Header("Patrol")]
    public float patrolWaitTime = 1.5f;        // seconds to pause at each point
    public float patrolArriveDistance = 0.4f;  // how close counts as "arrived" (independent of stoppingDistance)
    [HideInInspector] public bool patrolPingPong = true;         // true = go 0..end..0..end, false = loop 0..end..0..
    [HideInInspector] public float waypointSnapDistance = 2f; // how far we'll search for nearby navmesh

    // Perception
    [Header("Perception – Vision")]
    public float viewDistance = 35f;
    [Range(1f, 179f)]
    public float fovDegrees = 90f;
    public float fovStickyTime = 0.15f;
    public float hearingFallOff = 1f;
    public float hearingDetectionCap = 0.5f; // new

    [Header("Perception – Detection")]
    [HideInInspector]  public float detectionRateClose = 3.0f;   // fill rate at point blank
    [HideInInspector] public float detectionRateFar = 0.05f;   // fill rate at max view distance
    [HideInInspector] public float detectionCapClose = 1.0f;   // max detection up close
    [HideInInspector] public float detectionCapFar = 0.2f;   // max detection at view distance edge
    [HideInInspector] public float decayRateLow = 1.0f;   // decay when detection is near 0
    [HideInInspector] public float decayRateHigh = 0.2f;   // decay when detection is near 1

    // Brain – Investigate
    [Header("Brain – Investigate")]
    public float investigateDuration = 5f;
    public float investigateArriveDistance = 0.6f;
    [HideInInspector] public float turnSpeed = 120f;
    [HideInInspector] public float newLookInterval = 0.75f;
    [HideInInspector] public float investigateStartThreshold = 0.25f;
    [HideInInspector] public float investigateStopThreshold = 0.05f;
    [HideInInspector] public float hearingMemoryTime = 2f;
    [HideInInspector] public float investigateRepathInterval = 0.4f;
    [HideInInspector] public int investigateRepathAttempts = 4;

    // Brain - Alert / Search
    [Header("Brain – Alert / Search")]
    [HideInInspector] public float loseSightGraceTime = 5f;
    [HideInInspector] public float searchDuration = 4f;
    [HideInInspector] public float alertStopThreshold = 0.15f;
    [HideInInspector] public float searchRadius = 6f;
    [HideInInspector] public int searchTries = 8;
    [HideInInspector] public float searchArriveDistance = 0.8f;
    [HideInInspector] public float repathInterval = 0.5f;
    [HideInInspector] public float scanDurationPerPoint = 1.5f;
    [HideInInspector] public float alertThreshold = 0.8f;
    [SerializeField] private float alertMinDuration = 3f;


    // Convenience method to apply archetype settings to an NPCPatrol component
    /// <summary>
    /// Applies every value stored in this archetype to the supplied components.
    /// Call this from NPCArchetypeApplier.Awake() (or wherever you initialise the NPC).
    /// Any of the three parameters may be null — that component will simply be skipped.
    /// </summary>
    public void Apply(NPCBrain brain, NPCPatrol patrol, NPCPerception perception)
    {
        if (patrol != null)
        {
            patrol.Configure(
                patrolWaitTime,
                patrolArriveDistance,
                patrolPingPong,
                waypointSnapDistance);
        }

        if (perception != null)
        {
            perception.Configure(
                viewDistance,
                fovDegrees,
                fovStickyTime,
                hearingFallOff,
                hearingDetectionCap,
                detectionRateClose,
                detectionRateFar,
                detectionCapClose,
                detectionCapFar,
                decayRateLow,
                decayRateHigh
            );
        }

        if (brain != null)
        {
            brain.Configure(
                defaultState,
                investigateStartThreshold,
                investigateStopThreshold,
                alertThreshold,
                alertStopThreshold,
                investigateDuration,
                investigateArriveDistance,
                turnSpeed,
                newLookInterval,
                hearingMemoryTime,
                loseSightGraceTime,
                alertMinDuration,
                searchDuration,
                searchRadius,
                searchTries, 
                searchArriveDistance,
                scanDurationPerPoint,
                repathInterval
            );
        }
    }
}