using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Animator anim;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad de movimiento del personaje")]
    public float moveSpeed = 5.0f;
    [Tooltip("Fuerza de gravedad aplicada al personaje")]
    public float gravity = -9.81f;
    [Tooltip("Velocidad de giro del personaje")]
    public float rotationSpeed = 15f;

    [Header("Referencia Visual (Opcional)")]
    public Transform visualHolder;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Verifica si el personaje está en el suelo
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Obtiene la entrada del teclado
        float moveX = 0f;
        float moveZ = 0f;

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ -= 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ += 1f;
        }

        // 1. Calculamos el movimiento base en el plano XZ
        Vector3 move = new Vector3(moveX, 0f, moveZ).normalized * moveSpeed;

        // 2. Aplicamos la gravedad al eje Y de la velocidad
        velocity.y += gravity * Time.deltaTime;

        // 3. Mantenemos el intercambio de ejes adaptado a tu escena
        Vector3 finalMovement = new Vector3(move.z, velocity.y, move.x);
        controller.Move(finalMovement * Time.deltaTime);

        // 4. Rotar el personaje hacia la dirección del movimiento
        Vector3 direction = new Vector3(move.z, 0f, move.x);
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 5. Control de la animación basado en si se está moviendo
        bool is_moving = move.sqrMagnitude > 0.01f;
        if (anim != null)
        {
            anim.SetBool("is_moving", is_moving);
        }
    }
}