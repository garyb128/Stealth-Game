using UnityEngine;

public class RangedWeapon : MonoBehaviour, IWeaponAction
{
    public float damage = 25f;
    public float range = 100f;

    public Transform firePoint;

    public void Execute(WeaponData data)
    {
        RaycastHit hit;

        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);

            NPCHealth enemy = hit.collider.GetComponent<NPCHealth>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}