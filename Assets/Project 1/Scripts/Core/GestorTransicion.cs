using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cambio de escena con pantalla de carga: barra, consejos / historia al azar
/// y fade de TODO el audio del juego.
///
/// El cambio de escena es EL MISMO de antes: SceneManager.LoadScene (carga normal).
/// CambioEscena y ControlCinematica lo siguen llamando igual:
///     GestorTransicion.instancia.CambiarEscena("Esena 2");
///
/// Nota: como LoadScene carga de golpe, la barra es visual (avanza durante el
/// tiempo mínimo y se completa cuando la escena nueva ya cargó).
/// </summary>
public class GestorTransicion : MonoBehaviour
{
    public static GestorTransicion instancia;

    [Header("Configuración (consejos, historia, tiempos, audio, colores)")]
    [Tooltip("Se crea con Herramientas > Pantalla de Carga. Si está vacío, se busca Resources/ConfigPantallaCarga")]
    public ConfigPantallaCarga config;

    [Header("Referencias (las llena la herramienta, no hace falta tocarlas)")]
    public CanvasGroup pantallaNegra;
    public CanvasGroup contenido;
    public Image negro;
    public Image fondo;
    public AspectRatioFitter ajusteFondo;
    public RectTransform barraRelleno;
    public TextMeshProUGUI textoEtiqueta;
    public TextMeshProUGUI textoConsejo;
    public TextMeshProUGUI textoCargando;
    public TextMeshProUGUI textoPorcentaje;

    public bool EnTransicion => enTransicion;

    const string NombreUI = "PantallaCarga_UI";

    bool enTransicion;
    float progreso;
    float semilla;
    float proximoCambioTexto;
    int ultimoTexto = -1;
    int ultimoFondo = -1;
    float inicioTransicion;
    float volumenGuardado = 1f;
    AudioSource fuenteMusica;
    ConfigPantallaCarga porDefecto;

    // Si alguna escena no tiene Gestor de Transiciones (por ejemplo al dar Play
    // directo en una escena de prueba), se crea solo.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CrearSiFalta()
    {
        if (instancia == null)
            new GameObject("Gestor de Transiciones").AddComponent<GestorTransicion>();
    }

    ConfigPantallaCarga Cfg
    {
        get
        {
            if (config == null) config = Resources.Load<ConfigPantallaCarga>("ConfigPantallaCarga");
            if (config != null) return config;
            if (porDefecto == null) porDefecto = ScriptableObject.CreateInstance<ConfigPantallaCarga>();
            return porDefecto;
        }
    }

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        transform.SetParent(null);          // DontDestroyOnLoad solo funciona en objetos raíz
        DontDestroyOnLoad(gameObject);

        // Si la escena todavía tiene la pantalla negra vieja, se arma la nueva aquí.
        if (!UIValida()) ConstruirUI();
        Ocultar();

        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteMusica.loop = true;
        fuenteMusica.spatialBlend = 0f;
        fuenteMusica.ignoreListenerVolume = true;   // suena aunque el volumen general esté en 0
        fuenteMusica.ignoreListenerPause = true;
    }

    void OnDestroy()
    {
        if (instancia != this) return;
        // Por seguridad: si se destruye a media transición, no dejar el juego sin sonido.
        if (enTransicion && Cfg.bajarVolumen) AudioListener.volume = volumenGuardado;
        instancia = null;
    }

    // ===================== CAMBIO DE ESCENA (igual que antes) =====================

    public void CambiarEscena(string nombreEscena)
    {
        if (enTransicion) return;

        if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            Debug.LogError($"La escena '{nombreEscena}' no existe o no está en File > Build Profiles > Scene List.");
            return;
        }

        StartCoroutine(Transicion(nombreEscena));
    }

    IEnumerator Transicion(string nombreEscena)
    {
        var c = Cfg;
        enTransicion = true;
        volumenGuardado = AudioListener.volume;

        if (!UIValida()) ConstruirUI();

        progreso = 0f;
        semilla = Random.value * 100f;
        inicioTransicion = Time.unscaledTime;
        ElegirFondo();
        MostrarTextoNuevo();
        textoConsejo.alpha = 1f;
        textoEtiqueta.alpha = 1f;
        proximoCambioTexto = Time.unscaledTime + c.duracionFade + c.esperaEnNegro + c.duracionFadeContenido + c.cambiarTextoCada;
        contenido.alpha = 0f;   // al principio solo se ve el negro
        PintarBarra();
        pantallaNegra.blocksRaycasts = true;   // bloquea clics durante la transición

        if (c.musicaCarga != null)
        {
            fuenteMusica.clip = c.musicaCarga;
            fuenteMusica.volume = 0f;
            fuenteMusica.Play();
        }

        // 1. La pantalla se va a negro y baja el volumen del juego
        yield return Fundido(0f, 1f, volumenGuardado, 0f);

        // 2. Se queda en negro un momento
        yield return Esperar(c.esperaEnNegro);

        // 3. Aparecen la imagen, el consejo y la barra
        yield return FadeContenido(0f, 1f);

        // 4. La barra avanza hasta 90 % durante el tiempo mínimo
        float duracion = Mathf.Max(0.1f, c.tiempoMinimo);
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float x = Mathf.Clamp01(t / duracion);
            float curva = 1f - (1f - x) * (1f - x);
            float tirones = (Mathf.PerlinNoise(semilla, t * 1.3f) - 0.5f) * 0.3f * Mathf.Sin(x * Mathf.PI);
            progreso = Mathf.Max(progreso, 0.9f * Mathf.Clamp01(curva + tirones));
            yield return null;
        }
        progreso = Mathf.Max(progreso, 0.9f);
        yield return null;   // que se dibuje la barra antes del tirón de la carga

        if (c.reanudarTiempo)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        // 5. Carga NORMAL (la que sí funciona): descarga la escena actual completa
        //    y luego entra la nueva. La pantalla de carga tapa el tirón.
        SceneManager.LoadScene(nombreEscena);

        // Espera un par de frames a que la escena nueva haga su Awake/Start
        yield return null;
        yield return null;

        // 6. La barra llega a 100 %
        float desde = progreso;
        const float llenar = 0.4f;
        t = 0f;
        while (t < llenar)
        {
            t += Time.unscaledDeltaTime;
            progreso = Mathf.Lerp(desde, 1f, t / llenar);
            yield return null;
        }
        progreso = 1f;
        yield return Esperar(c.esperaAlLlegar100);

        // 7. Desaparecen la imagen, el consejo y la barra: queda solo el negro
        yield return FadeContenido(1f, 0f);

        // 8. Se queda en negro un momento
        yield return Esperar(c.esperaEnNegro);

        // 9. Se aclara el negro, se ve la escena nueva y sube el volumen
        yield return Fundido(1f, 0f, 0f, volumenGuardado);

        if (fuenteMusica.isPlaying) fuenteMusica.Stop();
        Ocultar();
        enTransicion = false;
    }

    IEnumerator Fundido(float alfaDesde, float alfaHasta, float volDesde, float volHasta)
    {
        float d = Mathf.Max(0.01f, Cfg.duracionFade);
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            AplicarFundido(Mathf.Lerp(alfaDesde, alfaHasta, k), Mathf.Lerp(volDesde, volHasta, k));
            yield return null;
        }
        AplicarFundido(alfaHasta, volHasta);
    }

    IEnumerator FadeContenido(float desde, float hasta)
    {
        float d = Mathf.Max(0.01f, Cfg.duracionFadeContenido);
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            contenido.alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(t / d));
            yield return null;
        }
        contenido.alpha = hasta;
    }

    void AplicarFundido(float alfa, float volumen)
    {
        if (pantallaNegra != null) pantallaNegra.alpha = alfa;
        if (Cfg.bajarVolumen) AudioListener.volume = volumen;
        if (fuenteMusica != null && fuenteMusica.clip != null)
            fuenteMusica.volume = alfa * Cfg.volumenMusicaCarga;
    }

    static IEnumerator Esperar(float segundos)
    {
        float t = 0f;
        while (t < segundos)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // ===================== BARRA Y TEXTOS =====================

    void Update()
    {
        if (!enTransicion || !UIValida()) return;

        PintarBarra();
        AnimarZoom();

        int puntos = (int)(Time.unscaledTime * 2.5f) % 4;
        textoCargando.text = Cfg.textoCargando + new string('.', puntos);

        if (progreso < 1f && Time.unscaledTime >= proximoCambioTexto)
        {
            proximoCambioTexto = Time.unscaledTime + Cfg.cambiarTextoCada;
            StartCoroutine(CambiarTextoAnimado());
        }
    }

    void PintarBarra()
    {
        if (barraRelleno != null) barraRelleno.anchorMax = new Vector2(progreso, 1f);
        if (textoPorcentaje != null) textoPorcentaje.text = Mathf.RoundToInt(progreso * 100f) + "%";
    }

    void ElegirFondo()
    {
        var lista = Cfg.imagenesFondo;
        int validas = 0;
        if (lista != null) foreach (var sp in lista) if (sp != null) validas++;

        if (validas == 0)
        {
            fondo.enabled = false;    // solo se ve el color de fondo
            return;
        }

        int i;
        do { i = Random.Range(0, lista.Length); }
        while (lista[i] == null || (validas > 1 && i == ultimoFondo));
        ultimoFondo = i;

        var sprite = lista[i];
        fondo.sprite = sprite;
        fondo.color = Color.white;
        fondo.enabled = true;
        ajusteFondo.aspectRatio = sprite.rect.width / sprite.rect.height;
        fondo.rectTransform.localScale = Vector3.one;
    }

    void AnimarZoom()
    {
        if (fondo == null || !fondo.enabled) return;
        if (!Cfg.zoomLento) { fondo.rectTransform.localScale = Vector3.one; return; }

        float total = Cfg.duracionFade * 2f + Cfg.esperaEnNegro * 2f + Cfg.duracionFadeContenido * 2f + Cfg.tiempoMinimo + 1f;
        float k = Mathf.Clamp01((Time.unscaledTime - inicioTransicion) / total);
        float escala = Mathf.Lerp(1f, Cfg.zoomMaximo, k * (2f - k));   // empieza rápido y frena
        fondo.rectTransform.localScale = new Vector3(escala, escala, 1f);
    }

    void MostrarTextoNuevo()
    {
        var lista = Cfg.textos;
        if (lista == null || lista.Length == 0)
        {
            textoEtiqueta.text = "";
            textoConsejo.text = "";
            return;
        }

        int i = Random.Range(0, lista.Length);
        if (lista.Length > 1 && i == ultimoTexto)
            i = (i + 1 + Random.Range(0, lista.Length - 1)) % lista.Length;   // nunca repite el anterior
        ultimoTexto = i;

        var elegido = lista[i];
        textoEtiqueta.text = elegido.esHistoria ? Cfg.etiquetaHistoria : Cfg.etiquetaConsejo;
        textoConsejo.text = elegido.esHistoria ? "«" + elegido.texto + "»" : elegido.texto;
        textoConsejo.fontStyle = elegido.esHistoria ? FontStyles.Italic : FontStyles.Normal;
    }

    IEnumerator CambiarTextoAnimado()
    {
        yield return FadeTexto(1f, 0f);
        MostrarTextoNuevo();
        yield return FadeTexto(0f, 1f);
    }

    IEnumerator FadeTexto(float desde, float hasta)
    {
        const float d = 0.35f;
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(desde, hasta, t / d);
            textoConsejo.alpha = a;
            textoEtiqueta.alpha = a;
            yield return null;
        }
        textoConsejo.alpha = hasta;
        textoEtiqueta.alpha = hasta;
    }

    // ===================== CONSTRUCCIÓN DE LA PANTALLA =====================

    public bool UIValida()
    {
        return pantallaNegra != null && pantallaNegra.name == NombreUI
            && contenido != null && negro != null && fondo != null && ajusteFondo != null
            && barraRelleno != null && textoEtiqueta != null && textoConsejo != null
            && textoCargando != null && textoPorcentaje != null;
    }

    public void Ocultar()
    {
        if (pantallaNegra == null) return;
        pantallaNegra.alpha = 0f;
        pantallaNegra.blocksRaycasts = false;
        pantallaNegra.interactable = false;
        if (contenido != null) contenido.alpha = 0f;
    }

    /// <summary>Para la herramienta del editor: muestra cómo se ve la pantalla.</summary>
    public void VistaPrevia(bool mostrar)
    {
        if (!UIValida()) return;
        if (!mostrar) { Ocultar(); return; }

        ElegirFondo();
        MostrarTextoNuevo();
        textoConsejo.alpha = 1f;
        textoEtiqueta.alpha = 1f;
        textoCargando.text = Cfg.textoCargando + "...";
        progreso = 0.6f;
        PintarBarra();
        contenido.alpha = 1f;
        pantallaNegra.alpha = 1f;
    }

    /// <summary>
    /// Borra la pantalla vieja (la pantalla negra anterior o una versión previa)
    /// y arma la pantalla de carga nueva como hija de este objeto.
    /// La usa la herramienta del editor y también se llama sola si hace falta.
    /// </summary>
    public void ConstruirUI()
    {
        var c = Cfg;

        // Quita la UI anterior (cualquier hijo con Canvas o CanvasGroup)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var hijo = transform.GetChild(i).gameObject;
            if (hijo.GetComponentInChildren<Canvas>(true) != null || hijo.GetComponentInChildren<CanvasGroup>(true) != null)
                Destruir(hijo);
        }

        // Canvas que tapa todo, encima de cualquier otra UI
        var raiz = NuevoRect(NombreUI, transform);
        var canvas = raiz.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var escalador = raiz.gameObject.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        raiz.gameObject.AddComponent<GraphicRaycaster>();
        pantallaNegra = raiz.gameObject.AddComponent<CanvasGroup>();

        // Color de fondo (se ve si no hay imágenes) y bloquea clics
        negro = NuevaImagen("Negro", raiz, c.colorFondo);
        negro.raycastTarget = true;
        Estirar(negro.rectTransform, 0f, 0f, 1f, 1f);

        // Todo lo que aparece sobre el negro (imagen, consejo, barra) va dentro de "Contenido"
        var cont = NuevoRect("Contenido", raiz);
        Estirar(cont, 0f, 0f, 1f, 1f);
        contenido = cont.gameObject.AddComponent<CanvasGroup>();
        contenido.interactable = false;
        contenido.blocksRaycasts = false;
        contenido.alpha = 0f;

        // Imagen de fondo: cubre toda la pantalla sin deformarse (recorta lo que sobre)
        fondo = NuevaImagen("Fondo", cont, Color.white);
        Estirar(fondo.rectTransform, 0f, 0f, 1f, 1f);
        ajusteFondo = fondo.gameObject.AddComponent<AspectRatioFitter>();
        ajusteFondo.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ajusteFondo.aspectRatio = 16f / 9f;
        fondo.enabled = false;

        // Sombra abajo para que el texto se lea sobre cualquier imagen
        var sombra = NuevaImagen("Sombra", cont, new Color(0f, 0f, 0f, 0.45f));
        Estirar(sombra.rectTransform, 0f, 0f, 1f, 0.38f);

        // Etiqueta: CONSEJO / LA LEYENDA DEL CAPI
        textoEtiqueta = NuevoTexto("Etiqueta", cont, c, 26f, c.colorEtiqueta);
        Anclar(textoEtiqueta.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 290f), new Vector2(1400f, 40f));
        textoEtiqueta.fontStyle = FontStyles.Bold;
        textoEtiqueta.characterSpacing = 15f;

        // Consejo / pedazo de historia
        textoConsejo = NuevoTexto("Consejo", cont, c, 36f, c.colorTexto);
        Anclar(textoConsejo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(1400f, 120f));
        textoConsejo.alignment = TextAlignmentOptions.Top;

        // Barra de carga
        var barra = NuevaImagen("Barra", cont, c.colorBarraFondo);
        Anclar(barra.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1200f, 10f));
        var borde = barra.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(c.colorBarra.r, c.colorBarra.g, c.colorBarra.b, 0.35f);
        borde.effectDistance = new Vector2(2f, 2f);

        var relleno = NuevaImagen("Relleno", barra.rectTransform, c.colorBarra);
        barraRelleno = relleno.rectTransform;
        barraRelleno.pivot = new Vector2(0f, 0.5f);
        barraRelleno.anchorMin = Vector2.zero;
        barraRelleno.anchorMax = new Vector2(0f, 1f);
        barraRelleno.offsetMin = Vector2.zero;
        barraRelleno.offsetMax = Vector2.zero;

        // "Cargando..." a la izquierda y "45%" a la derecha, encima de la barra
        textoCargando = NuevoTexto("Cargando", barra.rectTransform, c, 26f, c.colorTexto);
        Anclar(textoCargando.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 12f), new Vector2(600f, 40f));
        textoCargando.alignment = TextAlignmentOptions.BottomLeft;
        textoCargando.text = c.textoCargando;

        textoPorcentaje = NuevoTexto("Porcentaje", barra.rectTransform, c, 26f, c.colorTexto);
        Anclar(textoPorcentaje.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 12f), new Vector2(200f, 40f));
        textoPorcentaje.alignment = TextAlignmentOptions.BottomRight;
        textoPorcentaje.text = "0%";

        progreso = 0f;
        PintarBarra();
    }

    // ---- Ayudantes ----

    static void Destruir(GameObject go)
    {
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    static RectTransform NuevoRect(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;   // capa UI
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        return rt;
    }

    static Image NuevaImagen(string nombre, Transform padre, Color color)
    {
        var img = NuevoRect(nombre, padre).gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI NuevoTexto(string nombre, Transform padre, ConfigPantallaCarga c, float tamano, Color color)
    {
        var tx = NuevoRect(nombre, padre).gameObject.AddComponent<TextMeshProUGUI>();
        if (c.fuente != null) tx.font = c.fuente;
        tx.fontSize = tamano;
        tx.color = color;
        tx.alignment = TextAlignmentOptions.Center;
        tx.raycastTarget = false;
        tx.text = "";
        return tx;
    }

    static void Estirar(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tamano)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
    }
}