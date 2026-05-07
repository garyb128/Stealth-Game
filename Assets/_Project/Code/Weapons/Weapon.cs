using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData data;

    int currentAmmo;
    IWeaponAction action;

    float nextFireTime;

    void Awake()
    {
        action = GetComponent<IWeaponAction>();

        currentAmmo = data.maxAmmo;
    }

    public void Use()
    {
        // Fire rate check
        if (Time.time < nextFireTime)
        {
            return;
        }

        // Ammo check
        if (currentAmmo <= 0)
        {
            Debug.Log("No ammo!");
            return;
        }

        action?.Execute(data);

        currentAmmo--;

        // Convert RPM to delay
        float fireDelay = 60f / data.roundsPerMinute;

        nextFireTime = Time.time + fireDelay;
    }
}