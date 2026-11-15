using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cortinas de la ventana: se cierran mientras mantienes Espacio estando en la ventana
/// y se vuelven a abrir solas al soltar.
/// Cada cortina es un pivote en el borde de afuera que se estira en X (escala) para cerrar.
/// </summary>
public class CortinasNoche : MonoBehaviour
{
    [System.Serializable]
    public class Panel
    {
        public Transform panel;
        public Vector3 posicionAbierta, posicionCerrada;
        public Vector3 escalaAbierta, escalaCerrada;
    }

    [Header("Tus cortinas (lo llena la herramienta con el botón 'Mis cortinas')")]
    [Tooltip("Cada tela: cómo está abierta y cómo queda cerrada")]
    public Panel[] paneles;

    [Header("Cortinas provisionales (si no usas las tuyas)")]
    [Tooltip("Pivote de la cortina izquierda (en su borde de afuera)")]
    public Transform izquierda;
    [Tooltip("Pivote de la cortina derecha (en su borde de afuera)")]
    public Transform derecha;
    [Range(0.05f, 1f)] public float escalaAbierta = 0.2f;
    public float velocidadCerrar = 2.2f;
    public float velocidadAbrir = 1.4f;

    [Header("Sonidos")]
    public AudioClip sonidoCerrar;
    public AudioClip sonidoAbrir;
    [Range(0f, 1f)] public float volumen = 0.8f;

    public float Cierre => cierre;
    public bool Cerradas => cierre >= 0.97f;

    float cierre;
    bool cerrando;
    AudioSource fuente;
    ControlJugadorNoche jugador;

    void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0.6f;
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
        bool mantener = jugador != null && jugador.PuedeActuar
                     && jugador.VistaActual == VistaNoche.Ventana
                     && kb != null && kb.spaceKey.isPressed;

        if (mantener != cerrando)
        {
            cerrando = mantener;
            var clip = cerrando ? sonidoCerrar : sonidoAbrir;
            if (clip != null) fuente.PlayOneShot(clip, volumen);
        }

        float vel = cerrando ? velocidadCerrar : velocidadAbrir;
        cierre = Mathf.MoveTowards(cierre, cerrando ? 1f : 0f, Time.deltaTime * vel);
        Aplicar();
    }

    /// <summary>Para la herramienta: ver las cortinas abiertas (0) o cerradas (1) sin dar Play.</summary>
    public void Previsualizar(float valor)
    {
        cierre = Mathf.Clamp01(valor);
        Aplicar();
    }

    void Aplicar()
    {
        float e = cierre * cierre * (3f - 2f * cierre);

        if (paneles != null && paneles.Length > 0)
        {
            foreach (var p in paneles)
            {
                if (p == null || p.panel == null) continue;
                p.panel.localPosition = Vector3.Lerp(p.posicionAbierta, p.posicionCerrada, e);
                p.panel.localScale = Vector3.Lerp(p.escalaAbierta, p.escalaCerrada, e);
            }
            return;
        }

        float escala = Mathf.Lerp(escalaAbierta, 1f, e);
        Escalar(izquierda, escala);
        Escalar(derecha, escala);
    }

    static void Escalar(Transform t, float x)
    {
        if (t == null) return;
        var s = t.localScale;
        s.x = x;
        t.localScale = s;
    }
}
