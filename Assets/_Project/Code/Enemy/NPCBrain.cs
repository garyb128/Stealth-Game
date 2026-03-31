using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Rendering;
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
        Search,
        Unconscious,
        Asleep,
        Recovery,
        Dead
    }

    // -------------------------------------------------------------------------
    // References
    // -------------------------------------------------------------------------
    [Header("References")]
    [SerializeField] private NPCArchetype npcArchetype;
    [SerializeField] private Transform target;

    private NavMeshAgent agent;
    private NPCPatrol npcPatrol;
    private NPCPerception npcPerception;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------
    [Header("Debug")]
    [SerializeField] private EnemyState currentState;
    public EnemyState CurrentState => currentState;
    public string StateName => currentState.ToString();
    public NPCPerception Perception => npcPerception;

    // The state this NPC returns to after completing an alert cycle
    // Set via ScriptableObject — could be Patrol or Idle depending on NPC type
    private EnemyState defaultState = EnemyState.Patrol;

    // Attacking variables
    [Header("RangedAttack")]
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] float attackRange = 30f;
    [SerializeField] int attackDamage = 20;
    [SerializeField] LayerMask attackMask; // Player + obstacles

    float attackTimer = 0f;
    PlayerHealth playerHealth;

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------
    public event Action<NPCBrain, EnemyState, EnemyState> OnStateChanged;
    public event Action<NPCBrain> OnPlayerContact;

    // Hardcoded values
    private float defaultSpeed;
    private float defaultAngularSpeed;
    private float defaultAcceleration;

    // -------------------------------------------------------------------------
    // Tunable values (set via Configure() from NPCArchetype)
    // -------------------------------------------------------------------------

    // Detection thresholds
    float investigateStartThreshold = 0.25f;
    float investigateStopThreshold = 0.05f;
    float alertThreshold = 0.8f;
    float alertStopThreshold = 0.15f;

    // Investigate
    float investigateDuration = 5f;
    float arriveDistance = 0.6f;
    float turnSpeed = 90f;
    float newLookInterval = 0.75f;
    float hearingMemoryTime = 2.0f;

    // Alert
    float loseSightGraceTime = 5f;
    float alertMinDuration = 3f;

    // Search
    float searchDuration = 10f;
    float searchRadius = 6f;
    int searchTries = 8;
    float searchArriveDistance = 0.8f;
    float scanDurationPerPoint = 1.5f;
    float repathInterval = 0.5f;

    // Starting values
    Vector3 startingPosition;
    Quaternion startingRotation;

    // -------------------------------------------------------------------------
    // Internal state — Investigate
    // -------------------------------------------------------------------------
    private bool arrivedAtInvestigatePoint;
    private float investigateTimer;
    private float lookTimer;
    private float targetYaw;
    private Vector3 investigatePoint;

    // -------------------------------------------------------------------------
    // Internal state — Alert
    // -------------------------------------------------------------------------
    private float alertTimer;

    // -------------------------------------------------------------------------
    // Internal state — Search
    // -------------------------------------------------------------------------
    private Vector3 searchAnchor;
    private Vector3 currentSearchPoint;
    private float searchTimer;
    private float repathTimer;
    private bool scanning;
    private float scanTimer;
    private float searchLookTimer;

    // States for being unconscious/dead/asleep
    [HideInInspector] public bool isKnockedOut;


    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        npcPatrol = GetComponent<NPCPatrol>();
        npcPerception = GetComponentInChildren<NPCPerception>();

        if (target == null && npcPerception != null)
            target = npcPerception.target;

        if (npcArchetype != null)
            ApplyArchetype();

        // Get player health for when attacking
        playerHealth = target.GetComponentInChildren<PlayerHealth>();

        // Cache starting position and rotation
        startingPosition = transform.position;
        startingRotation = transform.rotation;

        // Set defaults for moving and turning speed
        defaultSpeed = agent.speed;
        defaultAngularSpeed = agent.angularSpeed;
        defaultAcceleration = agent.acceleration;
    }

    private void Start()
    {
        SwitchState(defaultState);
    }

    private void Update()
    {
        if (npcPerception == null) return;

        // Physical contact is handled by OnTriggerEnter

        // Commit to Alert if detection is high enough from any non-alert state
        if (currentState != EnemyState.Alert &&
            npcPerception.Detection >= alertThreshold)
        {
            SwitchState(EnemyState.Alert);
            return;
        }

        // Transition from default state to Investigate when detection builds
        if (currentState == defaultState &&
            npcPerception.Detection >= investigateStartThreshold)
        {
            SwitchState(EnemyState.Investigate);
            return;
        }

        switch (currentState)
        {
            case EnemyState.Idle: UpdateIdle(); break;
            case EnemyState.Investigate: UpdateInvestigate(); break;
            case EnemyState.Alert: UpdateAlert(); break;
            case EnemyState.Search: UpdateSearch(); break;
        }
    }

    // -------------------------------------------------------------------------
    // State machine
    // -------------------------------------------------------------------------
    private void SwitchState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Reset detection when returning to a calm state
        if (newState == defaultState)
            npcPerception.Detection = 0f;

        var oldState = currentState;
        currentState = newState;
        OnStateChanged?.Invoke(this, oldState, newState);

        OnEnterState(newState);
    }

    private void OnEnterState(EnemyState state)
    {
        //Have these set true as default, some states overwrite these
        agent.enabled = true;
        npcPerception.enabled = true;

        switch (state)
        {
            case EnemyState.Idle:
                SetPatrolEnabled(false);
                agent.speed = defaultSpeed;
                agent.angularSpeed = defaultAngularSpeed;
                agent.acceleration = defaultAcceleration;
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.SetDestination(startingPosition);
                break;

            case EnemyState.Patrol:
                agent.speed = defaultSpeed;
                agent.angularSpeed = defaultAngularSpeed;
                agent.acceleration = defaultAcceleration;
                SetPatrolEnabled(true);
                agent.isStopped = false;
                agent.updateRotation = true;
                break;

            case EnemyState.Investigate:
                SetPatrolEnabled(false);
                agent.isStopped = false;
                agent.updateRotation = true;

                arrivedAtInvestigatePoint = false;
                investigateTimer = investigateDuration;
                lookTimer = 0f;
                investigatePoint = PickInvestigateDestination();

                // Only pathfind if we have a valid destination
                if (investigatePoint != Vector3.negativeInfinity)
                {
                    if (TryGetWalkablePoint(investigatePoint, out Vector3 reachable))
                        investigatePoint = reachable;
                    else
                    {
                        investigatePoint = transform.position;
                        arrivedAtInvestigatePoint = true;
                    }
                    agent.SetDestination(investigatePoint);
                }
                else
                {
                    investigatePoint = transform.position;
                    arrivedAtInvestigatePoint = true;
                }
                break;

            case EnemyState.Alert:
                SetPatrolEnabled(false);
                agent.isStopped = false;
                agent.updateRotation = true;
               // agent.angularSpeed = 360f;
                //agent.acceleration = 25f;
                //agent.speed = 6f;
                alertTimer = loseSightGraceTime;
                break;

            case EnemyState.Search:
                SetPatrolEnabled(false);
                agent.isStopped = false;
                agent.updateRotation = true;

                scanning = false;
                scanTimer = 0f;
                searchLookTimer = 0f;
                repathTimer = 0f;
                searchTimer = searchDuration;

                searchAnchor = npcPerception.LastSeenPosition != Vector3.negativeInfinity
                    ? npcPerception.LastSeenPosition
                    : transform.position;

                PickNewSearchPoint();
                break;
            case EnemyState.Unconscious:
                //Stop movement and disable perception/patrol while unconscious
                SetPatrolEnabled(false);
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.enabled = false;
                }
                if (npcPerception != null)
                    npcPerception.enabled = false;
                break;
        }
    }

    private void UpdateIdle()
    {
        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            agent.isStopped = true;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                startingRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }

    // -------------------------------------------------------------------------
    // Update — Investigate
    // -------------------------------------------------------------------------
    private void UpdateInvestigate()
    {
        // Only upgrade destination if we haven't arrived yet
        // Once arrived, commit to looking around rather than chasing
        if (!arrivedAtInvestigatePoint &&
            npcPerception.CanSeeTarget &&
            npcPerception.LastSeenPosition != Vector3.negativeInfinity)
        {
            Vector3 upgraded = npcPerception.LastSeenPosition;
            if (Vector3.Distance(upgraded, investigatePoint) > 1f)
            {
                investigatePoint = upgraded;
                agent.SetDestination(investigatePoint);
            }
        }

        if (!arrivedAtInvestigatePoint)
        {
            // If path is invalid, investigate current position instead
            if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                investigatePoint = transform.position;
                arrivedAtInvestigatePoint = true;
                agent.ResetPath();
            }

            if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
                arrivedAtInvestigatePoint = true;

            if (!arrivedAtInvestigatePoint) return;
        }

        // Arrived — look around and tick down timer
        investigateTimer -= Time.deltaTime;

        lookTimer -= Time.deltaTime;
        if (lookTimer <= 0f)
        {
            PickNewLookDirection();
            lookTimer = newLookInterval;
        }

        RotateTowards(targetYaw);

        if (investigateTimer <= 0f)
        {
            if (npcPerception.Detection >= alertThreshold)
                SwitchState(EnemyState.Alert);
            else
                SwitchState(defaultState);
        }
    }

    // -------------------------------------------------------------------------
    // Update — Alert
    // -------------------------------------------------------------------------
    private void UpdateAlert()
    {
        if (target == null) return;

        attackTimer -= Time.deltaTime;

        // Always try to shoot if we have line of sight
        if (npcPerception.HasLineOfSight)
        {
            TryShoot(); 

            agent.SetDestination(target.position);
            alertTimer = loseSightGraceTime;
            return;
        }

        // Lost line of sight — move toward last known position
        alertTimer -= Time.deltaTime;

        if (npcPerception.LastSeenPosition != Vector3.negativeInfinity)
            agent.SetDestination(npcPerception.LastSeenPosition);

        if (npcPerception.Detection <= alertStopThreshold || alertTimer <= 0f)
            SwitchState(EnemyState.Search);
    }

    // -------------------------------------------------------------------------
    // Update — Search
    // -------------------------------------------------------------------------
    private void UpdateSearch()
    {
        // Regained line of sight — go back to Alert
        if (npcPerception.HasLineOfSight)
        {
            SwitchState(EnemyState.Alert);
            return;
        }

        bool arrived = !agent.pathPending &&
                        agent.hasPath &&
                        agent.remainingDistance <= searchArriveDistance;

        if (arrived || scanning)
            searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f && npcPerception.Detection <= investigateStopThreshold)
        {
            SwitchState(defaultState);
            return;
        }

        // Repath if needed
        repathTimer -= Time.deltaTime;
        if (!agent.pathPending && (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid))
        {
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                PickNewSearchPoint();
            }
            return;
        }

        // Start scanning when arrived at search point
        if (arrived && !scanning)
        {
            scanning = true;
            scanTimer = scanDurationPerPoint;
            agent.isStopped = false;
            agent.updateRotation = false;
        }

        if (scanning)
        {
            scanTimer -= Time.deltaTime;
            searchLookTimer -= Time.deltaTime;

            if (searchLookTimer <= 0f)
            {
                PickNewLookDirection();
                searchLookTimer = newLookInterval;
            }

            RotateTowards(targetYaw);

            if (scanTimer <= 0f)
            {
                scanning = false;
                agent.isStopped = false;
                agent.updateRotation = true;
                repathTimer = repathInterval;
                PickNewSearchPoint();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Physical contact — instant Alert from any state
    // -------------------------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        npcPerception.Detection = 1f;

        if (target != null)
            npcPerception.SetLastSeenPosition(target.position);

        OnPlayerContact?.Invoke(this);
        SwitchState(EnemyState.Alert);
    }

    // Try to shoot the enemy
    void TryShoot()
    {
        if (attackTimer > 0f)
            return;

        attackTimer = attackCooldown;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;


        if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange, attackMask))
        {
            Debug.Log($"{hit.collider.gameObject}");

            if (hit.collider.CompareTag("Player"))
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        // Debug line so you can see the shot
        Debug.DrawRay(origin, direction * attackRange, Color.red, 0.2f);
    }

    // Knockout the enemy
    public void Knockout(float duration)
    {
        // If already knocked out, return
        if (isKnockedOut) return;

        isKnockedOut = true;
        // Switch to the new state so the other systems can react
        SwitchState(EnemyState.Unconscious);

        StartCoroutine(RecoverFromKnockout(duration));
    }

    IEnumerator RecoverFromKnockout(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (!isKnockedOut) yield break;

        isKnockedOut = false;

        SwitchState(defaultState);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Picks the best investigate destination based on what the NPC knows.
    /// Heard takes priority for initial destination; sight upgrades it during investigation.
    /// </summary>
    private Vector3 PickInvestigateDestination()
    {
        bool heardRecently = (Time.time - npcPerception.LastHeardTime) <= hearingMemoryTime;

        if (heardRecently && npcPerception.LastHeardPosition != Vector3.negativeInfinity)
            return npcPerception.LastHeardPosition;

        if (npcPerception.LastSeenPosition != Vector3.negativeInfinity)
            return npcPerception.LastSeenPosition;

        // Nothing heard or seen — investigate current position
        return transform.position;
    }

    private bool TryGetWalkablePoint(Vector3 desired, out Vector3 closest)
    {
        NavMeshPath path = new NavMeshPath();

        if (agent.CalculatePath(desired, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                closest = desired;
                return true;
            }

            if (path.status == NavMeshPathStatus.PathPartial && path.corners.Length > 0)
            {
                closest = path.corners[path.corners.Length - 1];
                return true;
            }
        }

        // Fallback — sample points in a circle around the desired point
        float bestDist = float.MaxValue;
        closest = transform.position;
        bool found = false;

        for (int i = 0; i < 16; i++)
        {
            float angle = (i / 16f) * Mathf.PI * 2f;
            Vector3 candidate = desired + new Vector3(Mathf.Cos(angle) * 5f, 0f, Mathf.Sin(angle) * 5f);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                if (agent.CalculatePath(hit.position, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    float dist = Vector3.Distance(hit.position, desired);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        closest = hit.position;
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    private void PickNewSearchPoint()
    {
        for (int i = 0; i < searchTries; i++)
        {
            Vector2 random = UnityEngine.Random.insideUnitCircle * searchRadius;
            Vector3 candidate = searchAnchor + new Vector3(random.x, 0f, random.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                currentSearchPoint = hit.position;
                agent.isStopped = false;
                agent.SetDestination(currentSearchPoint);
                return;
            }
        }
    }

    private void PickNewLookDirection()
    {
        Vector2 random = UnityEngine.Random.insideUnitCircle.normalized;
        targetYaw = Mathf.Atan2(random.x, random.y) * Mathf.Rad2Deg;
    }

    private void RotateTowards(float yaw)
    {
        Quaternion target = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    private void SetPatrolEnabled(bool enabled)
    {
        if (npcPatrol != null)
            npcPatrol.enabled = enabled;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------
    public void ApplyArchetype()
    {
        if (npcArchetype == null) return;
        npcArchetype.Apply(this, npcPatrol, npcPerception);
    }

    public void SetDefaultState(EnemyState state)
    {
        defaultState = state;
    }

    public void Configure(
        EnemyState defaultState,
        float investigateStartThreshold,
        float investigateStopThreshold,
        float alertThreshold,
        float alertStopThreshold,
        float investigateDuration,
        float arriveDistance,
        float turnSpeed,
        float newLookInterval,
        float hearingMemoryTime,
        float loseSightGraceTime,
        float alertMinDuration,
        float searchDuration,
        float searchRadius,
        int searchTries,
        float searchArriveDistance,
        float scanDurationPerPoint,
        float repathInterval
    )
    {
        this.defaultState = defaultState;
        this.investigateStartThreshold = investigateStartThreshold;
        this.investigateStopThreshold = investigateStopThreshold;
        this.alertThreshold = alertThreshold;
        this.alertStopThreshold = alertStopThreshold;
        this.investigateDuration = investigateDuration;
        this.arriveDistance = arriveDistance;
        this.turnSpeed = turnSpeed;
        this.newLookInterval = newLookInterval;
        this.hearingMemoryTime = hearingMemoryTime;
        this.loseSightGraceTime = loseSightGraceTime;
        this.alertMinDuration = alertMinDuration;
        this.searchDuration = searchDuration;
        this.searchRadius = searchRadius;
        this.searchTries = searchTries;
        this.searchArriveDistance = searchArriveDistance;
        this.scanDurationPerPoint = scanDurationPerPoint;
        this.repathInterval = repathInterval;
    }
}