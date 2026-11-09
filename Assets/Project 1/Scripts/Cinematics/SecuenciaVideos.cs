using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

// Reproduce varios videos uno tras otro con pantalla negra
// al inicio, entre videos y al final. Ponlo en el objeto que tiene el VideoPlayer.
[RequireComponent(typeof(VideoPlayer))]
public class SecuenciaVideos : MonoBehaviour
{
    [Header("Videos en orden")]
    public VideoClip[] videos;

    [Header("Tiempos en negro (segundos)")]
    public float negroAlInicio = 1f;
    public float negroEntreVideos = 1f;
    public float negroAlFinal = 1f;

    [Header("Fade")]
    public float duracionFade = 0.6f;

    [Header("Saltar video con Espacio / Enter")]
    public bool permitirSaltar = true;

    [Header("Al terminar todo")]
    [Tooltip("Nombre EXACTO de la escena a cargar al final. Déjalo vacío para no cambiar.")]
    public string escenaAlTerminar = "";
    public UnityEvent alTerminar;

    private VideoPlayer vp;
    private Image negro;
    private bool saltar;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.waitForFirstFrame = true;

        // La cámara en negro para que no se vea el cielo antes/entre videos
        Camera cam = vp.targetCamera != null ? vp.targetCamera : Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        CrearPantallaNegra();
    }

    void Start()
    {
        StartCoroutine(Reproducir());
    }

    void Update()
    {
        if (!permitirSaltar || Keyboard.current == null) return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
            saltar = true;
    }

    IEnumerator Reproducir()
    {
        SetAlpha(1f);
        yield return new WaitForSecondsRealtime(negroAlInicio);

        for (int i = 0; i < videos.Length; i++)
        {
            if (videos[i] == null) continue;

            // Preparar el video (cargarlo) mientras sigue en negro
            vp.clip = videos[i];
            vp.Prepare();
            while (!vp.isPrepared) yield return null;

            saltar = false;
            vp.Play();

            // Aparece el video
            yield return Fade(1f, 0f);

            // Esperar hasta que falte poco para acabar (o hasta que el jugador lo salte)
            double finFade = videos[i].length - duracionFade;
            while (vp.isPlaying && vp.time < finFade && !saltar)
                yield return null;

            // Se va a negro
            yield return Fade(0f, 1f);
            vp.Stop();

            bool esUltimo = (i == videos.Length - 1);
            if (!esUltimo)
                yield return new WaitForSecondsRealtime(negroEntreVideos);
        }

        yield return new WaitForSecondsRealtime(negroAlFinal);

        alTerminar.Invoke();

        if (!string.IsNullOrEmpty(escenaAlTerminar))
        {
            if (Application.CanStreamedLevelBeLoaded(escenaAlTerminar))
                SceneManager.LoadScene(escenaAlTerminar);
            else
                Debug.LogError("[SecuenciaVideos] La escena '" + escenaAlTerminar +
                               "' no existe o no está en Build Profiles > Scene List.", this);
        }
    }

    IEnumerator Fade(float desde, float hasta)
    {
        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(desde, hasta, t / duracionFade));
            yield return null;
        }
        SetAlpha(hasta);
    }

    void SetAlpha(float a)
    {
        negro.color = new Color(0f, 0f, 0f, a);
    }

    // Crea solo un Canvas con una imagen negra encima de todo
    void CrearPantallaNegra()
    {
        GameObject canvasGO = new GameObject("PantallaNegraVideos");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imgGO = new GameObject("Negro");
        imgGO.transform.SetParent(canvasGO.transform, false);
        negro = imgGO.AddComponent<Image>();
        negro.raycastTarget = false;

        RectTransform rt = negro.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SetAlpha(1f);
    }
}