using UnityEngine;
using UnityEngine.Events;

public class CanastaSigue : MonoBehaviour
{
    [Header("A quién sigue")]
    public Transform jugador;

    [Header("Comportamiento de arrastre")]
    public float distancia = 1.5f;
    public float suavizado = 3f;
    public float alturaFija = 0f;

    [Header("Rotación")]
    public bool rotarHaciaJugador = false;

    [Header("Inicio")]
    public bool seguirDesdeInicio = false;

    [Header("Al terminar - regresar a su lugar")]
    [Tooltip("Objeto vacío puesto donde debe quedar el carrito. NO debe ser hijo del carrito")]
    public Transform lugarFinal;

    public bool regresoSuave = true;
    public float velocidadRegreso = 3f;
    public float velocidadGiro = 180f;

    [Tooltip("Si tarda más que esto en llegar, se coloca de golpe")]
    public float tiempoMaximoRegreso = 8f;

    public UnityEvent alLlegarASuLugar;

    [Header("Diagnóstico")]
    public bool mostrarDebug = true;

    private bool siguiendo = false;
    private bool regresando = false;
    private float cronometroRegreso = 0f;
    private Quaternion rotacionOriginal;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    void Start()
    {
        rotacionOriginal = transform.rotation;
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;

        if (alturaFija == 0f)
            alturaFija = transform.position.y;

        // Trampa clásica: el destino colgando del propio carrito
        if (lugarFinal != null && lugarFinal.IsChildOf(transform))
        {
            Debug.LogError("[Canasta] 'Lugar Final' es hijo del carrito, así nunca va a llegar. Sácalo de ahí y déjalo suelto en la escena.", this);
            lugarFinal.SetParent(null, true);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
            Debug.LogWarning("[Canasta] Tiene Rigidbody sin Is Kinematic: la física pelea contra el movimiento. Marca Is Kinematic.", this);

        if (seguirDesdeInicio)
            EmpezarASeguir();
    }

    public void EmpezarASeguir()
    {
        siguiendo = true;
        regresando = false;

        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }
    }

    public void DejarDeSeguir()
    {
        siguiendo = false;
        regresando = false;
    }

    /// <summary>Manda el carrito a su lugar. Va en el evento alCompletar.</summary>
    [ContextMenu("Probar regreso")]
    public void RegresarASuLugar()
    {
        siguiendo = false;
        cronometroRegreso = 0f;

        if (mostrarDebug)
            Debug.Log("[Canasta] RegresarASuLugar llamado. Destino: " + DestinoPos(), this);

        if (regresoSuave)
            regresando = true;
        else
            ColocarEnSuLugar();
    }

    void ColocarEnSuLugar()
    {
        transform.position = DestinoPos();
        transform.rotation = DestinoRot();
        regresando = false;

        if (mostrarDebug) Debug.Log("[Canasta] llegó a su lugar.", this);

        alLlegarASuLugar.Invoke();
    }

    Vector3 DestinoPos()
    {
        return lugarFinal != null ? lugarFinal.position : posicionInicial;
    }

    Quaternion DestinoRot()
    {
        return lugarFinal != null ? lugarFinal.rotation : rotacionInicial;
    }

    void Update()
    {
        if (regresando)
        {
            Vector3 destino = DestinoPos();

            transform.position = Vector3.MoveTowards(
                transform.position, destino, velocidadRegreso * Time.deltaTime);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, DestinoRot(), velocidadGiro * Time.deltaTime);

            cronometroRegreso += Time.deltaTime;

            if (Vector3.Distance(transform.position, destino) < 0.02f ||
                cronometroRegreso > tiempoMaximoRegreso)
            {
                ColocarEnSuLugar();
            }

            return;
        }

        if (!siguiendo || jugador == null) return;

        Vector3 atras = jugador.position - jugador.forward * distancia;
        atras.y = alturaFija;

        transform.position = Vector3.Lerp(transform.position, atras, suavizado * Time.deltaTime);

        if (!rotarHaciaJugador)
            transform.rotation = rotacionOriginal;
    }
}