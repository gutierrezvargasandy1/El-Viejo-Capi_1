using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pon este script en un GameObject con un Collider marcado como "Is Trigger".
/// - Cuando el jugador entra, se guarda la partida (o al presionar E si activas requiereInteraccion).
/// - Si venimos de "Continuar", coloca al jugador en este punto.
/// </summary>
[DefaultExecutionOrder(100)] // Corre después de los scripts del jugador para que no le pisen la posición
[RequireComponent(typeof(Collider))]
public class PuntoDeGuardado : MonoBehaviour
{
    [Header("Identificación (debe ser ÚNICO en todo el juego)")]
    [Tooltip("Ej: Esena1_Habitacion, Esena2_Pasillo")]
    public string id = "Esena1_Inicio";

    [Header("Dónde aparece el jugador al cargar")]
    [Tooltip("Opcional. Si lo dejas vacío usa la posición de este objeto.")]
    public Transform puntoDeAparicion;

    [Header("Cómo se guarda")]
    public bool guardarAlEntrar = true;
    public bool requiereInteraccion = false;    // true = hay que presionar E dentro del trigger
    public bool soloUnaVez = true;              // No volver a guardar si ya se guardó aquí

    [Header("Feedback (opcional)")]
    [Tooltip("Un texto/ícono de 'Guardando...' que se muestra unos segundos")]
    public GameObject avisoGuardando;
    public float duracionAviso = 2f;
    public AudioSource sonidoGuardar;

    [Header("Eventos")]
    [Tooltip("Se ejecuta cuando el jugador aparece aquí al Continuar (ej. desactivar la cinemática de inicio)")]
    public UnityEvent alCargarAqui;
    public UnityEvent alGuardar;

    Transform jugadorDentro;
    bool yaGuardado;

    void Start()
    {
        if (avisoGuardando) avisoGuardando.SetActive(false);

        // ¿Venimos del botón Continuar y este es el punto guardado?
        if (SistemaGuardado.PendienteDeCargar && SistemaGuardado.Datos.puntoId == id)
        {
            var jugador = BuscarJugador();
            if (jugador)
            {
                Transform destino = puntoDeAparicion ? puntoDeAparicion : transform;
                SistemaGuardado.Teletransportar(jugador, destino.position, destino.eulerAngles.y);
            }
            yaGuardado = true; // Para no volver a guardar justo al aparecer
            SistemaGuardado.MarcarCargado();
            alCargarAqui?.Invoke();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorDentro = other.transform.root;
        if (guardarAlEntrar && !requiereInteraccion) Guardar();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) jugadorDentro = null;
    }

    void Update()
    {
        if (!requiereInteraccion || jugadorDentro == null) return;
        if (PresionoE()) Guardar();
    }

    public void Guardar()
    {
        if (soloUnaVez && yaGuardado) return;
        var jugador = jugadorDentro ? jugadorDentro : BuscarJugador();
        if (!jugador) return;

        SistemaGuardado.Guardar(id, jugador);
        yaGuardado = true;
        alGuardar?.Invoke();
        if (sonidoGuardar) sonidoGuardar.Play();
        if (avisoGuardando) StartCoroutine(MostrarAviso());
    }

    IEnumerator MostrarAviso()
    {
        avisoGuardando.SetActive(true);
        yield return new WaitForSeconds(duracionAviso);
        avisoGuardando.SetActive(false);
    }

    static Transform BuscarJugador()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform.root : null;
    }

    static bool PresionoE()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    // Dibuja el punto en la escena para verlo fácil
    void OnDrawGizmos()
    {
        Transform t = puntoDeAparicion ? puntoDeAparicion : transform;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(t.position, 0.4f);
        Gizmos.DrawRay(t.position, t.forward * 1f);
    }
}