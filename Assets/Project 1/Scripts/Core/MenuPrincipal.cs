using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ponlo en un GameObject del menú (por ejemplo en el Canvas).
/// Conecta los botones en su On Click(): NuevoJuego() y Continuar().
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [Tooltip("Nombre EXACTO de la primera escena del juego (como está en Build Profiles / Build Settings)")]
    public string primeraEscena = "Esenas 1";

    [Tooltip("Arrastra aquí el botón Continuar: se desactiva si no hay partida guardada")]
    public Button botonContinuar;

    void Start()
    {
        if (botonContinuar) botonContinuar.interactable = SistemaGuardado.HayPartida();
    }

    public void NuevoJuego()
    {
        SistemaGuardado.BorrarPartida();
        SceneManager.LoadScene(primeraEscena);
    }

    public void Continuar()
    {
        if (!SistemaGuardado.CargarPartida())
            Debug.LogWarning("No hay partida guardada");
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}