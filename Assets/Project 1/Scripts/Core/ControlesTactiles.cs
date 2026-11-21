using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.EnhancedTouch;
using TMPro;
using Toque = UnityEngine.InputSystem.EnhancedTouch.Touch;
using FaseToque = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Controles para celular (Android). Se crea solo cuando el juego corre en un teléfono
/// (o en el Device Simulator de Unity). No hay que ponerlo en ninguna escena.
///
/// Cómo funciona: crea un TECLADO y un MOUSE "virtuales" y los aprieta con los dedos.
/// Así todos los scripts del juego (que leen WASD, E, F, Espacio, S, Esc, clic y mover el mouse)
/// funcionan igual sin cambiarles nada:
///   • Joystick (izquierda)        → W A S D  (sacar el dedo más allá del círculo = correr con Shift)
///   • Arrastrar el dedo (derecha) → mover el mouse (mirar)
///   • Tocar la pantalla           → clic izquierdo
///   • Botones                     → USAR (E), SALTAR (Espacio), LINTERNA (F), ACCIÓN (Espacio),
///                                   VOLVER (S), MENÚ (M), OMITIR (Espacio), PAUSA (Esc)
/// Los botones cambian solos según lo que pasa en la escena (caminar, la noche, cinemática, game over).
/// También cambia los textos "Presiona E" → "Toca USAR", "[Clic]" → "[Toca]", etc.
/// </summary>
[DefaultExecutionOrder(-900)]
public class ControlesTactiles : MonoBehaviour
{
    public static ControlesTactiles instancia;
    /// <summary>true cuando el juego se está jugando con los controles táctiles.</summary>
    public static bool Activo => instancia != null;

    const string ClaveSensibilidad = "SensibilidadTactil";
    const string ClaveForzar = "ForzarControlesTactiles";

    /// <summary>Qué tan rápido se mira al arrastrar el dedo (1 = normal).</summary>
    public static float Sensibilidad
    {
        get { return PlayerPrefs.GetFloat(ClaveSensibilidad, 1.2f); }
        set { PlayerPrefs.SetFloat(ClaveSensibilidad, Mathf.Clamp(value, 0.3f, 3f)); }
    }

    enum Modo { Nada, Caminar, Lateral, Noche, Cinematica, GameOver }
    enum Dueno { Nadie, Joystick, Boton, Mirar, Interfaz }

    class Boton
    {
        public string nombre;
        public RectTransform rt;
        public Image fondo, aro;
        public TextMeshProUGUI texto;
        public Key tecla;
        public bool redondo = true;
        public bool visible;
        public bool presionado;
        public int dedo = -1;
        public int frameApretado;
        public bool soltarPendiente;
    }

    class Dedo
    {
        public Dueno dueno;
        public Boton boton;
        public Vector2 inicio;
        public double tiempoInicio;
        public float recorrido;
    }

    // ---------------------------------------------------------------- arranque
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { instancia = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if (instancia != null || !EsCelular()) return;
        var go = new GameObject("Controles Tactiles");
        DontDestroyOnLoad(go);
        go.AddComponent<ControlesTactiles>();
    }

    /// <summary>¿Se juega en celular? (también true en el Device Simulator del editor)</summary>
    public static bool EsCelular()
    {
        if (Application.isEditor && PlayerPrefs.GetInt(ClaveForzar, 0) == 1) return true;
        return UnityEngine.Device.Application.isMobilePlatform;
    }

    // ---------------------------------------------------------------- estado
    Keyboard teclado;
    Mouse raton;
    Canvas canvas;
    CanvasGroup grupo;
    RectTransform raiz, zonaSegura;
    Rect ultimaZonaSegura;

    RectTransform baseJoy, perillaJoy;
    Image imgBaseJoy, imgPerillaJoy;
    Vector2 centroJoy;           // en pixeles de pantalla
    Vector2 vectorJoy;           // -1..1
    bool joyActivo, joyCorrer;

    Boton bUsar, bSaltar, bLinterna, bAccion, bVolver, bPausa, bOmitir, bMenu;
    readonly List<Boton> botones = new List<Boton>();
    readonly Dictionary<int, Dedo> dedos = new Dictionary<int, Dedo>();

    readonly HashSet<Key> teclasAhora = new HashSet<Key>();
    readonly HashSet<Key> teclasAntes = new HashSet<Key>();
    readonly List<Key> listaTeclas = new List<Key>();

    Vector2 deltaMirar;
    Vector2 posRaton;
    int clic;                    // 0 nada, 1 apretar, 2 soltar
    bool ratonSucio;

    Modo modo = Modo.Nada;
    bool oculto = true;
    float siguienteRevision, siguienteTextos;
    Sprite spriteCirculo, spriteAro;
    TMP_FontAsset fuente;
    ControlJugadorNoche noche;
    LinternaNoche linterna;

    readonly List<RaycastResult> resultadosUI = new List<RaycastResult>();
    PointerEventData datosPuntero;
    EventSystem sistemaDelPuntero;

    // ---------------------------------------------------------------- ciclo de vida
    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
        if (Application.isEditor && PlayerPrefs.GetInt(ClaveForzar, 0) == 1 && !UnityEngine.Device.Application.isMobilePlatform)
            TouchSimulation.Enable();   // en el editor: el mouse hace de dedo

        teclado = InputSystem.AddDevice<Keyboard>("TecladoTactil");
        raton = InputSystem.AddDevice<Mouse>("MouseTactil");
        posRaton = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        CrearInterfaz();
        SceneManager.sceneLoaded += AlCargarEscena;
        siguienteRevision = 0f;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
        SceneManager.sceneLoaded -= AlCargarEscena;
        if (teclado != null && teclado.added) InputSystem.RemoveDevice(teclado);
        if (raton != null && raton.added) InputSystem.RemoveDevice(raton);
    }

    void OnApplicationPause(bool pausa)
    {
        if (pausa) SoltarTodo();
    }

    void AlCargarEscena(Scene s, LoadSceneMode m)
    {
        SoltarTodo();
        noche = null;
        linterna = null;
        siguienteRevision = 0f;
        siguienteTextos = 0f;
    }

    void Update()
    {
        float ahora = Time.unscaledTime;
        if (ahora >= siguienteRevision)
        {
            siguienteRevision = ahora + 0.25f;
            modo = DetectarModo();
        }
        if (ahora >= siguienteTextos)
        {
            siguienteTextos = ahora + 0.5f;
            TextoTactil.AplicarATodos();
        }

        bool ocultar = modo == Modo.Nada || DebeOcultarse();
        if (ocultar != oculto)
        {
            oculto = ocultar;
            grupo.alpha = oculto ? 0f : 1f;
            if (oculto) SoltarTodo();
        }
        canvas.sortingOrder = modo == Modo.Cinematica ? 1001 : 600;   // encima de los videos (999)

        AjustarZonaSegura();
        ElegirBotones();
        LeerDedos();
        EnviarTeclado();
        EnviarRaton();
        Dibujar();
    }

    // ---------------------------------------------------------------- ¿qué está pasando en la escena?
    Modo DetectarModo()
    {
        var gestor = GestorNoche.instancia;
        if (gestor != null && gestor.EnGameOver) return Modo.GameOver;

        if (noche == null) noche = FindAnyObjectByType<ControlJugadorNoche>();
        if (noche != null && noche.isActiveAndEnabled && noche.Activo)
        {
            if (linterna == null) linterna = FindAnyObjectByType<LinternaNoche>();
            return Modo.Noche;
        }

        if (AlgunoActivo(FindObjectsByType<ControlCinematica>(FindObjectsSortMode.None))) return Modo.Cinematica;
        foreach (var s in FindObjectsByType<SecuenciaVideos>(FindObjectsSortMode.None))
            if (s.isActiveAndEnabled && s.permitirSaltar) return Modo.Cinematica;

        if (AlgunoActivo(FindObjectsByType<ControladorPrimeraPersona>(FindObjectsSortMode.None))) return Modo.Caminar;
        if (AlgunoActivo(FindObjectsByType<ControladorLateral>(FindObjectsSortMode.None))) return Modo.Lateral;
        if (AlgunoActivo(FindObjectsByType<PlayerController>(FindObjectsSortMode.None))) return Modo.Lateral;
        return Modo.Nada;
    }

    static bool AlgunoActivo<T>(T[] lista) where T : Behaviour
    {
        foreach (var c in lista) if (c != null && c.isActiveAndEnabled) return true;
        return false;
    }

    bool DebeOcultarse()
    {
        if (MenuPausa.EstaPausado) return true;
        if (TituloCapitulo.Activo) return true;
        var t = GestorTransicion.instancia;
        if (t != null && t.EnTransicion) return true;
        return false;
    }

    void ElegirBotones()
    {
        foreach (var b in botones) b.visible = false;
        if (oculto) return;

        switch (modo)
        {
            case Modo.Caminar:
                bUsar.visible = true;
                bSaltar.visible = true;
                bPausa.visible = true;
                break;
            case Modo.Lateral:
                bUsar.visible = true;
                bPausa.visible = true;
                break;
            case Modo.Noche:
                bPausa.visible = true;
                if (noche != null)
                {
                    bool quieto = noche.PuedeActuar;
                    var vista = noche.VistaActual;
                    bLinterna.visible = linterna != null && noche.PuedeUsarObjetos;
                    bAccion.visible = quieto && (vista == VistaNoche.Ventana || vista == VistaNoche.Chimenea);
                    bVolver.visible = quieto && vista != VistaNoche.Cama;
                }
                break;
            case Modo.Cinematica:
                bOmitir.visible = true;
                break;
            case Modo.GameOver:
                bMenu.visible = true;
                break;
        }

        // en la noche la hora está arriba a la derecha: la pausa va un poco más abajo
        bPausa.rt.anchoredPosition = modo == Modo.Noche ? new Vector2(-90f, -175f) : new Vector2(-90f, -90f);

        // un botón que se escondió mientras lo apretabas se suelta
        foreach (var b in botones)
            if (!b.visible && b.presionado) SoltarBoton(b, true);
    }

    bool UsaJoystick => !oculto && (modo == Modo.Caminar || modo == Modo.Lateral);
    bool UsaMirar => !oculto && (modo == Modo.Caminar || modo == Modo.Noche);
    bool UsaToque => !oculto && modo != Modo.Nada && modo != Modo.Cinematica;

    // ---------------------------------------------------------------- dedos
    void LeerDedos()
    {
        deltaMirar = Vector2.zero;
        var activos = Toque.activeTouches;
        for (int i = 0; i < activos.Count; i++)
        {
            var t = activos[i];
            int id = t.touchId;
            Dedo d;
            dedos.TryGetValue(id, out d);

            if (t.phase == FaseToque.Began || (d == null && t.phase != FaseToque.Ended && t.phase != FaseToque.Canceled))
            {
                d = EmpezarDedo(t.screenPosition, t.startTime);
                dedos[id] = d;
            }
            else if (d == null)
            {
                // empezó y terminó en el mismo frame: cuenta como toque rápido
                if (t.phase == FaseToque.Ended)
                {
                    var r = EmpezarDedo(t.screenPosition, t.startTime);
                    TerminarDedo(id, r, t.screenPosition, true);
                }
                continue;
            }

            if (t.phase == FaseToque.Moved || t.phase == FaseToque.Stationary || t.phase == FaseToque.Began)
                MoverDedo(d, t.screenPosition, t.delta);

            if (t.phase == FaseToque.Ended || t.phase == FaseToque.Canceled)
            {
                TerminarDedo(id, d, t.screenPosition, t.phase == FaseToque.Ended);
                dedos.Remove(id);
            }
        }

        // dedos que desaparecieron sin avisar
        if (dedos.Count > activos.Count)
        {
            var quitar = new List<int>();
            foreach (var kv in dedos)
            {
                bool sigue = false;
                for (int i = 0; i < activos.Count; i++) if (activos[i].touchId == kv.Key) { sigue = true; break; }
                if (!sigue) quitar.Add(kv.Key);
            }
            foreach (int id in quitar)
            {
                TerminarDedo(id, dedos[id], Vector2.zero, false);
                dedos.Remove(id);
            }
        }

        // los botones que se soltaron el mismo frame en que se apretaron se sueltan ahora
        foreach (var b in botones)
            if (b.soltarPendiente && Time.frameCount > b.frameApretado) SoltarBoton(b, true);
    }

    Dedo EmpezarDedo(Vector2 p, double inicio)
    {
        var d = new Dedo { dueno = Dueno.Nadie, inicio = p, tiempoInicio = inicio };
        if (oculto) return d;

        // 1) botones de la interfaz del juego (menús, game over...) mandan
        if (SobreInterfazDelJuego(p)) { d.dueno = Dueno.Interfaz; return d; }

        // 2) nuestros botones
        foreach (var b in botones)
        {
            if (!b.visible || b.presionado) continue;
            if (!Toca(b, p)) continue;
            d.dueno = Dueno.Boton;
            d.boton = b;
            b.presionado = true;
            b.dedo = 0;
            b.frameApretado = Time.frameCount;
            b.soltarPendiente = false;
            return d;
        }

        // 3) joystick (lado izquierdo, abajo)
        if (UsaJoystick && !joyActivo && p.x < Screen.width * 0.42f && p.y < Screen.height * 0.75f)
        {
            d.dueno = Dueno.Joystick;
            joyActivo = true;
            centroJoy = p;
            vectorJoy = Vector2.zero;
            return d;
        }

        // 4) lo demás: mirar / tocar
        d.dueno = Dueno.Mirar;
        return d;
    }

    void MoverDedo(Dedo d, Vector2 p, Vector2 delta)
    {
        switch (d.dueno)
        {
            case Dueno.Joystick:
                float radio = RadioJoystickPx();
                Vector2 v = (p - centroJoy) / Mathf.Max(1f, radio);
                float largo = v.magnitude;
                joyCorrer = largo > 1.45f;              // sacar el dedo más allá del círculo = correr
                if (largo > 1f) v = v / largo;
                vectorJoy = v;
                break;
            case Dueno.Mirar:
                d.recorrido += delta.magnitude;
                if (UsaMirar) deltaMirar += delta;
                break;
        }
    }

    void TerminarDedo(int id, Dedo d, Vector2 p, bool terminoBien)
    {
        switch (d.dueno)
        {
            case Dueno.Joystick:
                joyActivo = false;
                joyCorrer = false;
                vectorJoy = Vector2.zero;
                break;
            case Dueno.Boton:
                if (d.boton != null) SoltarBoton(d.boton, false);
                break;
            case Dueno.Mirar:
                double dura = Time.realtimeSinceStartupAsDouble - d.tiempoInicio;
                bool esToque = terminoBien && d.recorrido < Screen.height * 0.03f && (dura < 0.35 || d.tiempoInicio <= 0);
                if (esToque && UsaToque) Clic(p);
                break;
        }
    }

    void SoltarBoton(Boton b, bool forzar)
    {
        if (!b.presionado) return;
        if (!forzar && Time.frameCount <= b.frameApretado)
        {
            b.soltarPendiente = true;     // que alcance a contar como "apretado" al menos un frame
            return;
        }
        b.presionado = false;
        b.soltarPendiente = false;
        b.dedo = -1;
    }

    void SoltarTodo()
    {
        foreach (var b in botones) { b.presionado = false; b.soltarPendiente = false; b.dedo = -1; }
        dedos.Clear();
        joyActivo = false;
        joyCorrer = false;
        vectorJoy = Vector2.zero;
        deltaMirar = Vector2.zero;
        if (teclado != null && teclasAntes.Count > 0)
        {
            teclasAntes.Clear();
            InputSystem.QueueStateEvent(teclado, new KeyboardState());
        }
        if (clic == 2 && raton != null) { clic = 0; InputSystem.QueueStateEvent(raton, new MouseState { position = posRaton }); }
    }

    bool Toca(Boton b, Vector2 p)
    {
        if (!b.redondo) return RectTransformUtility.RectangleContainsScreenPoint(b.rt, p, null);
        Vector2 centro = RectTransformUtility.WorldToScreenPoint(null, b.rt.position);
        float radio = b.rt.rect.width * 0.5f * canvas.scaleFactor * 1.18f;   // un poco más grande que el dibujo
        return (p - centro).sqrMagnitude <= radio * radio;
    }

    float RadioJoystickPx() { return 120f * canvas.scaleFactor; }

    bool SobreInterfazDelJuego(Vector2 p)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        if (datosPuntero == null || sistemaDelPuntero != es)
        {
            datosPuntero = new PointerEventData(es);
            sistemaDelPuntero = es;
        }
        datosPuntero.position = p;
        resultadosUI.Clear();
        es.RaycastAll(datosPuntero, resultadosUI);
        foreach (var r in resultadosUI)
        {
            var go = r.gameObject;
            if (go == null) continue;
            // solo cuenta lo que de verdad se puede tocar (botones, sliders...); las imágenes de adorno no
            if (go.GetComponentInParent<Selectable>() != null) return true;
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(go) != null) return true;
            if (ExecuteEvents.GetEventHandler<IPointerDownHandler>(go) != null) return true;
        }
        return false;
    }

    void Clic(Vector2 p)
    {
        posRaton = p;
        clic = 1;
        ratonSucio = true;
    }

    // ---------------------------------------------------------------- teclado y mouse virtuales
    void EnviarTeclado()
    {
        teclasAhora.Clear();
        if (UsaJoystick && joyActivo)
        {
            const float umbral = 0.35f;
            if (vectorJoy.y > umbral) teclasAhora.Add(Key.W);
            if (vectorJoy.y < -umbral) teclasAhora.Add(Key.S);
            if (vectorJoy.x > umbral) teclasAhora.Add(Key.D);
            if (vectorJoy.x < -umbral) teclasAhora.Add(Key.A);
            if (modo == Modo.Caminar && joyCorrer) teclasAhora.Add(Key.LeftShift);
        }
        foreach (var b in botones)
            if (b.presionado) teclasAhora.Add(b.tecla);

        // Se vuelve a mandar si otro teclado (p. ej. el botón Atrás de Android) "le ganó": así ese frame
        // los scripts sí ven el Atrás, y al siguiente vuelve a mandar este teclado.
        bool cambio = !teclasAhora.SetEquals(teclasAntes);
        if (cambio || (teclasAhora.Count > 0 && Keyboard.current != teclado))
        {
            listaTeclas.Clear();
            listaTeclas.AddRange(teclasAhora);
            InputSystem.QueueStateEvent(teclado, new KeyboardState(listaTeclas.ToArray()));
            teclasAntes.Clear();
            foreach (var k in teclasAhora) teclasAntes.Add(k);
        }
    }

    void EnviarRaton()
    {
        if (Mouse.current != raton) raton.MakeCurrent();

        // pixeles del dedo → "movimiento de mouse" (igual en cualquier resolución)
        Vector2 d = deltaMirar * (1080f / Mathf.Max(1, Screen.height)) * Sensibilidad;

        if (d == Vector2.zero && clic == 0 && !ratonSucio) return;

        var estado = new MouseState { position = posRaton, delta = d };
        if (clic == 1)
        {
            estado = estado.WithButton(MouseButton.Left, true);
            clic = 2;                       // el siguiente frame se suelta
        }
        else if (clic == 2)
        {
            clic = 0;
        }
        ratonSucio = clic != 0;
        InputSystem.QueueStateEvent(raton, estado);
    }

    // ---------------------------------------------------------------- dibujo
    void Dibujar()
    {
        foreach (var b in botones)
        {
            if (b.rt.gameObject.activeSelf != b.visible) b.rt.gameObject.SetActive(b.visible);
            if (!b.visible) continue;
            float escala = b.presionado ? 0.9f : 1f;
            b.rt.localScale = Vector3.Lerp(b.rt.localScale, Vector3.one * escala, 1f - Mathf.Exp(-25f * Time.unscaledDeltaTime));
            b.fondo.color = b.presionado ? new Color(1f, 1f, 1f, 0.35f) : new Color(0f, 0f, 0f, 0.42f);
        }

        bool mostrarJoy = UsaJoystick;
        if (baseJoy.gameObject.activeSelf != mostrarJoy) baseJoy.gameObject.SetActive(mostrarJoy);
        if (!mostrarJoy) return;

        Vector2 local;
        if (joyActivo)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zonaSegura, centroJoy, null, out local);
            baseJoy.anchoredPosition = local;
            imgBaseJoy.color = new Color(1f, 1f, 1f, 0.55f);
            imgPerillaJoy.color = new Color(1f, 1f, 1f, 0.85f);
            perillaJoy.anchoredPosition = vectorJoy * 120f;
        }
        else
        {
            // en reposo se queda abajo a la izquierda, clarito, para que se sepa que ahí está
            baseJoy.anchoredPosition = PosicionReposoJoy();
            imgBaseJoy.color = new Color(1f, 1f, 1f, 0.22f);
            imgPerillaJoy.color = new Color(1f, 1f, 1f, 0.32f);
            perillaJoy.anchoredPosition = Vector2.zero;
        }
    }

    Vector2 PosicionReposoJoy()
    {
        // zonaSegura tiene el pivote al centro: abajo-izquierda = -tamaño/2
        Vector2 tam = zonaSegura.rect.size;
        return new Vector2(-tam.x * 0.5f + 260f, -tam.y * 0.5f + 250f);
    }

    void AjustarZonaSegura()
    {
        Rect s = Screen.safeArea;
        if (s == ultimaZonaSegura || Screen.width <= 0 || Screen.height <= 0) return;
        ultimaZonaSegura = s;
        zonaSegura.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
        zonaSegura.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
        zonaSegura.offsetMin = Vector2.zero;
        zonaSegura.offsetMax = Vector2.zero;
    }

    // ---------------------------------------------------------------- interfaz
    void CrearInterfaz()
    {
        spriteCirculo = CrearSprite(false);
        spriteAro = CrearSprite(true);
        try { fuente = TMP_Settings.defaultFontAsset; } catch (System.Exception) { fuente = null; }

        var go = new GameObject("Canvas Controles", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 1f;
        grupo = go.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;
        raiz = (RectTransform)go.transform;

        var zs = new GameObject("Zona Segura", typeof(RectTransform));
        zs.transform.SetParent(raiz, false);
        zonaSegura = (RectTransform)zs.transform;
        zonaSegura.anchorMin = Vector2.zero;
        zonaSegura.anchorMax = Vector2.one;
        zonaSegura.offsetMin = Vector2.zero;
        zonaSegura.offsetMax = Vector2.zero;

        // joystick
        baseJoy = NuevoRect("Joystick", zonaSegura, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 280f));
        imgBaseJoy = baseJoy.gameObject.AddComponent<Image>();
        imgBaseJoy.sprite = spriteAro;
        imgBaseJoy.raycastTarget = false;
        perillaJoy = NuevoRect("Perilla", baseJoy, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
        imgPerillaJoy = perillaJoy.gameObject.AddComponent<Image>();
        imgPerillaJoy.sprite = spriteCirculo;
        imgPerillaJoy.raycastTarget = false;

        // botones (posiciones en la resolución de 1920x1080)
        var abajoDer = new Vector2(1f, 0f);
        var arribaDer = new Vector2(1f, 1f);
        var abajoIzq = new Vector2(0f, 0f);
        bUsar = NuevoBoton("USAR", Key.E, abajoDer, new Vector2(-200f, 230f), 200f, 34f);
        bSaltar = NuevoBoton("SALTAR", Key.Space, abajoDer, new Vector2(-420f, 135f), 140f, 24f);
        bLinterna = NuevoBoton("LINTERNA", Key.F, abajoDer, new Vector2(-200f, 230f), 200f, 28f);
        bAccion = NuevoBoton("ACCIÓN", Key.Space, abajoDer, new Vector2(-430f, 160f), 170f, 28f);
        bVolver = NuevoBoton("VOLVER", Key.S, abajoIzq, new Vector2(170f, 330f), 170f, 28f);
        bPausa = NuevoBoton("II", Key.Escape, arribaDer, new Vector2(-90f, -90f), 104f, 34f);
        bMenu = NuevoBoton("MENÚ", Key.M, abajoDer, new Vector2(-200f, 190f), 190f, 32f);
        bOmitir = NuevoBoton("OMITIR  »", Key.Space, arribaDer, new Vector2(-180f, -80f), 0f, 30f);
        bOmitir.redondo = false;
        bOmitir.rt.sizeDelta = new Vector2(260f, 92f);
        bOmitir.fondo.sprite = null;
        bOmitir.aro.sprite = null;
        bOmitir.aro.color = new Color(1f, 1f, 1f, 0f);

        foreach (var b in botones) b.rt.gameObject.SetActive(false);
        baseJoy.gameObject.SetActive(false);
    }

    Boton NuevoBoton(string texto, Key tecla, Vector2 ancla, Vector2 pos, float tam, float tamLetra)
    {
        var b = new Boton { nombre = texto, tecla = tecla };
        b.rt = NuevoRect("Boton " + texto, zonaSegura, ancla, pos, new Vector2(tam, tam));
        b.fondo = b.rt.gameObject.AddComponent<Image>();
        b.fondo.sprite = spriteCirculo;
        b.fondo.color = new Color(0f, 0f, 0f, 0.42f);
        b.fondo.raycastTarget = false;

        var aro = NuevoRect("Aro", b.rt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        aro.anchorMin = Vector2.zero;
        aro.anchorMax = Vector2.one;
        aro.sizeDelta = Vector2.zero;
        b.aro = aro.gameObject.AddComponent<Image>();
        b.aro.sprite = spriteAro;
        b.aro.color = new Color(1f, 1f, 1f, 0.6f);
        b.aro.raycastTarget = false;

        var t = NuevoRect("Texto", b.rt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        t.anchorMin = Vector2.zero;
        t.anchorMax = Vector2.one;
        t.sizeDelta = Vector2.zero;
        b.texto = t.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) b.texto.font = fuente;
        b.texto.text = texto;
        b.texto.fontSize = tamLetra;
        b.texto.fontStyle = FontStyles.Bold;
        b.texto.alignment = TextAlignmentOptions.Center;
        b.texto.color = Color.white;
        b.texto.raycastTarget = false;

        botones.Add(b);
        return b;
    }

    static RectTransform NuevoRect(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    static Sprite CrearSprite(bool soloAro)
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[n * n];
        float r = n * 0.5f - 1f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - n * 0.5f, dy = y + 0.5f - n * 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d + 0.5f);                       // borde suave
                if (soloAro) a *= Mathf.Clamp01(d - (r - 6f) + 0.5f);         // solo la orilla
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}

/// <summary>
/// Cambia los textos de teclado por los de los botones táctiles ("Presiona E" → "Toca USAR").
/// Solo se usa en celular; en la PC los textos quedan como están.
/// </summary>
public class TextoTactil : ITextPreprocessor
{
    public static readonly TextoTactil instancia = new TextoTactil();

    static readonly string[,] cambios =
    {
        { "[Clic / Espacio]", "[Toca la pantalla]" },
        { "[S / Clic derecho]", "[VOLVER]" },
        { "[Espacio] Mantén para", "Mantén [ACCIÓN] para" },
        { "[Espacio]", "[ACCIÓN]" },
        { "[F] LINTERNA", "LINTERNA" },
        { "[F] Linterna", "[LINTERNA]" },
        { "[F]", "[LINTERNA]" },
        { "[Clic]", "[Toca]" },
        { "[Esc]", "[MENÚ]" },
        { "Presiona E", "Toca USAR" },
        { "presiona E", "toca USAR" },
        { "PRESIONA E", "TOCA USAR" },
        { "Presiona F", "Toca LINTERNA" },
        { "con Espacio o Enter", "con el botón OMITIR" },
        { "Espacio o Enter", "el botón OMITIR" },
    };

    public string PreprocessText(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        for (int i = 0; i < cambios.GetLength(0); i++)
            if (texto.Contains(cambios[i, 0])) texto = texto.Replace(cambios[i, 0], cambios[i, 1]);
        return texto;
    }

    /// <summary>Le pone el cambio de textos a todos los textos TMP de las escenas abiertas.</summary>
    public static void AplicarATodos()
    {
        var textos = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in textos)
        {
            if (t == null || t.textPreprocessor != null) continue;
            t.textPreprocessor = instancia;
            string s = t.text;
            if (!string.IsNullOrEmpty(s) && instancia.PreprocessText(s) != s)
            {
                t.text = string.Empty;   // fuerza a que vuelva a leer el texto ya cambiado
                t.text = s;
            }
        }
    }
}
