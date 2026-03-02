using Unity.VisualScripting;
using UnityEngine;

public class NPCDebugHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Selection")]
    [SerializeField] private float maxConsiderDistance = 50f;
    NPCBrain current;

    private void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        //Find the closest NPC
        var brains = FindObjectsOfType<NPCBrain>();
        float bestDist = maxConsiderDistance * maxConsiderDistance;
        NPCBrain best = null;

        foreach (var b in brains)
        {
            float dsqr = (b.transform.position - player.position).sqrMagnitude;
            if (dsqr < bestDist)
            {
                bestDist = dsqr;
                best = b;
            }

            current = best;
        }

        if(current != null)
        {
            Debug.DrawLine(player.position, current.transform.position, Color.magenta);
        }
    }

    void OnGUI()
    {
        GUI.matrix = Matrix4x4.Scale(new Vector3(5f, 5f, 1f));
        GUI.color = Color.black;

        if (player == null)
        {
            GUI.Label(new Rect(10, 10, 600, 20), "NPCDebugHUD: No player assigned and no object tagged 'Player'.");
            return;
        }

        if (current == null)
        {
            GUI.Label(new Rect(10, 10, 600, 20), "NPCDebugHUD: No NPCBrain found in range.");
            return;
        }

        var p = current.Perception;

        float dist = Vector3.Distance(player.position, current.transform.position);

        int y = 10;
        GUI.Label(new Rect(10, y, 600, 20), $"Nearest NPC: {current.name}   Dist: {dist:F1}"); y += 20;
        GUI.Label(new Rect(10, y, 600, 20), $"State: {current.StateName}"); y += 20;

        if (p != null)
        {
            GUI.Label(new Rect(10, y, 600, 20), $"Detection: {p.Detection:F2}"); y += 20;
            GUI.Label(new Rect(10, y, 600, 20), $"CanSeeTarget: {p.CanSeeTarget}"); y += 20;
            GUI.Label(new Rect(10, y, 600, 20), $"InRange: {p.InRange}   InFOV: {p.InFOV}   Potential: {p.CanPotentiallySee}"); y += 20;
            GUI.Label(new Rect(10, y, 600, 20), $"LastSeen: {p.LastSeenPosition}"); y += 20;

            // If you added hearing: these will compile only if those properties exist
            // GUI.Label(new Rect(10, y, 600, 20), $"LastHeard: {p.LastHeardPosition}  t={p.LastHeardTime:F1}"); y += 20;
        }
        else
        {
            GUI.Label(new Rect(10, y, 600, 20), "Perception: (missing)"); y += 20;
        }
    }
}
