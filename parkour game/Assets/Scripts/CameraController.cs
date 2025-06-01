using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float distance = 5;
    [SerializeField] private float Yposition;//ff naar kijken / tweaken
    [SerializeField] private float minVerticalAngle = -45;
    [SerializeField] private float maxVerticalAngle = 45;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float controllerSensitivity = 2f;
    [SerializeField] Vector2 framingOffset;

    private float currentSensitivity = 1f;
    private float rotationX;
    private float rotationY;
    private Vector2 lookInput;

    private PlayerInput playerInput;
    private InputAction lookAction;

    private void Awake()
    {
        playerInput = new PlayerInput();
        lookAction = playerInput.Game.Look;
    }

    private void OnEnable()
    {
        playerInput.Game.Enable();
    }

    private void OnDisable()
    {
        playerInput.Game.Disable();
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        lookInput = lookAction.ReadValue<Vector2>();
        // Bepaal gevoeligheid op basis van invoerapparaat
        if (Mouse.current != null && Mouse.current.delta.IsActuated())
        {
            currentSensitivity = mouseSensitivity;
        }
        else if (Gamepad.current != null && Gamepad.current.rightStick.IsActuated())
        {
            currentSensitivity = controllerSensitivity;
        }

        rotationX -= lookInput.y * currentSensitivity;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        rotationY += lookInput.x * currentSensitivity;

        var targetRotation = Quaternion.Euler(rotationX, rotationY, 0);
        var focusPosition = target.position + new Vector3(framingOffset.x, framingOffset.y);

        transform.position = target.position - targetRotation * new Vector3(0, Yposition, distance);
        transform.rotation = targetRotation;
    }

    public Quaternion PlanerRotation => Quaternion.Euler(0, rotationY, 0);
    public Quaternion GetPlanerRotation() => Quaternion.Euler(0, rotationY, 0);
}
