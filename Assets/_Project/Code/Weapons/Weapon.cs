using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData data;

    int currentAmmo;
    IWeaponAction action;

    void Awake()
    {
        action = GetComponent<IWeaponAction>();
        currentAmmo = data.maxAmmo;
    }

    public void Use()
    {
        if (UsesAmmo() && currentAmmo <= 0)
        {
            Debug.Log("No ammo!");
            return;
        }

        action?.Execute(data);

        if (UsesAmmo())
        {
            currentAmmo--;
        }
    }

    bool UsesAmmo()
    {
        return data.maxAmmo > 0;
    }
}
