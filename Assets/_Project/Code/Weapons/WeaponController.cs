using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] Weapon[] weaponsInInventory;

    int weaponIndex;
    public Weapon currentWeapon;
    public PlayerInputHandler input;

    private void Start()
    {
        // Default as 0 but maybe can change based on mission etc.
        weaponIndex = 0;

        // Set the current weapon from the available weapons
        currentWeapon = weaponsInInventory[weaponIndex];
    }

    void Update()
    {
        if (currentWeapon == null)
            return;

        if (currentWeapon.data.automatic)
        {
            if (input.FireHeld)
            {
                currentWeapon.Use();
            }
        }
        else
        {
            if (input.FirePressedThisFrame)
            {
                currentWeapon.Use();
            }
        }

        HandleWeaponSwitch();

        // Reload the gun
        if (input.Reload)
        {
            currentWeapon.Reload();
        }
    }

    // Replace with weapon wheel in the future, cycles index to switch weapons for now
    void HandleWeaponSwitch()
    {
        if (input.SwitchWeapon)
        {
            weaponsInInventory[weaponIndex].gameObject.SetActive(false); // Hide current weapon

            weaponIndex = (weaponIndex + 1) % weaponsInInventory.Length; // Increment index

            currentWeapon = weaponsInInventory[weaponIndex]; // Set current weapon to whatever the index is

            weaponsInInventory[weaponIndex].gameObject.SetActive(true); // Enable new weapon

            Debug.Log($"Switched to {currentWeapon.data.weaponName}");
        }
    }
}
