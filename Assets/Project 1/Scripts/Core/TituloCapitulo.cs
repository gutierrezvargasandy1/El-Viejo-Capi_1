using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Título de capítulo al empezar la escena ("CAPÍTULO 1 — Un día normal").
/// Ponlo en cualquier escena (la herramienta Herramientas > Capítulos y final lo hace sola).
/// - Sale después de la pantalla de carga, sobre negro, con un golpe de piano.
/// - Las letras del título aparecen una por una y al final se deshacen.
/// - Mientras está en pantalla la escena se queda congelada (la cinemática no avanza) y en silencio.
/// - Si reintentas la misma escena (Game Over) ya no vuelve a salir.
/// </summary>
[DefaultExecutionOrder(-500)]
public class TituloCapitulo : MonoBehaviour
{
    [Header("Texto")]
    public int numero = 1;
    public string titulo = "Un día normal";
    [Tooltip("Opcional: una línea chiquita debajo del título")]
    public string subtitulo = "";
    [Tooltip("Opcional: si lo dejas vacío dice CAPÍTULO + número")]
    public string textoCapitulo = "";
    [Tooltip("Opcional: fuente TextMeshPro. Vacío = la de TMP por defecto")]
    public TMP_FontAsset fuente;

    [Header("Tiempos (segundos)")]
    [Tooltip("Negro antes de que empiece a aparecer")]
    public float esperaInicial = 0.4f;
    [Tooltip("Cuánto se queda el título completo en pantalla")]
    public float duracion = 3.5f;
    [Tooltip("Cuánto tarda en aparecer la escena detrás")]
    public float fadeFondo = 1.4f;

    [Header("Opciones")]
    [Tooltip("La escena no avanza (cinemáticas, timelines) mientras está el título")]
    public bool congelarEscena = true;
    [Tooltip("Los sonidos de la escena esperan a que termine el título")]
    public bool pausarSonidosDeEscena = true;
    [Tooltip("Si reintentas la misma escena (por ejemplo después del Game Over) ya no sale otra vez")]
    public bool noRepetirAlReintentar = true;
    [Tooltip("Clic / Espacio / Enter para saltarlo")]
    public bool sePuedeSaltar = true;

    [Header("Sonido")]
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumen = 0.9f;
    [Tooltip("Segundo del sonido donde está el golpe (ahí aparece el título)")]
    public float golpeEn = 1.3f;

    [Header("Colores")]
    public Color colorCapitulo = new Color(0.72f, 0.64f, 0.55f);
    public Color colorTitulo = new Color(0.94f, 0.9f, 0.84f);
    public Color colorLinea = new Color(0.62f, 0.08f, 0.06f);
    public Color colorSubtitulo = new Color(0.6f, 0.56f, 0.52f);

    /// <summary>True mientras hay un título de capítulo en pantalla (la cinemática final lo espera).</summary>
    public static bool Activo { get; private set; }
    public static event System.Action AlTerminar;

    static string ultimaDescargada;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        Activo = false;
        ultimaDescargada = null;
        SceneManager.sceneUnloaded -= Descargada;
        SceneManager.sceneUnloaded += Descargada;
    }

    static void Descargada(Scene s) => ultimaDescargada = s.name;

    Canvas canvas;
    CanvasGroup grupo;
    TextMeshProUGUI txtCapitulo, txtTitulo, txtSub;
    RectTransform linea, rtTitulo;
    AudioSource fuenteAudio;
    float escalaAntes = 1f;
    bool congelado, pausado, terminado, saltar;
    float[] azar;

    const float AnchoLinea = 520f;

    void Awake()
    {
        Activo = true;
        Construir();
        if (congelarEscena)
        {
            escalaAntes = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            congelado = true;
        }
    }

    void Start()
    {
        if (noRepetirAlReintentar && ultimaDescargada == gameObject.scene.name)
        {
            Terminar();                     // es un reintento: directo al juego
            return;
        }
        StartCoroutine(Rutina());
    }

    void OnDestroy()
    {
        if (!terminado) Terminar();
    }

    // ───────────────────────── animación ─────────────────────────

    IEnumerator Rutina()
    {
        // espera a que se quite la pantalla de carga (si la hay)
        while (GestorTransicion.instancia != null && GestorTransicion.instancia.EnTransicion)
        {
            Silenciar();
            yield return null;
        }
        float t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < esperaInicial) { Silenciar(); yield return null; }

        if (sonido != null) fuenteAudio.PlayOneShot(sonido, volumen);
        float aparece = sonido != null ? Mathf.Max(0.3f, golpeEn) : 0.9f;      // el título sale con el golpe
        float completo = aparece + 0.045f * Mathf.Max(1, titulo.Length) + 1.0f;
        float salida = completo + duracion;
        float fin = salida + 1.1f;

        txtTitulo.alpha = 1f;
        float t = 0f;
        float tSalto = -1f;
        while (t < fin)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            Silenciar();

            if (sePuedeSaltar && tSalto < 0f && t > 0.6f && Presiono())
            {
                tSalto = t;
                if (t < salida) { float corte = salida - t; salida -= corte; fin -= corte; }
            }

            float sal = Mathf.Clamp01((t - salida) / 1.0f);          // 0..1 mientras desaparece

            // "CAPÍTULO N": aparece y se juntan las letras
            float a = Suave(Mathf.Clamp01(t / 1.0f)) * (1f - sal);
            txtCapitulo.alpha = a;
            txtCapitulo.characterSpacing = Mathf.Lerp(45f, 16f, Suave(Mathf.Clamp01(t / 1.8f)));

            // línea roja que se dibuja desde el centro
            float l = Suave(Mathf.Clamp01((t - 0.35f) / 1.2f));
            linea.sizeDelta = new Vector2(AnchoLinea * l * (1f - 0.4f * sal), 3f);
            linea.GetComponent<Image>().color = new Color(colorLinea.r, colorLinea.g, colorLinea.b, l * (1f - sal));

            // título: letra por letra, con un parpadeo suave de vela
            float esc = Mathf.Lerp(1.07f, 1f, Suave(Mathf.Clamp01((t - aparece) / 1.6f)));
            rtTitulo.localScale = new Vector3(esc, esc, 1f);
            float vela = t > completo ? 0.93f + 0.07f * Mathf.PerlinNoise(t * 2.5f, 3.3f) : 1f;
            AnimarLetras(txtTitulo, t - aparece, t - salida, vela);

            // subtítulo
            txtSub.alpha = Suave(Mathf.Clamp01((t - aparece - 0.9f) / 1.0f)) * (1f - sal);
            yield return null;
        }

        // aparece la escena detrás
        DescongelarYSonar();
        float f = 0f;
        while (f < fadeFondo)
        {
            f += Time.unscaledDeltaTime;
            grupo.alpha = 1f - Suave(Mathf.Clamp01(f / Mathf.Max(0.05f, fadeFondo)));
            yield return null;
        }
        Terminar();
    }

    void AnimarLetras(TextMeshProUGUI txt, float tEntrada, float tSalida, float brillo)
    {
        txt.ForceMeshUpdate();
        var info = txt.textInfo;
        if (azar == null || azar.Length < info.characterCount)
        {
            azar = new float[Mathf.Max(1, info.characterCount)];
            for (int i = 0; i < azar.Length; i++) azar[i] = Random.value;
        }
        for (int i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i];
            if (!ch.isVisible) continue;
            float e = Suave(Mathf.Clamp01((tEntrada - i * 0.045f) / 0.6f));
            float alfa = e;
            float dy = (1f - e) * -18f;
            if (tSalida > 0f)
            {
                float s = Suave(Mathf.Clamp01((tSalida - azar[i] * 0.45f) / 0.55f));
                alfa *= 1f - s;
                dy -= 12f * s;
            }
            alfa *= brillo;
            int mi = ch.materialReferenceIndex, vi = ch.vertexIndex;
            var colores = info.meshInfo[mi].colors32;
            var vertices = info.meshInfo[mi].vertices;
            byte a = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alfa));
            for (int k = 0; k < 4; k++)
            {
                colores[vi + k].a = a;
                vertices[vi + k].y += dy;
            }
        }
        txt.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices);
    }

    static float Suave(float x) => x * x * (3f - 2f * x);

    static bool Presiono()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;   // Esc es del menú de pausa
        var m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame) return true;
        var g = Gamepad.current;
        if (g != null && g.buttonSouth.wasPressedThisFrame) return true;
        var ts = Touchscreen.current;
        return ts != null && ts.primaryTouch.press.wasPressedThisFrame;
    }

    // la pantalla de carga quita la pausa del audio al terminar: aquí se vuelve a poner mientras esté el título
    void Silenciar()
    {
        if (!pausarSonidosDeEscena || terminado) return;
        if (!AudioListener.pause) AudioListener.pause = true;
        pausado = true;
    }

    void DescongelarYSonar()
    {
        if (congelado)
        {
            Time.timeScale = escalaAntes;
            congelado = false;
        }
        if (pausado)
        {
            AudioListener.pause = false;
            pausado = false;
        }
    }

    void Terminar()
    {
        if (terminado) return;
        terminado = true;
        DescongelarYSonar();
        Activo = false;
        if (canvas != null) Destroy(canvas.gameObject);
        AlTerminar?.Invoke();
    }

    // ───────────────────────── interfaz ─────────────────────────

    void Construir()
    {
        var go = new GameObject("TituloCapitulo_UI", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;                            // debajo de la pantalla de carga (1000)
        var escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        grupo = go.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = true;
        Transform raiz = go.transform;

        var fondo = Rect("Fondo", raiz).gameObject.AddComponent<Image>();
        fondo.color = Color.black;
        Estirar(fondo.rectTransform);

        string cap = string.IsNullOrEmpty(textoCapitulo) ? "CAPÍTULO " + numero : textoCapitulo;
        txtCapitulo = Texto("Capitulo", raiz, 40f, colorCapitulo, cap);
        Anclar(txtCapitulo.rectTransform, new Vector2(0f, 105f), new Vector2(1400f, 60f));
        txtCapitulo.alpha = 0f;

        var img = Rect("Linea", raiz).gameObject.AddComponent<Image>();
        img.color = new Color(colorLinea.r, colorLinea.g, colorLinea.b, 0f);
        img.raycastTarget = false;
        linea = img.rectTransform;
        Anclar(linea, new Vector2(0f, 66f), new Vector2(0f, 3f));

        txtTitulo = Texto("Titulo", raiz, 104f, colorTitulo, titulo);
        rtTitulo = txtTitulo.rectTransform;
        Anclar(rtTitulo, new Vector2(0f, -15f), new Vector2(1800f, 140f));
        txtTitulo.characterSpacing = 4f;

        txtSub = Texto("Subtitulo", raiz, 34f, colorSubtitulo, subtitulo);
        Anclar(txtSub.rectTransform, new Vector2(0f, -110f), new Vector2(1400f, 50f));
        txtSub.fontStyle = FontStyles.Italic;
        txtSub.alpha = 0f;

        txtTitulo.alpha = 0f;                                 // invisible hasta que empiece (luego letra por letra)

        fuenteAudio = gameObject.AddComponent<AudioSource>();
        fuenteAudio.playOnAwake = false;
        fuenteAudio.spatialBlend = 0f;
        fuenteAudio.ignoreListenerPause = true;
    }

    TextMeshProUGUI Texto(string nombre, Transform padre, float tam, Color color, string texto)
    {
        var tx = Rect(nombre, padre).gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) tx.font = fuente;
        tx.fontSize = tam;
        tx.color = color;
        tx.alignment = TextAlignmentOptions.Center;
        tx.raycastTarget = false;
        tx.text = texto;
        return tx;
    }

    static RectTransform Rect(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        return rt;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Anclar(RectTransform rt, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }
}
