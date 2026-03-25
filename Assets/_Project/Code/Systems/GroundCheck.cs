using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [Header("Ground Check Settings")]
    [SerializeField] float radius = 0.2f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float checkOffset = 0.05f; // lifts the sphere slightly to avoid clipping
    [SerializeField] bool grounded;
    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; }

    void Update()
    {
        Vector3 checkPosition = transform.position + Vector3.up * checkOffset;

        Collider[] hits = Physics.OverlapSphere(
            checkPosition,
            radius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        IsGrounded = hits.Length > 0;

        grounded = IsGrounded;

        // Optional: get ground normal (useful for slopes later)
        if (IsGrounded)
        {
            // Take the first valid hit
            if (Physics.Raycast(checkPosition, Vector3.down, out RaycastHit hit, radius + 0.2f, groundMask))
            {
                GroundNormal = hit.normal;
            }
            else
            {
                GroundNormal = Vector3.up;
            }
        }
        else
        {
            GroundNormal = Vector3.up;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = transform.position + Vector3.up * checkOffset;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(checkPosition, radius);
    }
}