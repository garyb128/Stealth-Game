using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public PlayerInputActions input;

    public Vector2 MoveInput {  get; private set; }
    public Vector2 LookInput {  get; private set; }
    public bool JumpPressed {  get; private set; }
    public bool CrouchPressed {  get; private set; }
    public bool WhistlePressed { get; private set; }
    public bool FirePressed { get; private set; }

    void Awake()
    {
        if (input == null)
        {
            input = new PlayerInputActions();
        }
    }


    void OnEnable()
    {
        input.Player.Enable();

        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMoveCancelled;
        input.Player.Look.performed += OnLook;
        input.Player.Look.canceled += OnLookCancelled;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMoveCancelled;
        input.Player.Look.performed -= OnLook;
        input.Player.Look.canceled -= OnLookCancelled;
        input.Player.Disable();
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    void OnMoveCancelled(InputAction.CallbackContext ctx)
    {
        MoveInput = Vector2.zero;
    }

    void OnLook(InputAction.CallbackContext ctx)
    {
        LookInput = ctx.ReadValue<Vector2>();
    }

    void OnLookCancelled(InputAction.CallbackContext ctx)
    {
        LookInput = Vector2.zero;
    }

    // Check if jump was pressed this frame
    public bool JumpPressedThisFrame => input.Player.Jump.WasPressedThisFrame();

    // Check if crouch was toggled
    public bool CrouchToggledThisFrame => input.Player.Crouch.WasPressedThisFrame();

    // Check if whistle was pressed this frame
    public bool WhistlePressedThisFrame => input.Player.Whistle.WasPressedThisFrame();

    public bool FireHeld => input.Player.Fire.IsPressed();

    public bool FirePressedThisFrame => input.Player.Fire.WasPressedThisFrame();
}
