using UnityEngine;
using UnityEngine.InputSystem;

public class MarionetaRecolectable : MonoBehaviour
{
    [Header("Texto de la pista")]
    public string mensaje = "Presiona E para recoger";

    [Header("Objeto a recoger (si lo dejas vacío, usa este mismo)")]
    public GameObject objeto;

    private bool jugadorCerca = false;
    private bool yaRecogida = false;

    void Start()
    {
        if (objeto == null)
            objeto = gameObject;
    }

    void OnTriggerEnter(Collider other)
    {
        if (yaRecogida) return;

        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;

            // Solo muestra la pista si es la fase 1 (ya desayunó)
            if (GestorSecuencia.instancia == null || GestorSecuencia.instancia.EsFase(1))
            {
                if (GestorPistas.instancia != null)
                    GestorPistas.instancia.MostrarPista(mensaje);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (GestorPistas.instancia != null)
                GestorPistas.instancia.Ocultar();
        }
    }

    void Update()
    {
        if (yaRecogida || !jugadorCerca) return;

        // Solo se recogen en la fase 1 (después de desayunar)
        if (GestorSecuencia.instancia != null && !GestorSecuencia.instancia.EsFase(1))
            return;

        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            Recoger();
    }

    void Recoger()
    {
        yaRecogida = true;

        if (GestorCanasta.instancia != null)
            GestorCanasta.instancia.Recolectar();

        if (GestorPistas.instancia != null)
            GestorPistas.instancia.Ocultar();

        objeto.SetActive(false);
    }
}