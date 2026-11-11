using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CambioEscena : MonoBehaviour
{
    [Header("Nombre EXACTO de la escena destino")]
    public string nombreEscena = "Esena 2";

    [Header("¿Requiere juntar todas las marionetas?")]
    public bool requiereMarionetas = true;
    public string mensajeFalta = "Aún te faltan marionetas";

    [Header("Llamado desde un evento (ej. Al Comer)")]
    [Tooltip("Segundos de espera antes de cambiar, para que termine el sonido")]
    public float esperaDesdeEvento = 1.6f;

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

        IrA(nombreEscena, 0f);
    }

    // ---- Para usar desde UnityEvents (aparecen en el menú del evento) ----

    // Va a la escena escrita en el campo "Nombre Escena"
    public void Cambiar()
    {
        IrA(nombreEscena, esperaDesdeEvento);
    }

    // Va a la escena que escribas en la cajita del evento
    public void CambiarA(string escena)
    {
        IrA(escena, esperaDesdeEvento);
    }

    // ----------------------------------------------------------------------

    void IrA(string escena, float espera)
    {
        if (yaActivado) return;

        if (!Application.CanStreamedLevelBeLoaded(escena))
        {
            Debug.LogError($"[CambioEscena] La escena '{escena}' no existe o no está en File > Build Profiles > Scene List.", this);
            return;
        }

        yaActivado = true;
        StartCoroutine(CargarDespues(escena, espera));
    }

    IEnumerator CargarDespues(string escena, float espera)
    {
        if (espera > 0f)
            yield return new WaitForSecondsRealtime(espera);

        if (GestorTransicion.instancia != null)
        {
            // Con pantalla negra (también usa LoadScene por dentro)
            GestorTransicion.instancia.CambiarEscena(escena);
        }
        else
        {
            // Sin gestor: carga normal directa
            SceneManager.LoadScene(escena);
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