using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ---------------------------------------------------------------------------
//  ControlCinematica
//  Reproduce las cinemáticas 2D con:
//   - pantalla negra al inicio, entre videos y al final (con fade)
//   - cada video puede ir más lento (dura más) y congelarse en el último cuadro
//   - sonido de entrada en negro (ej. tambores antes del show)
//   - ambiente y música de fondo por video (con fade entre uno y otro)
//   - efectos de sonido sincronizados al segundo exacto del video
//  Clic derecho sobre el componente > "Autoconfigurar sonidos: ..." para
//  llenar todo solo (busca los sonidos por nombre en tu proyecto).
// ---------------------------------------------------------------------------

[System.Serializable]
public class EventoSonido
{
    public string descripcion = "";
    [Tooltip("Segundo DEL VIDEO en que suena. Si es negativo, cuenta desde el final (-2 = 2 s antes de acabar). " +
             "No se desfasa aunque bajes la velocidad del video.")]
    public float segundo = 0f;
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumen = 1f;
    [Tooltip("Segundos de 'subida' que trae el sonido antes del golpe. Se adelanta solo para que el golpe caiga justo en 'Segundo'.")]
    public float adelanto = 0f;
    [Tooltip("Silencia casi todo el ambiente y la música durante estos segundos (hace que el susto pegue más). 0 = no.")]
    public float bajarFondo = 0f;
}

[System.Serializable]
public class AjustesVideo
{
    public string nombre = "Video";

    [Header("Duración")]
    [Tooltip("1 = normal. 0.8 = dura 25% más. 0.7 = dura 43% más.")]
    [Range(0.3f, 1.5f)] public float velocidad = 0.8f;
    [Tooltip("Segundos que se queda congelado el último cuadro antes de irse a negro")]
    public float congelarAlFinal = 1.5f;

    [Header("Antes de que aparezca (en negro)")]
    [Tooltip("Ej. los tambores antes de que empiece el show")]
    public AudioClip sonidoEntrada;
    [Range(0f, 1f)] public float volumenEntrada = 1f;
    [Tooltip("Segundos en negro antes de mostrar el video (el sonido de entrada sigue sonando)")]
    public float esperaEntrada = 0f;

    [Header("Fondo mientras dura este video (en loop)")]
    public AudioClip ambiente;
    [Range(0f, 1f)] public float volumenAmbiente = 0.5f;
    public AudioClip musica;
    [Range(0f, 1f)] public float volumenMusica = 0.4f;

    [Header("Efectos en momentos exactos")]
    public EventoSonido[] eventos = new EventoSonido[0];
}

[RequireComponent(typeof(VideoPlayer))]
public class ControlCinematica : MonoBehaviour
{
    [Header("Cámara (se pone en negro mientras no hay video)")]
    public Camera camara;

    [Header("1. Pantalla negra con sonido")]
    [Tooltip("Sonido de personas llegando y sentándose (o déjalo vacío)")]
    public AudioClip sonidoLlegada;
    [Range(0f, 1f)] public float volumenSonido = 1f;
    [Tooltip("Segundos en negro. Si lo dejas en 0, dura lo que dure el sonido")]
    public float duracionPantallaNegra = 0f;
    [Tooltip("Si está activo, el sonido se apaga poco a poco cuando empieza el primer video")]
    public bool apagarSonidoAlEmpezarVideo = true;
    public float duracionFadeSonido = 1.5f;

    [Header("2. Videos (en orden)")]
    public VideoClip[] videos;
    [Tooltip("Segundos en negro entre un video y otro")]
    public float pausaEntreVideos = 1f;

    [Header("Sonido y duración de cada video (mismo orden que Videos)")]
    public AjustesVideo[] ajustes = new AjustesVideo[0];

    [Header("Fades de imagen")]
    public float duracionFade = 0.8f;
    [Tooltip("Segundos en negro al terminar todo, antes de cambiar de escena")]
    public float negroAlFinal = 1.5f;

    [Header("3. Al terminar")]
    [Tooltip("Nombre de la escena siguiente. Déjalo vacío si no quieres cambiar de escena")]
    public string escenaSiguiente = "";
    public UnityEvent alTerminar;

    [Header("Saltar: Espacio = siguiente video, Escape = toda la cinemática")]
    public bool permitirSaltar = true;

    private VideoPlayer videoPlayer;
    private AudioSource fuenteAudio;      // sonido de llegada
    private AudioSource fuenteAmbiente;
    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;
    private Image negro;
    private float alphaActual = 1f;
    private Coroutine rutinaImagen, rutinaAmbiente, rutinaMusica;

    private bool videoTermino;
    private bool saltarVideo;
    private bool saltarTodo;

    // Volumen "base" de cada fondo (los fades lo mueven) y factor para silenciar en los sustos
    private float baseAmbiente, baseMusica;
    private float factorFondo = 1f, factorFondoObjetivo = 1f;
    private float finSilencio = -1f;

    // ------------------------------------------------------------------ setup
    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.Stop();

        fuenteAudio = GetComponent<AudioSource>();
        if (fuenteAudio == null) fuenteAudio = gameObject.AddComponent<AudioSource>();
        Configurar(fuenteAudio, false);

        fuenteAmbiente = gameObject.AddComponent<AudioSource>(); Configurar(fuenteAmbiente, true);
        fuenteMusica = gameObject.AddComponent<AudioSource>();   Configurar(fuenteMusica, true);
        fuenteEfectos = gameObject.AddComponent<AudioSource>();  Configurar(fuenteEfectos, false);

        if (camara == null) camara = Camera.main;
        if (camara != null)
        {
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            videoPlayer.targetCamera = camara;
        }

        videoPlayer.loopPointReached += AlTerminarVideo;
        videoPlayer.errorReceived += AlErrorVideo;

        CrearPantallaNegra();
    }

    void Configurar(AudioSource s, bool loop)
    {
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        s.volume = loop ? 0f : 1f;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= AlTerminarVideo;
            videoPlayer.errorReceived -= AlErrorVideo;
        }
    }

    void OnValidate()
    {
        // Mantiene "ajustes" del mismo tamaño que "videos"
        int n = videos != null ? videos.Length : 0;
        if (ajustes == null) ajustes = new AjustesVideo[0];
        if (ajustes.Length != n)
        {
            var nuevo = new AjustesVideo[n];
            for (int i = 0; i < n; i++)
                nuevo[i] = (i < ajustes.Length && ajustes[i] != null) ? ajustes[i] : new AjustesVideo { nombre = "Video " + i };
            ajustes = nuevo;
        }
    }

    void Start()
    {
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        // Silencio de los sustos: baja rápido, regresa poco a poco
        if (finSilencio > 0f && Time.unscaledTime >= finSilencio)
        {
            factorFondoObjetivo = 1f;
            finSilencio = -1f;
        }
        float velocidadFactor = factorFondoObjetivo < factorFondo ? 8f : 0.6f;
        factorFondo = Mathf.MoveTowards(factorFondo, factorFondoObjetivo, velocidadFactor * Time.unscaledDeltaTime);
        if (fuenteAmbiente != null) fuenteAmbiente.volume = baseAmbiente * factorFondo;
        if (fuenteMusica != null) fuenteMusica.volume = baseMusica * factorFondo;

        if (!permitirSaltar) return;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;
        if (k.spaceKey.wasPressedThisFrame) saltarVideo = true;
        if (k.escapeKey.wasPressedThisFrame) saltarTodo = true;
#else
        if (Input.GetKeyDown(KeyCode.Space)) saltarVideo = true;
        if (Input.GetKeyDown(KeyCode.Escape)) saltarTodo = true;
#endif
    }

    AjustesVideo Ajustes(int i)
    {
        if (ajustes != null && i < ajustes.Length && ajustes[i] != null) return ajustes[i];
        return new AjustesVideo { velocidad = 1f, congelarAlFinal = 0f };
    }

    // ------------------------------------------------------------------ secuencia
    IEnumerator Secuencia()
    {
        SetAlpha(1f);

        // ---------- 1. PANTALLA NEGRA + SONIDO ----------
        if (sonidoLlegada != null)
        {
            fuenteAudio.clip = sonidoLlegada;
            fuenteAudio.volume = volumenSonido;
            fuenteAudio.Play();
        }

        float tiempoNegro = duracionPantallaNegra > 0f
            ? duracionPantallaNegra
            : (sonidoLlegada != null ? sonidoLlegada.length : 1.5f);

        if (videos != null && videos.Length > 0 && videos[0] != null)
        {
            videoPlayer.clip = videos[0];
            videoPlayer.Prepare();
        }

        saltarVideo = false;
        yield return Esperar(tiempoNegro);

        if (apagarSonidoAlEmpezarVideo && fuenteAudio.isPlaying)
            StartCoroutine(FadeVolumen(fuenteAudio, fuenteAudio.volume, 0f, duracionFadeSonido, true));

        // ---------- 2. VIDEOS EN ORDEN ----------
        if (videos != null)
        {
            for (int i = 0; i < videos.Length && !saltarTodo; i++)
            {
                if (videos[i] == null) continue;
                AjustesVideo a = Ajustes(i);

                // Fondo de este video (si es el mismo que el anterior, sigue sin cortarse)
                CambiarFondo(fuenteAmbiente, a.ambiente, a.volumenAmbiente, ref rutinaAmbiente);
                CambiarFondo(fuenteMusica, a.musica, a.volumenMusica, ref rutinaMusica);

                // Sonido de entrada en negro (ej. tambores)
                saltarVideo = false;
                if (videoPlayer.clip != videos[i])
                {
                    videoPlayer.clip = videos[i];
                    videoPlayer.Prepare();
                }
                if (a.sonidoEntrada != null)
                    fuenteEfectos.PlayOneShot(a.sonidoEntrada, a.volumenEntrada);
                yield return Esperar(a.esperaEntrada);

                while (!videoPlayer.isPrepared && !saltarTodo)
                    yield return null;
                if (saltarTodo) break;

                yield return ReproducirVideo(videos[i], a);

                if (i < videos.Length - 1 && !saltarTodo)
                {
                    saltarVideo = false;
                    yield return Esperar(pausaEntreVideos);
                }
            }
        }

        // ---------- 3. FINAL ----------
        float fadeFinal = saltarTodo ? 0.3f : duracionFadeSonido;
        if (saltarTodo)
        {
            yield return FadeImagen(1f, 0.3f);
            fuenteEfectos.Stop();
        }
        videoPlayer.Stop();

        CambiarFondo(fuenteAmbiente, null, 0f, ref rutinaAmbiente, fadeFinal);
        CambiarFondo(fuenteMusica, null, 0f, ref rutinaMusica, fadeFinal);
        if (fuenteAudio.isPlaying)
            StartCoroutine(FadeVolumen(fuenteAudio, fuenteAudio.volume, 0f, fadeFinal, true));

        float espera = saltarTodo ? 0.4f : Mathf.Max(negroAlFinal, fadeFinal);
        float t = 0f;
        while (t < espera) { t += Time.unscaledDeltaTime; yield return null; }

        alTerminar.Invoke();

        if (!string.IsNullOrEmpty(escenaSiguiente))
        {
            if (!Application.CanStreamedLevelBeLoaded(escenaSiguiente))
            {
                Debug.LogError("[ControlCinematica] La escena '" + escenaSiguiente +
                               "' no existe o no está en File > Build Profiles > Scene List.", this);
            }
            else if (GestorTransicion.instancia != null)
                GestorTransicion.instancia.CambiarEscena(escenaSiguiente);
            else
                SceneManager.LoadScene(escenaSiguiente);
        }
    }

    IEnumerator ReproducirVideo(VideoClip clip, AjustesVideo a)
    {
        videoTermino = false;
        saltarVideo = false;

        videoPlayer.playbackSpeed = a.velocidad > 0.05f ? a.velocidad : 1f;
        videoPlayer.Play();

        // Espera el primer cuadro y aparece desde negro
        float espera = 0f;
        while (videoPlayer.frame < 0 && espera < 2f) { espera += Time.unscaledDeltaTime; yield return null; }
        IniciarFadeImagen(0f, duracionFade);

        bool[] sonado = new bool[a.eventos != null ? a.eventos.Length : 0];
        double largo = clip.length;
        double fin = largo - 0.12;   // se pausa justo antes del final para congelar el último cuadro

        while (!videoTermino && videoPlayer.time < fin && !saltarVideo && !saltarTodo)
        {
            DispararEventos(a, sonado, videoPlayer.time, largo);
            yield return null;
        }

        bool saltado = saltarVideo || saltarTodo;
        if (!saltado) DispararEventos(a, sonado, largo, largo);

        // Congelar último cuadro
        if (videoPlayer.isPlaying) videoPlayer.Pause();
        if (!saltado) yield return Esperar(a.congelarAlFinal);

        // A negro
        yield return FadeImagen(1f, saltado ? 0.35f : duracionFade);
        videoPlayer.Stop();
        if (saltado) fuenteEfectos.Stop();
    }

    void DispararEventos(AjustesVideo a, bool[] sonado, double tiempo, double largo)
    {
        if (a.eventos == null) return;
        for (int i = 0; i < a.eventos.Length && i < sonado.Length; i++)
        {
            EventoSonido e = a.eventos[i];
            if (sonado[i] || e == null || e.sonido == null) continue;
            double s = e.segundo >= 0f ? e.segundo : largo + e.segundo;
            s -= e.adelanto * videoPlayer.playbackSpeed;   // la "subida" empieza antes para que el golpe caiga exacto
            if (tiempo >= s)
            {
                sonado[i] = true;
                fuenteEfectos.PlayOneShot(e.sonido, e.volumen);
                if (e.bajarFondo > 0f)
                {
                    factorFondoObjetivo = 0.08f;
                    finSilencio = Time.unscaledTime + e.bajarFondo;
                }
            }
        }
    }

    // Espera en tiempo real; Espacio corta la espera actual, Escape corta todo
    IEnumerator Esperar(float segundos)
    {
        float t = 0f;
        while (t < segundos && !saltarVideo && !saltarTodo)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // ------------------------------------------------------------------ audio de fondo
    void CambiarFondo(AudioSource src, AudioClip clip, float vol, ref Coroutine rutina, float dur = -1f)
    {
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(TransicionFondo(src, clip, vol, dur < 0f ? duracionFadeSonido : dur));
    }

    IEnumerator TransicionFondo(AudioSource src, AudioClip clip, float vol, float dur)
    {
        if (src.clip == clip && src.isPlaying)
        {
            yield return FadeBase(src, GetBase(src), vol, dur);
            yield break;
        }

        if (src.isPlaying)
        {
            yield return FadeBase(src, GetBase(src), 0f, dur * 0.5f);
            src.Stop();
        }

        src.clip = clip;
        if (clip == null) yield break;

        SetBase(src, 0f);
        src.time = 0f;
        src.Play();
        yield return FadeBase(src, 0f, vol, dur);
    }

    float GetBase(AudioSource s) { return s == fuenteAmbiente ? baseAmbiente : baseMusica; }

    void SetBase(AudioSource s, float v)
    {
        if (s == fuenteAmbiente) baseAmbiente = v; else baseMusica = v;
    }

    IEnumerator FadeBase(AudioSource s, float desde, float hasta, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            SetBase(s, Mathf.Lerp(desde, hasta, t / dur));
            yield return null;
        }
        SetBase(s, hasta);
    }

    IEnumerator FadeVolumen(AudioSource s, float desde, float hasta, float dur, bool detenerAlFinal)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            s.volume = Mathf.Lerp(desde, hasta, t / dur);
            yield return null;
        }
        s.volume = hasta;
        if (detenerAlFinal) s.Stop();
    }

    // ------------------------------------------------------------------ imagen negra
    void IniciarFadeImagen(float hasta, float dur)
    {
        if (rutinaImagen != null) StopCoroutine(rutinaImagen);
        rutinaImagen = StartCoroutine(FadeImagen(hasta, dur));
    }

    IEnumerator FadeImagen(float hasta, float dur)
    {
        float desde = alphaActual;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(desde, hasta, t / dur));
            yield return null;
        }
        SetAlpha(hasta);
    }

    void SetAlpha(float a)
    {
        alphaActual = a;
        if (negro != null) negro.color = new Color(0f, 0f, 0f, a);
    }

    void CrearPantallaNegra()
    {
        GameObject canvasGO = new GameObject("PantallaNegraCinematica");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imgGO = new GameObject("Negro");
        imgGO.transform.SetParent(canvasGO.transform, false);
        negro = imgGO.AddComponent<Image>();
        negro.raycastTarget = false;
        RectTransform rt = negro.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        SetAlpha(1f);
    }

    void AlTerminarVideo(VideoPlayer vp) { videoTermino = true; }

    void AlErrorVideo(VideoPlayer vp, string mensaje)
    {
        Debug.LogError("ControlCinematica: error en el video -> " + mensaje);
        videoTermino = true;
    }

    // ------------------------------------------------------------------ autoconfiguración (solo editor)
#if UNITY_EDITOR
    [ContextMenu("Autoconfigurar sonidos: Show (teatro + guardar marionetas)")]
    void AutoconfigurarShow()
    {
        UnityEditor.Undo.RecordObject(this, "Autoconfigurar Show");
        Asegurar(2);

        var a0 = ajustes[0];
        a0.nombre = "Teatro (show)";
        a0.velocidad = 0.8f;
        a0.congelarAlFinal = 1.5f;
        a0.sonidoEntrada = Buscar("Tambores_Inicio"); a0.volumenEntrada = 1f;
        a0.esperaEntrada = 3f;    // el redoble suena en negro y el video aparece con el platillo
        a0.ambiente = Buscar("Ambiente_Plaza"); a0.volumenAmbiente = 0.45f;
        a0.musica = Buscar("Musica_Teatro"); a0.volumenMusica = 0.28f;
        a0.eventos = new[]
        {
            Ev("Ta-dán del final del show", -2.5f, "Tambores_Final", 0.9f),
        };

        var a1 = ajustes[1];
        a1.nombre = "Aplausos y guardar marionetas";
        a1.velocidad = 0.8f;
        a1.congelarAlFinal = 2f;
        a1.sonidoEntrada = null; a1.esperaEntrada = 0f;
        a1.ambiente = Buscar("Ambiente_Plaza"); a1.volumenAmbiente = 0.3f;
        a1.musica = null;
        a1.eventos = new[]
        {
            Ev("Aplausos del público", 0f, "Aplausos", 1f),
            Ev("Click click de los broches", 5.6f, "Cerrojos_Click", 0.9f),
            Ev("Abre la maleta", 6.5f, "Maleta_Abrir", 0.9f),
            Ev("Guarda las marionetas", 10.8f, "Guardar_Marionetas", 0.85f),
        };

        pausaEntreVideos = 1f;
        duracionFade = 0.8f;
        negroAlFinal = 1.5f;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[ControlCinematica] Show configurado. Revisa en la consola si faltó algún sonido.", this);
    }

    [ContextMenu("Autoconfigurar sonidos: Cena (terror)")]
    void AutoconfigurarCena()
    {
        UnityEditor.Undo.RecordObject(this, "Autoconfigurar Cena");
        Asegurar(2);

        if (sonidoLlegada == null && duracionPantallaNegra <= 0f) duracionPantallaNegra = 1f;

        var a0 = ajustes[0];
        a0.nombre = "Cena";
        a0.velocidad = 0.8f;
        a0.congelarAlFinal = 1.5f;
        a0.sonidoEntrada = null; a0.esperaEntrada = 1.5f;   // se oye la casa en negro antes de ver
        a0.ambiente = Buscar("Ambiente_CasaNoche"); a0.volumenAmbiente = 0.55f;
        a0.musica = Buscar("MusicaVidaCotidiana_Oscura", false); a0.volumenMusica = 0.3f;
        a0.eventos = new[]
        {
            Ev("Bocados (come, mastica, traga)", 0f, "Bocados", 0.9f),
            Ev("Cuchara en el plato", 2.2f, "Cuchara_Plato", 0.5f),
            Ev("MARIONETA gira la cabeza y mira al Capi", 4.5f, "Marioneta_GiraCabeza", 1f, 0.9f, 5f),
            Ev("Se levanta: silla arrastrando", 6.8f, "Silla_Arrastrar", 0.9f),
            Ev("Camina a la cocina", 10.5f, "Pasos_Madera", 0.8f),
        };

        var a1 = ajustes[1];
        a1.nombre = "Pasillo (susto)";
        a1.velocidad = 0.8f;
        a1.congelarAlFinal = 2.5f;
        a1.sonidoEntrada = null; a1.esperaEntrada = 0f;
        a1.ambiente = Buscar("Ambiente_CasaNoche"); a1.volumenAmbiente = 0.4f;
        a1.musica = Buscar("Drone_Terror"); a1.volumenMusica = 0.6f;
        a1.eventos = new[]
        {
            Ev("Pasos en el pasillo", 0f, "Pasos_Madera", 0.7f),
            Ev("Aparece el fantasma", 2.6f, "Susurro_Fantasma", 0.9f),
            Ev("Latido de tensión", 3.6f, "Latido_Tension", 1f),
            Ev("SUSTO: monstruo rojo", 7.4f, "Jumpscare_Monstruo", 1f, 1.2f, 3f),
            Ev("Corre a su cuarto y azota la puerta", 10.2f, "Correr_Portazo", 1f),
            Ev("Respiración de miedo (recargado en la puerta)", 11.6f, "Respiracion_Miedo", 0.6f),
            Ev("El monstruo empuja la puerta", 11.8f, "Monstruo_Empuja_Puerta", 1f),
        };

        pausaEntreVideos = 1f;
        duracionFade = 0.8f;
        negroAlFinal = 2f;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[ControlCinematica] Cena configurada. Revisa en la consola si faltó algún sonido.", this);
    }

    void Asegurar(int n)
    {
        if (videos == null || videos.Length < n)
        {
            var v = new VideoClip[n];
            if (videos != null) for (int i = 0; i < videos.Length; i++) v[i] = videos[i];
            videos = v;
        }
        OnValidate();
    }

    EventoSonido Ev(string desc, float seg, string nombreClip, float vol, float adelanto = 0f, float bajarFondo = 0f)
    {
        return new EventoSonido
        {
            descripcion = desc, segundo = seg, sonido = Buscar(nombreClip), volumen = vol,
            adelanto = adelanto, bajarFondo = bajarFondo
        };
    }

    AudioClip Buscar(string nombre, bool avisar = true)
    {
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets(nombre + " t:AudioClip"))
        {
            string ruta = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(ruta) == nombre)
                return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }
        if (avisar) Debug.LogWarning("[ControlCinematica] No encontré el sonido '" + nombre + "' en el proyecto.", this);
        return null;
    }
#endif
}