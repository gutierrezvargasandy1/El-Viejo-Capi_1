using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CargarMainMenu : MonoBehaviour
{
    [Header("tiempos de espera (segundos)")]
    public float tiempoCargaWarning = 15f;

    [Header("configuración del fade out de audio")]
    public float tiempoParaIniciarFade = 7f; // segundo exacto en el que empieza a bajar el volumen
    public float duracionFade = 2f;           // tiempo que tarda en bajar el volumen
    [Range(0f, 1f)]
    public float volumenObjetivo = 0.1f;      // volumen final al terminar el fade out

    [Header("nombre de la siguiente escena")]
    public string nombreEscenaMenu = "MainMenu";

    [Header("audio de la escena")]
    public AudioSource audioSource;
    public AudioClip sonidoWarning;

    private void Start()
    {
        if (audioSource != null && sonidoWarning != null)
        {
            audioSource.clip = sonidoWarning;
            audioSource.Play();
        }

        StartCoroutine(SecuenciaWarning());
    }

    private IEnumerator SecuenciaWarning()
    {
        // espera hasta el segundo indicado para iniciar el fade out
        yield return new WaitForSeconds(tiempoParaIniciarFade);

        // ejecuta el fade out progresivo
        yield return StartCoroutine(BajarVolumenProgresivo());

        // calcula el tiempo restante antes de cambiar de escena si el tiempo total es mayor
        float tiempoRestante = tiempoCargaWarning - (tiempoParaIniciarFade + duracionFade);
        if (tiempoRestante > 0f)
        {
            yield return new WaitForSeconds(tiempoRestante);
        }

        SceneManager.LoadScene(nombreEscenaMenu);
    }

    private IEnumerator BajarVolumenProgresivo()
    {
        if (audioSource == null) yield break;

        float volumenInicial = audioSource.volume;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionFade)
        {
            tiempoTranscurrido += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(volumenInicial, volumenObjetivo, tiempoTranscurrido / duracionFade);
            yield return null;
        }

        audioSource.volume = volumenObjetivo;
    }
}