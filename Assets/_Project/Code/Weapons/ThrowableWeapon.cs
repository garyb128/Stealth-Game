using UnityEngine;

public class ThrowableWeapon : Weapon
{
    [Header("Throwable Refs")]
    public Transform throwPoint;          // Where the object spawns (often same as muzzlePoint)

    protected int currentCount;           // How many of this throwable we have

    protected override void Start()
    {
        base.Start();
        currentCount = runtimeData != null ? runtimeData.clipSize : 1;
    }

    public override bool CanUse()
    {
        return base.CanUse() && currentCount > 0;
    }

    public override void Use()
    {
        if (!CanUse()) return;

        Throw();
        base.Use();   // Cooldown
    }

    protected virtual void Throw()
    {
        currentCount--;

        if (runtimeData.projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned for throwable.");
            return;
        }

        GameObject thrown = Instantiate(runtimeData.projectilePrefab, throwPoint.position, throwPoint.rotation);
        Rigidbody rb = thrown.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = throwPoint.forward * runtimeData.throwForce;
        }

        //var throwableScript = thrown.GetComponent<ThrowableProjectile>();
        //if (throwableScript != null)
        //{
        //    throwableScript.damage = runtimeData.damage;
        //    throwableScript.owner = transform.root.gameObject;
        //    throwableScript.fuseTime = runtimeData.fuseTime;
        //    throwableScript.explodesOnImpact = runtimeData.explodesOnImpact;
        //    throwableScript.explosionEffect = runtimeData.explosionEffect;
        //    throwableScript.OnHitEvent = OnHit;
        //}

        EmitNoise(throwPoint.position);

        // If we're out, optionally destroy or unequip the weapon
        if (currentCount <= 0)
        {
            // You can call a method on WeaponManager to switch away or destroy this weapon
            Debug.Log("Out of throwables!");
        }
    }
}