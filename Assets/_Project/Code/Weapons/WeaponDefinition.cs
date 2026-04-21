using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    public enum WeaponType
    {
        Melee,
        Ranged,
        Throwable,
        UtilityThrowable
    }

    [Header("Identity")]
    public string weaponID;
    public string weaponName;
    public WeaponType weaponType;

    [Header("Stats")]
    public float damage;
    public float noise;

    [Header("Behaviour")]
    public WeaponBehaviour behaviour;

    [Header("View")]
    public WeaponViewDefinition view;
}
