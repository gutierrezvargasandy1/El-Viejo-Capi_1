using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    [Header("Nombre EXACTO de la escena destino")]
    public string nombreEscena = "Esena 2";

    [Header("¿Requiere juntar todas las marionetas?")]
    public bool requiereMarionetas = true;
    public string mensajeFalta = "Aún te faltan marionetas";

    private bool yaActivado = false;

    void OnTriggerEnter(Collider other)
    {
        if (yaActivado || !other.CompareTag("Player")) return;

        if (requiereMarionetas && !MarionetasListas())
        {
            Debug.Log("[CambioEscena] Faltan marionetas, no cambia.");
            if (GestorPistas.instancia != null)
                GestorPistas.instancia.MostrarPista(mensajeFalta);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            Debug.LogError($"[CambioEscena] La escena '{nombreEscena}' no existe o no está en File > Build Profiles > Scene List.");
            return;
        }

        yaActivado = true;

        if (GestorTransicion.instancia != null)
        {
            // Con pantalla negra (también usa LoadScene por dentro)
            GestorTransicion.instancia.CambiarEscena(nombreEscena);
        }
        else
        {
            // Sin gestor: carga normal directa
            SceneManager.LoadScene(nombreEscena);
        }
    }

    // Sirve igual en la escena de recolectar que en la de devolver
    bool MarionetasListas()
    {
        if (GestorCanastaDevolver.instancia != null)
            return GestorCanastaDevolver.instancia.Completado;

        if (GestorCanasta.instancia != null)
            return GestorCanasta.instancia.Completado;

        return false;
    }
}