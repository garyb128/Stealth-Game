using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public PlayerInputActions input;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    void Awake()
    {
        input ??= new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Player.Enable();

        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMove;

        input.Player.Look.performed += OnLook;
        input.Player.Look.canceled += OnLook;
    }

    void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMove;

        input.Player.Look.performed -= OnLook;
        input.Player.Look.canceled -= OnLook;

        input.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext ctx)
    {
        LookInput = ctx.ReadValue<Vector2>();
    }

    // Buttons (polling is correct here)
    public bool JumpPressedThisFrame => input.Player.Jump.WasPressedThisFrame();

    public bool CrouchPressedThisFrame => input.Player.Crouch.WasPressedThisFrame();

    public bool WhistlePressedThisFrame => input.Player.Whistle.WasPressedThisFrame();

    public bool FirePressedThisFrame => input.Player.Fire.WasPressedThisFrame();

    public bool FireHeld => input.Player.Fire.IsPressed();

    public bool AimHeld => input.Player.Aim.IsPressed();
}