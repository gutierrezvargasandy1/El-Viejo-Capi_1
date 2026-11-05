using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GestorTransicion : MonoBehaviour
{
    public static GestorTransicion instancia;

    [Header("Pantalla negra (CanvasGroup que cubre todo)")]
    [Tooltip("Debe ser HIJO de este objeto, para que sobreviva al cambio de escena")]
    public CanvasGroup pantallaNegra;

    [Header("Tiempos")]
    public float duracionFade = 1f;
    [Tooltip("Cuánto se queda la pantalla en negro antes de entrar a la escena nueva")]
    public float tiempoEnNegro = 2f;

    private bool enTransicion = false;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        transform.SetParent(null);          // DontDestroyOnLoad solo funciona en objetos raíz
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (pantallaNegra != null)
        {
            pantallaNegra.alpha = 0f;
            pantallaNegra.blocksRaycasts = false;
        }
        else
        {
            Debug.LogWarning("GestorTransicion: no tiene asignada la pantallaNegra.");
        }
    }

    public void CambiarEscena(string nombreEscena)
    {
        if (enTransicion) return;

        if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            Debug.LogError($"La escena '{nombreEscena}' no existe o no está en File > Build Profiles > Scene List.");
            return;
        }

        StartCoroutine(Transicion(nombreEscena));
    }

    IEnumerator Transicion(string nombreEscena)
    {
        enTransicion = true;
        if (pantallaNegra != null) pantallaNegra.blocksRaycasts = true;   // bloquea clics durante la transición

        // 1. Oscurece
        yield return Fade(0f, 1f);

        // 2. Se queda en negro el tiempo pedido
        float t = 0f;
        while (t < tiempoEnNegro)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3. Carga NORMAL (la que sí funciona): descarga la escena actual completa
        //    y luego entra la nueva. Como la pantalla está en negro, no se nota el tirón.
        SceneManager.LoadScene(nombreEscena);

        // Espera un par de frames a que la escena nueva haga su Awake/Start
        yield return null;
        yield return null;

        // 4. Aclara ya en la escena nueva
        yield return Fade(1f, 0f);

        Terminar();
    }

    void Terminar()
    {
        if (pantallaNegra != null) pantallaNegra.blocksRaycasts = false;
        enTransicion = false;
    }

    IEnumerator Fade(float desde, float hasta)
    {
        if (pantallaNegra == null) yield break;

        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            if (pantallaNegra == null) yield break;   // por si se destruyó durante la carga
            pantallaNegra.alpha = Mathf.Lerp(desde, hasta, t / duracionFade);
            yield return null;
        }
        pantallaNegra.alpha = hasta;
    }
}