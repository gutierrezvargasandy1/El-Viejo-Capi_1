using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Oportera : MonoBehaviour
{
    public enum EjeGiro { X, Y, Z }

    [Header("Giro")]
    [Tooltip("MUNDO: gira alrededor del eje vertical real, sin importar como este orientado el pivote.")]
    public bool girarEnEspacioMundo = true;
    public EjeGiro ejeGiroLocal = EjeGiro.Y;
    public float anguloRotacion = -90f;

    [Header("Tiempos")]
    [Tooltip("Segundos que tarda la puerta en abrir o cerrar.")]
    public float duracionApertura = 1f;
    [Tooltip("Segundos de espera tras pulsar la tecla antes de que la puerta empiece a moverse. Sirve para que coincida con el momento en que la mano toca la puerta en la animacion.")]
    public float retrasoAntesDeMover = 0.35f;
    [Tooltip("Forma del movimiento. Por defecto arranca y frena suave, como una puerta real.")]
    public AnimationCurve curvaMovimiento = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Animacion del jugador")]
    [Tooltip("Animator del personaje. Dejalo vacio si no quieres animacion.")]
    public Animator animadorJugador;
    [Tooltip("Nombre del parametro Trigger en el Animator Controller.")]
    public string triggerAnimacion = "AbrirPuerta";
    [Tooltip("Si esta activo, la duracion se toma de la longitud del clip en vez del campo de arriba.")]
    public bool usarDuracionDelClip = false;
    [Tooltip("Nombre exacto del AnimationClip del que leer la duracion.")]
    public string nombreDelClip = "AbrirPuerta";

    [Header("Sonido")]
    [Tooltip("AudioSource de la puerta. Si lo dejas vacio se busca uno en este mismo objeto.")]
    public AudioSource fuenteAudio;
    [Tooltip("Chirrido o sonido largo mientras la puerta se abre.")]
    public AudioClip sonidoAbrir;
    [Tooltip("Sonido mientras la puerta se cierra. Si lo dejas vacio se usa el de abrir.")]
    public AudioClip sonidoCerrar;
    [Tooltip("Sonido corto al terminar el movimiento: el pestillo o el golpe contra el marco.")]
    public AudioClip sonidoFinal;
    [Tooltip("Sonido cuando intentas abrir desde demasiado lejos. Opcional.")]
    public AudioClip sonidoLejos;
    [Range(0f, 1f)] public float volumen = 0.8f;
    [Tooltip("Variacion aleatoria del tono para que no suene identico cada vez. 0 = sin variacion.")]
    [Range(0f, 0.3f)] public float variacionTono = 0.05f;
    [Tooltip("Estira o encoge el clip para que dure exactamente lo que el giro. Deforma un poco el tono.")]
    public bool ajustarTonoALaDuracion = false;

    [Header("Interaccion")]
    public float distanciaPermitida = 4f;
    public KeyCode tecla = KeyCode.E;
    public bool medirDesdeElCollider = false;

    [Header("Estado")]
    public bool estaAbierta = false;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private Transform jugador;
    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;
    private Collider colliderPuerta;
    private bool enMovimiento = false;

    void Start()
    {
        if (girarEnEspacioMundo)
        {
            rotacionCerrada = transform.rotation;
            rotacionAbierta = Quaternion.AngleAxis(anguloRotacion, Vector3.up) * rotacionCerrada;
        }
        else
        {
            rotacionCerrada = transform.localRotation;

            Vector3 euler;
            switch (ejeGiroLocal)
            {
                case EjeGiro.X: euler = new Vector3(anguloRotacion, 0f, 0f); break;
                case EjeGiro.Z: euler = new Vector3(0f, 0f, anguloRotacion); break;
                default: euler = new Vector3(0f, anguloRotacion, 0f); break;
            }
            rotacionAbierta = rotacionCerrada * Quaternion.Euler(euler);
        }

        colliderPuerta = GetComponent<Collider>();
        AvisarEscalaNoUniforme();
        BuscarJugador();

        // Si no se asigno Animator a mano, intentamos tomarlo del jugador
        if (animadorJugador == null && jugador != null)
            animadorJugador = jugador.GetComponentInChildren<Animator>();

        PrepararAudio();
    }

    void PrepararAudio()
    {
        if (fuenteAudio == null) fuenteAudio = GetComponent<AudioSource>();

        if (fuenteAudio == null)
        {
            bool hayClips = sonidoAbrir != null || sonidoCerrar != null || sonidoFinal != null;
            if (hayClips && mostrarDebug)
            {
                Debug.LogWarning("[Oportera] " + name + ": asignaste clips pero no hay AudioSource. " +
                                 "Agrega un Audio Source a este objeto o arrastra uno al campo 'Fuente Audio'.", this);
            }
            return;
        }

        // La puerta no debe sonar sola al cargar la escena
        fuenteAudio.playOnAwake = false;
        fuenteAudio.loop = false;
    }

    void AvisarEscalaNoUniforme()
    {
        Vector3 s = transform.lossyScale;
        float max = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        float min = Mathf.Min(s.x, Mathf.Min(s.y, s.z));
        if (max - min > 0.001f && mostrarDebug)
        {
            Debug.LogWarning("[Oportera] " + name + ": escala no uniforme " + s +
                ". El objeto que rota se deformara al girar. Pon la escala del pivote en (1,1,1).", this);
        }
    }

    void BuscarJugador()
    {
        GameObject obj = GameObject.FindWithTag("Player");

        if (obj == null)
        {
            if (Camera.main != null)
            {
                obj = Camera.main.gameObject;
                if (mostrarDebug)
                    Debug.LogWarning("[Oportera] " + name + ": ningun objeto tiene el tag 'Player'. Usando Camera.main.", this);
            }
            else if (mostrarDebug)
            {
                Debug.LogError("[Oportera] " + name + ": no encuentro al jugador. Ponle el tag 'Player'.", this);
            }
        }

        if (obj != null) jugador = obj.transform;
    }

    void Update()
    {
        if (jugador == null)
        {
            BuscarJugador();
            if (jugador == null) return;
        }

        if (enMovimiento) return; // ignora la tecla mientras la puerta se mueve

        Vector3 posicionPuerta = (medirDesdeElCollider && colliderPuerta != null)
            ? colliderPuerta.bounds.center
            : transform.position;

        float distancia = Vector3.Distance(posicionPuerta, jugador.position);

        if (TeclaPresionada())
        {
            if (distancia <= distanciaPermitida)
            {
                StartCoroutine(Accionar());
            }
            else
            {
                Reproducir(sonidoLejos);

                if (mostrarDebug)
                {
                    Debug.Log("[Oportera] " + name + ": tecla pulsada pero estas a " + distancia.ToString("F2") +
                              " y el limite es " + distanciaPermitida.ToString("F2"), this);
                }
            }
        }
    }

    IEnumerator Accionar()
    {
        enMovimiento = true;
        bool abriendo = !estaAbierta;

        // Lanza la animacion del personaje
        if (animadorJugador != null && !string.IsNullOrEmpty(triggerAnimacion))
            animadorJugador.SetTrigger(triggerAnimacion);

        float duracion = DuracionEfectiva();

        if (mostrarDebug)
            Debug.Log("[Oportera] " + name + ": " + (abriendo ? "abriendo" : "cerrando") +
                      " | retraso " + retrasoAntesDeMover.ToString("F2") + "s | duracion " + duracion.ToString("F2") + "s", this);

        // Espera a que la animacion llegue al momento del contacto
        if (retrasoAntesDeMover > 0f)
            yield return new WaitForSeconds(retrasoAntesDeMover);

        // El sonido arranca justo cuando la puerta empieza a girar, no al pulsar la tecla
        AudioClip clipMovimiento = abriendo ? sonidoAbrir : (sonidoCerrar != null ? sonidoCerrar : sonidoAbrir);
        Reproducir(clipMovimiento, duracion);

        Quaternion desde = girarEnEspacioMundo ? transform.rotation : transform.localRotation;
        Quaternion hasta = abriendo ? rotacionAbierta : rotacionCerrada;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duracion);
            float k = curvaMovimiento.Evaluate(Mathf.Clamp01(t));

            if (girarEnEspacioMundo)
                transform.rotation = Quaternion.Slerp(desde, hasta, k);
            else
                transform.localRotation = Quaternion.Slerp(desde, hasta, k);

            yield return null;
        }

        // Valor final exacto, sin residuos de interpolacion
        if (girarEnEspacioMundo) transform.rotation = hasta;
        else transform.localRotation = hasta;

        estaAbierta = abriendo;
        enMovimiento = false;

        // Pestillo o golpe final
        Reproducir(sonidoFinal);
    }

    void Reproducir(AudioClip clip, float duracionObjetivo = -1f)
    {
        if (fuenteAudio == null || clip == null) return;

        if (ajustarTonoALaDuracion && duracionObjetivo > 0.01f && clip.length > 0.01f)
        {
            // Estira o encoge el clip para que termine junto con el giro
            fuenteAudio.pitch = clip.length / duracionObjetivo;
        }
        else
        {
            fuenteAudio.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        }

        fuenteAudio.PlayOneShot(clip, volumen);
    }

    float DuracionEfectiva()
    {
        if (usarDuracionDelClip)
        {
            float largo = LongitudDelClip(nombreDelClip);
            if (largo > 0f)
            {
                // La puerta debe terminar cuando termina el clip
                float restante = largo - retrasoAntesDeMover;
                if (restante > 0.05f) return restante;

                if (mostrarDebug)
                    Debug.LogWarning("[Oportera] " + name + ": el retraso (" + retrasoAntesDeMover +
                                     "s) es mayor o igual que el clip (" + largo + "s). Usando duracionApertura.", this);
            }
            else if (mostrarDebug)
            {
                Debug.LogWarning("[Oportera] " + name + ": no encontre el clip '" + nombreDelClip +
                                 "'. Revisa el nombre exacto. Usando duracionApertura.", this);
            }
        }

        return duracionApertura;
    }

    float LongitudDelClip(string nombre)
    {
        if (animadorJugador == null || animadorJugador.runtimeAnimatorController == null) return -1f;

        AnimationClip[] clips = animadorJugador.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == nombre) return clips[i].length;
        }
        return -1f;
    }

    bool TeclaPresionada()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(tecla);
#endif
    }

    void OnDrawGizmosSelected()
    {
        Collider c = GetComponent<Collider>();
        Vector3 centro = (medirDesdeElCollider && c != null) ? c.bounds.center : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centro, distanciaPermitida);

        if (girarEnEspacioMundo)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position - Vector3.up * 2f, transform.position + Vector3.up * 2f);
        }
        else
        {
            Vector3 eje = ejeGiroLocal == EjeGiro.X ? transform.right
                        : ejeGiroLocal == EjeGiro.Z ? transform.forward
                        : transform.up;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position - eje * 2f, transform.position + eje * 2f);
        }
    }
}