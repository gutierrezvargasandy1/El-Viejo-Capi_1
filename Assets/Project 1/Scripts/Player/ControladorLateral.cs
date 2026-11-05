using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ControladorLateral : MonoBehaviour
{
    private CharacterController controller;
    private Animator anim;
    private Vector3 velocity;

    [Header("Movimiento")]
    public float moveSpeed = 5.0f;
    public float gravity = -9.81f;

    [Header("Dirección (ajusta si va al revés)")]
    [Tooltip("Marca para mover en el eje X. Desmarca para el eje Z.")]
    public bool usarEjeX = true;
    [Tooltip("Marca si A/D van al revés (D debe ir a la derecha)")]
    public bool invertir = false;

    [Header("Giro")]
    [Tooltip("Marca para que el personaje mire al instante (evita el baile al picar rápido)")]
    public bool giroInstantaneo = true;
    public float rotationSpeed = 15f;

    private Vector3 ultimaDireccion = Vector3.forward;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Entrada A/D
        float input = 0f;
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) input += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed)  input -= 1f;
        }

        if (invertir) input = -input;

        // Dirección según el eje elegido
        Vector3 dir = usarEjeX ? new Vector3(input, 0f, 0f)
                               : new Vector3(0f, 0f, input);

        Vector3 movimiento = dir * moveSpeed;

        velocity.y += gravity * Time.deltaTime;
        movimiento.y = velocity.y;

        controller.Move(movimiento * Time.deltaTime);

        // Mirar hacia donde camina
        if (dir.sqrMagnitude > 0.01f)
        {
            ultimaDireccion = dir;

            Quaternion objetivo = Quaternion.LookRotation(dir);

            if (giroInstantaneo)
                transform.rotation = objetivo;                 // instante, sin baile
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, rotationSpeed * Time.deltaTime);
        }

        if (anim != null)
            anim.SetBool("is_moving", Mathf.Abs(input) > 0.01f);
    }
}