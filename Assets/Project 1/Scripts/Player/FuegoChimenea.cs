using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// La leña de la chimenea: estando en la chimenea, Espacio la enciende o la apaga.
/// Mientras está encendida gasta leña; cuando se acaba, se apaga y ya no prende.
/// </summary>
public class FuegoChimenea : MonoBehaviour
{
    [Header("Leña")]
    [Tooltip("Segundos de fuego que alcanza la leña en toda la noche")]
    public float duracionLena = 80f;

    [Header("Visual")]
    public ParticleSystem particulas;
    public Light luz;
    public float intensidadLuz = 3f;
    [Tooltip("Troncos que brillan cuando hay fuego")]
    public Renderer[] brasas;
    public Color colorBrasas = new Color(1f, 0.35f, 0.05f);

    [Header("Sonidos")]
    public AudioClip sonidoEncender;
    public AudioClip sonidoApagar;
    public AudioClip sonidoLoop;
    public AudioClip sonidoSinLena;
    [Range(0f, 1f)] public float volumen = 0.9f;

    public bool Encendido => encendido;
    public float Lena01 => lena;

    bool encendido;
    float lena = 1f, brillo;
    AudioSource fuenteLoop, fuente;
    Material[] materialesBrasas;
    ControlJugadorNoche jugador;
    static readonly int Emision = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0.7f;

        fuenteLoop = gameObject.AddComponent<AudioSource>();
        fuenteLoop.playOnAwake = false;
        fuenteLoop.loop = true;
        fuenteLoop.spatialBlend = 0.7f;
        fuenteLoop.volume = 0f;

        if (brasas != null)
        {
            materialesBrasas = new Material[brasas.Length];
            for (int i = 0; i < brasas.Length; i++)
            {
                if (brasas[i] == null) continue;
                materialesBrasas[i] = brasas[i].material;     // copia propia para no cambiar el asset
                materialesBrasas[i].EnableKeyword("_EMISSION");
            }
        }

        if (particulas != null) particulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (luz != null) luz.enabled = false;
    }

    void Start()
    {
        // por si tu fuego tenía "Play On Awake": empieza apagado
        if (particulas != null && !encendido) particulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        jugador = GestorNoche.instancia != null && GestorNoche.instancia.jugador != null
            ? GestorNoche.instancia.jugador
            : FindAnyObjectByType<ControlJugadorNoche>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (jugador != null && jugador.PuedeActuar && jugador.VistaActual == VistaNoche.Chimenea
            && kb != null && kb.spaceKey.wasPressedThisFrame)
            Alternar();

        if (encendido)
        {
            lena -= Time.deltaTime / Mathf.Max(1f, duracionLena);
            if (lena <= 0f)
            {
                lena = 0f;
                Apagar();
            }
        }

        brillo = Mathf.MoveTowards(brillo, encendido ? 1f : 0f, Time.deltaTime * 1.5f);
        float parpadeo = 0.8f + 0.35f * Mathf.PerlinNoise(Time.time * 7f, 1f);

        if (luz != null)
        {
            luz.enabled = brillo > 0.01f;
            luz.intensity = intensidadLuz * brillo * parpadeo;
        }

        if (materialesBrasas != null)
            foreach (var m in materialesBrasas)
                if (m != null) m.SetColor(Emision, colorBrasas * (brillo * 2f * parpadeo));

        if (sonidoLoop != null)
        {
            if (brillo > 0.01f && !fuenteLoop.isPlaying)
            {
                fuenteLoop.clip = sonidoLoop;
                fuenteLoop.Play();
            }
            fuenteLoop.volume = volumen * 0.8f * brillo;
            if (brillo <= 0.01f && fuenteLoop.isPlaying) fuenteLoop.Stop();
        }
    }

    public void Alternar()
    {
        if (encendido) Apagar();
        else Encender();
    }

    public void Encender()
    {
        if (encendido) return;
        if (lena <= 0f)
        {
            if (sonidoSinLena != null) fuente.PlayOneShot(sonidoSinLena, volumen);
            return;
        }
        encendido = true;
        if (particulas != null) particulas.Play();
        if (sonidoEncender != null) fuente.PlayOneShot(sonidoEncender, volumen);
    }

    public void Apagar()
    {
        if (!encendido) return;
        encendido = false;
        if (particulas != null) particulas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (sonidoApagar != null) fuente.PlayOneShot(sonidoApagar, volumen);
    }
}
