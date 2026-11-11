using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class ComerWaffle : MonoBehaviour
{
    [Header("Texto de la pista")]
    public string mensaje = "Presiona E para comer";

    [Header("El waffle (se oculta al comer)")]
    public GameObject waffle;

    [Header("Parpadeo")]
    public bool parpadearSiempre = true;
    public float velocidadParpadeo = 3f;

    [Header("Sonido al comer")]
    public AudioClip sonidoComer;
    [Range(0f, 1f)] public float volumen = 1f;

    [Header("Qué pasa al comer (opcional)")]
    public UnityEvent alComer;

    private bool jugadorCerca = false;
    private bool yaComido = false;
    private Renderer rendererWaffle;

    void Start()
    {
        if (waffle != null)
            rendererWaffle = waffle.GetComponentInChildren<Renderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (yaComido) return;

        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (GestorPistas.instancia != null)
                GestorPistas.instancia.MostrarPista(mensaje);
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
        if (!yaComido && rendererWaffle != null && (parpadearSiempre || jugadorCerca))
        {
            bool visible = Mathf.Sin(Time.time * velocidadParpadeo) > 0f;
            rendererWaffle.enabled = visible;
        }

        if (yaComido || !jugadorCerca) return;

        // Solo se come en la fase 0 (desayunar)
        if (GestorSecuencia.instancia != null && !GestorSecuencia.instancia.EsFase(0))
            return;

        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            Comer();
    }

    void Comer()
    {
        yaComido = true;

        ReproducirSonido();

        if (rendererWaffle != null)
            rendererWaffle.enabled = true;

        if (waffle != null)
            waffle.SetActive(false);

        if (GestorPistas.instancia != null)
            GestorPistas.instancia.Ocultar();

        alComer.Invoke();

        // Avanza a fase 1 (marionetas)
        if (GestorSecuencia.instancia != null)
            GestorSecuencia.instancia.AvanzarFase();

        gameObject.SetActive(false);
    }

    // Se reproduce desde un objeto temporal para que no se corte
    // cuando este GameObject se desactiva.
    void ReproducirSonido()
    {
        if (sonidoComer == null) return;

        GameObject temp = new GameObject("SonidoComer");
        AudioSource fuente = temp.AddComponent<AudioSource>();
        fuente.clip = sonidoComer;
        fuente.volume = volumen;
        fuente.spatialBlend = 0f; // 2D: se oye "en la boca" del jugador
        fuente.Play();
        Destroy(temp, sonidoComer.length + 0.1f);
    }
}