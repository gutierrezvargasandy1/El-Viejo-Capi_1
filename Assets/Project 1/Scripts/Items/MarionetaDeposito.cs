using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class MarionetaDeposito : MonoBehaviour
{
    [Header("Texto de la pista")]
    public string mensaje = "Presiona E para guardar la marioneta";
    public string mensajeSinMarionetas = "Ya no traes marionetas";

    [Header("Marioneta que aparece aquí")]
    [Tooltip("Déjalo vacío para usar este mismo objeto")]
    public GameObject marionetaEnSuLugar;

    [Header("Sonido al dejar la marioneta")]
    public AudioClip sonidoDejar;
    [Range(0f, 1f)] public float volumen = 1f;

    [Header("Diagnóstico")]
    public bool mostrarDebug = true;

    private bool jugadorCerca, yaGuardada, mismoObjeto;

    void Start()
    {
        mismoObjeto = (marionetaEnSuLugar == null || marionetaEnSuLugar == gameObject);
        if (marionetaEnSuLugar == null) marionetaEnSuLugar = gameObject;

        Ver(false);

        Collider c = GetComponent<Collider>();
        if (c != null && !c.isTrigger)
            Debug.LogWarning("[Deposito] '" + name + "' no tiene Is Trigger marcado.", this);
    }

    void Ver(bool visible)
    {
        if (mismoObjeto)
        {
            // No apagamos el objeto: mataríamos el trigger. Solo ocultamos la malla.
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                r.enabled = visible;
        }
        else
        {
            marionetaEnSuLugar.SetActive(visible);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (yaGuardada) return;

        if (!other.CompareTag("Player"))
        {
            if (mostrarDebug)
                Debug.Log("[Deposito] entró '" + other.name + "' con tag '" + other.tag + "'", this);
            return;
        }

        jugadorCerca = true;
        if (mostrarDebug) Debug.Log("[Deposito] jugador cerca de '" + name + "'", this);
        Pista();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorCerca = false;
        if (GestorPistas.instancia != null) GestorPistas.instancia.Ocultar();
    }

    void Update()
    {
        if (yaGuardada || !jugadorCerca || Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame) Guardar();
    }

    void Pista()
    {
        if (GestorPistas.instancia == null) return;

        bool trae = GestorCanastaDevolver.instancia != null &&
                    GestorCanastaDevolver.instancia.QuedanMarionetas;

        GestorPistas.instancia.MostrarPista(trae ? mensaje : mensajeSinMarionetas);
    }

    void Guardar()
    {
        if (GestorCanastaDevolver.instancia == null)
        {
            Debug.LogError("[Deposito] no hay GestorCanastaDevolver en la escena.", this);
            return;
        }

        if (!GestorCanastaDevolver.instancia.Depositar())
        {
            if (mostrarDebug) Debug.Log("[Deposito] la canasta ya está vacía.", this);
            return;
        }

        yaGuardada = true;
        jugadorCerca = false;
        Ver(true);

        ReproducirSonido();

        if (GestorPistas.instancia != null) GestorPistas.instancia.Ocultar();

        Collider c = GetComponent<Collider>();
        if (c != null) c.enabled = false;
    }

    // Se reproduce desde un objeto temporal para que nunca se corte,
    // aunque este objeto o la marioneta se desactiven.
    void ReproducirSonido()
    {
        if (sonidoDejar == null)
        {
            if (mostrarDebug) Debug.LogWarning("[Deposito] no tiene asignado el sonidoDejar.", this);
            return;
        }

        GameObject temp = new GameObject("SonidoDejarMarioneta");
        AudioSource fuente = temp.AddComponent<AudioSource>();
        fuente.clip = sonidoDejar;
        fuente.volume = volumen;
        fuente.spatialBlend = 0f; // 2D
        fuente.pitch = Random.Range(0.95f, 1.05f); // ligera variación entre marionetas
        fuente.Play();
        Destroy(temp, sonidoDejar.length / fuente.pitch + 0.1f);
    }
}