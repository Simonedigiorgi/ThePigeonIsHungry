using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public static FirstPersonController Instance { get; private set; }

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 100f;

    private const float MaxLookAngle = 90f;

    private CharacterController controller;
    private InputSystem_Actions inputActions;
    private InputAction moveAction;
    private InputAction lookAction;

    private float verticalVelocity;
    private float cameraPitch;

    private bool controlsEnabled = true;
    public bool ControlsEnabled
    {
        get => controlsEnabled;
        set => controlsEnabled = value;
    }

    private void Awake()
    {
        Instance = this;   // ⭐ AGGIUNTO

        controller = GetComponent<CharacterController>();

        inputActions = new InputSystem_Actions();
        moveAction = inputActions.Player.Move;
        lookAction = inputActions.Player.Look;
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

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
            LockCursor(shouldLock);
        }
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * 0.01f;
        float mouseY = lookInput.y * mouseSensitivity * 0.01f;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -MaxLookAngle, MaxLookAngle);

        if (cameraTransform != null)
        {
            Vector3 euler = cameraTransform.localEulerAngles;
            euler.x = cameraPitch;
            cameraTransform.localEulerAngles = euler;
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 move = transform.TransformDirection(input) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1f;

        verticalVelocity += Physics.gravity.y * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    private static void LockCursor(bool value)
    {
        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }
}
