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

    CanvasGroup grupoHUD, grupoIntro, grupoFinal;
    TextMeshProUGUI textoHora, introTitulo, introSub, textoPista, finalTitulo, finalSub, textoPila, textoLena;
    RectTransform rellenoPila, rellenoLena;
    Image imgPila, imgLena, mira, flash, negro;
    GameObject panelLena;
    Coroutine rutinaFlash;

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

    public void PonerPila(float v, bool encendida)
    {
        rellenoPila.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
        imgPila.color = v < 0.2f ? colorRojo : colorPila;
        textoPila.text = "[F] LINTERNA  " + Mathf.CeilToInt(v * 100f) + "%";
        textoPila.color = encendida ? colorPila : colorTexto * 0.75f;
    }

    public void PonerLena(float v, bool encendido)
    {
        rellenoLena.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
        imgLena.color = v < 0.2f ? colorRojo : colorLena;
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
