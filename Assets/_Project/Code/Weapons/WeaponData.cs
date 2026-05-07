using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identification")]
    public int weaponID;
    public string weaponName;

    [Space(10)]

    [Header("Damage")]
    public float damage;

    [Space(10)]

    [Header("Firing")]
    public float roundsPerMinute = 600f;
    public bool automatic;

    [Space(10)]

    [Header("Ammo")]
    public int maxAmmo;

    [Space(10)]

    [Header("Ballistics")]
    public float muzzleVelocity = 100f;
    public float gravity = 9.81f;
    public float maxSimulationTime = 3f;

    [Space(10)]

    [Header("Accuracy")]
    public float spreadAngle = 1f;
}