using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public Weapon currentWeapon;
    PlayerController controller;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (controller.input.FirePressedThisFrame)
        {
            currentWeapon?.Use();
        }
    }
}