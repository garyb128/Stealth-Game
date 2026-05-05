using UnityEngine;

public class MeleeWeapon : MonoBehaviour, IWeaponAction
{
    public void Execute(WeaponData data)
    {
        Debug.Log($"Swing with damage: {data.damage}");
    }
}