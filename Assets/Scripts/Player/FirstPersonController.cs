using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public static FirstPersonController Instance { get; private set; }

    private const float MaxLookAngle = 90f;

    [BoxGroup("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [BoxGroup("Camera")]
    [SerializeField] private float mouseSensitivity = 100f;

    [BoxGroup("Head Bobbing")]
    [SerializeField] private bool enableHeadBob = true;

    [BoxGroup("Head Bobbing")]
    [SerializeField, MinValue(0f)]
    private float bobFrequency = 8f;

    [BoxGroup("Head Bobbing")]
    [SerializeField, MinValue(0f)]
    private float bobVerticalAmplitude = 0.05f;

    [BoxGroup("Head Bobbing")]
    [SerializeField, MinValue(0f)]
    private float bobHorizontalAmplitude = 0.03f;

    [BoxGroup("Head Bobbing")]
    [SerializeField, MinValue(0f)]
    private float bobSmoothing = 8f;

    private CharacterController controller;
    private InputSystem_Actions inputActions;
    private InputAction moveAction;
    private InputAction lookAction;

    private Transform cameraTransform;
    private float verticalVelocity;
    private float cameraPitch;

    private Vector3 cameraInitialLocalPos;
    private float bobTimer = 0f;

    private bool controlsEnabled = true;
    public bool ControlsEnabled
    {
        get => controlsEnabled;
        set => controlsEnabled = value;
    }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        controller = GetComponent<CharacterController>();

        // Input
        inputActions = new InputSystem_Actions();
        moveAction = inputActions.Player.Move;
        lookAction = inputActions.Player.Look;

        // Camera detection
        var cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null)
        {
            cameraTransform = cam.transform;
            cameraInitialLocalPos = cameraTransform.localPosition;
        }
        else
        {
            Debug.LogError("[FirstPersonController] No camera found.");
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        LockCursor(true);
    }

    private void OnDisable()
    {
        inputActions.Disable();
        LockCursor(false);
    }


    private void Update()
    {
        if (!controlsEnabled)
            return;

        HandleLook();
        HandleMovement();
        HandleHeadBob();

        // ESC unlock cursor
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
            LockCursor(shouldLock);
        }
    }

    // -------------------- LOOK --------------------
    private void HandleLook()
    {
        if (cameraTransform == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * 0.01f;
        float mouseY = lookInput.y * mouseSensitivity * 0.01f;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -MaxLookAngle, MaxLookAngle);

        Vector3 euler = cameraTransform.localEulerAngles;
        euler.x = cameraPitch;
        cameraTransform.localEulerAngles = euler;
    }

    // -------------------- MOVEMENT --------------------
    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);

        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 move = transform.TransformDirection(input) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1f;

        verticalVelocity += Physics.gravity.y * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    // -------------------- HEAD BOB --------------------
    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null)
            return;

        bool isMoving = controller.velocity.magnitude > 0.1f && controller.isGrounded;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;

            float vertical = Mathf.Sin(bobTimer) * bobVerticalAmplitude;
            float horizontal = Mathf.Cos(bobTimer * 0.5f) * bobHorizontalAmplitude;

            Vector3 targetPos = cameraInitialLocalPos + new Vector3(horizontal, vertical, 0f);

            cameraTransform.localPosition =
                Vector3.Lerp(cameraTransform.localPosition, targetPos, Time.deltaTime * bobSmoothing);
        }
        else
        {
            // Return to initial pos when stopped
            cameraTransform.localPosition =
                Vector3.Lerp(cameraTransform.localPosition, cameraInitialLocalPos, Time.deltaTime * bobSmoothing);
        }
    }

    // -------------------- CURSOR --------------------
    private static void LockCursor(bool value)
    {
        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }
}
