using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCPatrol : MonoBehaviour
{
    [Header("Patrol Points")]
    [SerializeField] private Transform[] patrolPoints;
    [HideInInspector] public int patrolPointsNum;

    [Header("Patrol Settings")]
    private float waitTime = 1.5f;        // seconds to pause at each point
    private float arriveDistance = 0.4f;  // how close counts as "arrived" (independent of stoppingDistance)
    private bool pingPong = true;         // true = go 0..end..0..end, false = loop 0..end..0..

    private float waypointSnapDistance = 2f; // how far we'll search for nearby navmesh
    private NavMeshAgent agent;
    private int index = 0;
    private int dir = 1; // 1 forward, -1 backward

    private float waitTimer = 0f;
    private bool waiting = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        // Ensure agent is allowed to move when patrol takes control again
        agent.isStopped = false;
        agent.ResetPath();

        waiting = false;
        waitTimer = 0f;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            // Find nearest patrol point instead of using current index
            index = FindNearestPatrolPoint();
            SetDestinationToIndex();
            patrolPointsNum = patrolPoints.Length;
        }
    }

    // Add this new method:
    private int FindNearestPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return 0;

        int nearest = 0;
        float shortestDist = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            float dist = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (dist < shortestDist)
            {
                shortestDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }


    private void Update()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        // If we're waiting at a point, count down then go next
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                GoToNextPoint();
            }
            return;
        }

        // Don't make decisions while Unity is still computing a path
        if (agent.pathPending)
            return;

        // If agent can't reach current point, skip it (prevents getting "stuck")
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            GoToNextPoint();
            return;
        }

        // Arrival check: use remainingDistance when possible, and fall back to manual distance.
        float stopDist = Mathf.Max(agent.stoppingDistance, arriveDistance);
        bool arrived =
            agent.pathStatus == NavMeshPathStatus.PathComplete &&
            agent.remainingDistance <= stopDist + 0.05f; // small tolerance

        if (arrived)
        {
            waiting = true;
            waitTimer = waitTime;
        }
    }

    private void GoToNextPoint()
    {
        if (patrolPoints.Length == 1)
        {
            SetDestinationToIndex();
            return;
        }

        if (pingPong)
        {
            // Reverse direction at the ends
            if (index >= patrolPoints.Length - 1) dir = -1;
            else if (index <= 0) dir = 1;

            index += dir;
        }
        else
        {
            // Simple loop
            index = (index + 1) % patrolPoints.Length;
        }

        SetDestinationToIndex();
    }

    private void SetDestinationToIndex()
    {
        Transform point = patrolPoints[index];
        if (point == null) return;

        // Snap waypoint to the nearest walkable navmesh position
        if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, waypointSnapDistance, NavMesh.AllAreas))
        {
            // Can't find navmesh near this point: skip it so we don't stall.
            GoToNextPoint();
            return;
        }

        agent.isStopped = false;

        // SetDestination returns false if it cannot compute a path to that position
        bool ok = agent.SetDestination(hit.position);
        if (!ok)
        {
            GoToNextPoint();
        }
    }

    public void Configure(float waitTime, float arriveDistance, bool pingPong, float waypointSnapDistance)
    {
        this.waitTime = waitTime;
        this.arriveDistance = arriveDistance;
        this.pingPong = pingPong;
        this.waypointSnapDistance = waypointSnapDistance;
    }
}