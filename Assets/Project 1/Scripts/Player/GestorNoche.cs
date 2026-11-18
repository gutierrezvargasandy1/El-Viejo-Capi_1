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
    [Tooltip("Máximo de monstruos atacando al mismo tiempo (12, 1, 2, 3, 4 y 5 AM)")]
    public int[] maxMonstruosJuntos = { 1, 1, 2, 2, 3, 3 };
    [Tooltip("Probabilidad (0 a 1) de que lleguen 2 a la vez")]
    public float[] probabilidadDoble = { 0f, 0.15f, 0.3f, 0.45f, 0.55f, 0.65f };
    [Tooltip("Probabilidad (0 a 1) de que lleguen 3 a la vez (ventana, puerta y chimenea)")]
    public float[] probabilidadTriple = { 0f, 0f, 0f, 0.1f, 0.2f, 0.3f };
    [Tooltip("Segundos entre que aparece uno y el siguiente cuando llegan juntos")]
    public Vector2 separacionEntreMonstruos = new Vector2(1.5f, 3.5f);
    [Tooltip("Tiempo extra para defender cuando hay varios (0.25 = 25% más) para que sea difícil pero justo")]
    [Range(0f, 1f)] public float tiempoExtraSiSonVarios = 0.25f;

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

    [Header("Pantalla de GAME OVER")]
    [Tooltip("La imagen del Game Over (la pone la herramienta: GameOver_Capi). Vacío = pantalla de texto 'TE ATRAPÓ'")]
    public Texture imagenGameOver;
    [Tooltip("Música deprimente que se repite mientras está la pantalla")]
    public AudioClip musicaGameOver;
    [Tooltip("Golpe grave cuando aparece la pantalla")]
    public AudioClip golpeGameOver;
    [Range(0f, 1f)] public float volumenMusicaGameOver = 0.85f;
    [Tooltip("Segundos antes de que aparezca 'Intentar otra vez'")]
    public float esperaParaContinuar = 3f;
    [Tooltip("0 = espera a que presiones algo. Con segundos, vuelve a intentar solo")]
    public float reintentarSoloDespuesDe = 0f;
    [Tooltip("Escena del menú principal (Esc en el Game Over). Vacío = sin esa opción")]
    public string escenaMenuPrincipal = "MainMenu";

    [Header("Otros")]
    public UnityEvent alIniciar;
    [Tooltip("Solo en el editor: F1 ventana, F2 puerta, F3 chimenea, F4 adelantar a las 5:50 AM, F5 dos a la vez, F6 tres a la vez")]
    public bool teclasDePrueba = true;

    /// <summary>Se llama al cambiar de hora (1 a 5). Lo usa EfectosNoche.</summary>
    public static event System.Action<int> AlCambiarHora;

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
            ui.AnunciarHora(hora);
            if (sonidoHora != null) fuente2D.PlayOneShot(sonidoHora, 0.8f);
            if (AlCambiarHora != null) AlCambiarHora(hora);
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

        int cupo = Valor(maxMonstruosJuntos, hora, 1) - activos;
        if (cupo <= 0 || libres.Count == 0) return;

        // ¿cuántos llegan esta vez? (más tarde en la noche = más seguido llegan varios)
        int cuantos = 1;
        float r = Random.value;
        float triple = Valor(probabilidadTriple, hora, 0f);
        if (r < triple) cuantos = 3;
        else if (r < triple + Valor(probabilidadDoble, hora, 0f)) cuantos = 2;
        cuantos = Mathf.Min(Mathf.Min(cuantos, cupo), libres.Count);

        if (libres.Count > cuantos) libres.Remove(ultima);   // que no repita la misma entrada seguido
        LanzarVarios(libres, cuantos, activos + cuantos > 1);
    }

    // lanza 'cuantos' entradas de la lista: la primera ya, las demás unos segundos después
    void LanzarVarios(List<EntradaMonstruo> libres, int cuantos, bool sonVarios)
    {
        float tiempoBase = Valor(tiempoParaEntrar, hora, 10f) * (sonVarios ? 1f + tiempoExtraSiSonVarios : 1f);
        float espera = 0f;
        for (int i = 0; i < cuantos && libres.Count > 0; i++)
        {
            var elegida = libres[Random.Range(0, libres.Count)];
            libres.Remove(elegida);
            ultima = elegida;
            if (i == 0) elegida.IniciarAtaque(tiempoBase);
            else
            {
                espera += Random.Range(separacionEntreMonstruos.x, separacionEntreMonstruos.y);
                StartCoroutine(LanzarDespues(elegida, espera, tiempoBase));
            }
        }
    }

    IEnumerator LanzarDespues(EntradaMonstruo e, float espera, float duracion)
    {
        yield return new WaitForSeconds(espera);
        if (!enCurso || e == null || e.Atacando || !e.Disponible) yield break;
        e.IniciarAtaque(duracion);
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
        if (imagenGameOver != null)
        {
            yield return GameOver(como);
            yield break;
        }

        ui.MostrarFinal("TE ATRAPÓ", como, new Color(0.8f, 0.12f, 0.09f));
        alPerder.Invoke();

        yield return new WaitForSecondsRealtime(esperaAlPerder);

        if (reiniciarAlPerder)
            Cambiar(string.IsNullOrEmpty(escenaAlPerder) ? SceneManager.GetActiveScene().name : escenaAlPerder);
    }

    // Pantalla de GAME OVER con la imagen y la música; espera a que el jugador elija
    IEnumerator GameOver(string como)
    {
        if (golpeGameOver != null) fuente2D.PlayOneShot(golpeGameOver, 1f);
        AudioSource musica = null;
        if (musicaGameOver != null)
        {
            musica = gameObject.AddComponent<AudioSource>();
            musica.playOnAwake = false;
            musica.loop = true;
            musica.spatialBlend = 0f;
            musica.volume = 0f;
            musica.clip = musicaGameOver;
            musica.Play();
        }
        yield return new WaitForSecondsRealtime(0.4f);
        ui.MostrarGameOver(imagenGameOver, como);
        alPerder.Invoke();

        bool hayMenu = !string.IsNullOrEmpty(escenaMenuPrincipal) && Application.CanStreamedLevelBeLoaded(escenaMenuPrincipal);
        float t = 0f;
        bool puede = false;
        int accion = 0;                  // 1 = otra vez, 2 = menú
        while (accion == 0)
        {
            t += Time.unscaledDeltaTime;
            if (musica != null)
                musica.volume = Mathf.MoveTowards(musica.volume, volumenMusicaGameOver, Time.unscaledDeltaTime / 2.5f);

            if (reiniciarAlPerder)
            {
                if (!puede && t >= esperaParaContinuar)
                {
                    puede = true;
                    MostrarCursor();
                    ui.PromptGameOver("[Clic / Espacio]  Intentar otra vez" + (hayMenu ? "      [Esc]  Menú principal" : ""));
                }
                if (puede)
                {
                    if (PresionoReintentar()) accion = 1;
                    else if (hayMenu && PresionoMenu()) accion = 2;
                }
                if (reintentarSoloDespuesDe > 0f && t >= reintentarSoloDespuesDe) accion = 1;
            }
            yield return null;
        }

        ui.ElegidoGameOver();
        MostrarCursor();                 // el menú necesita el cursor (en la noche estaba bloqueado)
        if (accion == 2) Cambiar(escenaMenuPrincipal);
        else Cambiar(string.IsNullOrEmpty(escenaAlPerder) ? SceneManager.GetActiveScene().name : escenaAlPerder);
    }

    static void MostrarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static bool PresionoReintentar()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame)) return true;
        var m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame) return true;
        var g = Gamepad.current;
        if (g != null && g.buttonSouth.wasPressedThisFrame) return true;
        var ts = Touchscreen.current;
        return ts != null && ts.primaryTouch.press.wasPressedThisFrame;
    }

    static bool PresionoMenu()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame)) return true;   // en Android el botón Atrás es Escape
        var g = Gamepad.current;
        return g != null && g.buttonEast.wasPressedThisFrame;
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
        if (EfectosNoche.instancia != null) EfectosNoche.instancia.EmpezarJumpscare();

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
        ui.Estatica(0.55f, 0.6f);
        if (EfectosNoche.instancia != null) EfectosNoche.instancia.TerminarJumpscare();
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
        if (kb.f5Key.wasPressedThisFrame) ForzarVarios(2);
        if (kb.f6Key.wasPressedThisFrame) ForzarVarios(3);
    }

    /// <summary>Prueba: llegan varios monstruos a la vez por entradas distintas.</summary>
    public void ForzarVarios(int cuantos)
    {
        if (!enCurso) return;
        var libres = new List<EntradaMonstruo>();
        foreach (var e in entradas)
            if (e != null && e.isActiveAndEnabled && !e.Atacando) libres.Add(e);
        LanzarVarios(libres, Mathf.Min(cuantos, libres.Count), true);
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

    [ContextMenu("Probar: DOS a la vez")] void ProbarDos() => ForzarVarios(2);
    [ContextMenu("Probar: TRES a la vez")] void ProbarTres() => ForzarVarios(3);

    [ContextMenu("Probar: adelantar a las 5:50 AM")]
    void AdelantarA550()
    {
        if (enCurso) tiempo = Mathf.Max(tiempo, segundosPorHora * 6f - 10f);
    }
}
