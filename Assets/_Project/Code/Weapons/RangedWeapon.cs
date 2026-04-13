using UnityEngine;

public class RangedWeapon : Weapon
{
    [Header("Ranged Refs")]
    public Transform muzzlePoint;
    public ParticleSystem muzzleFlash;
    public AudioSource fireSound;

    protected int currentAmmoInClip;
    protected int currentReserveAmmo;

    protected override void Start()
    {
        base.Start();
        if (runtimeData != null)
        {
            currentAmmoInClip = runtimeData.clipSize;
            currentReserveAmmo = runtimeData.reserveAmmo;
        }
    }

    public override bool CanUse()
    {
        return base.CanUse() && currentAmmoInClip > 0;
    }

    public override void Use()
    {
        if (!CanUse()) return;
        Fire();
        base.Use();   // starts cooldown and OnUse
    }

    protected virtual void Fire()
    {
        if (muzzleFlash) muzzleFlash.Play();
        if (fireSound) fireSound.PlayOneShot(fireSound.clip);

        currentAmmoInClip--;

        //if (runtimeData.useHitscan)
        //    PerformHitscan();
        //else
        //    LaunchProjectile();

        EmitNoise(muzzlePoint.position);
    }

    //void PerformHitscan()
    //{
    //    for (int i = 0; i < runtimeData.pellets; i++)
    //    {
    //        Vector3 direction = GetSpreadDirection(muzzlePoint.forward);
    //        if (Physics.Raycast(muzzlePoint.position, direction, out RaycastHit hit, runtimeData.fireRange, runtimeData.hitMask))
    //        {
    //            var damageable = hit.collider.GetComponent<IDamageable>();
    //            damageable?.TakeDamage(runtimeData.damage);
    //            OnHit?.Invoke();
    //        }
    //    }
    //}

    void LaunchProjectile()
    {
        if (runtimeData.projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned.");
            return;
        }

        GameObject proj = Instantiate(runtimeData.projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = GetSpreadDirection(muzzlePoint.forward);
            rb.linearVelocity = dir * runtimeData.projectileSpeed;
        }

        //// Optionally set damage/range on the projectile script
        //var projectileScript = proj.GetComponent<Projectile>();
        //if (projectileScript != null)
        //{
        //    projectileScript.damage = runtimeData.damage;
        //    projectileScript.owner = transform.root.gameObject;
        //    projectileScript.hitMask = runtimeData.hitMask;
        //    projectileScript.range = runtimeData.fireRange;
        //    projectileScript.OnHitEvent = OnHit;   // Forward the OnHit event
        //}
    }

    Vector3 GetSpreadDirection(Vector3 baseDir)
    {
        if (runtimeData.bulletSpread <= 0) return baseDir;
        float spread = runtimeData.bulletSpread * 0.5f;
        Vector3 offset = new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0
        );
        return Quaternion.Euler(offset) * baseDir;
    }

    public virtual void Reload()
    {
        if (currentReserveAmmo <= 0 || currentAmmoInClip == runtimeData.clipSize) return;

        int needed = runtimeData.clipSize - currentAmmoInClip;
        int toReload = Mathf.Min(needed, currentReserveAmmo);
        currentAmmoInClip += toReload;
        currentReserveAmmo -= toReload;

        cooldownTimer = runtimeData.reloadTime;
    }
}