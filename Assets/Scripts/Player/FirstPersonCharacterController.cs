using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float crouchSpeedMultiplier = 0.45f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float crouchEdgeOverhang = 0.35f;
    [SerializeField] private float crouchHeight = 1.5f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float cameraStandY = 1.62f;
    [SerializeField] private float cameraCrouchY = 1.27f;

    [Header("Look")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction crouchAction;

    private float verticalVelocity;
    private float pitch;
    private bool gameplayCaptured = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        EnsureCamera();
        SetupInputActions();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        crouchAction?.Enable();
        LockCursor(true);
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        crouchAction?.Disable();
        LockCursor(false);
    }

    public void SetGameplayCaptured(bool captured)
    {
        gameplayCaptured = captured;
        LockCursor(captured);
    }

    public void Teleport(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
        verticalVelocity = 0f;
    }

    private void Update()
    {
        if (!gameplayCaptured)
        {
            return;
        }

        var look = lookAction.ReadValue<Vector2>();
        RotateView(look);

        var isCrouching = crouchAction.IsPressed();
        ApplyCrouch(isCrouching);

        var moveInput = moveAction.ReadValue<Vector2>();
        var move = transform.right * moveInput.x + transform.forward * moveInput.y;
        var speed = moveSpeed * (isCrouching ? crouchSpeedMultiplier : 1f);
        var horizontalMove = move * speed;
        horizontalMove = ApplyEdgeProtection(horizontalMove, isCrouching);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (jumpAction.WasPressedThisFrame() && characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        var velocity = horizontalMove + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void SetupInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");

        crouchAction = new InputAction("Crouch", InputActionType.Button);
        crouchAction.AddBinding("<Keyboard>/leftShift");
        crouchAction.AddBinding("<Keyboard>/rightShift");
    }

    private void RotateView(Vector2 lookInput)
    {
        var yaw = lookInput.x * lookSensitivity;
        var pitchDelta = lookInput.y * lookSensitivity;

        transform.Rotate(Vector3.up * yaw);
        pitch = Mathf.Clamp(pitch - pitchDelta, minPitch, maxPitch);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void EnsureCamera()
    {
        if (playerCamera != null)
        {
            return;
        }

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            return;
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, cameraStandY, 0f);
        playerCamera = cameraObject.AddComponent<Camera>();
    }

    private static void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private void ApplyCrouch(bool isCrouching)
    {
        var targetHeight = isCrouching ? crouchHeight : standHeight;
        characterController.height = targetHeight;
        characterController.center = new Vector3(0f, targetHeight * 0.5f, 0f);

        var cameraTransform = playerCamera.transform;
        var localPosition = cameraTransform.localPosition;
        localPosition.y = isCrouching ? cameraCrouchY : cameraStandY;
        cameraTransform.localPosition = localPosition;
    }

    private Vector3 ApplyEdgeProtection(Vector3 horizontalMove, bool isCrouching)
    {
        if (!isCrouching || !characterController.isGrounded || horizontalMove.sqrMagnitude <= 0f)
        {
            return horizontalMove;
        }

        var dt = Time.deltaTime;
        var nextPosition = transform.position + horizontalMove * dt;
        var moveDirection = horizontalMove.normalized;
        return HasGroundSupport(nextPosition, moveDirection) ? horizontalMove : Vector3.zero;
    }

    private bool HasGroundSupport(Vector3 worldPosition, Vector3 moveDirection)
    {
        var footY = worldPosition.y + characterController.center.y - characterController.height * 0.5f;
        var bodyCenter = new Vector3(worldPosition.x, footY + 0.02f, worldPosition.z);
        var probeRadius = Mathf.Max(characterController.radius - 0.03f, 0.05f);
        var side = Vector3.Cross(Vector3.up, moveDirection).normalized;
        if (side.sqrMagnitude <= 0.001f)
        {
            side = transform.right;
        }

        var centerProbe = bodyCenter - moveDirection * crouchEdgeOverhang;
        var leftProbe = centerProbe + side * probeRadius;
        var rightProbe = centerProbe - side * probeRadius;
        var rayStartY = Vector3.up * 0.12f;
        const float rayDistance = 0.28f;

        var probes = new[]
        {
            centerProbe,
            leftProbe,
            rightProbe,
            bodyCenter
        };

        for (int i = 0; i < probes.Length; i++)
        {
            if (Physics.Raycast(probes[i] + rayStartY, Vector3.down, rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }

        return false;
    }
}
