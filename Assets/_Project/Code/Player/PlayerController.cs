using Unity.Cinemachine;
using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Variables")]
    float speed = 3f;
    public float standingSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 6f;
    float gravity = -9.81f;
    public float jumpBufferTime = 0.2f;
    public float coyoteTime = 0.15f;
    public float groundCheckDistance = 0.2f;
    [SerializeField] float turnInputThreshold = 0.05f;
    [SerializeField, Range(0.5f,1f)] float sprintInputThreshold = 0.9f; // full-tilt threshold
    float standingHeight;
    float footstepTimer; // Will be replaced with movement based checks
    bool wasGrounded; // Was the player grounded?

    [Header("Movement Feel")]
    [SerializeField] float acceleration = 18f;
    [SerializeField] float deceleration = 12f;
    Vector3 planarVelocity; // world-space, y always 0
    Vector3 velocity;

    [Header("Turning Feel")]
    [SerializeField] float turnSpeedStanding = 10f; // deg/sec
    [SerializeField] float turnSpeedCrouched = 3f; // deg/sec
    [SerializeField] float turnAccel = 14f;  // how fast you can start turning
    [SerializeField] float turnDecel = 18f;  // how fast you settle when target stops changing
    float turnBlend; // internal 0..1-ish “turn power”

    private float jumpBufferCounter;
    private float coyoteTimeCounter; // Add this
    private bool isGrounded;
    bool isCrouching = false;

    // Cached references
    CharacterController characterController;
    PlayerInputHandler playerInputHandler;
    [SerializeField] CinemachineCamera cam;
    PlayerNoise playerNoise;

    private void Awake()
    {
        if(characterController == null) characterController = GetComponent<CharacterController>();
        if(playerInputHandler == null)  playerInputHandler = GetComponent<PlayerInputHandler>();
        if(playerNoise == null) playerNoise = GetComponent<PlayerNoise>();
        standingHeight = characterController.height;
    }

    void Update()
    {
        if (characterController != null && playerInputHandler != null)
        {
            // Better ground check using raycast
            isGrounded = Physics.Raycast(transform.position, Vector3.down,
                                         characterController.height / 2 + groundCheckDistance);

            // Handle movement
            Vector2 move = playerInputHandler.MoveInput;

            //Sprinting = full-tilt and not crouching
            bool isSprinting = !isCrouching && (move.sqrMagnitude >= sprintInputThreshold * sprintInputThreshold);

            Vector3 camForward = cam.transform.forward;
            Vector3 camRight = cam.transform.right;
            
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camRight * move.x + camForward * move.y;

            ////Rotate player to face movement direction
            if (move.sqrMagnitude > turnInputThreshold)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);

                float baseTurnSpeed = isCrouching ? turnSpeedCrouched : turnSpeedStanding;

                //Heaviness: turnblend ramps up when input is strong, down when weak
                float angle = Quaternion.Angle(transform.rotation, targetRot);
                bool needsTurn = angle > 0.1f;

                float targetBlend = needsTurn ? 1f : 0f;
                float turnRate = targetBlend > turnBlend ? turnAccel : turnDecel;
                turnBlend = Mathf.MoveTowards(turnBlend, targetBlend, turnRate * Time.deltaTime);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, baseTurnSpeed * turnBlend * Time.deltaTime);
            }
            else 
            {
                // bleed off turnblend when no input, so you settle into final facing direction
                turnBlend = Mathf.MoveTowards(turnBlend, 0f, turnDecel * Time.deltaTime);
            }

            bool hasInput = moveDir.sqrMagnitude > 0.0001f;
            Vector3 targetPlanar = hasInput ? (moveDir.normalized * speed) : Vector3.zero;

            float rate = hasInput ? acceleration : deceleration;
            
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanar, rate * Time.deltaTime);

            characterController.Move(planarVelocity * Time.deltaTime);

            // Handle crouching toggle
            if (isGrounded && !isCrouching && playerInputHandler.CrouchToggledThisFrame)
            {
                isCrouching = true;
            }
            else if (isCrouching && playerInputHandler.CrouchToggledThisFrame)
            {
                isCrouching = false;
            }

            // Adjust speed and height based on crouching state
            if (isCrouching)
            {
                speed = 2.5f; // Reduced speed when crouching
                characterController.height = standingHeight / 2; // Reduce height when crouching
            }
            else
            {
                speed = 5f; // Normal speed
                characterController.height = standingHeight; // Reset height when not crouching
            }

            // Simple footstep emit
            if (isGrounded && planarVelocity.magnitude > 0.1f)
            {
                float interval = isSprinting ? 0.3f : (isCrouching ? 0.6f : 0.45f);
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    playerNoise.EmitFootstep(isSprinting, isCrouching);
                    footstepTimer = interval;
                }
            }

            // Update coyote time counter
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime; // Reset when grounded
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime; // Tick down when in air
            }

            // Reset velocity when grounded
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // Buffer jump input
            if (playerInputHandler.JumpPressedThisFrame)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            // Jump if buffered input exists and we're grounded (or in coyote time)
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isCrouching)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpBufferCounter = 0f;
            }

            //Apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);

            // In Update:
            if (isGrounded && !wasGrounded)
                playerNoise.EmitLanding(1f);

            wasGrounded = isGrounded;
        }
       
    }
}