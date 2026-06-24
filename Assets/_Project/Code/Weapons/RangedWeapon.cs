using UnityEngine;

public class RangedWeapon : MonoBehaviour, IWeaponAction
{
    public Transform firePoint;
    [HideInInspector] public Camera playerCamera;

    void Awake()
    {
        if(playerCamera == null)
            playerCamera = Camera.main;
    }

    public void Execute(WeaponData data)
    {
        RaycastHit hit;

        // Cast from screen centre
        Ray cameraRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(cameraRay, out RaycastHit camHit, 1000f))
            targetPoint = camHit.point;
        else
            // No hit - use a point far away along the camera forward
            targetPoint = cameraRay.GetPoint(1000f);

        // Calculate spread direction
        Vector3 baseDirection = (targetPoint - firePoint.position).normalized;
        Vector3 shootDirection = GetSpreadDirection(baseDirection, data.spreadAngle);

        bool didHit = Ballistics.FireBullet(
            firePoint.position,
            shootDirection,
            data.muzzleVelocity,
            data.gravity,
            data.maxSimulationTime,
            out hit
        );

        if (didHit)
        {
            Debug.Log("Hit: " + hit.collider.name);

            if (hit.collider.TryGetComponent(out NPCHealth enemy))
            {
                enemy.TakeDamage(data.damage);
            }
        }
    }

    Vector3 GetSpreadDirection(Vector3 forward, float spreadAngle)
    {
        Vector2 spread = Random.insideUnitCircle * spreadAngle;

        Quaternion spreadRotation =
            Quaternion.Euler(spread.y, spread.x, 0);

        return spreadRotation * forward;
    }
}