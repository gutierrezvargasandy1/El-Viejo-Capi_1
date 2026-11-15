using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Linterna del Capi: F la enciende / apaga. Gasta pila mientras está encendida.
/// Con poca pila parpadea; sin pila ya no prende.
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

    [Header("Sonidos")]
    public AudioClip sonidoClick;
    public AudioClip sonidoSinPila;
    [Range(0f, 1f)] public float volumen = 0.8f;

    public bool Encendida => encendida && bateria > 0f;
    public float Bateria01 => bateria;

    bool encendida;
    float bateria = 1f;
    AudioSource fuente;
    ControlJugadorNoche jugador;

    void Awake()
    {
        if (luz == null) luz = GetComponent<Light>();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
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

    void Aplicar()
    {
        if (luz == null) return;
        luz.enabled = Encendida;
        if (!Encendida) return;

        float f = 1f;
        if (bateria < 0.15f)   // poca pila: parpadea
            f = Mathf.PerlinNoise(Time.time * 12f, 0f) > 0.35f ? 1f : 0.15f;
        luz.intensity = intensidad * f;
    }
}
