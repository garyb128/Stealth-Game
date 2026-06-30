using NUnit.Framework.Interfaces;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float standingSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float acceleration = 18f;
    public float deceleration = 12f;

    [Header("Jumping")]
    public float jumpHeight = 6f;
    public float gravity = -9.81f;
    public float jumpBufferTime = 0.2f;
    public float coyoteTime = 0.15f;
    [SerializeField]  bool canQuietLand;
    [SerializeField] float quietLandThreshold = 2f;

    [Header("Turning")]
    [SerializeField] float turnInputThreshold = 0.05f;
    [SerializeField] float turnSpeedStanding = 10f;
    [SerializeField] float turnSpeedCrouched = 3f;
    [SerializeField] float turnAccel = 14f;
    [SerializeField] float turnDecel = 18f;

    [Header("Sprint")]
    [SerializeField, Range(0.5f, 1f)] float sprintInputThreshold = 0.9f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheckTransform;
    [SerializeField] float groundCheckOffset = 0.05f;
    [SerializeField] float distanceToGround;
    [SerializeField] LayerMask groundMask;

    [Header("References")]
    [SerializeField] Camera cam;

    // State
    bool isGrounded;
    bool wasGrounded;
    bool isCrouching;
    [HideInInspector] public bool isAiming;

    float speed;
    float standingHeight;

    // Movement
    Vector3 planarVelocity;
    Vector3 velocity;

    // Turning
    float turnBlend;

    // Timers
    float jumpBufferCounter;
    float coyoteTimeCounter;
    float footstepTimer;

    // References
    CharacterController controller;
    [HideInInspector] public PlayerInputHandler input;
    PlayerNoise noise;
    GroundCheck groundCheck;

    void Awake()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputHandler>();
        noise = GetComponent<PlayerNoise>();
        groundCheck = GetComponentInChildren<GroundCheck>();

        if (cam == null)
            cam = Camera.main;

        standingHeight = controller.height;
    }

    void Update()
    {
        if (controller == null || input == null) return;

        UpdateGrounding();
        HandleCrouch();
        UpdateSpeed();

        Vector3 moveDir = GetMoveDirection();
        HandleRotation(moveDir);
        HandleMovement(moveDir);

        // Check if player is aiming
        isAiming = input.AimHeld && isGrounded;

        HandleJump();
        ApplyGravity();

        MoveCharacter();
        UpdateGroundCheckPosition();

        HandleFootsteps(moveDir);
        HandleLandingNoise();
        HandleWhistle();
    }

    // -------------------------
    // Core Systems
    // -------------------------

    void UpdateGrounding()
    {
        isGrounded = groundCheck.IsGrounded;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            distanceToGround = 0;
            canQuietLand = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;

            RaycastHit hit;

            Vector3 origin = transform.position;
            Vector3 direction = Vector3.down;

            if (Physics.Raycast(origin, direction, out hit, Mathf.Infinity, groundMask, QueryTriggerInteraction.UseGlobal))
            {
                distanceToGround = hit.distance;
            }

            //Conditions for enabling a quiet landing
            canQuietLand = distanceToGround <= quietLandThreshold && !isGrounded && velocity.y < 0;
        }
            
    }

    void HandleCrouch()
    {
        //CONDITIONS FOR CROUCHING, CAN EXPAND AT A LATER DATE
        //MUST BE GROUNDED OR IF IN THE AIR MUST SATISFY QUIETLAND
        bool canCrouch = isGrounded || canQuietLand;

        if (input.CrouchPressedThisFrame && canCrouch)
        {
            isCrouching = !isCrouching;
        }

        controller.height = isCrouching ? standingHeight / 2f : standingHeight;
    }

    void UpdateSpeed()
    {
        speed = isCrouching ? crouchSpeed : standingSpeed;
    }

    Vector3 GetMoveDirection()
    {
        Vector2 move = input.MoveInput;

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        return right * move.x + forward * move.y;
    }

    void HandleRotation(Vector3 moveDir)
    {
        // Determine target direction
        Vector3 targetDir = Vector3.zero;

        if (isAiming)
        {
            // Aiming: face camera forward
            Vector3 camForward = cam.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            targetDir = camForward;
        }
        else
        {
            // Not aiming: face movement direction if there is any input
            // Use a tiny epsilon to catch even the slightest stick movement
            if (moveDir.sqrMagnitude > 0.00001f) // practically zero
            {
                targetDir = moveDir.normalized;
            }
        }

        // If no valid direction, decelerate blend and exit
        if (targetDir.sqrMagnitude < 0.0001f)
        {
            turnBlend = Mathf.MoveTowards(turnBlend, 0f, turnDecel * Time.deltaTime);
            return;
        }

        // Accelerate blending
        turnBlend = Mathf.MoveTowards(turnBlend, 1f, turnAccel * Time.deltaTime);

        // Base speed (multiplier for Slerp – higher = faster rotation)
        float baseSpeed = isCrouching ? turnSpeedCrouched : turnSpeedStanding;

        // Slerp factor – larger values make rotation snappier
        float slerpFactor = baseSpeed * turnBlend * Time.deltaTime;
        // Clamp to avoid overshoot (optional)
        slerpFactor = Mathf.Clamp(slerpFactor, 0f, 1f);

        Quaternion targetRot = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, slerpFactor);
    }

    void HandleMovement(Vector3 moveDir)
    {
        bool hasInput = moveDir.sqrMagnitude > 0.0001f;

        Vector3 target = hasInput ? moveDir * speed : Vector3.zero;
        float rate = hasInput ? acceleration : deceleration;

        planarVelocity = Vector3.MoveTowards(planarVelocity, target, rate * Time.deltaTime);
    }

    void HandleJump()
    {
        if (isAiming) // Can't jump if aiming
            return;

        if (input.JumpPressedThisFrame)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
    }

    void MoveCharacter()
    {
        Vector3 finalVelocity = planarVelocity;
        finalVelocity.y = velocity.y;

        controller.Move(finalVelocity * Time.deltaTime);
    }

    void UpdateGroundCheckPosition()
    {
        float bottom = controller.center.y - (controller.height / 2f);

        Vector3 pos = transform.position;
        pos.y += bottom + groundCheckOffset;

        groundCheckTransform.position = pos;
    }

    // -------------------------
    // Feedback Systems
    // -------------------------

    void HandleFootsteps(Vector3 moveDir)
    {
        if (!isGrounded || planarVelocity.magnitude < 0.1f) return;

        bool isSprinting = !isCrouching && moveDir.sqrMagnitude >= sprintInputThreshold * sprintInputThreshold;

        float interval = isSprinting ? 0.3f : (isCrouching ? 0.6f : 0.45f);

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            noise.EmitFootstep(isSprinting, isCrouching);
            footstepTimer = interval;
        }
    }

    void HandleLandingNoise()
    {
        if (isGrounded && !wasGrounded)
        {
            //Player has triggered quiet landing so don't make a sound
            if (canQuietLand)
                return;

            //If not crouching then player a sound
            if(!isCrouching)
               noise.EmitNoise(5f);
        }
       
        wasGrounded = isGrounded;
    }

    void HandleWhistle()
    {
        if (input.WhistlePressedThisFrame)
            noise.EmitNoise(10f);
    }
}