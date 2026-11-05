using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(VideoPlayer))]
public class ControlCinematica : MonoBehaviour
{
    [Header("Cámara (se pone en negro mientras no hay video)")]
    public Camera camara;

    [Header("1. Pantalla negra con sonido")]
    [Tooltip("Sonido de personas llegando y sentándose")]
    public AudioClip sonidoLlegada;
    [Range(0f, 1f)] public float volumenSonido = 1f;
    [Tooltip("Segundos en negro. Si lo dejas en 0, dura lo que dure el sonido")]
    public float duracionPantallaNegra = 0f;
    [Tooltip("Si está activo, el sonido se apaga poco a poco cuando empieza el primer video")]
    public bool apagarSonidoAlEmpezarVideo = true;
    public float duracionFadeSonido = 1.5f;

    [Header("2. Videos (en orden)")]
    [Tooltip("Elemento 0 = video del teatro, Elemento 1 = video donde guarda las cosas")]
    public VideoClip[] videos;
    [Tooltip("Segundos en negro entre un video y otro")]
    public float pausaEntreVideos = 0.5f;

    [Header("3. Al terminar")]
    [Tooltip("Nombre de la escena siguiente. Déjalo vacío si no quieres cambiar de escena")]
    public string escenaSiguiente = "";

    [Header("Pruebas")]
    [Tooltip("Permite saltar la cinemática con Espacio o Escape")]
    public bool permitirSaltar = true;

    private VideoPlayer videoPlayer;
    private AudioSource fuenteAudio;
    private bool videoTermino;
    private bool saltar;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.Stop();

        fuenteAudio = GetComponent<AudioSource>();
        if (fuenteAudio == null) fuenteAudio = gameObject.AddComponent<AudioSource>();
        fuenteAudio.playOnAwake = false;
        fuenteAudio.loop = false;
        fuenteAudio.spatialBlend = 0f;   // sonido 2D

        if (camara == null) camara = Camera.main;
        if (camara != null)
        {
            // Fondo negro: así, cuando no hay video, se ve negro y no el cielo
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            videoPlayer.targetCamera = camara;
        }

        videoPlayer.loopPointReached += AlTerminarVideo;
        videoPlayer.errorReceived += AlErrorVideo;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= AlTerminarVideo;
            videoPlayer.errorReceived -= AlErrorVideo;
        }
    }

    void Start()
    {
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (!permitirSaltar) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            saltar = true;
#else
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            saltar = true;
#endif
    }

    IEnumerator Secuencia()
    {
        // ---------- 1. PANTALLA NEGRA + SONIDO ----------
        if (sonidoLlegada != null)
        {
            fuenteAudio.clip = sonidoLlegada;
            fuenteAudio.volume = volumenSonido;
            fuenteAudio.Play();
        }

        float tiempoNegro = duracionPantallaNegra > 0f
            ? duracionPantallaNegra
            : (sonidoLlegada != null ? sonidoLlegada.length : 2f);

        // Mientras se espera, se prepara el primer video para que arranque sin tirón
        if (videos != null && videos.Length > 0 && videos[0] != null)
        {
            videoPlayer.clip = videos[0];
            videoPlayer.Prepare();
        }

        yield return EsperarSegundos(tiempoNegro);

        if (apagarSonidoAlEmpezarVideo && fuenteAudio.isPlaying)
            StartCoroutine(FadeOutSonido());

        // ---------- 2. VIDEOS EN ORDEN ----------
        if (videos != null)
        {
            for (int i = 0; i < videos.Length; i++)
            {
                if (videos[i] == null) continue;
                if (saltar) break;

                yield return ReproducirVideo(videos[i]);

                // Pausa en negro entre videos (no después del último)
                if (i < videos.Length - 1 && !saltar)
                    yield return EsperarSegundos(pausaEntreVideos);
            }
        }

        // ---------- 3. FINAL ----------
        videoPlayer.Stop();
        fuenteAudio.Stop();

        if (!string.IsNullOrEmpty(escenaSiguiente))
        {
            if (GestorTransicion.instancia != null)
                GestorTransicion.instancia.CambiarEscena(escenaSiguiente);
            else
                SceneManager.LoadScene(escenaSiguiente);
        }
    }

    IEnumerator ReproducirVideo(VideoClip clip)
    {
        // Prepara el video (si no es el que ya estaba preparado)
        if (videoPlayer.clip != clip)
        {
            videoPlayer.clip = clip;
            videoPlayer.Prepare();
        }

        while (!videoPlayer.isPrepared)
        {
            if (saltar) yield break;
            yield return null;
        }

        videoTermino = false;
        videoPlayer.Play();

        // Espera a que termine (o a que se salte)
        while (!videoTermino && !saltar)
            yield return null;

        videoPlayer.Stop();   // al detenerse, la cámara vuelve a verse negra
    }

    IEnumerator EsperarSegundos(float segundos)
    {
        float t = 0f;
        while (t < segundos && !saltar)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    IEnumerator FadeOutSonido()
    {
        float inicio = fuenteAudio.volume;
        float t = 0f;
        while (t < duracionFadeSonido)
        {
            t += Time.unscaledDeltaTime;
            fuenteAudio.volume = Mathf.Lerp(inicio, 0f, t / duracionFadeSonido);
            yield return null;
        }
        fuenteAudio.Stop();
        fuenteAudio.volume = inicio;
    }

    void AlTerminarVideo(VideoPlayer vp)
    {
        videoTermino = true;
    }

    void AlErrorVideo(VideoPlayer vp, string mensaje)
    {
        Debug.LogError("ControlCinematica: error en el video -> " + mensaje);
        videoTermino = true;   // para que no se quede atorado
    }
}