using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged,
    Throwable
}

[CreateAssetMenu(menuName = "Weapon/WeaponData", fileName = "NewWeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("General")]
    public string weaponName = "New Weapon";
    public Sprite uiIcon;
    public WeaponType weaponType = WeaponType.Melee;

    [Header("Attributes")]
    public float damage;
    public float coolDown;                  // Seconds between uses
    public bool lethal = false;

    [Header("Melee (only if WeaponType = Melee)")]
    public float meleeRange = 1.4f;
    public float meleeRadius = 0.6f;
    public float meleeKnockOutDuration = 12f;
    [Tooltip("Dot product threshold for backstab. -1 = behind, 1 = front")]
    public float backstabDotThreshold = -0.65f;

    [Header("Ranged (WeaponType = Ranged or Throwable)")]
    public GameObject projectilePrefab;      // The actual projectile to spawn
    public float projectileSpeed = 30f;
    public float fireRange = 50f;            // Max distance for hitscan or projectile lifetime
    public int clipSize = 10;
    public int reserveAmmo = 30;
    public float reloadTime = 1.5f;
    public float bulletSpread = 0f;          // Degrees, 0 = perfect accuracy
    public int pellets = 1;                  // For shotguns
    public bool useHitscan = false;          // If false, spawns projectilePrefab
    public LayerMask hitMask = ~0;           // What the projectile/raycast can hit

    [Header("Throwable (WeaponType = Throwable)")]
    public float throwForce = 15f;           // Initial velocity when thrown
    public float fuseTime = 3f;              // Seconds until explosion/detonation
    public bool explodesOnImpact = true;     // If false, uses fuse timer
    public GameObject explosionEffect;       // Optional VFX prefab

    [Header("Noise")]
    public bool createsNoise = true;
    [Range(0f, 1f)] public float noiseLoudness = 0.8f;
    public float noiseRadius = 6f;
}