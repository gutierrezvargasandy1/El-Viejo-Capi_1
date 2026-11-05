using UnityEngine;
using UnityEngine.Events;

public class ZonaObjetivo : MonoBehaviour
{
    public string objetivo = "Ir a la cocina";
    public bool usarSoloUnaVez = true;

    [Header("Fase requerida (-1 = cualquiera)")]
    [Tooltip("0=desayunar, 1=marionetas, 2=salir. Déjalo en -1 si no importa la fase")]
    public int faseRequerida = -1;

    [Header("Acción extra al entrar (opcional)")]
    public UnityEvent alEntrar;   // aquí engancharás mostrar el contador

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Si pide una fase específica y no estamos en ella, no hace nada
            if (faseRequerida != -1 && GestorSecuencia.instancia != null
                && !GestorSecuencia.instancia.EsFase(faseRequerida))
                return;

            if (GestorObjetivos.instancia != null)
                GestorObjetivos.instancia.MostrarObjetivo(objetivo);

            alEntrar.Invoke();   // dispara lo extra (mostrar contador, etc.)

            if (usarSoloUnaVez)
                gameObject.SetActive(false);
        }
    }
}