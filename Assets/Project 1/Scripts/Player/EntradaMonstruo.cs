using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Un lugar por donde el monstruo intenta entrar: Ventana, Puerta o Chimenea.
/// - Ventana: ve a la ventana y mantén Espacio (cortinas cerradas) hasta que se vaya.
/// - Puerta: apúntale con la linterna encendida hasta que se vaya.
/// - Chimenea: ten la leña encendida hasta que se vaya.
/// Si se acaba el tiempo sin defender, entra y hay jumpscare.
/// </summary>
public class EntradaMonstruo : MonoBehaviour
{
    public TipoEntrada tipo = TipoEntrada.Ventana;

    [Header("Monstruo")]
    [Tooltip("Hijo de este objeto. Su posición final es (0,0,0) local; el frente (Z azul) mira hacia el cuarto")]
    public MonstruoVisual monstruo;
    [Tooltip("Desde dónde llega, en local. Ventana: desde abajo y atrás. Puerta: desde atrás. Chimenea: desde arriba")]
    public Vector3 desplazamientoInicial = new Vector3(0f, -1.4f, -1f);

    [Header("Defensa")]
    [Tooltip("Segundos que hay que defender (sin parar) para que se vaya")]
    public float tiempoParaAhuyentar = 3f;
    [Tooltip("Qué tan rápido se pierde lo defendido si dejas de defender")]
    public float perdidaDefensa = 0.6f;
    [Tooltip("Segundos que esta entrada descansa después de ahuyentarlo")]
    public float enfriamiento = 8f;

    [Header("Ventana")]
    public CortinasNoche cortinas;

    [Header("Puerta")]
    [Tooltip("A dónde hay que apuntar la linterna (el vidrio de la puerta)")]
    public Transform puntoMirar;
    [Range(5f, 60f)] public float anguloLinterna = 20f;
    public float distanciaLinterna = 12f;
    [Tooltip("Opcional: la puerta (el modelo) para que tiemble cuando la empuja")]
    public Transform puertaQueTiembla;
    public float tiembloGrados = 2.5f;

    [Header("Chimenea")]
    public FuegoChimenea fuego;

    [Header("Sonidos (cada entrada con el suyo)")]
    [Tooltip("Se repite mientras intenta entrar")]
    public AudioClip sonidoIntento;
    [Range(0f, 1f)] public float volumenIntento = 1f;
    public AudioClip sonidoSeVa;
    [Tooltip("Cuando logra entrar (justo antes del jumpscare)")]
    public AudioClip sonidoEntra;

    [Header("Eventos")]
    public UnityEvent alEmpezar;
    public UnityEvent alAhuyentar;
    public UnityEvent alEntrar;

    public bool Atacando => atacando;
    public bool Disponible => !atacando && Time.time >= finEnfriamiento;
    public float Progreso => total > 0f ? 1f - restante / total : 0f;
    public float Defensa01 => tiempoParaAhuyentar > 0f ? defensa / tiempoParaAhuyentar : 0f;

    bool atacando;
    float restante, total, defensa, finEnfriamiento;
    AudioSource fuenteLoop, fuenteUno;
    Quaternion rotPuerta;
    Vector3 posPuerta;
    LinternaNoche linterna;
    ControlJugadorNoche jugador;

    void Awake()
    {
        fuenteLoop = CrearFuente(true);
        fuenteUno = CrearFuente(false);
        if (puertaQueTiembla != null)
        {
            rotPuerta = puertaQueTiembla.rotation;
            posPuerta = puertaQueTiembla.position;
        }
        if (monstruo != null) monstruo.Ocultar();
    }

    AudioSource CrearFuente(bool loop)
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.loop = loop;
        f.spatialBlend = 0.85f;      // se oye de qué lado viene
        f.minDistance = 2f;
        f.maxDistance = 25f;
        return f;
    }

    void Start()
    {
        var g = GestorNoche.instancia;
        linterna = g != null && g.linterna != null ? g.linterna : FindAnyObjectByType<LinternaNoche>();
        jugador = g != null && g.jugador != null ? g.jugador : FindAnyObjectByType<ControlJugadorNoche>();
        if (fuego == null && tipo == TipoEntrada.Chimenea) fuego = FindAnyObjectByType<FuegoChimenea>();
    }

    public void IniciarAtaque(float duracion)
    {
        if (atacando) return;
        atacando = true;
        total = restante = Mathf.Max(1f, duracion);
        defensa = 0f;

        if (sonidoIntento != null)
        {
            fuenteLoop.clip = sonidoIntento;
            fuenteLoop.volume = volumenIntento * 0.5f;
            fuenteLoop.Play();
        }
        if (monstruo != null) monstruo.Aparecer(desplazamientoInicial);
        alEmpezar.Invoke();
    }

    void Update()
    {
        if (!atacando || Time.timeScale <= 0f) return;

        float dt = Time.deltaTime;
        bool defendida = Defendida();
        if (defendida) defensa += dt;
        else
        {
            defensa = Mathf.Max(0f, defensa - dt * perdidaDefensa);
            restante -= dt;
        }

        float p = Progreso;
        if (monstruo != null) monstruo.Acercar(p, Defensa01);
        fuenteLoop.volume = volumenIntento * Mathf.Lerp(0.5f, 1f, p);

        if (puertaQueTiembla != null)
        {
            float ruido = (Mathf.PerlinNoise(Time.time * 9f, 3f) - 0.5f) * 2f;
            float golpe = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * 2.3f)), 8f);
            float fuerza = Mathf.Lerp(0.4f, 1f, p);
            float grados = (ruido * 0.4f + golpe) * tiembloGrados * fuerza;
            // gira sobre el eje vertical del mundo (aunque tu modelo venga rotado) y se empuja hacia el cuarto
            puertaQueTiembla.SetPositionAndRotation(
                posPuerta + transform.forward * (golpe * 0.012f * fuerza),
                Quaternion.AngleAxis(grados, Vector3.up) * rotPuerta);
        }

        if (defensa >= tiempoParaAhuyentar) Ahuyentar();
        else if (restante <= 0f) Entrar();
    }

    public bool Defendida()
    {
        switch (tipo)
        {
            case TipoEntrada.Ventana:
                return cortinas != null && cortinas.Cerradas
                    && jugador != null && jugador.VistaActual == VistaNoche.Ventana;

            case TipoEntrada.Puerta:
                if (linterna == null || !linterna.Encendida) return false;
                Transform objetivo = puntoMirar != null ? puntoMirar : transform;
                Vector3 dir = objetivo.position - linterna.transform.position;
                if (dir.magnitude > distanciaLinterna) return false;
                return Vector3.Angle(linterna.transform.forward, dir) <= anguloLinterna;

            case TipoEntrada.Chimenea:
                return fuego != null && fuego.Encendido;
        }
        return false;
    }

    void Ahuyentar()
    {
        Terminar();
        if (sonidoSeVa != null) fuenteUno.PlayOneShot(sonidoSeVa, 1f);
        if (monstruo != null) monstruo.Retirarse(desplazamientoInicial);
        finEnfriamiento = Time.time + enfriamiento;
        alAhuyentar.Invoke();
    }

    void Entrar()
    {
        Terminar();
        alEntrar.Invoke();
        if (GestorNoche.instancia != null) GestorNoche.instancia.MonstruoEntro(this);
    }

    /// <summary>Para cuando termina la noche (ganaste o te atraparon por otro lado).</summary>
    public void Cancelar(bool retirarse)
    {
        bool visible = monstruo != null && monstruo.gameObject.activeSelf;
        if (!atacando && !visible) return;
        Terminar();
        if (monstruo == null) return;
        if (retirarse) monstruo.Retirarse(desplazamientoInicial);
        else monstruo.Ocultar();
    }

    public void OcultarMonstruo()
    {
        if (monstruo != null) monstruo.Ocultar();
    }

    void Terminar()
    {
        atacando = false;
        fuenteLoop.Stop();
        if (puertaQueTiembla != null) puertaQueTiembla.SetPositionAndRotation(posPuerta, rotPuerta);
    }

    void OnDrawGizmos()
    {
        Color c = tipo == TipoEntrada.Ventana ? new Color(0.4f, 0.8f, 1f)
                : tipo == TipoEntrada.Puerta ? new Color(1f, 0.9f, 0.3f)
                : new Color(1f, 0.45f, 0.1f);
        Gizmos.color = c;
        Vector3 inicio = transform.TransformPoint(desplazamientoInicial);
        Gizmos.DrawWireSphere(transform.position, 0.15f);
        Gizmos.DrawLine(inicio, transform.position);
        Gizmos.DrawWireCube(inicio, Vector3.one * 0.15f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
        if (tipo == TipoEntrada.Puerta && puntoMirar != null) Gizmos.DrawWireSphere(puntoMirar.position, 0.2f);
    }
}
