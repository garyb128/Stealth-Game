using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData data;

    int currentAmmo;
    int currentReserve;

    IWeaponAction action;

    float nextFireTime;

    public Coroutine reloadCoroutine;

    void Awake()
    {
        action = GetComponent<IWeaponAction>();

        currentAmmo = data.magazineSize;
        currentReserve = data.startingReserveAmmo;
    }

    void OnDestroy()
    {
        reloadCoroutine = null;
    }

    public void Use()
    {
        // Block firing while reloading
        if (reloadCoroutine != null)
            return;

        // Fire rate check
        if (Time.time < nextFireTime)
            return;

        // No ammo in magazine → try reload
        if (currentAmmo <= 0)
        {
            Reload();
            return;
        }

        action?.Execute(data);

        currentAmmo--;

        float fireDelay = 60f / data.roundsPerMinute;
        nextFireTime = Time.time + fireDelay;
    }

    public void Reload()
    {
        // Already reloading
        if (reloadCoroutine != null)
            return;

        // Magazine already full
        if (currentAmmo >= data.magazineSize)
            return;

        // No reserve ammo
        if (currentReserve <= 0)
            return;

        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        Debug.Log("reloading...");

        yield return new WaitForSeconds(data.reloadTime);

        int needed = data.magazineSize - currentAmmo;
        int toLoad = Mathf.Min(needed, currentReserve);

        currentAmmo += toLoad;
        currentReserve -= toLoad;

        Debug.Log($"Reload finished: {currentAmmo}/{currentReserve}");

        reloadCoroutine = null;
    }
}