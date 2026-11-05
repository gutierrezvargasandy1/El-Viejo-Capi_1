using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CargarWarningScene : MonoBehaviour
{
    [Header("tiempos de espera (segundos)")]
    public float tiempoCargaLogo = 4f;

    [Header("nombre de la siguiente escena")]
    public string nombreEscenaWarning = "WarningScene";

    [Header("audio de la escena")]
    public AudioSource audioSource;
    public AudioClip sonidoLogo;

    private void Start()
    {
        if (audioSource != null && sonidoLogo != null)
        {
            audioSource.PlayOneShot(sonidoLogo);
        }

        StartCoroutine(SecuenciaLogo());
    }

    private IEnumerator SecuenciaLogo()
    {
        yield return new WaitForSeconds(tiempoCargaLogo);
        SceneManager.LoadScene(nombreEscenaWarning);
    }
}