using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz de la noche, se arma sola al dar Play: hora, linterna, leña,
/// mira, pistas de teclas, "SOBREVIVE HASTA LAS 6 AM", flash rojo y pantalla final.
/// </summary>
public class UINoche : MonoBehaviour
{
    [Header("Aspecto")]
    [Tooltip("Opcional: fuente TextMeshPro")]
    public TMP_FontAsset fuente;
    public Color colorTexto = new Color(0.88f, 0.85f, 0.78f);
    public Color colorRojo = new Color(0.8f, 0.12f, 0.09f);
    public Color colorPila = new Color(0.9f, 0.88f, 0.65f);
    public Color colorLena = new Color(0.95f, 0.5f, 0.15f);
    [Tooltip("Game Over: la imagen brinca un instante de vez en cuando (sin estática)")]
    public bool fallasEnGameOver = true;

    CanvasGroup grupoHUD, grupoIntro, grupoFinal, grupoAnuncio;
    TextMeshProUGUI textoAnuncio;
    RawImage estatica;
    Texture2D texturaEstatica;
    float estaticaBase, estaticaGolpe, finEstatica;
    Coroutine rutinaHora;
    TextMeshProUGUI textoHora, introTitulo, introSub, textoPista, finalTitulo, finalSub, textoPila, textoLena;
    RectTransform rellenoPila, rellenoLena;
    Image imgPila, imgLena, mira, flash, negro;
    GameObject panelLena;
    Coroutine rutinaFlash;

    // Game Over
    CanvasGroup grupoGameOver, grupoPrompt;
    RectTransform rtImagenGO;
    RawImage imgGameOver;
    AspectRatioFitter ajusteGO;
    TextMeshProUGUI causaGO, promptGO;
    bool gameOverActivo, elegido;
    float tGameOver, finGlitch, proximoGlitch, semillaGO;

    void Awake()
    {
        Construir();
        Mostrar(false);
    }

    // ===================== API =====================

    public void Mostrar(bool visible) => grupoHUD.alpha = visible ? 1f : 0f;
    public void MostrarLena(bool visible) => panelLena.SetActive(visible);

    public void PonerHora(int h)
    {
        textoHora.text = (h <= 0 ? 12 : h) + " AM";
    }

    /// <summary>Cambio de hora con animación: el reloj salta en rojo y se anuncia al centro.</summary>
    public void AnunciarHora(int h)
    {
        PonerHora(h);
        if (rutinaHora != null) StopCoroutine(rutinaHora);
        rutinaHora = StartCoroutine(AnimarHora((h <= 0 ? 12 : h) + " AM"));
    }

    IEnumerator AnimarHora(string texto)
    {
        textoAnuncio.text = texto;
        float t = 0f;
        while (t < 3f)
        {
            t += Time.unscaledDeltaTime;
            float golpe = Mathf.Clamp01(1f - t / 0.6f);
            textoHora.rectTransform.localScale = Vector3.one * (1f + 0.5f * golpe * golpe);
            textoHora.color = Color.Lerp(colorTexto, colorRojo, golpe);
            grupoAnuncio.alpha = t < 0.5f ? t / 0.5f : Mathf.Clamp01((3f - t) / 1f);
            yield return null;
        }
        textoHora.rectTransform.localScale = Vector3.one;
        textoHora.color = colorTexto;
        grupoAnuncio.alpha = 0f;
        rutinaHora = null;
    }

    /// <summary>Estática de TV encima de todo (alfa 0..1, segundos).</summary>
    public void Estatica(float alfa, float duracion)
    {
        estaticaGolpe = Mathf.Max(estaticaGolpe, alfa);
        finEstatica = Time.unscaledTime + duracion;
    }

    /// <summary>Estática de fondo constante (sube con el peligro).</summary>
    public void EstaticaBase(float alfa) => estaticaBase = Mathf.Clamp01(alfa);

    void Update()
    {
        if (gameOverActivo) AnimarGameOver();
        if (estatica == null) return;
        if (Time.unscaledTime > finEstatica) estaticaGolpe = Mathf.MoveTowards(estaticaGolpe, 0f, Time.unscaledDeltaTime * 3f);
        float a = Mathf.Max(estaticaBase, estaticaGolpe);
        estatica.enabled = a > 0.003f;
        if (!estatica.enabled) return;
        var c = estatica.color;
        c.a = a * (0.8f + 0.2f * Random.value);
        estatica.color = c;
        estatica.uvRect = new Rect(Random.value, Random.value, 2.5f, 1.4f);   // se mueve como TV
    }

    public void PonerPila(float v, bool encendida)
    {
        rellenoPila.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
        imgPila.color = v < 0.2f ? (Mathf.Repeat(Time.time * 3f, 1f) < 0.5f ? colorRojo : colorRojo * 0.4f) : colorPila;
        textoPila.text = "[F] LINTERNA  " + Mathf.CeilToInt(v * 100f) + "%";
        textoPila.color = encendida ? colorPila : colorTexto * 0.75f;
    }

    public void PonerLena(float v, bool encendido)
    {
        rellenoLena.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
        imgLena.color = v < 0.2f ? (Mathf.Repeat(Time.time * 3f, 1f) < 0.5f ? colorRojo : colorRojo * 0.4f) : colorLena;
        textoLena.text = "LEÑA  " + Mathf.CeilToInt(v * 100f) + "%" + (encendido ? "  (ardiendo)" : "");
        textoLena.color = encendido ? colorLena : colorTexto * 0.75f;
    }

    public void PonerMira(bool resaltada)
    {
        float t = resaltada ? 16f : 6f;
        mira.rectTransform.sizeDelta = new Vector2(t, t);
        mira.color = resaltada ? new Color(1f, 0.9f, 0.6f, 0.9f) : new Color(1f, 1f, 1f, 0.45f);
    }

    public void PonerPista(string s)
    {
        if (textoPista.text != s) textoPista.text = s;
    }

    public void MostrarIntro(string titulo, string subtitulo)
    {
        StartCoroutine(Intro(titulo, subtitulo));
    }

    public void FlashRojo()
    {
        if (rutinaFlash != null) StopCoroutine(rutinaFlash);
        rutinaFlash = StartCoroutine(Flash());
    }

    public void Negro(float alfa)
    {
        var c = negro.color;
        c.a = alfa;
        negro.color = c;
    }

    public void MostrarFinal(string titulo, string subtitulo, Color color)
    {
        StartCoroutine(Final(titulo, subtitulo, color));
    }

    // ===================== GAME OVER =====================

    /// <summary>Pantalla de Game Over con imagen: sale de lo negro, zoom lento y parpadeo de foco viejo (sin estática).</summary>
    public void MostrarGameOver(Texture imagen, string causa)
    {
        Mostrar(false);
        Negro(1f);
        imgGameOver.texture = imagen;
        if (imagen != null && imagen.height > 0) ajusteGO.aspectRatio = imagen.width / (float)imagen.height;
        causaGO.text = causa;
        promptGO.text = "";
        grupoPrompt.alpha = 0f;
        grupoGameOver.alpha = 0f;
        tGameOver = 0f;
        elegido = false;
        semillaGO = Random.value * 50f;
        proximoGlitch = 5f;
        gameOverActivo = true;
    }

    /// <summary>Muestra "[Clic] Intentar otra vez..." abajo del GAME OVER.</summary>
    public void PromptGameOver(string texto)
    {
        promptGO.text = texto;
    }

    /// <summary>Cuando el jugador ya eligió: el texto parpadea antes de cambiar de escena.</summary>
    public void ElegidoGameOver()
    {
        elegido = true;
    }

    void AnimarGameOver()
    {
        float dt = Time.unscaledDeltaTime;
        tGameOver += dt;
        float t = tGameOver;

        // aparece desde lo negro (2.5 s)
        grupoGameOver.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 2.5f));

        // zoom muy lento hacia el monstruo + respiración y temblor leves
        float zoom = 1f + 0.08f * (1f - Mathf.Exp(-t / 18f)) + 0.006f * Mathf.Sin(t * 0.9f);
        rtImagenGO.localScale = new Vector3(zoom, zoom, 1f);
        Vector2 deriva = new Vector2(-14f, 4f) * (1f - Mathf.Exp(-t / 18f));
        Vector2 temblor = new Vector2(Mathf.PerlinNoise(semillaGO, t * 6f) - 0.5f, Mathf.PerlinNoise(t * 6f, semillaGO) - 0.5f) * 3f;

        // la imagen brinca un instante de vez en cuando (sin estática)
        if (fallasEnGameOver && t >= proximoGlitch)
        {
            finGlitch = t + Random.Range(0.06f, 0.16f);
            proximoGlitch = t + Random.Range(4f, 9f);
        }
        bool glitch = fallasEnGameOver && t < finGlitch;
        if (glitch) temblor += new Vector2(Random.Range(-18f, 18f), Random.Range(-3f, 3f));
        rtImagenGO.anchoredPosition = deriva + temblor;

        // la luz parpadea como foco viejo
        float luz = 0.82f + 0.18f * Mathf.PerlinNoise(t * 2.3f, semillaGO + 7f);
        if (Mathf.PerlinNoise(t * 9f, semillaGO + 3f) > 0.78f) luz *= 0.7f;
        Color c = glitch ? new Color(luz * 1.15f, luz * 0.7f, luz * 0.7f, 1f) : new Color(luz, luz, luz, 1f);
        imgGameOver.color = c;

        // texto para continuar: aparece y "late" despacio
        if (!string.IsNullOrEmpty(promptGO.text))
        {
            float meta = elegido ? (Mathf.Repeat(t * 12f, 1f) > 0.5f ? 1f : 0.2f) : 0.45f + 0.35f * Mathf.Sin(t * 2f);
            grupoPrompt.alpha = Mathf.MoveTowards(grupoPrompt.alpha, meta, dt * (elegido ? 20f : 1.5f));
        }
    }

    static Texture2D CrearVineta()
    {
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d));
                px[y * N + x] = new Color32(0, 0, 0, (byte)(a * 235f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // ===================== animaciones =====================

    IEnumerator Intro(string titulo, string subtitulo)
    {
        introTitulo.text = titulo;
        introSub.text = subtitulo;
        yield return Fade(grupoIntro, 0f, 1f, 0.8f);
        yield return new WaitForSeconds(3f);
        yield return Fade(grupoIntro, 1f, 0f, 1.2f);
    }

    IEnumerator Flash()
    {
        float t = 0f;
        while (t < 0.9f)
        {
            t += Time.unscaledDeltaTime;
            var c = flash.color;
            c.a = Mathf.Lerp(0.75f, 0f, t / 0.9f);
            flash.color = c;
            yield return null;
        }
        rutinaFlash = null;
    }

    IEnumerator Final(string titulo, string subtitulo, Color color)
    {
        finalTitulo.text = titulo;
        finalTitulo.color = color;
        finalSub.text = subtitulo;

        float a0 = negro.color.a, t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            Negro(Mathf.Lerp(a0, 1f, t / 0.6f));
            yield return null;
        }
        Negro(1f);
        yield return Fade(grupoFinal, 0f, 1f, 1f);
    }

    static IEnumerator Fade(CanvasGroup g, float desde, float hasta, float d)
    {
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(desde, hasta, t / d);
            yield return null;
        }
        g.alpha = hasta;
    }

    // ===================== construcción =====================

    void Construir()
    {
        var raiz = Rect("UI_Noche", transform);
        var canvas = raiz.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;   // debajo de la pantalla de carga (1000)
        var esc = raiz.gameObject.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;

        // ---- HUD ----
        var hud = Rect("HUD", raiz);
        Estirar(hud);
        grupoHUD = hud.gameObject.AddComponent<CanvasGroup>();
        grupoHUD.blocksRaycasts = false;

        textoHora = Texto("Hora", hud, 58f, colorTexto, TextAlignmentOptions.TopRight);
        Anclar(textoHora.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -36f), new Vector2(400f, 80f));
        textoHora.fontStyle = FontStyles.Bold;

        // linterna
        textoPila = Texto("TextoPila", hud, 24f, colorTexto, TextAlignmentOptions.BottomLeft);
        Anclar(textoPila.rectTransform, Vector2.zero, Vector2.zero, new Vector2(56f, 124f), new Vector2(500f, 32f));
        rellenoPila = Barra("BarraPila", hud, new Vector2(56f, 108f), colorPila, out imgPila);

        // leña
        panelLena = Rect("Lena", hud).gameObject;
        Estirar((RectTransform)panelLena.transform);
        textoLena = Texto("TextoLena", panelLena.transform, 24f, colorTexto, TextAlignmentOptions.BottomLeft);
        Anclar(textoLena.rectTransform, Vector2.zero, Vector2.zero, new Vector2(56f, 66f), new Vector2(500f, 32f));
        rellenoLena = Barra("BarraLena", panelLena.transform, new Vector2(56f, 50f), colorLena, out imgLena);

        // pista de teclas
        textoPista = Texto("Pista", hud, 26f, colorTexto, TextAlignmentOptions.Bottom);
        Anclar(textoPista.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(1500f, 44f));

        // mira
        mira = Imagen("Mira", hud, Color.white);
        Anclar(mira.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));
        PonerMira(false);

        // ---- Intro ----
        var intro = Rect("Intro", raiz);
        Estirar(intro);
        grupoIntro = intro.gameObject.AddComponent<CanvasGroup>();
        grupoIntro.alpha = 0f;
        grupoIntro.blocksRaycasts = false;
        introTitulo = Texto("Titulo", intro, 64f, colorTexto, TextAlignmentOptions.Center);
        Anclar(introTitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1600f, 100f));
        introTitulo.fontStyle = FontStyles.Bold;
        introTitulo.characterSpacing = 10f;
        introSub = Texto("Sub", intro, 42f, colorRojo, TextAlignmentOptions.Center);
        Anclar(introSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(800f, 60f));

        // ---- Anuncio de hora ----
        var anuncio = Rect("Anuncio", raiz);
        Estirar(anuncio);
        grupoAnuncio = anuncio.gameObject.AddComponent<CanvasGroup>();
        grupoAnuncio.alpha = 0f;
        grupoAnuncio.blocksRaycasts = false;
        textoAnuncio = Texto("Hora", anuncio, 72f, colorTexto, TextAlignmentOptions.Center);
        Anclar(textoAnuncio.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(800f, 100f));
        textoAnuncio.fontStyle = FontStyles.Bold;
        textoAnuncio.characterSpacing = 12f;

        // ---- Estática de TV ----
        texturaEstatica = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        texturaEstatica.filterMode = FilterMode.Point;
        texturaEstatica.wrapMode = TextureWrapMode.Repeat;
        var px = new Color32[256 * 256];
        for (int i = 0; i < px.Length; i++)
        {
            byte g = (byte)Random.Range(0, 256);
            px[i] = new Color32(g, g, g, 255);
        }
        texturaEstatica.SetPixels32(px);
        texturaEstatica.Apply();
        estatica = Rect("Estatica", raiz).gameObject.AddComponent<RawImage>();
        estatica.texture = texturaEstatica;
        estatica.color = new Color(1f, 1f, 1f, 0f);
        estatica.raycastTarget = false;
        Estirar(estatica.rectTransform);
        estatica.enabled = false;

        // ---- Flash, negro y final ----
        flash = Imagen("FlashRojo", raiz, new Color(0.7f, 0f, 0f, 0f));
        Estirar(flash.rectTransform);
        negro = Imagen("Negro", raiz, new Color(0f, 0f, 0f, 0f));
        Estirar(negro.rectTransform);

        var fin = Rect("Final", raiz);
        Estirar(fin);
        grupoFinal = fin.gameObject.AddComponent<CanvasGroup>();
        grupoFinal.alpha = 0f;
        grupoFinal.blocksRaycasts = false;
        finalTitulo = Texto("Titulo", fin, 120f, colorTexto, TextAlignmentOptions.Center);
        Anclar(finalTitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1600f, 160f));
        finalTitulo.fontStyle = FontStyles.Bold;
        finalTitulo.characterSpacing = 8f;
        finalSub = Texto("Sub", fin, 36f, colorTexto, TextAlignmentOptions.Center);
        Anclar(finalSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1200f, 60f));

        // ---- GAME OVER con imagen (encima de todo) ----
        var go = Rect("GameOver", raiz);
        Estirar(go);
        go.gameObject.AddComponent<RectMask2D>();
        grupoGameOver = go.gameObject.AddComponent<CanvasGroup>();
        grupoGameOver.alpha = 0f;
        grupoGameOver.blocksRaycasts = false;
        var fondoGO = Imagen("Fondo", go, Color.black);
        Estirar(fondoGO.rectTransform);

        // contenedor que se mueve (zoom, temblor) y adentro la imagen que llena la pantalla sin deformarse
        rtImagenGO = Rect("Contenedor", go);
        Estirar(rtImagenGO);
        rtImagenGO.pivot = new Vector2(0.62f, 0.5f);           // el zoom se acerca al monstruo
        imgGameOver = Rect("Imagen", rtImagenGO).gameObject.AddComponent<RawImage>();
        imgGameOver.raycastTarget = false;
        var rtImg = imgGameOver.rectTransform;
        Estirar(rtImg);
        ajusteGO = imgGameOver.gameObject.AddComponent<AspectRatioFitter>();
        ajusteGO.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ajusteGO.aspectRatio = 16f / 9f;

        // causa ("Entró por la ventana"), debajo del GAME OVER de la imagen
        causaGO = Texto("Causa", rtImg, 34f, new Color(0.62f, 0.5f, 0.46f), TextAlignmentOptions.Center);
        causaGO.rectTransform.anchorMin = new Vector2(0.06f, 0.38f);
        causaGO.rectTransform.anchorMax = new Vector2(0.44f, 0.45f);
        causaGO.rectTransform.offsetMin = causaGO.rectTransform.offsetMax = Vector2.zero;
        causaGO.characterSpacing = 4f;

        var promptRt = Rect("Continuar", rtImg);
        promptRt.anchorMin = new Vector2(0.04f, 0.12f);
        promptRt.anchorMax = new Vector2(0.46f, 0.19f);
        promptRt.offsetMin = promptRt.offsetMax = Vector2.zero;
        grupoPrompt = promptRt.gameObject.AddComponent<CanvasGroup>();
        grupoPrompt.alpha = 0f;
        promptGO = Texto("Texto", promptRt, 24f, new Color(0.75f, 0.68f, 0.62f), TextAlignmentOptions.Center);
        Estirar(promptGO.rectTransform);
        promptGO.characterSpacing = 2f;

        var vin = Rect("Vineta", go).gameObject.AddComponent<RawImage>();
        vin.texture = CrearVineta();
        vin.raycastTarget = false;
        Estirar(vin.rectTransform);
    }

    RectTransform Barra(string nombre, Transform padre, Vector2 pos, Color color, out Image relleno)
    {
        var fondo = Imagen(nombre, padre, new Color(0.08f, 0.07f, 0.07f, 0.85f));
        Anclar(fondo.rectTransform, Vector2.zero, Vector2.zero, pos, new Vector2(280f, 10f));
        relleno = Imagen("Relleno", fondo.transform, color);
        var rt = relleno.rectTransform;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static RectTransform Rect(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        return rt;
    }

    static Image Imagen(string nombre, Transform padre, Color color)
    {
        var img = Rect(nombre, padre).gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI Texto(string nombre, Transform padre, float tam, Color color, TextAlignmentOptions alin)
    {
        var tx = Rect(nombre, padre).gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) tx.font = fuente;
        tx.fontSize = tam;
        tx.color = color;
        tx.alignment = alin;
        tx.raycastTarget = false;
        tx.text = "";
        return tx;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }
}
