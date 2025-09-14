using System.Collections.Generic;
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
    private InputAction attackAction;

    [Header("Player settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 500f;

    private Quaternion targetRotation;
    private Vector2 inputMovement;
    private bool isSprinting;
    private bool sprintHeld;


    private int currentAttack = 0;
    private float LastAttackTime;
    [SerializeField] private float comboResetTime = 1.2f;


    [SerializeField] private List<GameObject> PickupObjects = new List<GameObject>();

    private void Awake()
    {
        playerInput = new PlayerInput();
        moveAction = playerInput.Game.Move;
        sprintAction = playerInput.Game.Sprint;
        playerInput.Game.Move.performed += ctx => inputMovement = ctx.ReadValue<Vector2>();
        playerInput.Game.Move.canceled += ctx => inputMovement = Vector2.zero;
        attackAction = playerInput.Game.Attack;
        attackAction.performed += HandleAttacking;
    }

    private void OnEnable()
    {
        playerInput.Game.Enable();
    }

    private void OnDisable()
    {
        playerInput.Game.Disable();
        attackAction.performed -= HandleAttacking;
    }
    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        cameraController = Camera.main.GetComponent<CameraController>();
    }

    public void PickupedObjects(int id, GameObject pickup)
    {
        switch (id)
        {
            case 0:
                PickupObjects.Add(pickup);
                pickup.SetActive(false);
                break;
        }
    }

    private void Update()
    {
        // Lees de input van de speler (WASD of controllerstick)
        Vector3 moveInput = new Vector3(inputMovement.x, 0, inputMovement.y);

        // Hoe sterk (groot) is de input? (0 = stilstand, 1 = volle input)
        float inputMagnitude = moveInput.magnitude;

        // Check of de sprintknop wordt ingedrukt
        bool sprintButtonPressed = sprintAction.IsPressed();

        // Detecteer of speler een controller gebruikt (meestal zachtere input)
        bool usingController = inputMagnitude > 0f && inputMagnitude < 0.99f;

        // Bepaal of de speler aan het sprinten is
        if (usingController)
        {
            isSprinting = inputMagnitude > 0.9f; // sprinten bij bijna volledige input
        }
        else
        {
            isSprinting = inputMagnitude > 0.1f && sprintButtonPressed; // alleen sprinten als sprintknop ingedrukt is
        }

        // Verwerk beweging
        HandleMovement(moveInput.normalized, inputMagnitude);
    }

    private void HandleMovement(Vector3 moveInput, float inputMagnitude)
    {
        // Bepaal of speler daadwerkelijk probeert te bewegen
        bool isMoving = inputMagnitude > 0.1f;

        // Bepaal of speler moet rennen
        bool shouldRun = isMoving && isSprinting;

        // Als speler niet beweegt, zet animaties uit en return
        if (!isMoving)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsRunning", false);
            return;
        }

        // Zet bewegingsrichting gebaseerd op camera-oriëntatie
        Vector3 moveDirection = cameraController.PlanerRotation * moveInput;

        // Kies snelheid op basis van rennen of lopen
        float currentSpeed = shouldRun ? runSpeed : walkSpeed;

        // Zet animatieparameters
        animator.SetBool("IsMoving", true);
        animator.SetBool("IsRunning", shouldRun);

        // Verplaats speler in gekozen richting met juiste snelheid
        transform.position += moveDirection * currentSpeed * Time.deltaTime;

        // Draai speler geleidelijk richting bewegingsrichting
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection), rotationSpeed * Time.deltaTime);
    }

    public void HandleAttacking(InputAction.CallbackContext context)
    {
        Debug.Log("play animation");
        animator.SetTrigger("AttackTrigger");
    }


    /*public void HandleAttacking(InputAction.CallbackContext context)
    {
      // Zorg dat de actie alleen wordt verwerkt als hij echt uitgevoerd is
        if (!context.performed) return;

        // Bereken tijd sinds laatste aanval
        float timeSinceLastAttack = Time.time - LastAttackTime;

        // Als er te veel tijd is verstreken, reset de combo
        if (timeSinceLastAttack > comboResetTime)
        {
            currentAttack = 0;
        }

        // Update tijd van laatste aanval
        LastAttackTime = Time.time;

        // Ga naar volgende aanval in combo
        currentAttack++;

        // Beperk combo tot maximaal 3 aanvallen, daarna opnieuw beginnen
        if (currentAttack > 3) currentAttack = 1;

        // Speel juiste animatie af voor de huidige aanval
        animator.SetTrigger($"Attack{currentAttack}Trigger");

        // Debug-log om te zien welke aanval wordt uitgevoerd
        Debug.Log($"Aanval: Attack{currentAttack}");
    }*/
}
