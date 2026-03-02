using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCBrain : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Investigate,
        Alert,
        Search
    }

    [Header("References")]
    NPCPatrol nPCPatrol;
    NPCPerception npcPerception;
    [SerializeField] NPCArchetype npcArchetype;
    [SerializeField] Transform target;
    Vector3 investigatePoint;

    [Header("Investigate Settings")]
    float investigateDuration = 5f; // How long to investigate before giving up
    float arriveDistance = 0.6f; // Distance to consider "arrived" at investigate point
    float turnSpeed = 90f; // Degrees per second to turn when investigating
    float newLookInterval = 0.75f; // How often to pick a new look direction when investigating
    float investigateStartThreshold = 0.25f; // must reach this to start investigating
    float investigateStopThreshold = 0.05f; // must fall below this before giving up
    float hearingMemoryTime = 2.0f; // seconds: how long a noise is considered "fresh"
    float investigateRepathInterval = 0.4f;
    int investigateRepathAttempts = 5;

    float investigateRepathTimer;
    int investigateRepathCount;

    [Header("Alert/Search Settings")]
    float loseSightGraceTime = 5f; // How long to keep being alert after losing sight of target
    float searchDuration = 4f; // How long to search after losing sight of target
    float alertStopThreshold = 0.15f; // If detection falls below this, go to search
    float searchRadius = 6f; // Radius around last seen point to search
    int searchTries = 8; // How many random points to try when picking a search point
    float searchArriveDistance = 0.8f; // How close to a search point to consider "arrived"
    float repathInterval = 0.5f; // How often to re-path to last seen position during search 
    float scanDurationPerPoint = 1.5f; // how long to scan at each search point
    float alertThreshold = 0.8f; // Detection level to go straight to alert from investigate

    bool scanning;
    float scanTimer;
    float loseSightTimer;
    float searchTimer;
    float repathTimer;
    float searchLookTimer;
    float alertMinDuration = 3f;
    float alertTimer;
    Vector3 searchAnchor; // usually seen last position when starting search
    Vector3 currentSearchPoint; // current point we're searching towards

    bool arrivedAtInvestigatePoint;
    float investigateTimer;
    float lookTimer;
    float targetYaw; // desired facing direction (y rotation)

    NavMeshAgent agent;
    [SerializeField] EnemyState enemyState;
    public event Action<NPCBrain,EnemyState,EnemyState> OnStateChanged;
    public event Action<NPCBrain> OnPlayerContact;

    public EnemyState CurrentState => enemyState;

    [Header("Debug")]
    public string StateName => enemyState.ToString();//Convenience for debugging
    public NPCPerception Perception => npcPerception; // Expose perception for debugging or other scripts

    private void Awake()
    {
        //Get nav mesh agent
        agent = GetComponent<NavMeshAgent>();

        //Get other components
        nPCPatrol = GetComponent<NPCPatrol>();
        npcPerception = GetComponentInChildren<NPCPerception>();

        // If you didn't assign target here, try to pull it from perception (since you already store it there).
        if (target == null && npcPerception != null)
            target = npcPerception.target;

        //Apply archetype settings if assigned
        if(npcArchetype != null)
        {
            ApplyArchetype();
        }
    }

    private void Start()
    {
        //Starting state is patrol(for now)
        SwitchState(EnemyState.Patrol);
    }

    public void ApplyArchetype()
    {
        if (npcArchetype == null) return;
        npcArchetype.Apply(this, nPCPatrol, npcPerception);
    }

    // Update is called once per frame
    void Update()
    {
        if (npcPerception == null)
            return;

        //Enter Alert when detection is high enough (commit)
        if (enemyState != EnemyState.Alert &&
            npcPerception.Detection >= alertThreshold)
        {
            SwitchState(EnemyState.Alert);
            return;
        }


        //If patrolling and detect player, switch to investigate
        if (enemyState == EnemyState.Patrol &&
            npcPerception.Detection >= investigateStartThreshold)
        {
            // In NPCBrain, wherever you call SwitchState(EnemyState.Investigate)
            Debug.Log($"[NPCBrain] Switching to Investigate. LastSeenPosition: {npcPerception.LastSeenPosition}, Detection: {npcPerception.Detection}");
            SwitchState(EnemyState.Investigate);
        }


        //Update investigate state
        if (enemyState == EnemyState.Investigate)
        {
            UpdateInvestigate();
        }

        //Update alert state
        if (enemyState == EnemyState.Alert)
        {
            UpdateAlert();
        }

        //Update search state
        if (enemyState == EnemyState.Search)
        {
            UpdateSearch();
        }
    }

    void SwitchState(EnemyState newState)
    {
        if (enemyState == newState)
            return;//Already in this state

        var oldState = enemyState;
        enemyState = newState;
        OnStateChanged?.Invoke(this, oldState, newState);

        enemyState = newState;

        switch (enemyState)
        {
            case EnemyState.Idle:
                //Stop patrol so it doesn't fight agent destination
                if (nPCPatrol != null)
                    nPCPatrol.enabled = false;
                break;
            case EnemyState.Investigate:
                //Stop patrol so it doesn't fight agent destination
                if (nPCPatrol != null)
                    nPCPatrol.enabled = false;

                arrivedAtInvestigatePoint = false;
                investigateTimer = investigateDuration;
                lookTimer = 0f;
                PickNewLookDirection();

                //Check if the last seen position is walkable AND reachable
                Vector3 desired;

                //Prefer vision anchor if we have one
                desired = npcPerception.LastSeenPosition;

                //If we can't see the target, but heard them recently, investigate the noise position
                bool heardRecently = (Time.time - npcPerception.LastHeardTime) <= hearingMemoryTime;

                if (!npcPerception.CanSeeTarget && heardRecently)
                {
                    desired = npcPerception.LastHeardPosition;
                }

                // Get the closest reachable point to the desired position
                if (TryGetWalkablePoint(desired, out Vector3 reachable))
                {
                    investigatePoint = reachable;
                }
                else
                {
                    // Absolute fallback - investigate current position
                    investigatePoint = transform.position;
                    arrivedAtInvestigatePoint = true;
                }

                agent.SetDestination(investigatePoint);
                break;
            case EnemyState.Patrol:
                //Enable patrol
                if (nPCPatrol != null)
                    nPCPatrol.enabled = true;

                agent.updateRotation = true;

                break;
            case EnemyState.Alert:
                if (nPCPatrol != null)
                    nPCPatrol.enabled = false;
                agent.updateRotation = true;
                alertTimer = alertMinDuration; // reset timer on entering alert
                break;
            case EnemyState.Search:
                //Search behaviour
                if (nPCPatrol != null)
                    nPCPatrol.enabled = false;

                agent.updateRotation = true;
                agent.isStopped = false;

                scanning = false;
                scanTimer = 0f;
                searchLookTimer = 0f;

                // Search longer - minimum 3 seconds even if detection is low
                searchTimer = Mathf.Lerp(10f, searchDuration, npcPerception.Detection); // Changed from 2f to 3f minimum
                repathTimer = 0f; // Force immediate pathing

                //Anchor search around last seen position
                searchAnchor = npcPerception.LastSeenPosition;

                //Pick initial search point
                PickNewSearchPoint(searchAnchor);

                searchLookTimer = 0f; // force immediate look direction pick
                PickNewLookDirection();
                break;

        }
    }



    void UpdateInvestigate()
    {
        // If we're already marked as arrived, skip path checks
        if (arrivedAtInvestigatePoint)
        {
            // Skip to the investigation behavior
        }
        else
        {
            // If agent can't reach the point, investigate here instead
            if (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                investigatePoint = transform.position;
                arrivedAtInvestigatePoint = true;
                agent.ResetPath(); // Clear the invalid path
            }

            //Check arrival
            if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            {
                arrivedAtInvestigatePoint = true;
            }

            if (!arrivedAtInvestigatePoint)
                return; // Don't start looking around or ticking down timer until we arrive
        }

        // Investigation behavior (look around, tick timer)
        investigateTimer -= Time.deltaTime;

        if (investigateTimer <= 0f &&
            npcPerception.Detection <= investigateStopThreshold)
        {
            SwitchState(EnemyState.Patrol);
            return;
        }

        lookTimer -= Time.deltaTime;

        if (lookTimer <= 0f)
        {
            PickNewLookDirection();
            lookTimer = newLookInterval;
        }

        RotateTowardsLookDirection();
    }

    bool TryGetWalkablePoint(Vector3 desired, out Vector3 closest)
    {
        NavMeshPath path = new NavMeshPath();

        if (agent.CalculatePath(desired, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                // Perfect! We can reach the exact spot
                closest = desired;
                return true;
            }
            else if (path.status == NavMeshPathStatus.PathPartial && path.corners.Length > 0)
            {
                // Can't reach the exact spot, but we got a partial path
                // Use the furthest point we CAN reach
                closest = path.corners[path.corners.Length - 1];
                return true;
            }
        }

        // Fallback: sample points in a circle and find the closest reachable one
        float bestDistance = float.MaxValue;
        closest = transform.position;
        bool foundAny = false;

        for (int i = 0; i < 16; i++) // Try 16 points around the target
        {
            float angle = (i / 16f) * Mathf.PI * 2f;
            float radius = 5f; // Start 5 units away from desired point

            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 candidate = desired + offset;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    float dist = Vector3.Distance(hit.position, desired);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        closest = hit.position;
                        foundAny = true;
                    }
                }
            }
        }

        return foundAny;
    }

    bool PickNewSearchPoint(Vector3 searchAnchor)
    {
        for(int i = 0; i<searchTries; i++)
        {
            //Random point in circle
            Vector2 random = UnityEngine.Random.insideUnitCircle * searchRadius;
            Vector3 candidate = searchAnchor + new Vector3(random.x, 0f, random.y);

            //Snap that candidate to navmesh
            if(NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                currentSearchPoint = hit.position;
                agent.isStopped = false;
                agent.SetDestination(currentSearchPoint);
                return true;
            }
        }

        return false;
    }


    private void RotateTowardsLookDirection()
    {
        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void PickNewLookDirection()
    {
        Vector2 random = UnityEngine.Random.insideUnitCircle.normalized;
        targetYaw = Mathf.Atan2(random.x, random.y) * Mathf.Rad2Deg;
    }

    private void UpdateAlert()
    {
        if (target == null)
            return;

        //If we can see the target, reset lose sight timer
        if (npcPerception.HasLineOfSight)
        {
            agent.SetDestination(target.position);
            loseSightTimer = loseSightGraceTime;
            return;
        }

        //If we can't see the target, start lose sight timer
        loseSightTimer -= Time.deltaTime;

        alertTimer -= Time.deltaTime;

        if (npcPerception.Detection <= alertStopThreshold && alertTimer <= 0f)
        {
            SwitchState(EnemyState.Search);
            return;
        }

        // Keep moving to last seen position
        if (npcPerception.LastSeenPosition != Vector3.negativeInfinity)
            agent.SetDestination(npcPerception.LastSeenPosition);

        if (loseSightTimer <= 0f && alertTimer <= 0f)
        {
            SwitchState(EnemyState.Search);
        }

    }

    void UpdateSearch()
    {
        //If we can see the target, go back to alert
        if (npcPerception.CanSeeTarget)
        {
            SwitchState(EnemyState.Alert);
            return;
        }

        //Check if we've arrived at the current search point
        bool arrived = !agent.pathPending && agent.hasPath && agent.remainingDistance <= searchArriveDistance;

        // Only count down search timer if we've arrived at a search point
        if (arrived || scanning)
        {
            searchTimer -= Time.deltaTime;
        }

        //When search timer runs out, and suspicion is low, go back to patrol
        if (searchTimer <= 0f &&
            npcPerception.Detection <= investigateStopThreshold)
        {
            SwitchState(EnemyState.Patrol);
            return;
        }

        //Repath to last seen position at intervals
        repathTimer -= Time.deltaTime;

        //Pick new search point if we arrived at current one or path is invalid
        if (!agent.pathPending && (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid))
        {
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                PickNewSearchPoint(searchAnchor);
            }
            return;
        }

        //If at search point, look around
        if (arrived && !scanning)
        {
            scanning = true;
            scanTimer = scanDurationPerPoint;

            agent.isStopped = true; //freeze movement while scanning
            agent.updateRotation = false; //disable auto-rotation while scanning
        }

        //If scanning, rotate to look directions
        if (scanning)
        {
            scanTimer -= Time.deltaTime;

            searchLookTimer -= Time.deltaTime;
            if (searchLookTimer <= 0f)
            {
                PickNewLookDirection();
                searchLookTimer = newLookInterval;
            }

            RotateTowardsLookDirection();

            //If done scanning, pick new search point
            if (scanTimer <= 0f)
            {
                scanning = false;
                agent.isStopped = false;
                agent.updateRotation = true; //re-enable auto-rotation

                //Pick new search point
                repathTimer = repathInterval;
                PickNewSearchPoint(searchAnchor);
            }
            return;
        }
    }

    public void Configure(
    float investigateDuration,
    float arriveDistance,
    float turnSpeed,
    float newLookInterval,
    float investigateStartThreshold,
    float investigateStopThreshold,
    int investigateRepathAttempts,
    float hearingMemoryTime,
    float loseSightGraceTime,
    float searchDuration,
    float alertStopThreshold,
    float searchRadius,
    int searchTries,
    float searchArriveDistance,
    float repathInterval,
    float scanDurationPerPoint,
    float alertThreshold
)
    {
        this.investigateDuration = investigateDuration;
        this.arriveDistance = arriveDistance;
        this.turnSpeed = turnSpeed;
        this.newLookInterval = newLookInterval;
        this.investigateStartThreshold = investigateStartThreshold;
        this.investigateStopThreshold = investigateStopThreshold;
        this.investigateRepathAttempts = investigateRepathAttempts;        
        this.hearingMemoryTime = hearingMemoryTime;
        this.loseSightGraceTime = loseSightGraceTime;
        this.searchDuration = searchDuration;
        this.alertStopThreshold = alertStopThreshold;
        this.searchRadius = searchRadius;
        this.searchTries = searchTries;
        this.searchArriveDistance = searchArriveDistance;
        this.repathInterval = repathInterval;
        this.scanDurationPerPoint = scanDurationPerPoint;
        this.alertThreshold = alertThreshold;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        npcPerception.Detection = 1f;
        npcPerception.SetLastSeenPosition(target.position);
        OnPlayerContact?.Invoke(this);
        SwitchState(EnemyState.Alert);
    }
}
