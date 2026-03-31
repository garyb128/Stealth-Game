using System.ComponentModel.Design;
using Unity.Physics.GraphicsIntegration;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/WeaponData", fileName ="NewWeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("General")]
    public string weaponName = "New Weapon";
    public Sprite uiIcon;

    [Header("Attributes")]
    public float damage;
    public float clipSize;
    public float reserveAmmo;
    public float coolDown;
    public float reloadTime;

    [Header("Melee")]
    public bool isMelee = true;
    public float meleeRange = 1.4f;
    public float meleeRadius = 0.6f;
    public float meleeKnockOutDuration = 12f;
    [Tooltip("dot product threshold for 'behind' check. -1 = behind, 1 = front")]
    public float backstabDotThreshold = -0.65f;

    [Header("Noise")]
    public bool createsNoise = true;
    [Range(0f, 1f)] public float noiseLoudness = 0.8f;
    public float noiseRadius = 6f;

    [Header("Other")]
    public bool lethal = false;
}
