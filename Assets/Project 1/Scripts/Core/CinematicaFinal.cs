using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// CAPÍTULO 4 — EL FINAL (mal final).
/// Reproduce la cinemática final (video) con su sonido sincronizado y efectos encima:
/// la imagen tiembla con cada golpe del monstruo en la puerta, late con el corazón del Capi,
/// destellos rojos, y el remolino gira al final. Después: pantalla de FIN y regreso al menú.
/// Lo arma la herramienta: Herramientas > Capítulos y final > "Armar escena final".
/// </summary>
public class CinematicaFinal : MonoBehaviour
{
    [Header("Video y sonido")]
    public VideoClip video;
    [Tooltip("Final_Pista: el sonido hecho a la medida del video")]
    public AudioClip pista;
    [Range(0f, 1f)] public float volumenPista = 1f;
    [Tooltip("Final_Musica: música de la pantalla de FIN")]
    public AudioClip musicaFin;
    [Range(0f, 1f)] public float volumenMusicaFin = 0.8f;
    [Tooltip("Segundo del video en el que todo se va a negro y sale el FIN")]
    public float momentoFin = 33.6f;

    [Header("Pantalla de FIN")]
    public string textoFin = "FIN";
    public string textoMalFinal = "MAL FINAL";
    public string frase = "La noche terminó. La locura, no.";
    public string gracias = "Gracias por jugar El Viejo Capi";
    public string textoContinuar = "[Clic / Espacio]  Volver al menú";
    public string escenaMenu = "MainMenu";
    [Tooltip("Segundos antes de poder volver al menú")]
    public float esperaParaContinuar = 5f;
    [Tooltip("Opcional: fuente TextMeshPro")]
    public TMP_FontAsset fuente;

    [Header("Efectos encima del video")]
    public bool efectos = true;
    [Range(0f, 2f)] public float fuerzaTemblor = 1f;

    // ---- momentos del sonido (los saqué de la misma pista Final_Pista) ----
    static readonly float[] GolpesT = { 0.3f, 0.95f, 1.5f, 1.95f, 2.35f, 2.7f, 3f, 3.3f, 13.6f, 17.5f, 18.2f, 22.85f, 27f, 29.95f };
    static readonly float[] GolpesF = { 0.75f, 0.85f, 0.95f, 0.9f, 1f, 1f, 1.1f, 1.4f, 0.8f, 0.6f, 0.36f, 0.27f, 0.3f, 0.9f };
    static readonly float[] LatidosT = { 0.1f, 0.6f, 1.091f, 1.573f, 2.047f, 2.513f, 2.972f, 3.423f, 4.6f, 5.252f, 13.9f, 14.669f, 15.349f, 15.964f, 16.532f, 17.061f, 18.4f, 18.945f, 19.486f, 20.021f, 20.551f, 21.077f, 21.598f, 22.114f, 22.627f, 23.134f, 23.638f, 24.138f, 24.633f, 25.125f, 25.613f, 26.097f, 26.577f, 27.054f, 27.528f, 27.998f, 28.465f, 28.929f, 29.389f, 30.1f, 30.544f, 30.973f, 31.389f, 31.791f, 32.182f, 32.562f, 32.933f, 33.294f };
    static readonly float[] LatidosF = { 0.35f, 0.36f, 0.38f, 0.39f, 0.41f, 0.42f, 0.44f, 0.45f, 0.22f, 0.17f, 0.4f, 0.44f, 0.48f, 0.52f, 0.55f, 0.58f, 0.55f, 0.56f, 0.56f, 0.57f, 0.58f, 0.59f, 0.59f, 0.6f, 0.61f, 0.61f, 0.62f, 0.63f, 0.63f, 0.64f, 0.64f, 0.65f, 0.66f, 0.66f, 0.67f, 0.68f, 0.68f, 0.69f, 0.69f, 0.6f, 0.63f, 0.65f, 0.68f, 0.71f, 0.73f, 0.75f, 0.78f, 0.8f };
    static readonly float[] DestellosRojos = { 13.6f, 29.95f };

    // ---- curvas por tiempo del video ----
    static readonly float[] ZoomT = { 0f, 3.5f, 4.2f, 5.4f, 11.2f, 13.6f, 17.4f, 17.6f, 18.2f, 29.8f, 30.0f, 33.4f };
    static readonly float[] ZoomV = { 1f, 1.04f, 1.04f, 1f, 1.025f, 1.06f, 1.09f, 1f, 1f, 1.04f, 1.03f, 1.15f };
    static readonly float[] GiroT = { 0f, 29.9f, 33.4f };
    static readonly float[] GiroV = { 0f, 0f, 3.5f };
    static readonly float[] TemblorT = { 0f, 3.5f, 4.0f, 11.2f, 13.6f, 17.4f, 18.2f, 29.8f, 33.4f };
    static readonly float[] TemblorV = { 1.5f, 2.5f, 0f, 0f, 1.5f, 3f, 2f, 3f, 6f };
    static readonly float[] VinetaT = { 0f, 3.5f, 5.4f, 8.5f, 11.2f, 13.6f, 17.4f, 18.2f, 29.8f, 33.4f };
    static readonly float[] VinetaV = { 0.55f, 0.6f, 0.3f, 0.12f, 0.12f, 0.55f, 0.65f, 0.6f, 0.65f, 0.85f };

    VideoPlayer vp;
    AudioSource srcPista, srcMusica;
    RenderTexture rt;
    RectTransform contenedor;
    RawImage imgVideo, vineta;
    Image oscuro, rojo;
    CanvasGroup grupoFin, grupoContinuar;
    TextMeshProUGUI txtFin, txtMalFinal, txtFrase, txtGracias, txtContinuar;
    float trauma, pulso, flashOscuro, flashRojo, ultimoT = -1f, semilla;
    bool termino;

    IEnumerator Start()
    {
        Cursor.visible = false;
        semilla = Random.value * 50f;
        Construir();

        if (video != null)
        {
            vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.waitForFirstFrame = true;
            vp.skipOnDrop = true;
            vp.source = VideoSource.VideoClip;
            vp.clip = video;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            int w = video.width > 0 ? (int)video.width : 1920, h = video.height > 0 ? (int)video.height : 1080;
            rt = new RenderTexture(w, h, 0);
            rt.Create();
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt;
            imgVideo.texture = rt;
            imgVideo.GetComponent<AspectRatioFitter>().aspectRatio = w / (float)h;
            vp.loopPointReached += _ => termino = true;
            vp.Prepare();
        }
        if (pista != null && pista.loadState != AudioDataLoadState.Loaded) pista.LoadAudioData();

        // espera la pantalla de carga y el título "CAPÍTULO 4"
        while (TituloCapitulo.Activo || (GestorTransicion.instancia != null && GestorTransicion.instancia.EnTransicion))
            yield return null;

        float limite = Time.realtimeSinceStartup + 10f;
        while (vp != null && !vp.isPrepared && Time.realtimeSinceStartup < limite) yield return null;
        while (pista != null && pista.loadState == AudioDataLoadState.Loading) yield return null;

        if (vp != null && vp.isPrepared)
        {
            vp.Play();
            float espera = Time.realtimeSinceStartup + 3f;
            while (vp.frame < 1 && Time.realtimeSinceStartup < espera) yield return null;
            if (pista != null)
            {
                srcPista.clip = pista;
                srcPista.volume = volumenPista;
                srcPista.time = Mathf.Clamp((float)vp.time, 0f, pista.length - 0.05f);
                srcPista.Play();
            }
            bool enPausa = false;
            while (!termino && vp.time < momentoFin)
            {
                // menú de pausa (Esc): se detienen el video y el sonido
                if (Time.timeScale <= 0f && !TituloCapitulo.Activo)
                {
                    if (!enPausa) { enPausa = true; vp.Pause(); srcPista.Pause(); }
                    yield return null;
                    continue;
                }
                if (enPausa) { enPausa = false; vp.Play(); srcPista.UnPause(); }
                Sincronizar();
                imgVideo.color = new Color(1f, 1f, 1f, Mathf.Clamp01((float)vp.time / 0.6f));   // entra suave desde negro
                if (efectos) Efectos((float)vp.time, Time.unscaledDeltaTime);
                yield return null;
            }
            vp.Pause();
        }
        else if (pista != null)
        {
            // sin video (no se encontró): al menos el sonido, sobre negro
            srcPista.clip = pista;
            srcPista.Play();
            while (srcPista.isPlaying && srcPista.time < momentoFin) yield return null;
        }

        yield return PantallaFin();
    }

    // el sonido sigue al video (si el video se atora en el celular, el sonido lo espera)
    void Sincronizar()
    {
        if (srcPista == null || !srcPista.isPlaying || vp == null) return;
        float vt = (float)vp.time;
        if (Mathf.Abs(srcPista.time - vt) > 0.12f && vt < pista.length - 0.1f) srcPista.time = vt;
    }

    void Efectos(float t, float dt)
    {
        // golpes y latidos que acaban de pasar
        if (ultimoT >= 0f && t > ultimoT)
        {
            for (int i = 0; i < GolpesT.Length; i++)
                if (GolpesT[i] > ultimoT && GolpesT[i] <= t)
                {
                    trauma = Mathf.Max(trauma, GolpesF[i]);
                    flashOscuro = Mathf.Max(flashOscuro, 0.3f * GolpesF[i]);
                }
            for (int i = 0; i < LatidosT.Length; i++)
                if (LatidosT[i] > ultimoT && LatidosT[i] <= t) pulso = Mathf.Max(pulso, LatidosF[i]);
            foreach (var d in DestellosRojos)
                if (d > ultimoT && d <= t) flashRojo = 1f;
        }
        ultimoT = t;

        trauma = Mathf.MoveTowards(trauma, 0f, dt * 3.2f);
        pulso = Mathf.MoveTowards(pulso, 0f, dt * 5f);
        flashOscuro = Mathf.MoveTowards(flashOscuro, 0f, dt * 3f);
        flashRojo = Mathf.MoveTowards(flashRojo, 0f, dt * 1.4f);

        float zoom = Curva(t, ZoomT, ZoomV) * (1f + 0.012f * pulso);
        float giro = Curva(t, GiroT, GiroV);
        float temblor = Curva(t, TemblorT, TemblorV) * fuerzaTemblor;

        float tt = Time.unscaledTime * 18f;
        Vector2 jit = new Vector2(Mathf.PerlinNoise(semilla, tt) - 0.5f, Mathf.PerlinNoise(tt, semilla) - 0.5f) * 2f * temblor;
        float tr = trauma * trauma;
        Vector2 sacudida = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 26f * tr * fuerzaTemblor;
        float giroSacudida = Random.Range(-1f, 1f) * 1.2f * tr * fuerzaTemblor;

        contenedor.anchoredPosition = jit + sacudida;
        contenedor.localScale = new Vector3(zoom, zoom, 1f);
        contenedor.localRotation = Quaternion.Euler(0f, 0f, giro + giroSacudida);

        oscuro.color = new Color(0f, 0f, 0f, flashOscuro);
        rojo.color = new Color(0.75f, 0.02f, 0.02f, 0.4f * flashRojo * flashRojo);
        vineta.color = new Color(1f, 1f, 1f, Mathf.Clamp01(Curva(t, VinetaT, VinetaV) + 0.12f * pulso));
    }

    static float Curva(float t, float[] ts, float[] vs)
    {
        if (t <= ts[0]) return vs[0];
        for (int i = 1; i < ts.Length; i++)
            if (t <= ts[i])
            {
                float k = (t - ts[i - 1]) / Mathf.Max(0.0001f, ts[i] - ts[i - 1]);
                return Mathf.Lerp(vs[i - 1], vs[i], k);
            }
        return vs[vs.Length - 1];
    }

    // ───────────────────────── FIN ─────────────────────────

    IEnumerator PantallaFin()
    {
        imgVideo.color = new Color(1f, 1f, 1f, 0f);
        oscuro.color = Color.clear;
        rojo.color = Color.clear;
        vineta.color = new Color(1f, 1f, 1f, 0.8f);
        contenedor.localRotation = Quaternion.identity;

        if (musicaFin != null)
        {
            srcMusica.clip = musicaFin;
            srcMusica.loop = true;
            srcMusica.volume = 0f;
            srcMusica.Play();
        }

        grupoFin.alpha = 1f;
        float t = 0f;
        bool puede = false;
        while (true)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            if (srcMusica.isPlaying) srcMusica.volume = Mathf.MoveTowards(srcMusica.volume, volumenMusicaFin, dt / 4f);
            if (srcPista.isPlaying && t > 1.5f) srcPista.volume = Mathf.MoveTowards(srcPista.volume, 0f, dt / 3f);

            float a1 = Suave(Mathf.Clamp01((t - 0.8f) / 2.2f));
            txtFin.alpha = a1 * (0.92f + 0.08f * Mathf.PerlinNoise(t * 2f, 1.7f));
            txtFin.characterSpacing = Mathf.Lerp(70f, 24f, Suave(Mathf.Clamp01((t - 0.8f) / 4f)));
            float esc = Mathf.Lerp(1.08f, 1f, Suave(Mathf.Clamp01((t - 0.8f) / 4f)));
            txtFin.rectTransform.localScale = new Vector3(esc, esc, 1f);
            txtMalFinal.alpha = Suave(Mathf.Clamp01((t - 2.4f) / 1.2f));
            txtFrase.alpha = Suave(Mathf.Clamp01((t - 3.4f) / 1.6f));
            txtGracias.alpha = Suave(Mathf.Clamp01((t - 5.0f) / 1.6f)) * 0.8f;

            if (!puede && t >= esperaParaContinuar)
            {
                puede = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            if (puede)
            {
                grupoContinuar.alpha = Mathf.MoveTowards(grupoContinuar.alpha, 0.45f + 0.35f * Mathf.Sin(t * 2f), dt * 1.5f);
                if (Presiono()) break;
            }
            yield return null;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (GestorTransicion.instancia != null) GestorTransicion.instancia.CambiarEscena(escenaMenu);
        else SceneManager.LoadScene(escenaMenu);
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

    void OnDestroy()
    {
        if (rt != null) rt.Release();
    }

    // ───────────────────────── interfaz ─────────────────────────

    void Construir()
    {
        srcPista = gameObject.AddComponent<AudioSource>();
        srcPista.playOnAwake = false;
        srcPista.spatialBlend = 0f;
        srcMusica = gameObject.AddComponent<AudioSource>();
        srcMusica.playOnAwake = false;
        srcMusica.spatialBlend = 0f;

        var go = new GameObject("CinematicaFinal_UI", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        Transform raiz = go.transform;

        var fondo = Rect("Fondo", raiz).gameObject.AddComponent<Image>();
        fondo.color = Color.black;
        fondo.raycastTarget = false;
        Estirar(fondo.rectTransform);

        contenedor = Rect("Contenedor", raiz);
        Estirar(contenedor);
        imgVideo = Rect("Video", contenedor).gameObject.AddComponent<RawImage>();
        imgVideo.raycastTarget = false;
        imgVideo.color = new Color(1f, 1f, 1f, 0f);
        Estirar(imgVideo.rectTransform);
        var ajuste = imgVideo.gameObject.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ajuste.aspectRatio = 16f / 9f;

        oscuro = Rect("Oscuro", raiz).gameObject.AddComponent<Image>();
        oscuro.color = Color.clear;
        oscuro.raycastTarget = false;
        Estirar(oscuro.rectTransform);
        rojo = Rect("Rojo", raiz).gameObject.AddComponent<Image>();
        rojo.color = Color.clear;
        rojo.raycastTarget = false;
        Estirar(rojo.rectTransform);
        vineta = Rect("Vineta", raiz).gameObject.AddComponent<RawImage>();
        vineta.texture = CrearVineta();
        vineta.color = new Color(1f, 1f, 1f, 0f);
        vineta.raycastTarget = false;
        Estirar(vineta.rectTransform);

        // pantalla de FIN
        var fin = Rect("Fin", raiz);
        Estirar(fin);
        grupoFin = fin.gameObject.AddComponent<CanvasGroup>();
        grupoFin.alpha = 0f;
        txtFin = Texto("FIN", fin, 170f, new Color(0.92f, 0.88f, 0.82f), textoFin, new Vector2(0f, 120f), new Vector2(1400f, 220f));
        txtMalFinal = Texto("MalFinal", fin, 36f, new Color(0.78f, 0.1f, 0.07f), textoMalFinal, new Vector2(0f, -10f), new Vector2(1200f, 60f));
        txtMalFinal.characterSpacing = 22f;
        txtFrase = Texto("Frase", fin, 38f, new Color(0.7f, 0.65f, 0.6f), frase, new Vector2(0f, -85f), new Vector2(1600f, 60f));
        txtFrase.fontStyle = FontStyles.Italic;
        txtGracias = Texto("Gracias", fin, 28f, new Color(0.55f, 0.52f, 0.5f), gracias, new Vector2(0f, -200f), new Vector2(1400f, 50f));
        var cont = Rect("Continuar", fin);
        cont.anchorMin = cont.anchorMax = new Vector2(0.5f, 0f);
        cont.pivot = new Vector2(0.5f, 0f);
        cont.anchoredPosition = new Vector2(0f, 70f);
        cont.sizeDelta = new Vector2(1200f, 50f);
        grupoContinuar = cont.gameObject.AddComponent<CanvasGroup>();
        grupoContinuar.alpha = 0f;
        txtContinuar = Texto("Texto", cont, 26f, new Color(0.7f, 0.64f, 0.58f), textoContinuar, Vector2.zero, new Vector2(1200f, 50f));
        foreach (var tx in new[] { txtFin, txtMalFinal, txtFrase, txtGracias }) tx.alpha = 0f;
    }

    TextMeshProUGUI Texto(string nombre, Transform padre, float tam, Color color, string texto, Vector2 pos, Vector2 tamRect)
    {
        var tx = Rect(nombre, padre).gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) tx.font = fuente;
        tx.fontSize = tam;
        tx.color = color;
        tx.alignment = TextAlignmentOptions.Center;
        tx.raycastTarget = false;
        tx.text = texto;
        var r = tx.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tamRect;
        return tx;
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
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1.3f, d));
                px[y * N + x] = new Color32(0, 0, 0, (byte)(a * 245f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    static RectTransform Rect(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        var r = (RectTransform)go.transform;
        r.SetParent(padre, false);
        return r;
    }

    static void Estirar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
