using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class ControladorPrimeraPersona : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 4f;
    public float velocidadCorrer = 7f;
    public float gravedad = -9.81f;
    public float alturaSalto = 1.2f;

    [Header("Cámara / Mouse")]
    public Transform camara;
    public float sensibilidad = 0.1f;   // con el nuevo sistema el valor es más chico
    public float limiteVertical = 80f;

    [Header("Sonido de pasos")]
    public AudioClip sonidoPasos;          // arrastra aquí foot.mp3
    [Range(0f, 1f)] public float volumenPasos = 0.8f;
    public float pitchCaminar = 1f;        // velocidad normal del audio
    public float pitchCorrer = 1.4f;       // pasos más rápidos al correr

    private CharacterController controlador;
    private AudioSource audioPasos;
    private float rotacionX = 0f;
    private Vector3 velocidadVertical;
    private float tiempoEnAire = 0f;       // evita cortes cuando isGrounded parpadea

    void Start()
    {
        controlador = GetComponent<CharacterController>();

        // Configurar el AudioSource para los pasos
        audioPasos = GetComponent<AudioSource>();
        audioPasos.clip = sonidoPasos;
        audioPasos.loop = true;
        audioPasos.playOnAwake = false;
        audioPasos.spatialBlend = 0f;      // 2D: son los pasos del propio jugador
        audioPasos.volume = volumenPasos;

        if (camara == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                camara = cam.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        MirarConMouse();
        Mover();
    }

    void MirarConMouse()
    {
        if (camara == null || Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        float mouseX = delta.x * sensibilidad;
        float mouseY = delta.y * sensibilidad;

        // Girar el cuerpo en horizontal
        transform.Rotate(Vector3.up * mouseX);

        // Girar la cámara en vertical (con límite)
        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -limiteVertical, limiteVertical);
        camara.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
    }

    void Mover()
    {
        if (Keyboard.current == null) return;

        bool enSuelo = controlador.isGrounded;

        if (enSuelo && velocidadVertical.y < 0)
            velocidadVertical.y = -2f;

        // Leer teclas WASD manualmente
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;

        Vector3 direccion = transform.right * x + transform.forward * z;

        // Correr con Shift
        bool corriendo = Keyboard.current.leftShiftKey.isPressed;
        float velActual = corriendo ? velocidadCorrer : velocidad;

        controlador.Move(direccion * velActual * Time.deltaTime);

        // Salto con Espacio
        if (enSuelo && Keyboard.current.spaceKey.wasPressedThisFrame)
            velocidadVertical.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);

        // Gravedad
        velocidadVertical.y += gravedad * Time.deltaTime;
        controlador.Move(velocidadVertical * Time.deltaTime);

        // Pasos
        bool moviendose = direccion.sqrMagnitude > 0.01f;
        ActualizarPasos(moviendose, enSuelo, corriendo);
    }

    void ActualizarPasos(bool moviendose, bool enSuelo, bool corriendo)
    {
        if (audioPasos == null || sonidoPasos == null) return;

        // Pequeño margen: isGrounded a veces falla un frame y cortaría el sonido
        tiempoEnAire = enSuelo ? 0f : tiempoEnAire + Time.deltaTime;
        bool tocandoSuelo = tiempoEnAire < 0.15f;

        if (moviendose && tocandoSuelo)
        {
            audioPasos.pitch = corriendo ? pitchCorrer : pitchCaminar;
            audioPasos.volume = volumenPasos;

            if (!audioPasos.isPlaying)
                audioPasos.Play();   // arranca desde el inicio: el primer paso suena de inmediato
        }
        else if (audioPasos.isPlaying)
        {
            audioPasos.Stop();
        }
    }
}