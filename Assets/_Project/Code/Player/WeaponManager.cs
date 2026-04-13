using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public Weapon[] weaponSlots;        // Assign all weapons the player can carry
    public int currentWeaponIndex = 0;

    public Weapon CurrentWeapon => weaponSlots[currentWeaponIndex];

    void Start()
    {
        // Deactivate all weapons except the first
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
                weaponSlots[i].gameObject.SetActive(i == currentWeaponIndex);
        }
    }

    void Update()
    {
        // Attack input (left mouse)
        if (Input.GetButtonDown("Fire1"))
        {
            CurrentWeapon?.Use();
        }

        // Reload input (R key)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (CurrentWeapon is RangedWeapon ranged)
                ranged.Reload();
        }

        // Scroll wheel to switch weapons
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int dir = scroll > 0 ? 1 : -1;
            SwitchWeapon((currentWeaponIndex + dir + weaponSlots.Length) % weaponSlots.Length);
        }

        // Number keys 1–9 to switch
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SwitchWeapon(i);
        }
    }

    public void SwitchWeapon(int index)
    {
        if (index == currentWeaponIndex) return;
        if (index < 0 || index >= weaponSlots.Length) return;

        // Unequip current
        CurrentWeapon?.OnUnequip();
        CurrentWeapon?.gameObject.SetActive(false);

        currentWeaponIndex = index;

        // Equip new
        CurrentWeapon?.gameObject.SetActive(true);
        CurrentWeapon?.OnEquip();
    }
}