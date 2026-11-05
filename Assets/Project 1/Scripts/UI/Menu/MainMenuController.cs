using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Nombre de la Escena del Juego")]
    public string nombreEscenaJuego = "GameScene";

    [Header("Audio de la Escena")]
    public AudioSource audioAmbiente;
    public AudioSource audioEfectos;
    public AudioClip musicaMenu;
    public AudioClip sonidoBoton;

    [Header("Configuración de Desvanecimiento Visual y Audio")]
    public CanvasGroup panelPanoramico; // Asigna un Canvas Group de una imagen negra que cubra toda la pantalla
    public float duracionFade = 1.5f;
    [Range(0f, 1f)]
    public float volumenMaximo = 1f; // Volumen al que llegará la música tras el fade in

    private void Start()
    {
        // Inicia la música con volumen en cero si está asignada
        if (audioAmbiente != null && musicaMenu != null)
        {
            audioAmbiente.clip = musicaMenu;
            audioAmbiente.loop = true;
            audioAmbiente.volume = 0f; // Empieza en silencio
            audioAmbiente.Play();
        }

        // Si hay un panel asignado, inicia la transición de entrada (Fade In visual y de audio)
        if (panelPanoramico != null)
        {
            StartCoroutine(FadeIn());
        }
        else if (audioAmbiente != null)
        {
            // Si no hay panel visual, sube la música de todos modos
            StartCoroutine(SubirMusica());
        }
    }

    public void ReproducirSonidoBoton()
    {
        if (audioEfectos != null && sonidoBoton != null)
        {
            audioEfectos.PlayOneShot(sonidoBoton);
        }
    }

    public void CargarJuego()
    {
        ReproducirSonidoBoton();
        StartCoroutine(CambiarConDesvanecimiento(nombreEscenaJuego));
    }

    public void CargarNivel(string nombreDeLaEscena = "HabitacionScene")
    {
        ReproducirSonidoBoton();
        StartCoroutine(CambiarConDesvanecimiento(nombreDeLaEscena));
    }

    private IEnumerator FadeIn()
    {
        float tiempo = 0f;
        panelPanoramico.alpha = 1f;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            float porcentaje = tiempo / duracionFade;

            // Desvanecimiento visual de la pantalla (de oscuro a transparente)
            panelPanoramico.alpha = Mathf.Lerp(1f, 0f, porcentaje);

            // Incremento progresivo del volumen de la música (de 0 a volumenMaximo)
            if (audioAmbiente != null)
            {
                audioAmbiente.volume = Mathf.Lerp(0f, volumenMaximo, porcentaje);
            }

            yield return null;
        }

        panelPanoramico.alpha = 0f;
        panelPanoramico.blocksRaycasts = false; // Permite interactuar con la interfaz

        if (audioAmbiente != null)
        {
            audioAmbiente.volume = volumenMaximo;
        }
    }

    private IEnumerator SubirMusica()
    {
        float tiempo = 0f;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            if (audioAmbiente != null)
            {
                audioAmbiente.volume = Mathf.Lerp(0f, volumenMaximo, tiempo / duracionFade);
            }
            yield return null;
        }

        if (audioAmbiente != null)
        {
            audioAmbiente.volume = volumenMaximo;
        }
    }

    private IEnumerator CambiarConDesvanecimiento(string nombreDeLaEscena)
    {
        float tiempo = 0f;
        float volumenInicial = audioAmbiente != null ? audioAmbiente.volume : 0f;

        if (panelPanoramico != null)
        {
            panelPanoramico.blocksRaycasts = true; // Bloquea interacción durante la transición
        }

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            float porcentaje = tiempo / duracionFade;

            // Reducción progresiva del volumen de la música (Fade Out)
            if (audioAmbiente != null)
            {
                audioAmbiente.volume = Mathf.Lerp(volumenInicial, 0f, porcentaje);
            }

            // Desvanecimiento visual a negro (Fade Out)
            if (panelPanoramico != null)
            {
                panelPanoramico.alpha = Mathf.Lerp(0f, 1f, porcentaje);
            }

            yield return null;
        }

        if (audioAmbiente != null)
        {
            audioAmbiente.Stop();
        }

        SceneManager.LoadScene(nombreDeLaEscena);
    }

    public void SalirDelJuego()
    {
        ReproducirSonidoBoton();
        Debug.Log("Saliendo del juego...");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}