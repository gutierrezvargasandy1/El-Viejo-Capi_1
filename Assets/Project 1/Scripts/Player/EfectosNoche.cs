using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// El "director" de efectos de la noche (estilo The Joy of Creation).
/// Todo reacciona al PELIGRO (qué tan cerca está el monstruo de entrar):
/// - Post-procesado: viñeta que late, aberración cromática, grano, desaturación, bloom, distorsión.
/// - Cámara viva: temblor por golpes y sustos (trauma), temblor de miedo, campo de visión que se cierra.
/// - Linterna que parpadea con el miedo y con cada golpe.
/// - Audio: latido, tensión y respiración del Capi que suben con el peligro.
/// - Respiración AGITADA del Capi frente al monstruo (ventana, chimenea o mirando la puerta),
///   con la cámara temblando de miedo: temblor, jadeo, sacudidas, visión de túnel.
/// - Estática de TV (imagen y sonido) SOLO cuando un monstruo está a punto de entrar.
/// - Relámpagos por la ventana (a veces se ve la silueta del monstruo) y truenos.
/// - Sustos de ambiente: pasos en el techo, golpes, rechinidos, susurros.
/// - Partículas: polvo en la linterna, polvo que cae de la puerta, hollín de la chimenea, chispas al ahuyentarlo.
/// - Jumpscare con golpe de cámara, distorsión y estática.
/// Va en el objeto NOCHE. La herramienta lo agrega y le pone los sonidos.
/// </summary>
public class EfectosNoche : MonoBehaviour
{
    public static EfectosNoche instancia;

    [Header("Referencias (las llena la herramienta o se buscan solas)")]
    public GestorNoche gestor;
    public ControlJugadorNoche jugador;
    public LinternaNoche linterna;
    public UINoche ui;
    public Material materialAditivo;
    public Material materialPolvo;

    [Header("Post-procesado")]
    public bool usarPostProcesado = true;
    [Range(0f, 1f)] public float vinetaBase = 0.33f;
    [Tooltip("Grano de película cuando todo está tranquilo (0 = nada)")]
    [Range(0f, 1f)] public float granoEnCalma = 0f;
    [Tooltip("Grano que aparece SOLO cuando el monstruo está por entrar")]
    [Range(0f, 1f)] public float granoConPeligro = 0.3f;
    [Range(-100f, 0f)] public float saturacionBase = -18f;
    [Range(0f, 3f)] public float bloom = 0.9f;

    [Header("Estática (solo cuando está a punto de entrar)")]
    public bool estaticaAlPuntoDeEntrar = true;
    [Tooltip("Desde qué avance del ataque empieza (0.7 = cuando ya lleva 70% del tiempo para entrar)")]
    [Range(0.3f, 0.95f)] public float empiezaEstatica = 0.7f;
    [Tooltip("Qué tan fuerte se ve justo antes de entrar")]
    [Range(0f, 0.6f)] public float estaticaMaxima = 0.22f;
    [Range(0f, 1f)] public float volumenEstatica = 0.35f;

    [Header("Miedo (con la respiración agitada)")]
    [Tooltip("Grados del temblor de la cámara cuando el Capi está frente al monstruo")]
    [Range(0f, 3f)] public float temblorMiedo = 1.1f;
    [Tooltip("La cámara sube y baja con el jadeo")]
    [Range(0f, 2f)] public float jadeoCamara = 0.7f;
    [Tooltip("Sacudidas bruscas de susto de vez en cuando")]
    public bool sacudidasDeMiedo = true;
    [Tooltip("Cuánto se cierra la vista (visión de túnel)")]
    [Range(0f, 12f)] public float cierreVisionMiedo = 5f;

    [Header("Cámara")]
    [Tooltip("Grados máximos de temblor con un golpe fuerte")]
    public float temblorMaximo = 3.5f;
    [Tooltip("Cuánto se cierra el campo de visión con el peligro")]
    public float cierreVision = 6f;

    [Header("Relámpagos")]
    public bool relampagos = true;
    public Vector2 esperaRelampago = new Vector2(25f, 60f);
    [Range(0f, 1f)] public float probabilidadSilueta = 0.35f;
    public Color colorRelampago = new Color(0.75f, 0.85f, 1f);
    public float intensidadRelampago = 25f;

    [Header("Sustos de ambiente")]
    public Vector2 esperaSustos = new Vector2(14f, 32f);
    public AudioClip[] sustos;
    [Range(0f, 1f)] public float volumenSustos = 0.75f;

    [Header("Sonidos")]
    public AudioClip latido;
    public AudioClip tension;
    public AudioClip respiracion;
    public AudioClip trueno;
    public AudioClip stingerAparicion;
    public AudioClip monstruoHuye;
    public AudioClip estatica;
    [Tooltip("Estática que se repite (la que suena cuando está a punto de entrar)")]
    public AudioClip estaticaLoop;
    [Tooltip("Respiración agitada del Capi frente al monstruo (ventana / chimenea)")]
    public AudioClip respiracionAgitada;
    [Range(0f, 1f)] public float volumenAgitada = 0.9f;
    [Range(0f, 1f)] public float volumenLatido = 0.9f;
    [Range(0f, 1f)] public float volumenTension = 0.45f;
    [Range(0f, 1f)] public float volumenRespiracion = 0.6f;

    public float Peligro => peligro;

    float peligro, trauma, fovBase, fovGolpe, distorsionGolpe, aberracionGolpe, exposicionGolpe;
    float proximoRelampago, proximoSusto, factorEventos = 1f, semilla;
    float aPunto, finRacha, agitado, agitadoHasta, intensidadAgitada, proximaSacudida;
    Vector3 sacudida;
    bool jumpscare, listo, nocheActiva;
    Camera cam;
    Volume volumen;
    VolumeProfile perfil;
    Vignette vineta;
    ChromaticAberration aberracion;
    FilmGrain grano;
    ColorAdjustments color;
    Bloom brillo;
    LensDistortion distorsion;
    AudioSource srcLatido, srcTension, srcRespiracion, src2D, srcEstatica, srcAgitada;
    Light luzRelampago;
    ParticleSystem polvoLinterna, polvoPuerta, hollin, chispas;

    // ===================================================================

    void Awake()
    {
        instancia = this;
        semilla = Random.value * 100f;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
        if (perfil != null) Destroy(perfil);
    }

    void OnEnable()
    {
        EntradaMonstruo.AlEmpezarAtaque += AlAtaque;
        EntradaMonstruo.AlGolpe += AlGolpe;
        EntradaMonstruo.AlAhuyentarlo += AlAhuyentar;
        GestorNoche.AlCambiarHora += AlCambiarHora;
    }

    void OnDisable()
    {
        EntradaMonstruo.AlEmpezarAtaque -= AlAtaque;
        EntradaMonstruo.AlGolpe -= AlGolpe;
        EntradaMonstruo.AlAhuyentarlo -= AlAhuyentar;
        GestorNoche.AlCambiarHora -= AlCambiarHora;
    }

    void Start()
    {
        if (gestor == null) gestor = GestorNoche.instancia != null ? GestorNoche.instancia : FindAnyObjectByType<GestorNoche>();
        if (gestor != null)
        {
            if (jugador == null) jugador = gestor.jugador;
            if (linterna == null) linterna = gestor.linterna;
            if (ui == null) ui = gestor.ui;
        }
        if (jugador == null) jugador = FindAnyObjectByType<ControlJugadorNoche>();
        if (linterna == null) linterna = FindAnyObjectByType<LinternaNoche>();
        cam = jugador != null && jugador.camara != null ? jugador.camara : Camera.main;
        if (cam == null) { enabled = false; return; }
        fovBase = cam.fieldOfView;

        srcLatido = CrearLoop(latido);
        srcTension = CrearLoop(tension);
        srcRespiracion = CrearLoop(respiracion);
        srcEstatica = CrearLoop(estaticaLoop != null ? estaticaLoop : estatica);
        srcAgitada = CrearLoop(respiracionAgitada != null ? respiracionAgitada : respiracion);
        src2D = gameObject.AddComponent<AudioSource>();
        src2D.playOnAwake = false;
        src2D.spatialBlend = 0f;

        if (usarPostProcesado) CrearPostProcesado();
        CrearRelampago();
        CrearParticulas();

        proximoRelampago = Time.time + Random.Range(esperaRelampago.x, esperaRelampago.y) * 0.5f;
        proximoSusto = Time.time + Random.Range(esperaSustos.x, esperaSustos.y);
        listo = true;
    }

    AudioSource CrearLoop(AudioClip clip)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = true;
        s.spatialBlend = 0f;
        s.volume = 0f;
        s.clip = clip;
        return s;
    }

    // ===================================================================
    // Cada frame

    void Update()
    {
        if (!listo) return;
        float dt = Time.deltaTime;
        bool enCurso = gestor != null && gestor.EnCurso;

        if (enCurso && !nocheActiva)
        {
            nocheActiva = true;
            fovBase = cam.fieldOfView;      // el campo de visión con el que empieza la noche
            Reproducir(srcLatido);
            Reproducir(srcTension);
            Reproducir(srcRespiracion);
            Reproducir(srcEstatica);
            Reproducir(srcAgitada);
        }

        // ---- peligro: lo más cerca que está el monstruo de entrar ----
        float objetivo = 0f;
        if (enCurso && gestor.entradas != null)
            foreach (var e in gestor.entradas)
                if (e != null && e.Atacando)
                    objetivo = Mathf.Max(objetivo, e.Progreso * (1f - 0.5f * e.Defensa01));
        if (jumpscare) objetivo = 1f;
        peligro = Mathf.MoveTowards(peligro, objetivo, dt * (objetivo > peligro ? 0.8f : 0.35f));

        // ---- audio ----
        float fade = enCurso || jumpscare ? 1f : 0f;
        if (srcLatido != null)
        {
            srcLatido.volume = Mathf.MoveTowards(srcLatido.volume, volumenLatido * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 1f, peligro)) * fade, dt);
            srcLatido.pitch = 0.85f + 0.5f * peligro;
        }
        if (srcTension != null)
            srcTension.volume = Mathf.MoveTowards(srcTension.volume, volumenTension * (0.25f + 0.75f * peligro) * fade, dt * 0.5f);
        ActualizarAgitada(enCurso, dt);
        if (srcRespiracion != null)
        {
            bool esfuerzo = jugador != null && jugador.VistaActual == VistaNoche.Ventana && enCurso;
            float meta = Mathf.Max(Mathf.InverseLerp(0.45f, 1f, peligro), esfuerzo ? 0.4f : 0f) * (1f - agitado);
            srcRespiracion.volume = Mathf.MoveTowards(srcRespiracion.volume, volumenRespiracion * meta * fade, dt);
        }

        // ---- golpes que se desvanecen ----
        trauma = Mathf.Max(0f, trauma - dt * 1.3f);
        fovGolpe = Mathf.MoveTowards(fovGolpe, 0f, dt * (jumpscare ? 4f : 18f));
        distorsionGolpe = Mathf.MoveTowards(distorsionGolpe, 0f, dt * 1.2f);
        aberracionGolpe = Mathf.MoveTowards(aberracionGolpe, 0f, dt * 1.5f);
        exposicionGolpe = Mathf.MoveTowards(exposicionGolpe, 0f, dt * 6f);

        float bpm = 60f + 70f * peligro;
        float pulso = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * Mathf.PI * 2f * bpm / 60f)), 12f);

        // antes de la noche (cinemática) no se toca la cámara ni el post-procesado
        if (volumen != null) volumen.weight = Mathf.MoveTowards(volumen.weight, nocheActiva ? 1f : 0f, dt * 0.5f);
        if (nocheActiva) AplicarCamara(enCurso);
        AplicarPost(pulso);

        if (linterna != null) linterna.parpadeoExterno = enCurso ? Mathf.Max(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, peligro)) * 0.6f, 0.3f * Miedo) : 0f;
        ActualizarEstatica(enCurso, dt);

        // polvo en el haz de la linterna
        if (polvoLinterna != null)
        {
            bool on = linterna != null && linterna.Encendida;
            var em = polvoLinterna.emission;
            if (em.enabled != on)
            {
                em.enabled = on;
                if (!on) polvoLinterna.Clear();
                else polvoLinterna.Play();
            }
        }

        // hollín mientras intenta bajar por la chimenea
        if (hollin != null && gestor != null && gestor.entradas != null)
        {
            float tasa = 0f;
            foreach (var e in gestor.entradas)
                if (e != null && e.Atacando && e.tipo == TipoEntrada.Chimenea) tasa = 8f + 30f * e.Progreso;
            var em = hollin.emission;
            em.rateOverTime = tasa;
        }

        if (!enCurso) return;

        // ---- eventos ----
        if (relampagos && luzRelampago != null && Time.time >= proximoRelampago)
        {
            proximoRelampago = Time.time + Random.Range(esperaRelampago.x, esperaRelampago.y) * factorEventos;
            StartCoroutine(Relampago(Random.value < probabilidadSilueta));
        }
        if (Time.time >= proximoSusto)
        {
            proximoSusto = Time.time + Random.Range(esperaSustos.x, esperaSustos.y) * factorEventos;
            Susto();
        }
    }

    // Estática de TV (imagen + sonido) SOLO cuando algún monstruo está a punto de entrar.
    // Si lo estás ahuyentando, baja.
    void ActualizarEstatica(bool enCurso, float dt)
    {
        float meta = 0f;
        if (estaticaAlPuntoDeEntrar && enCurso && gestor.entradas != null)
            foreach (var e in gestor.entradas)
                if (e != null && e.Atacando)
                    meta = Mathf.Max(meta, Mathf.InverseLerp(empiezaEstatica, 1f, e.Progreso) * (1f - e.Defensa01));
        aPunto = Mathf.MoveTowards(aPunto, meta, dt * (meta > aPunto ? 1.2f : 2.5f));

        float est = 0f;
        if (aPunto > 0.001f)
        {
            float parpadeo = 0.5f + 0.5f * Mathf.PerlinNoise(Time.time * 7f, semilla + 3f);
            if (Time.time >= finRacha && Random.value < dt * (0.8f + 4f * aPunto))
                finRacha = Time.time + Random.Range(0.05f, 0.2f);          // chispazos de interferencia
            if (Time.time < finRacha) parpadeo = 1.7f;
            est = estaticaMaxima * Mathf.SmoothStep(0f, 1f, aPunto) * parpadeo;
        }
        if (ui != null) ui.EstaticaBase(est);
        if (srcEstatica != null)
        {
            float v = estaticaMaxima > 0f ? Mathf.Clamp01(est / estaticaMaxima) : 0f;
            srcEstatica.volume = volumenEstatica * v;
            srcEstatica.pitch = 0.9f + 0.25f * aPunto;
        }
    }

    // Respiración agitada del Capi: cuando vas (o estás) en la ventana o la chimenea y ahí está el monstruo.
    void ActualizarAgitada(bool enCurso, float dt)
    {
        bool frente = false;
        float intensidad = 0f;
        if (enCurso && jugador != null && gestor.entradas != null)
        {
            VistaNoche hacia = jugador.VistaHacia;
            foreach (var e in gestor.entradas)
            {
                if (e == null || !e.Atacando) continue;
                bool aqui = (e.tipo == TipoEntrada.Ventana && hacia == VistaNoche.Ventana)
                         || (e.tipo == TipoEntrada.Chimenea && hacia == VistaNoche.Chimenea)
                         || (e.tipo == TipoEntrada.Puerta && MirandoPuerta(e, hacia));
                if (!aqui) continue;
                frente = true;
                intensidad = Mathf.Max(intensidad, e.Progreso);
            }
        }
        if (frente)
        {
            agitadoHasta = Time.time + 1.5f;                 // ya que se va, todavía jadea un poco
            intensidadAgitada = intensidad;
        }
        float meta = enCurso && Time.time < agitadoHasta ? 1f : 0f;
        float vel = meta > agitado ? 2.5f : (enCurso ? 0.45f : 3f);
        agitado = Mathf.MoveTowards(agitado, meta, dt * vel);

        if (srcAgitada == null) return;
        srcAgitada.volume = volumenAgitada * agitado * (0.75f + 0.25f * intensidadAgitada);
        float tono = respiracionAgitada != null ? 1f : 1.35f;   // sin el sonido nuevo usa la respiración normal más rápida
        srcAgitada.pitch = tono * (0.95f + 0.15f * intensidadAgitada);
    }

    // La puerta se ahuyenta desde la cama: cuenta si la estás mirando o alumbrando
    bool MirandoPuerta(EntradaMonstruo e, VistaNoche hacia)
    {
        if (hacia != VistaNoche.Cama || cam == null) return false;
        if (e.Defensa01 > 0.05f) return true;
        Transform obj = e.puntoMirar != null ? e.puntoMirar : e.transform;
        return Vector3.Angle(cam.transform.forward, obj.position - cam.transform.position) < 30f;
    }

    /// <summary>0..1: qué tanto miedo tiene el Capi ahorita (frente al monstruo).</summary>
    public float Miedo => agitado * (0.6f + 0.4f * intensidadAgitada);

    void AplicarCamara(bool enCurso)
    {
        if (cam == null) return;
        float t = Time.time * 22f;
        float s = trauma * trauma + (enCurso ? peligro * peligro * 0.08f : 0f);
        Vector3 ruido = new Vector3(
            Mathf.PerlinNoise(semilla, t) - 0.5f,
            Mathf.PerlinNoise(semilla + 1f, t) - 0.5f,
            Mathf.PerlinNoise(semilla + 2f, t) - 0.5f);
        Vector3 rot = ruido * 2f * temblorMaximo * s;
        rot.z += Mathf.Sin(Time.time * 0.45f) * 0.35f * (1f + peligro);   // balanceo lento
        Vector3 pos = new Vector3(ruido.y, ruido.z, ruido.x) * 0.05f * s;

        // ---- MIEDO: frente al monstruo con la respiración agitada ----
        float miedo = enCurso ? Miedo : 0f;
        float dt = Time.deltaTime;
        if (miedo > 0.001f)
        {
            // 1) tiembla todo el cuerpo (rápido y fino)
            float tf = Time.time * 32f;
            Vector3 tiembla = new Vector3(
                Mathf.PerlinNoise(semilla + 10f, tf) - 0.5f,
                Mathf.PerlinNoise(semilla + 11f, tf) - 0.5f,
                Mathf.PerlinNoise(semilla + 12f, tf) - 0.5f) * 2f;
            rot += tiembla * temblorMiedo * miedo;
            pos += new Vector3(tiembla.y, tiembla.x, 0f) * 0.004f * miedo;

            // 2) jadeo: la cabeza sube y baja al ritmo de la respiración
            float jadeo = Mathf.Sin(Time.time * Mathf.PI * 2f * (1.5f + 0.4f * intensidadAgitada));
            rot.x += jadeo * jadeoCamara * miedo;
            pos.y += jadeo * 0.012f * jadeoCamara * miedo;

            // 3) sacudidas de susto (se encoge de golpe)
            if (sacudidasDeMiedo && Time.time >= proximaSacudida)
            {
                proximaSacudida = Time.time + Random.Range(1.4f, 3.2f) * Mathf.Lerp(1.2f, 0.6f, intensidadAgitada);
                sacudida = new Vector3(Random.Range(1.5f, 3f), Random.Range(-2.5f, 2.5f), Random.Range(-2f, 2f)) * miedo;
                fovGolpe -= 1.5f * miedo;
            }
        }
        sacudida = Vector3.Lerp(sacudida, Vector3.zero, 1f - Mathf.Exp(-7f * dt));
        rot += sacudida;

        if (jugador != null)
        {
            jugador.efectoPosicion = enCurso ? pos : Vector3.zero;
            jugador.efectoRotacion = enCurso ? rot : Vector3.zero;
        }

        float jadeoFov = Mathf.Sin(Time.time * Mathf.PI * 2f * 1.6f) * 0.8f * miedo;
        float meta = fovBase - (enCurso ? cierreVision * peligro + cierreVisionMiedo * miedo : 0f) + fovGolpe + jadeoFov;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, meta, 1f - Mathf.Exp(-10f * Time.deltaTime));
    }

    void AplicarPost(float pulso)
    {
        if (vineta == null) return;
        float rojo = jumpscare ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, peligro)) * 0.7f;
        float miedo = nocheActiva && gestor != null && gestor.EnCurso ? Miedo : 0f;
        float latidoMiedo = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * Mathf.PI * 2f * 1.6f)), 6f) * miedo;
        vineta.intensity.value = Mathf.Clamp01(vinetaBase + 0.22f * peligro + 0.07f * pulso * peligro + 0.14f * miedo + 0.08f * latidoMiedo + (jumpscare ? 0.25f : 0f));
        vineta.color.value = Color.Lerp(Color.black, new Color(0.4f, 0f, 0f), rojo);
        aberracion.intensity.value = Mathf.Clamp01(0.06f + 0.5f * peligro + 0.3f * miedo + 0.2f * latidoMiedo + aberracionGolpe);
        float g = Mathf.Clamp01(granoEnCalma + granoConPeligro * Mathf.InverseLerp(0.5f, 1f, peligro) + (jumpscare ? 0.6f : 0f));
        grano.intensity.value = g;
        grano.active = g > 0.001f;      // en calma: sin grano
        color.saturation.value = saturacionBase - 22f * peligro - 15f * miedo + (jumpscare ? 30f : 0f);
        color.contrast.value = 12f + 12f * peligro;
        color.postExposure.value = -0.15f + exposicionGolpe;
        distorsion.intensity.value = Mathf.Clamp(distorsionGolpe - 0.07f * pulso * peligro - 0.1f * latidoMiedo, -1f, 1f);
    }

    // ===================================================================
    // Reacciones

    void AlAtaque(EntradaMonstruo e)
    {
        trauma = Mathf.Min(1f, trauma + 0.12f);
        if (stingerAparicion != null && Random.value < 0.4f) src2D.PlayOneShot(stingerAparicion, 0.35f);
        // cuando llega a la ventana, a veces un relámpago lo revela
        if (e.tipo == TipoEntrada.Ventana && luzRelampago != null && Random.value < 0.5f)
            StartCoroutine(Relampago(false, Random.Range(1f, 3f)));
    }

    void AlGolpe(EntradaMonstruo e, float fuerza)
    {
        trauma = Mathf.Min(1f, trauma + 0.25f * fuerza);
        fovGolpe += 2.5f * fuerza;
        if (linterna != null && Random.value < 0.7f) linterna.Golpe(0.15f + 0.2f * fuerza);

        if (e.tipo == TipoEntrada.Puerta && polvoPuerta != null)
        {
            polvoPuerta.transform.position = e.PuntoEfectos;
            polvoPuerta.Emit(Mathf.RoundToInt(30 * fuerza) + 10);
        }
        else if (e.tipo == TipoEntrada.Chimenea && hollin != null)
        {
            hollin.transform.position = e.PuntoEfectos;
            hollin.Emit(Mathf.RoundToInt(40 * fuerza) + 10);
        }
    }

    void AlAhuyentar(EntradaMonstruo e)
    {
        trauma = Mathf.Min(1f, trauma + 0.2f);
        if (monstruoHuye != null) AudioSource.PlayClipAtPoint(monstruoHuye, e.transform.position + Vector3.up * 1.5f, 0.9f);
        if (e.tipo == TipoEntrada.Chimenea && chispas != null)
        {
            chispas.transform.position = e.PuntoEfectos - Vector3.up * 0.6f;
            chispas.Emit(70);
        }
    }

    void AlCambiarHora(int h)
    {
        factorEventos = Mathf.Lerp(1f, 0.55f, h / 5f);   // cada hora pasan más cosas
        trauma = Mathf.Min(1f, trauma + 0.1f);
        if (Random.value < 0.5f) proximoRelampago = Time.time + Random.Range(2f, 5f);
    }

    public void EmpezarJumpscare()
    {
        jumpscare = true;
        trauma = 1f;
        fovGolpe = 16f;
        distorsionGolpe = -0.55f;
        aberracionGolpe = 1f;
        exposicionGolpe = 0.6f;
    }

    public void TerminarJumpscare()
    {
        jumpscare = false;
        if (estatica != null) src2D.PlayOneShot(estatica, 0.8f);
    }

    // ===================================================================
    // Relámpagos y sustos

    IEnumerator Relampago(bool silueta, float espera = 0f)
    {
        if (espera > 0f) yield return new WaitForSeconds(espera);
        if (luzRelampago == null) yield break;

        EntradaMonstruo ventana = Buscar(TipoEntrada.Ventana);
        bool mostrar = silueta && ventana != null && !ventana.Atacando && ventana.monstruo != null
                       && !ventana.monstruo.gameObject.activeSelf;
        if (mostrar) ventana.monstruo.MostrarSilueta();

        yield return Destello(1f, 0.06f);
        yield return new WaitForSeconds(0.07f);
        yield return Destello(0.6f, 0.05f);
        yield return new WaitForSeconds(0.1f);
        if (mostrar && stingerAparicion != null) src2D.PlayOneShot(stingerAparicion, 0.7f);
        yield return Destello(1.2f, 0.35f);

        if (mostrar && ventana.monstruo != null && !ventana.Atacando) ventana.monstruo.Ocultar();

        yield return new WaitForSeconds(Random.Range(0.6f, 2.2f));
        if (trueno != null) src2D.PlayOneShot(trueno, 0.85f);
        trauma = Mathf.Min(1f, trauma + 0.18f);
    }

    IEnumerator Destello(float fuerza, float duracion)
    {
        luzRelampago.enabled = true;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float k = 1f - t / duracion;
            luzRelampago.intensity = intensidadRelampago * fuerza * k;
            exposicionGolpe = Mathf.Max(exposicionGolpe, 0.9f * fuerza * k);
            yield return null;
        }
        luzRelampago.enabled = false;
    }

    void Susto()
    {
        if (sustos == null || sustos.Length == 0 || cam == null) return;
        var clip = sustos[Random.Range(0, sustos.Length)];
        if (clip == null) return;
        Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
        Vector3 pos = cam.transform.position + dir * Random.Range(3f, 6f) + Vector3.up * Random.Range(-0.5f, 2.5f);
        AudioSource.PlayClipAtPoint(clip, pos, volumenSustos);
        if (linterna != null && Random.value < 0.3f) linterna.Golpe(0.4f);
        trauma = Mathf.Min(1f, trauma + 0.06f);
    }

    EntradaMonstruo Buscar(TipoEntrada tipo)
    {
        if (gestor == null || gestor.entradas == null) return null;
        foreach (var e in gestor.entradas)
            if (e != null && e.tipo == tipo) return e;
        return null;
    }

    // ===================================================================
    // Construcción

    void CrearPostProcesado()
    {
        var datos = cam.GetUniversalAdditionalCameraData();
        if (datos != null) datos.renderPostProcessing = true;

        var go = new GameObject("Volumen_Noche");
        go.transform.SetParent(transform, false);
        volumen = go.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 50f;
        volumen.weight = 0f;
        perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        volumen.profile = perfil;

        vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(vinetaBase);
        vineta.smoothness.Override(0.45f);
        vineta.color.Override(Color.black);

        aberracion = perfil.Add<ChromaticAberration>(true);
        aberracion.intensity.Override(0.06f);

        grano = perfil.Add<FilmGrain>(true);
        grano.type.Override(FilmGrainLookup.Thin2);
        grano.intensity.Override(granoEnCalma);
        grano.response.Override(0.8f);
        grano.active = granoEnCalma > 0.001f;

        color = perfil.Add<ColorAdjustments>(true);
        color.saturation.Override(saturacionBase);
        color.contrast.Override(12f);
        color.postExposure.Override(-0.15f);

        brillo = perfil.Add<Bloom>(true);
        brillo.intensity.Override(bloom);
        brillo.threshold.Override(1f);
        brillo.scatter.Override(0.6f);

        distorsion = perfil.Add<LensDistortion>(true);
        distorsion.intensity.Override(0f);
    }

    void CrearRelampago()
    {
        EntradaMonstruo ventana = Buscar(TipoEntrada.Ventana);
        if (ventana == null) return;
        Vector3 adentro = ventana.transform.forward;
        Vector3 centro = ventana.transform.position + Vector3.up * 1.9f;   // la cabeza queda a la altura de la ventana
        var go = new GameObject("Luz_Relampago");
        go.transform.SetParent(transform, false);
        go.transform.position = centro - adentro * 4f + Vector3.up * 1.5f;
        go.transform.rotation = Quaternion.LookRotation((adentro - Vector3.up * 0.35f).normalized);
        luzRelampago = go.AddComponent<Light>();
        luzRelampago.type = LightType.Spot;
        luzRelampago.spotAngle = 75f;
        luzRelampago.range = 25f;
        luzRelampago.color = colorRelampago;
        luzRelampago.shadows = LightShadows.Hard;     // marca la sombra de la ventana y del monstruo
        luzRelampago.intensity = 0f;
        luzRelampago.enabled = false;
    }

    void CrearParticulas()
    {
        Material polvo = materialPolvo != null ? materialPolvo : MaterialRespaldo(false);
        Material aditivo = materialAditivo != null ? materialAditivo : MaterialRespaldo(true);

        // polvo flotando en el haz de la linterna
        if (linterna != null)
        {
            polvoLinterna = Particulas("Polvo_Linterna", linterna.transform, polvo, false);
            polvoLinterna.transform.localPosition = new Vector3(0f, 0f, 2.2f);
            var main = polvoLinterna.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.016f);
            main.startColor = new Color(1f, 0.97f, 0.9f, 0.45f);
            main.maxParticles = 250;
            var em = polvoLinterna.emission;
            em.rateOverTime = 18f;
            em.enabled = false;
            var forma = polvoLinterna.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(1.4f, 1.4f, 3.2f);
            var ruido = polvoLinterna.noise;
            ruido.enabled = true;
            ruido.strength = 0.04f;
            ruido.frequency = 0.25f;
            polvoLinterna.Play();
        }

        // polvo que cae del marco de la puerta en cada empujón
        polvoPuerta = Particulas("Polvo_Puerta", transform, polvo, true);
        {
            var main = polvoPuerta.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
            main.startColor = new Color(0.55f, 0.5f, 0.45f, 0.6f);
            main.gravityModifier = 0.12f;
            var forma = polvoPuerta.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(1.1f, 0.05f, 0.15f);
        }

        // hollín de la chimenea
        hollin = Particulas("Hollin_Chimenea", transform, polvo, false);
        {
            var main = hollin.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(0.08f, 0.07f, 0.06f, 0.85f);
            main.gravityModifier = 0.25f;
            var forma = hollin.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(0.7f, 0.05f, 0.4f);
            var em = hollin.emission;
            em.rateOverTime = 0f;
            EntradaMonstruo chimenea = Buscar(TipoEntrada.Chimenea);
            if (chimenea != null) hollin.transform.position = chimenea.PuntoEfectos;
            hollin.Play();
        }

        // chispas cuando el fuego lo ahuyenta
        chispas = Particulas("Chispas", transform, aditivo, true);
        {
            var main = chispas.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.35f, 0.05f));
            main.gravityModifier = 0.6f;
            var forma = chispas.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = 35f;
            forma.radius = 0.2f;
            chispas.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        }
    }

    ParticleSystem Particulas(string nombre, Transform padre, Material mat, bool soloRafagas)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        var em = ps.emission;
        em.rateOverTime = 0f;
        if (soloRafagas) em.enabled = true;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        if (soloRafagas) ps.Play();
        return ps;
    }

    static Material MaterialRespaldo(bool aditivo)
    {
        var s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) return null;
        var m = new Material(s);
        if (aditivo)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }
        else
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }

    static void Reproducir(AudioSource s)
    {
        if (s != null && s.clip != null && !s.isPlaying) s.Play();
    }
}
