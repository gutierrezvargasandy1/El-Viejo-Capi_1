using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Linterna del Capi: F la enciende / apaga. Gasta pila mientras está encendida.
/// Se mueve con inercia (pesa en la mano), parpadea con poca pila y cuando el
/// monstruo está cerca o golpea algo.
/// Va como hijo de la cámara, con una Spot Light.
/// </summary>
public class LinternaNoche : MonoBehaviour
{
    public Light luz;
    public float intensidad = 6f;

    [Header("Pila")]
    [Tooltip("Segundos de luz que dura la pila en toda la noche")]
    public float duracionPila = 90f;
    [Tooltip("Porcentaje que recupera por segundo apagada (0 = no se recarga)")]
    public float recargaPorSegundo = 0f;

    [Header("Movimiento")]
    [Tooltip("Qué tan rápido alcanza a la cámara (menos = más pesada)")]
    public float inercia = 9f;
    [Tooltip("Vaivén de la mano en grados")]
    public float vaiven = 1.2f;

    [Header("Sonidos")]
    public AudioClip sonidoClick;
    public AudioClip sonidoSinPila;
    [Range(0f, 1f)] public float volumen = 0.8f;

    /// <summary>0..1: lo pone EfectosNoche cuando hay peligro (parpadeos).</summary>
    [HideInInspector] public float parpadeoExterno;

    public bool Encendida => encendida && bateria > 0f;
    public float Bateria01 => bateria;

    bool encendida;
    float bateria = 1f, finGolpe;
    AudioSource fuente;
    ControlJugadorNoche jugador;
    Quaternion rotLocalBase, rotMundo;

    void Awake()
    {
        if (luz == null) luz = GetComponent<Light>();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        rotLocalBase = transform.localRotation;
        rotMundo = transform.rotation;
        Aplicar();
    }

    void Start()
    {
        jugador = GestorNoche.instancia != null && GestorNoche.instancia.jugador != null
            ? GestorNoche.instancia.jugador
            : FindAnyObjectByType<ControlJugadorNoche>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        bool puede = jugador == null || jugador.PuedeUsarObjetos;
        if (puede && kb != null && kb.fKey.wasPressedThisFrame) Alternar();

        if (encendida)
        {
            bateria -= Time.deltaTime / Mathf.Max(1f, duracionPila);
            if (bateria <= 0f)
            {
                bateria = 0f;
                encendida = false;
                if (sonidoSinPila != null) fuente.PlayOneShot(sonidoSinPila, volumen);
            }
        }
        else if (recargaPorSegundo > 0f)
        {
            bateria = Mathf.Min(1f, bateria + recargaPorSegundo / 100f * Time.deltaTime);
        }

        Aplicar();
    }

    void LateUpdate()
    {
        // inercia: la luz llega un poquito tarde a donde miras, como si la cargaras en la mano
        Transform padre = transform.parent;
        if (padre == null) return;
        Quaternion objetivo = padre.rotation * rotLocalBase;
        rotMundo = Quaternion.Slerp(rotMundo, objetivo, 1f - Mathf.Exp(-inercia * Time.deltaTime));
        float t = Time.time;
        Quaternion mano = Quaternion.Euler(Mathf.Sin(t * 1.3f) * vaiven * 0.5f, Mathf.Sin(t * 0.9f) * vaiven, 0f);
        transform.rotation = rotMundo * mano;
    }

    public void Alternar()
    {
        if (!encendida && bateria <= 0f)
        {
            if (sonidoSinPila != null) fuente.PlayOneShot(sonidoSinPila, volumen);
            return;
        }
        encendida = !encendida;
        if (sonidoClick != null) fuente.PlayOneShot(sonidoClick, volumen);
        Aplicar();
    }

    public void Apagar()
    {
        encendida = false;
        Aplicar();
    }

    /// <summary>Parpadeo corto (cuando algo golpea fuerte).</summary>
    public void Golpe(float duracion = 0.3f)
    {
        finGolpe = Mathf.Max(finGolpe, Time.time + duracion);
    }

    void Aplicar()
    {
        if (luz == null) return;
        luz.enabled = Encendida;
        if (!Encendida) return;

        float t = Time.time;
        float f = 1f;
        if (bateria < 0.15f && Mathf.PerlinNoise(t * 12f, 0f) < 0.35f) f = 0.15f;          // poca pila
        if (parpadeoExterno > 0f && Mathf.PerlinNoise(t * 22f, 5f) < parpadeoExterno * 0.55f) f = Mathf.Min(f, 0.1f);   // miedo
        if (t < finGolpe && Random.value < 0.6f) f = Mathf.Min(f, Random.Range(0f, 0.35f));                            // golpe
        luz.intensity = intensidad * f;
    }
}
