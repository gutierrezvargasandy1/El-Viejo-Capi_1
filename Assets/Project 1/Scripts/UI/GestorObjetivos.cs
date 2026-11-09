using UnityEngine;
using System.Collections;
using TMPro;

public class GestorObjetivos : MonoBehaviour
{
    public static GestorObjetivos instancia;

    [Header("Referencia UI")]
    public TMP_Text textoObjetivo;
    public CanvasGroup grupoObjetivo;

    [Header("Tiempos")]
    public float duracionFade = 0.5f;    // cuánto tarda en aparecer/desaparecer
    public float tiempoVisible = 3f;     // cuánto se queda visible antes de irse

    [Header("Sonido")]
    [Tooltip("El sonido que se reproduce cada vez que aparece un objetivo")]
    public AudioClip sonidoObjetivo;
    [Range(0f, 1f)]
    public float volumen = 1f;

    private AudioSource fuenteAudio;
    private Coroutine rutinaActual;

    void Awake()
    {
        instancia = this;

        // Busca un AudioSource en este objeto; si no hay, lo crea
        fuenteAudio = GetComponent<AudioSource>();
        if (fuenteAudio == null)
            fuenteAudio = gameObject.AddComponent<AudioSource>();

        fuenteAudio.playOnAwake = false;
        fuenteAudio.loop = false;
        fuenteAudio.spatialBlend = 0f;   // 2D: se oye igual sin importar dónde esté el jugador
    }

    void Start()
    {
        if (grupoObjetivo != null)
            grupoObjetivo.alpha = 0f;
    }

    // Esto es lo que llama cada zona
    public void MostrarObjetivo(string nuevoObjetivo)
    {
        if (textoObjetivo != null)
            textoObjetivo.text =  nuevoObjetivo;

        // Sonido de nuevo objetivo
        if (sonidoObjetivo != null)
            fuenteAudio.PlayOneShot(sonidoObjetivo, volumen);
        else
            Debug.LogWarning("GestorObjetivos: no tiene asignado el sonidoObjetivo.");

        if (rutinaActual != null)
            StopCoroutine(rutinaActual);

        rutinaActual = StartCoroutine(FadeInEsperaFadeOut());
    }

    private IEnumerator FadeInEsperaFadeOut()
    {
        // FADE IN (desde el alpha actual, por si ya se estaba mostrando otro objetivo)
        yield return Fade(grupoObjetivo.alpha, 1f);

        // ESPERA visible (tiempo real, igual que el fade, así no se congela si pausas el juego)
        yield return new WaitForSecondsRealtime(tiempoVisible);

        // FADE OUT
        yield return Fade(1f, 0f);

        rutinaActual = null;
    }

    private IEnumerator Fade(float desde, float hasta)
    {
        if (grupoObjetivo == null) yield break;

        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            grupoObjetivo.alpha = Mathf.Lerp(desde, hasta, t / duracionFade);
            yield return null;
        }
        grupoObjetivo.alpha = hasta;
    }
}
