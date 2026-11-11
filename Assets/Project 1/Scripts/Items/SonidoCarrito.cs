using UnityEngine;

// Ponlo en el carrito. Suena cuando se mueve y se calla cuando se detiene.
// Funciona sin importar cómo se mueva el carrito (siguiendo al jugador, Rigidbody, animación...).
[RequireComponent(typeof(AudioSource))]
public class SonidoCarrito : MonoBehaviour
{
    [Header("Sonido (en loop)")]
    public AudioClip sonidoRodando;
    [Range(0f, 1f)] public float volumenMaximo = 0.7f;

    [Header("Velocidad")]
    [Tooltip("Por debajo de esta velocidad el carrito no suena")]
    public float velocidadMinima = 0.1f;
    [Tooltip("A esta velocidad el sonido llega a su volumen máximo")]
    public float velocidadMaxima = 2.5f;

    [Header("Tono según la velocidad")]
    public float pitchLento = 0.85f;
    public float pitchRapido = 1.15f;

    [Header("Suavizado (más alto = reacciona más rápido)")]
    public float suavizado = 6f;

    [Header("Espacial (0 = 2D, 1 = 3D)")]
    [Range(0f, 1f)] public float sonido3D = 0.6f;

    private AudioSource fuente;
    private Vector3 posAnterior;
    private float volumenActual;
    private float pitchActual;

    void Awake()
    {
        fuente = GetComponent<AudioSource>();
        fuente.clip = sonidoRodando;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.volume = 0f;
        fuente.spatialBlend = sonido3D;
        fuente.minDistance = 3f;
        fuente.maxDistance = 25f;
    }

    void Start()
    {
        posAnterior = transform.position;
        pitchActual = pitchLento;

        if (sonidoRodando != null)
        {
            // Empieza en un punto al azar del loop para que no suene siempre igual
            fuente.time = Random.Range(0f, sonidoRodando.length);
            fuente.Play();
        }
        else
        {
            Debug.LogWarning("SonidoCarrito: no tiene asignado el sonidoRodando.");
        }
    }

    void Update()
    {
        if (sonidoRodando == null) return;

        // Juego en pausa (timeScale = 0): pausa también el sonido
        if (Time.deltaTime <= 0f)
        {
            if (fuente.isPlaying) fuente.Pause();
            return;
        }
        if (!fuente.isPlaying) fuente.UnPause();

        // Velocidad horizontal del carrito
        Vector3 delta = transform.position - posAnterior;
        delta.y = 0f;
        float velocidad = delta.magnitude / Time.deltaTime;
        posAnterior = transform.position;

        float t = Mathf.InverseLerp(velocidadMinima, velocidadMaxima, velocidad);

        volumenActual = Mathf.Lerp(volumenActual, t * volumenMaximo, suavizado * Time.deltaTime);
        pitchActual = Mathf.Lerp(pitchActual, Mathf.Lerp(pitchLento, pitchRapido, t), suavizado * Time.deltaTime);

        fuente.volume = volumenActual;
        fuente.pitch = pitchActual;
    }
}