using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum VistaNoche { Cama, Ventana, Chimenea }
public enum TipoEntrada { Ventana, Puerta, Chimenea }

/// <summary>
/// Cerebro de la noche estilo FNAF: reloj de 12 AM a 6 AM, dificultad por hora,
/// decide cuándo y por dónde intenta entrar el monstruo, jumpscare y final.
/// </summary>
public class GestorNoche : MonoBehaviour
{
    public static GestorNoche instancia;

    [Header("Inicio")]
    [Tooltip("Desmárcalo si la noche empieza después de una cinemática: llama a IniciarNoche() desde un evento o Signal")]
    public bool iniciarAlEmpezar = true;
    public float esperaAntesDeEmpezar = 0.5f;
    public string tituloInicio = "SOBREVIVE HASTA LAS 6 AM";

    [Header("Duración")]
    [Tooltip("Segundos reales que dura cada hora (6 horas: de 12 AM a 6 AM). 60 = 6 minutos de noche")]
    public float segundosPorHora = 60f;

    [Header("Dificultad (una casilla por hora: 12, 1, 2, 3, 4 y 5 AM)")]
    [Tooltip("Segundos sin ataques al empezar la noche")]
    public float graciaInicial = 15f;
    [Tooltip("Segundos entre un intento y otro (mínimo, máximo)")]
    public Vector2[] esperaEntreAtaques =
    {
        new Vector2(22f, 32f), new Vector2(18f, 27f), new Vector2(15f, 23f),
        new Vector2(12f, 19f), new Vector2(10f, 16f), new Vector2(8f, 13f)
    };
    [Tooltip("Segundos que tienes para defender antes de que entre")]
    public float[] tiempoParaEntrar = { 14f, 12.5f, 11f, 10f, 9f, 8f };
    [Tooltip("Cuántas entradas puede atacar al mismo tiempo")]
    public int[] ataquesALaVez = { 1, 1, 1, 2, 2, 2 };

    [Header("Referencias (las llena la herramienta)")]
    public ControlJugadorNoche jugador;
    public LinternaNoche linterna;
    public FuegoChimenea fuego;
    public EntradaMonstruo[] entradas;
    public UINoche ui;

    [Header("Jumpscare")]
    [Tooltip("El modelo que salta a la cámara. Vacío = usa el monstruo de la entrada por donde se metió")]
    public GameObject prefabJumpscare;
    public AudioClip sonidoJumpscare;
    [Range(0f, 1f)] public float volumenJumpscare = 1f;
    public float duracionJumpscare = 1.5f;

    [Header("Sonidos")]
    public AudioClip ambiente;
    [Range(0f, 1f)] public float volumenAmbiente = 0.5f;
    public AudioClip sonidoHora;
    public AudioClip sonidoVictoria;

    [Header("Al ganar (6 AM)")]
    [Tooltip("Escena a la que va al sobrevivir. Vacío = se queda aquí (usa el evento Al Ganar)")]
    public string escenaAlGanar = "";
    public float esperaAlGanar = 6f;
    public UnityEvent alGanar;

    [Header("Al perder")]
    public bool reiniciarAlPerder = true;
    [Tooltip("Vacío = reinicia esta misma escena")]
    public string escenaAlPerder = "";
    public float esperaAlPerder = 3.5f;
    public UnityEvent alPerder;

    [Header("Otros")]
    public UnityEvent alIniciar;
    [Tooltip("Solo en el editor: F1 ventana, F2 puerta, F3 chimenea, F4 adelantar a las 5:50 AM")]
    public bool teclasDePrueba = true;

    public bool EnCurso => enCurso;
    public int Hora => hora;

    bool enCurso, terminado;
    float tiempo, proximoAtaque;
    int hora;
    EntradaMonstruo ultima;
    AudioSource fuenteAmbiente, fuente2D;

    void Awake()
    {
        instancia = this;

        fuenteAmbiente = gameObject.AddComponent<AudioSource>();
        fuenteAmbiente.loop = true;
        fuenteAmbiente.playOnAwake = false;
        fuenteAmbiente.spatialBlend = 0f;

        fuente2D = gameObject.AddComponent<AudioSource>();
        fuente2D.playOnAwake = false;
        fuente2D.spatialBlend = 0f;

        if (ui == null) ui = GetComponent<UINoche>();
        if (ui == null) ui = gameObject.AddComponent<UINoche>();
        if (jugador == null) jugador = FindAnyObjectByType<ControlJugadorNoche>();
        if (linterna == null) linterna = FindAnyObjectByType<LinternaNoche>();
        if (fuego == null) fuego = FindAnyObjectByType<FuegoChimenea>();
        if (entradas == null || entradas.Length == 0)
            entradas = FindObjectsByType<EntradaMonstruo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    IEnumerator Start()
    {
        if (!iniciarAlEmpezar) yield break;
        yield return new WaitForSeconds(esperaAntesDeEmpezar);
        IniciarNoche();
    }

    // ===================================================================

    /// <summary>Empieza la noche. Llámalo desde un evento o Signal si "Iniciar Al Empezar" está apagado.</summary>
    public void IniciarNoche()
    {
        if (enCurso || terminado) return;

        enCurso = true;
        tiempo = 0f;
        hora = 0;
        proximoAtaque = graciaInicial;

        if (jugador != null) jugador.Activar();

        ui.Mostrar(true);
        ui.MostrarLena(fuego != null);
        ui.PonerHora(0);
        ui.MostrarIntro(tituloInicio, "12 AM");

        if (ambiente != null)
        {
            fuenteAmbiente.clip = ambiente;
            fuenteAmbiente.volume = volumenAmbiente;
            fuenteAmbiente.Play();
        }

        alIniciar.Invoke();
    }

    void Update()
    {
        if (!enCurso) return;

        PruebasEditor();

        tiempo += Time.deltaTime;
        int h = Mathf.FloorToInt(tiempo / Mathf.Max(1f, segundosPorHora));
        if (h != hora)
        {
            hora = h;
            if (hora >= 6)
            {
                StartCoroutine(Ganar());
                return;
            }
            ui.PonerHora(hora);
            if (sonidoHora != null) fuente2D.PlayOneShot(sonidoHora, 0.8f);
        }

        ActualizarUI();

        proximoAtaque -= Time.deltaTime;
        if (proximoAtaque <= 0f)
        {
            LanzarAtaque();
            Vector2 r = Valor(esperaEntreAtaques, hora, new Vector2(15f, 25f));
            proximoAtaque = Random.Range(r.x, r.y);
        }
    }

    static T Valor<T>(T[] lista, int i, T porDefecto)
    {
        if (lista == null || lista.Length == 0) return porDefecto;
        return lista[Mathf.Clamp(i, 0, lista.Length - 1)];
    }

    void LanzarAtaque()
    {
        int activos = 0;
        var libres = new List<EntradaMonstruo>();
        foreach (var e in entradas)
        {
            if (e == null || !e.isActiveAndEnabled) continue;
            if (e.Atacando) activos++;
            else if (e.Disponible) libres.Add(e);
        }

        if (activos >= Valor(ataquesALaVez, hora, 1) || libres.Count == 0) return;
        if (libres.Count > 1) libres.Remove(ultima);   // que no repita la misma entrada seguido

        var elegida = libres[Random.Range(0, libres.Count)];
        ultima = elegida;
        elegida.IniciarAtaque(Valor(tiempoParaEntrar, hora, 10f));
    }

    void ActualizarUI()
    {
        if (linterna != null) ui.PonerPila(linterna.Bateria01, linterna.Encendida);
        if (fuego != null) ui.PonerLena(fuego.Lena01, fuego.Encendido);
        if (jugador == null) return;

        ui.PonerMira(jugador.Apuntando != null);

        string pista = "";
        if (!jugador.EnTransicion)
        {
            switch (jugador.VistaActual)
            {
                case VistaNoche.Cama:
                    pista = (jugador.Apuntando != null ? "[Clic] Ir a la " + jugador.Apuntando.nombre + "        " : "")
                            + "[F] Linterna";
                    break;
                case VistaNoche.Ventana:
                    pista = "[Espacio] Mantén para cerrar las cortinas        [S / Clic derecho] Volver a la cama";
                    break;
                case VistaNoche.Chimenea:
                    pista = (fuego != null && fuego.Encendido ? "[Espacio] Apagar el fuego" : "[Espacio] Encender la leña")
                            + "        [S / Clic derecho] Volver a la cama";
                    break;
            }
        }
        ui.PonerPista(pista);
    }

    // ===================================================================

    /// <summary>Lo llama una entrada cuando se le acabó el tiempo y el monstruo se metió.</summary>
    public void MonstruoEntro(EntradaMonstruo culpable)
    {
        if (!enCurso) return;
        StartCoroutine(Perder(culpable));
    }

    IEnumerator Perder(EntradaMonstruo culpable)
    {
        enCurso = false;
        terminado = true;

        foreach (var e in entradas)
            if (e != null && e != culpable) e.Cancelar(false);

        if (jugador != null) jugador.Bloquear(true);
        if (linterna != null) linterna.Apagar();
        fuenteAmbiente.Stop();
        ui.PonerPista("");
        ui.PonerMira(false);
        ui.Mostrar(false);

        if (culpable != null && culpable.sonidoEntra != null) fuente2D.PlayOneShot(culpable.sonidoEntra, 1f);
        yield return new WaitForSeconds(0.35f);

        yield return Jumpscare(culpable);

        string como = "";
        if (culpable != null)
        {
            switch (culpable.tipo)
            {
                case TipoEntrada.Ventana: como = "Entró por la ventana"; break;
                case TipoEntrada.Puerta: como = "Entró por la puerta"; break;
                case TipoEntrada.Chimenea: como = "Bajó por la chimenea"; break;
            }
        }
        ui.MostrarFinal("TE ATRAPÓ", como, new Color(0.8f, 0.12f, 0.09f));
        alPerder.Invoke();

        yield return new WaitForSecondsRealtime(esperaAlPerder);

        if (reiniciarAlPerder)
            Cambiar(string.IsNullOrEmpty(escenaAlPerder) ? SceneManager.GetActiveScene().name : escenaAlPerder);
    }

    IEnumerator Jumpscare(EntradaMonstruo culpable)
    {
        Camera cam = jugador != null && jugador.camara != null ? jugador.camara : Camera.main;
        if (culpable != null) culpable.OcultarMonstruo();
        if (cam == null) yield break;

        GameObject molde = prefabJumpscare;
        if (molde == null && culpable != null && culpable.monstruo != null) molde = culpable.monstruo.gameObject;

        GameObject m = null;
        Transform cabeza = null;
        if (molde != null)
        {
            m = Instantiate(molde);
            m.SetActive(true);
            var mv = m.GetComponent<MonstruoVisual>();
            if (mv != null)
            {
                cabeza = mv.cabeza;
                mv.ModoJumpscare();
            }
        }

        if (sonidoJumpscare != null) fuente2D.PlayOneShot(sonidoJumpscare, volumenJumpscare);
        ui.FlashRojo();

        Transform ct = cam.transform;

        // luz para que se le vea la cara aunque el cuarto esté oscuro
        var goLuz = new GameObject("LuzJumpscare");
        var luzCara = goLuz.AddComponent<Light>();
        luzCara.type = LightType.Point;
        luzCara.color = new Color(1f, 0.6f, 0.55f);
        luzCara.range = 2.5f;
        luzCara.intensity = 2.5f;
        goLuz.transform.position = ct.position + ct.forward * 0.15f + Vector3.up * 0.25f;

        Vector3 posCam = ct.position;
        Quaternion rotCam = ct.rotation;
        float nearOriginal = cam.nearClipPlane;
        cam.nearClipPlane = 0.02f;

        float t = 0f;
        while (t < duracionJumpscare)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 0.18f);                       // embestida rápida
            float distancia = Mathf.Lerp(1.8f, 0.42f, 1f - (1f - k) * (1f - k));

            if (m != null)
            {
                Vector3 adelante = rotCam * Vector3.forward;
                m.transform.rotation = Quaternion.LookRotation(-adelante, Vector3.up);
                Vector3 desfase = cabeza != null ? cabeza.position - m.transform.position : Vector3.up * 1.9f;
                m.transform.position = posCam + adelante * distancia - desfase + Random.insideUnitSphere * 0.015f;
            }

            luzCara.intensity = 2.5f * (Random.value > 0.25f ? 1f : 0.3f);
            float temblor = Mathf.Lerp(0.06f, 0.015f, t / duracionJumpscare);
            ct.position = posCam + Random.insideUnitSphere * temblor;
            ct.rotation = rotCam * Quaternion.Euler(Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-4f, 4f));
            yield return null;
        }

        ui.Negro(1f);
        if (m != null) Destroy(m);
        Destroy(goLuz);
        ct.SetPositionAndRotation(posCam, rotCam);
        cam.nearClipPlane = nearOriginal;
    }

    IEnumerator Ganar()
    {
        enCurso = false;
        terminado = true;

        foreach (var e in entradas)
            if (e != null) e.Cancelar(true);

        if (jugador != null) jugador.Bloquear(true);
        if (linterna != null) linterna.Apagar();

        ui.PonerHora(6);
        ui.PonerPista("");
        ui.PonerMira(false);
        ui.Mostrar(false);
        if (sonidoVictoria != null) fuente2D.PlayOneShot(sonidoVictoria, 1f);
        ui.MostrarFinal("6 AM", "Sobreviviste a la noche", new Color(0.95f, 0.85f, 0.6f));

        // baja el ambiente poco a poco
        float v0 = fuenteAmbiente.volume, t = 0f;
        while (t < 2f)
        {
            t += Time.deltaTime;
            fuenteAmbiente.volume = Mathf.Lerp(v0, 0f, t / 2f);
            yield return null;
        }
        fuenteAmbiente.Stop();

        alGanar.Invoke();
        yield return new WaitForSeconds(Mathf.Max(0f, esperaAlGanar - 2f));

        if (!string.IsNullOrEmpty(escenaAlGanar)) Cambiar(escenaAlGanar);
    }

    // Usa el mismo cambio de escena de siempre
    void Cambiar(string escena)
    {
        if (GestorTransicion.instancia != null) GestorTransicion.instancia.CambiarEscena(escena);
        else SceneManager.LoadScene(escena);
    }

    // ===================================================================
    // Pruebas

    void PruebasEditor()
    {
        if (!teclasDePrueba || !Application.isEditor) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f1Key.wasPressedThisFrame) ForzarAtaque(TipoEntrada.Ventana);
        if (kb.f2Key.wasPressedThisFrame) ForzarAtaque(TipoEntrada.Puerta);
        if (kb.f3Key.wasPressedThisFrame) ForzarAtaque(TipoEntrada.Chimenea);
        if (kb.f4Key.wasPressedThisFrame) AdelantarA550();
    }

    public void ForzarAtaque(TipoEntrada tipo)
    {
        if (!enCurso) return;
        foreach (var e in entradas)
            if (e != null && e.tipo == tipo && !e.Atacando)
                e.IniciarAtaque(Valor(tiempoParaEntrar, hora, 10f));
    }

    [ContextMenu("Probar: ataque por la VENTANA")] void ProbarVentana() => ForzarAtaque(TipoEntrada.Ventana);
    [ContextMenu("Probar: ataque por la PUERTA")] void ProbarPuerta() => ForzarAtaque(TipoEntrada.Puerta);
    [ContextMenu("Probar: ataque por la CHIMENEA")] void ProbarChimenea() => ForzarAtaque(TipoEntrada.Chimenea);

    [ContextMenu("Probar: adelantar a las 5:50 AM")]
    void AdelantarA550()
    {
        if (enCurso) tiempo = Mathf.Max(tiempo, segundosPorHora * 6f - 10f);
    }
}
