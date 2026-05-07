using UnityEngine;

public class RangedWeapon : MonoBehaviour, IWeaponAction
{
    public Transform firePoint;

    public void Execute(WeaponData data)
    {
        RaycastHit hit;

        // Generate spread direction
        Vector3 shootDirection = GetSpreadDirection(
            firePoint.forward,
            data.spreadAngle
            );

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