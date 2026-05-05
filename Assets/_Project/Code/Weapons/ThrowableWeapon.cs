using UnityEngine;

public class ThrowableWeapon : MonoBehaviour, IWeaponAction
{
    public GameObject throwablePrefab;

    public void Execute(WeaponData data)
    {
        Debug.Log("Throw object");
    }
}