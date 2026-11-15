using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// El jugador en la noche: mira desde la cama (con límites, como FNAF),
/// da clic en la ventana o la chimenea para caminar hasta ahí (mini cinemática)
/// y regresa a la cama con S o clic derecho.
/// </summary>
public class ControlJugadorNoche : MonoBehaviour
{
    public Camera camara;
    [Tooltip("La vista desde la cama")]
    public PuntoVista vistaCama;

    [Header("Mirar")]
    public float sensibilidad = 0.08f;
    public float suavizado = 12f;

    [Header("Caminar a la ventana / chimenea")]
    public float duracionCaminar = 1.4f;
    [Tooltip("Cuánto sube la cámara al levantarse de la cama durante el camino")]
    public float alturaLevantarse = 0.35f;
    [Tooltip("Lo que tarda la cámara en llegar a la cama al empezar la noche")]
    public float duracionEntrada = 1.2f;

    [Header("Clic")]
    public float distanciaClick = 20f;

    [Header("Sonidos")]
    public AudioClip sonidoPasos;
    public AudioClip sonidoCama;
    [Range(0f, 1f)] public float volumen = 0.8f;

    [Header("Apagar mientras dura la noche")]
    [Tooltip("Arrastra aquí el controlador del jugador u otros scripts que muevan la cámara")]
    public Behaviour[] desactivarDuranteNoche;
    [Tooltip("Objetos que se esconden mientras dura la noche (ej. tu linterna anterior)")]
    public GameObject[] ocultarDuranteNoche;
    [Tooltip("Apaga el CinemachineBrain de la cámara para que no le gane el control")]
    public bool desactivarCinemachineBrain = true;

    public VistaNoche VistaActual => vistaActual != null ? vistaActual.tipo : VistaNoche.Cama;
    public bool EnTransicion => enTransicion;
    public bool Activo => activo;
    public PuntoVista Apuntando { get; private set; }
    /// <summary>Puede usar cortinas / fuego (está quieto en un punto).</summary>
    public bool PuedeActuar => activo && !bloqueado && !enTransicion && Time.timeScale > 0f;
    /// <summary>Puede usar la linterna (también mientras camina).</summary>
    public bool PuedeUsarObjetos => activo && !bloqueado && Time.timeScale > 0f;

    PuntoVista vistaActual;
    bool activo, bloqueado, enTransicion;
    float yaw, pitch, yawSuave, pitchSuave;
    AudioSource fuente;

    void Awake()
    {
        if (camara == null) camara = Camera.main;
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
    }

    public void Activar()
    {
        if (activo) return;
        activo = true;

        if (desactivarDuranteNoche != null)
            foreach (var b in desactivarDuranteNoche)
                if (b != null) b.enabled = false;

        if (ocultarDuranteNoche != null)
            foreach (var go in ocultarDuranteNoche)
                if (go != null) go.SetActive(false);

        if (desactivarCinemachineBrain && camara != null)
        {
            var brain = camara.GetComponent("CinemachineBrain") as Behaviour;
            if (brain != null) brain.enabled = false;
        }

        if (vistaCama != null) StartCoroutine(Mover(vistaCama, duracionEntrada, false));
    }

    public void Bloquear(bool valor)
    {
        bloqueado = valor;
        if (valor) Apuntando = null;
    }

    void Update()
    {
        if (!activo || camara == null) return;
        if (Time.timeScale <= 0f) return;   // menú de pausa: no tocar cámara ni cursor

        if (!bloqueado)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (enTransicion || bloqueado || vistaActual == null) return;

        var mouse = Mouse.current;
        var kb = Keyboard.current;

        // Mirar con límites
        if (mouse != null)
        {
            Vector2 d = mouse.delta.ReadValue() * sensibilidad;
            yaw += d.x;
            pitch -= d.y;
        }
        yaw = Mathf.Clamp(yaw, vistaActual.limiteYaw.x, vistaActual.limiteYaw.y);
        pitch = Mathf.Clamp(pitch, vistaActual.limitePitch.x, vistaActual.limitePitch.y);
        float k = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
        yawSuave = Mathf.Lerp(yawSuave, yaw, k);
        pitchSuave = Mathf.Lerp(pitchSuave, pitch, k);

        Transform pose = vistaActual.Pose;
        float respiracion = Mathf.Sin(Time.time * 1.6f) * 0.006f;
        camara.transform.SetPositionAndRotation(
            pose.position + Vector3.up * respiracion,
            pose.rotation * Quaternion.Euler(pitchSuave, yawSuave, 0f));

        // Clic para ir a un punto / volver a la cama
        Apuntando = null;
        if (vistaActual.tipo == VistaNoche.Cama)
        {
            Apuntando = BuscarPunto();
            if (Apuntando != null && mouse != null && mouse.leftButton.wasPressedThisFrame)
                IrA(Apuntando);
        }
        else
        {
            bool volver = (mouse != null && mouse.rightButton.wasPressedThisFrame)
                       || (kb != null && kb.sKey.wasPressedThisFrame);
            if (volver) IrA(vistaCama);
        }
    }

    PuntoVista BuscarPunto()
    {
        Ray rayo = camara.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var golpes = Physics.RaycastAll(rayo, distanciaClick, ~0, QueryTriggerInteraction.Collide);
        PuntoVista mejor = null;
        float dMin = float.MaxValue;
        foreach (var g in golpes)
        {
            var p = g.collider.GetComponentInParent<PuntoVista>();
            if (p == null || p == vistaActual || p.tipo == VistaNoche.Cama) continue;
            if (g.distance < dMin)
            {
                dMin = g.distance;
                mejor = p;
            }
        }
        return mejor;
    }

    public void IrA(PuntoVista destino)
    {
        if (destino == null || enTransicion || bloqueado) return;
        StartCoroutine(Mover(destino, duracionCaminar, true));
    }

    IEnumerator Mover(PuntoVista destino, float duracion, bool caminar)
    {
        enTransicion = true;
        Apuntando = null;

        bool desdeCama = vistaActual == null || vistaActual.tipo == VistaNoche.Cama;
        bool haciaCama = destino.tipo == VistaNoche.Cama;
        if (caminar && sonidoCama != null && desdeCama != haciaCama) fuente.PlayOneShot(sonidoCama, volumen);
        if (caminar && sonidoPasos != null) fuente.PlayOneShot(sonidoPasos, volumen);

        Transform ct = camara.transform;
        Vector3 p0 = ct.position;
        Quaternion r0 = ct.rotation;
        Transform pose = destino.Pose;
        Vector3 p2 = pose.position;
        Quaternion r2 = pose.rotation;
        Vector3 p1 = (p0 + p2) * 0.5f + Vector3.up * (caminar ? alturaLevantarse : 0f);

        float t = 0f;
        duracion = Mathf.Max(0.05f, duracion);
        while (t < duracion)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / duracion);
            float e = x * x * (3f - 2f * x);
            float u = 1f - e;
            Vector3 pos = u * u * p0 + 2f * u * e * p1 + e * e * p2;     // curva: se levanta y camina
            Quaternion rot = Quaternion.Slerp(r0, r2, e);
            if (caminar)
            {
                float paso = Mathf.Sin(x * Mathf.PI);
                pos += Vector3.up * Mathf.Sin(x * Mathf.PI * 5f) * 0.02f * paso;           // balanceo de pasos
                rot *= Quaternion.Euler(0f, 0f, Mathf.Sin(x * Mathf.PI * 2.5f) * 1.2f * paso);
            }
            ct.SetPositionAndRotation(pos, rot);
            yield return null;
        }

        vistaActual = destino;
        yaw = pitch = yawSuave = pitchSuave = 0f;
        enTransicion = false;
    }
}