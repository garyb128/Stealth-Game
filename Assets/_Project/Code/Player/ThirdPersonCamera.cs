using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform cameraPivot;
    [SerializeField] PlayerInputHandler input;
    [SerializeField] PlayerController playerController; // PlayerController reference

    [Header("Follow Settings")]
    [SerializeField] Vector3 offset = new Vector3(0, 2, -4f);
    [SerializeField] float positionSmoothTime = 0.05f;

    [Header("Look Settings")]
    [SerializeField] float sensitivity = 1.5f;
    [SerializeField] float lookSmoothTime = 0.1f;
    [SerializeField] float minPitch = -40f;
    [SerializeField] float maxPitch = 75f;

    [Header("Aiming Zoom")]
    [SerializeField] Vector3 aimOffset = new Vector3(0, 1.5f, -2f);
    [SerializeField] float zoomSpeed = 5f; // how fast camera zooms in/out
    Vector3 currentOffset; // smoothly interpolated offset

    [Header("Collision")]
    [SerializeField] LayerMask collisionMask = ~0;
    [SerializeField] float collisionRadius = 0.2f;
    [SerializeField] float collisionSmoothTime = 0.1f;

    float yaw;
    float pitch;
    float currentYawVelocity;
    float currentPitchVelocity;
    Vector3 currentVelocity;
    float currentCollisionDistance;
    float collisionVelocity;

    void Start()
    {
        currentCollisionDistance = offset.magnitude;


        // Get offset
        currentOffset = offset;

        // Exclude the layer of the cameraPivot (assumed to be the player's layer)
        collisionMask &= ~(1 << cameraPivot.gameObject.layer);
    }

    void LateUpdate()
    {
        if (input != null)
        {
            HandleLook();
            HandleRotation();
            UpdateZoomOffset();
            HandlePosition();
        }
    }

    void HandleLook()
    {
        Vector2 look = input.LookInput;
        float targetYaw = yaw + look.x * sensitivity;
        float targetPitch = pitch - look.y * sensitivity;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref currentYawVelocity, lookSmoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref currentPitchVelocity, lookSmoothTime);
    }

    void HandleRotation()
    {
        cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandlePosition()
    {
        // Calculate desired camera position in world space
        Vector3 desiredOffset = cameraPivot.rotation * currentOffset;
        Vector3 desiredPosition = cameraPivot.position + desiredOffset;

        // Cast from desired position back to pivot to check for obstacles
        Vector3 directionToPivot = (cameraPivot.position - desiredPosition).normalized;
        float distanceToPivot = Vector3.Distance(desiredPosition, cameraPivot.position);

        float hitDistance = distanceToPivot;
        RaycastHit hit;
        if (Physics.SphereCast(desiredPosition, collisionRadius, directionToPivot, out hit, distanceToPivot, collisionMask))
        {
            // Move camera just before the obstacle
            hitDistance = Mathf.Max(0.3f, hit.distance - collisionRadius);
        }

        // Final camera position: from pivot back along direction by hitDistance
        Vector3 targetPosition = cameraPivot.position - directionToPivot * hitDistance;

        // Smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

        Vector3 lookTarget = cameraPivot.position;
        if(playerController != null && playerController.isAiming)
        {
            lookTarget = cameraPivot.position + cameraPivot.forward * 5f; // or a serialized float
        }

        // Always look at pivot
        transform.LookAt(lookTarget);
    }

    void UpdateZoomOffset()
    {
        Vector3 targetOffset = playerController != null && playerController.isAiming ? aimOffset : offset;
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, zoomSpeed * Time.deltaTime);
    }
}