using UnityEngine;

[CreateAssetMenu(menuName ="Weapons/Weapon Data")]
public class WeaponData: ScriptableObject
{
    public int weaponID;
    public string weaponName;
    public float damage;
    public int maxAmmo;
}