using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    private CameraController cameraController;
    private Rigidbody rigidBody;
    private Animator animator;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;


    [Header("Player settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 500f;

    private Quaternion targetRotation;
    private Vector2 inputMovement;
    private bool isSprinting;
    private bool sprintHeld;

    private void Awake()
    {
        playerInput = new PlayerInput();

        moveAction = playerInput.Game.Move;
        sprintAction = playerInput.Game.Sprint;
        playerInput.Game.Move.performed += ctx => inputMovement = ctx.ReadValue<Vector2>();
        playerInput.Game.Move.canceled += ctx => inputMovement = Vector2.zero;
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
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        cameraController = Camera.main.GetComponent<CameraController>();
    }

    private void Update()
    {
        Vector3 moveInput = new Vector3(inputMovement.x, 0, inputMovement.y);
        float inputMagnitude = moveInput.magnitude;
        bool sprintButtonPressed = sprintAction.IsPressed();
        bool usingController = inputMagnitude > 0f && inputMagnitude < 0.99f;

        if (usingController)
        {
            isSprinting = inputMagnitude > 0.9f;
        }
        else
        {
            isSprinting = inputMagnitude > 0.1f && sprintButtonPressed;
        }

        HandleMovement(moveInput.normalized, inputMagnitude);
    }

    private void HandleMovement(Vector3 moveInput, float inputMagnitude)
    {
        bool isMoving = inputMagnitude > 0.1f;
        bool shouldRun = isMoving && isSprinting;

        if (!isMoving)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsRunning", false);
            return;
        }

        Vector3 moveDirection = cameraController.PlanerRotation * moveInput;
        float currentSpeed = shouldRun ? runSpeed : walkSpeed;

        animator.SetBool("IsMoving", true);
        animator.SetBool("IsRunning", shouldRun);

        transform.position += moveDirection * currentSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection), rotationSpeed * Time.deltaTime);
    }

    public void HandleAttacking(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Ik val aan");
        }
    }
}
