using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public Weapon currentWeapon;
    public PlayerInputHandler input;

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
    }
}