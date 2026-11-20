using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// CAPÍTULO 4 — FINAL BUENO (en el bosque, primera persona).
/// 1) Toma la pala.  2) Cava un hoyo.  3) Echa las 5 marionetas al hoyo, una por una.
/// 4) Entiérralas.  5) "FINAL BUENO — La pesadilla terminó."
/// Usa los mismos objetivos, pistas ("Presiona E…") y contador que las demás escenas.
/// Lo arma la herramienta: Herramientas > Capítulos y final > "Armar final bueno".
/// </summary>
public class FinalBueno : MonoBehaviour
{
    enum Fase { Pala, Cavar, Marionetas, Enterrar, Final }

    [Header("Referencias (las pone la herramienta)")]
    public ControladorPrimeraPersona jugador;
    [Tooltip("La pala que está en la escena")]
    public GameObject palaEnElSuelo;
    [Tooltip("Dónde se cava el hoyo. Muévelo si quieres el hoyo en otro lado")]
    public Transform puntoHoyo;
    [Tooltip("Las marionetas que se echan al hoyo (empiezan escondidas)")]
    public GameObject[] marionetas;
    [Tooltip("La canasta que te sigue (opcional)")]
    public CanastaSigue canasta;
    [Tooltip("El sol de la escena (opcional): se pone más cálido al final")]
    public Light luzSol;

    [Header("Pala en la mano (lo calcula la herramienta)")]
    [Tooltip("Dirección del mango hacia la hoja, en el espacio de la pala")]
    public Vector3 palaEje = Vector3.down;
    [Tooltip("Hacia dónde mira la cara de la hoja, en el espacio de la pala")]
    public Vector3 palaNormal = Vector3.forward;
    [Tooltip("Punta del mango, en el espacio de la pala")]
    public Vector3 palaMango = Vector3.up;
    [Tooltip("Largo de la pala en su espacio")]
    public float palaLargo = 1f;
    [Tooltip("Dónde va la mano (metros, respecto a la cámara)")]
    public Vector3 manoPosicion = new Vector3(0.3f, -0.33f, 0.2f);
    [Tooltip("Largo de la pala en la mano (metros)")]
    public float largoEnMano = 1.15f;

    [Header("Juego")]
    public float distanciaInteraccion = 2.3f;
    public int golpesParaCavar = 5;
    public int golpesParaEnterrar = 4;
    public float radioHoyo = 0.6f;

    [Header("Objetivos")]
    public string objetivoPala = "Toma la pala";
    public string objetivoCavar = "Cava un hoyo junto a la pala";
    public string objetivoMarionetas = "Echa tus marionetas al hoyo, una por una";
    public string objetivoEnterrar = "Entierra las marionetas";

    [Header("Pistas")]
    public string pistaPala = "Presiona E para tomar la pala";
    public string pistaSinPala = "Necesitas la pala para cavar";
    public string pistaCavar = "Presiona E para cavar";
    public string pistaMarioneta = "Presiona E para echar la marioneta al hoyo";
    public string pistaSinMarionetas = "Ya no traes marionetas";
    public string pistaEnterrar = "Presiona E para enterrar";

    [Header("Materiales (los pone la herramienta)")]
    public Material materialTierra;
    public Material materialParticulas;

    [Header("Sonidos")]
    public AudioClip ambienteBosque;
    [Range(0f, 1f)] public float volumenAmbiente = 0.6f;
    public AudioClip sonidoTomarPala;
    public AudioClip sonidoCavar1;
    public AudioClip sonidoCavar2;
    public AudioClip sonidoTierra;
    public AudioClip sonidoMarioneta;
    public AudioClip sonidoEnterrar;
    public AudioClip sonidoSuspiro;
    public AudioClip sonidoAmanecer;
    public AudioClip musicaFinal;
    [Range(0f, 1f)] public float volumenMusica = 0.8f;

    [Header("Pantalla final")]
    public string textoFinal = "FINAL BUENO";
    public string frase = "La pesadilla terminó.";
    public string gracias = "Gracias por jugar El Viejo Capi";
    public string textoContinuar = "[Clic / Espacio]  Volver al menú";
    public string escenaMenu = "MainMenu";
    public float esperaParaContinuar = 5f;
    public TMP_FontAsset fuente;

    // ---- estado ----
    Fase fase = Fase.Pala;
    bool animando, empezo;
    int golpes, echadas, paladas, totalMarionetas;
    string pistaActual = "";
    Transform cam, mano, palaMano;
    Vector3 manoBase;
    Quaternion manoRotBase = Quaternion.identity;
    AudioSource src2D, srcAmbiente, srcMusica;
    readonly List<Transform> ignorar = new List<Transform>();
    float offsetCanasta;
    bool hayCanasta;
    CharacterController ccJugador;
    Vector3 centroPala;

    // hoyo
    Transform hoyo, discoHoyo, borde, monton, tumba;
    Material matHoyo, matTierra;
    Vector3 centroHoyo, normalHoyo = Vector3.up, posMonton;
    ParticleSystem tierraPS;

    // interfaz propia (respaldo y pantalla final)
    TextMeshProUGUI txtObjetivoRespaldo, txtPistaRespaldo, txtContadorRespaldo;
    CanvasGroup grupoFinal, grupoContinuar, grupoBlanco;
    TextMeshProUGUI txtFinal, txtFrase, txtGracias;

    // ───────────────────────── inicio ─────────────────────────

    IEnumerator Start()
    {
        if (jugador == null) jugador = FindFirstObjectByType<ControladorPrimeraPersona>();
        if (jugador != null)
        {
            cam = jugador.camara != null ? jugador.camara : jugador.GetComponentInChildren<Camera>()?.transform;
            ignorar.Add(jugador.transform);
        }
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam != null)
        {
            var camara = cam.GetComponent<Camera>();
            if (camara != null && camara.nearClipPlane > 0.08f) camara.nearClipPlane = 0.08f;   // que la pala en la mano no se corte
        }
        if (palaEnElSuelo != null) centroPala = PosPala();
        if (palaEnElSuelo != null) ignorar.Add(palaEnElSuelo.transform);
        if (canasta != null) { ignorar.Add(canasta.transform); hayCanasta = true; }
        if (marionetas == null) marionetas = new GameObject[0];
        foreach (var m in marionetas) if (m != null) ignorar.Add(m.transform);   // las de la canasta se ven; las sueltas empiezan escondidas

        totalMarionetas = GestorCanastaDevolver.instancia != null ? GestorCanastaDevolver.instancia.totalMarionetas : marionetas.Length;
        if (totalMarionetas <= 0) totalMarionetas = Mathf.Max(1, marionetas.Length);

        ConstruirAudio();
        ConstruirInterfaz();
        ConstruirHoyo();
        if (hayCanasta) offsetCanasta = canasta.transform.position.y - AlturaSuelo(canasta.transform.position, out _);
        if (GestorCanastaDevolver.instancia != null) GestorCanastaDevolver.instancia.OcultarContador();

        // espera la pantalla de carga y el título "CAPÍTULO 4"
        while (TituloCapitulo.Activo || (GestorTransicion.instancia != null && GestorTransicion.instancia.EnTransicion))
            yield return null;
        yield return new WaitForSeconds(0.8f);
        empezo = true;
        Objetivo(objetivoPala);
    }

    // ───────────────────────── juego ─────────────────────────

    void Update()
    {
        if (hayCanasta && canasta != null)
            canasta.alturaFija = AlturaSuelo(canasta.transform.position, out _) + offsetCanasta;   // la canasta sigue el terreno

        if (!empezo || fase == Fase.Final || jugador == null) return;

        Vector3 p = PosJugador();
        bool cercaPala = fase == Fase.Pala && palaEnElSuelo != null && DistanciaXZ(p, centroPala) < distanciaInteraccion;
        bool cercaHoyo = DistanciaXZ(p, centroHoyo) < distanciaInteraccion + radioHoyo;

        string pista = "";
        if (cercaPala) pista = pistaPala;
        else if (cercaHoyo)
        {
            switch (fase)
            {
                case Fase.Pala: pista = pistaSinPala; break;
                case Fase.Cavar: pista = pistaCavar; break;
                case Fase.Marionetas: pista = echadas < totalMarionetas ? pistaMarioneta : pistaSinMarionetas; break;
                case Fase.Enterrar: pista = pistaEnterrar; break;
            }
        }
        Pista(pista);

        if (animando || !PresionoE()) return;
        if (cercaPala) TomarPala();
        else if (cercaHoyo)
        {
            if (fase == Fase.Cavar) StartCoroutine(Cavar());
            else if (fase == Fase.Marionetas && echadas < totalMarionetas) StartCoroutine(EcharMarioneta());
            else if (fase == Fase.Enterrar) StartCoroutine(Enterrar());
        }
    }

    // dónde está de verdad el Capi (su cápsula), aunque su punto de origen esté en otro lado
    Vector3 PosJugador()
    {
        if (jugador == null) return Vector3.zero;
        if (ccJugador == null) ccJugador = jugador.GetComponent<CharacterController>();
        return ccJugador != null ? jugador.transform.TransformPoint(ccJugador.center) : jugador.transform.position;
    }

    Vector3 PosPala()
    {
        if (palaEnElSuelo == null) return Vector3.zero;
        return Limites(palaEnElSuelo).center;
    }

    static bool PresionoE()
    {
        if (MenuPausa.EstaPausado) return false;
        var kb = Keyboard.current;
        return kb != null && kb.eKey.wasPressedThisFrame;
    }

    static float DistanciaXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // 1) la pala
    void TomarPala()
    {
        Sonar(sonidoTomarPala, 1f);
        CrearPalaEnMano();
        if (palaEnElSuelo != null) palaEnElSuelo.SetActive(false);
        fase = Fase.Cavar;
        Pista("");
        Objetivo(objetivoCavar);
        StartCoroutine(SubirPala());
    }

    void CrearPalaEnMano()
    {
        if (cam == null || palaEnElSuelo == null) return;
        mano = new GameObject("PalaEnMano").transform;
        mano.SetParent(cam, false);
        Vector3 ls = cam.lossyScale;
        Vector3 inv = new Vector3(1f / Mathf.Max(0.0001f, ls.x), 1f / Mathf.Max(0.0001f, ls.y), 1f / Mathf.Max(0.0001f, ls.z));
        mano.localScale = inv;
        manoBase = Vector3.Scale(manoPosicion, inv);
        mano.localPosition = manoBase;

        var copia = Instantiate(palaEnElSuelo, mano);
        copia.name = "Pala";
        copia.SetActive(true);
        foreach (var c in copia.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (var rb in copia.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (var r in copia.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        palaMano = copia.transform;

        float s = largoEnMano / Mathf.Max(0.0001f, palaLargo);
        Vector3 dir = new Vector3(-0.16f, -0.36f, 0.92f).normalized;
        Quaternion qPala = Quaternion.LookRotation(palaEje.normalized, palaNormal.normalized);
        Quaternion qMano = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion rot = qMano * Quaternion.Inverse(qPala);
        palaMano.localRotation = rot;
        palaMano.localScale = Vector3.one * s;
        palaMano.localPosition = -(rot * (palaMango * s));
    }

    IEnumerator SubirPala()
    {
        if (mano == null) yield break;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float k = Suave(Mathf.Clamp01(t / 0.5f));
            mano.localPosition = manoBase + Vector3.Scale(new Vector3(0f, -0.35f, 0f), mano.localScale) * (1f - k);
            mano.localRotation = Quaternion.Euler(25f * (1f - k), 0f, 0f);
            yield return null;
        }
    }

    // 2) cavar: cada E es una palada
    IEnumerator Cavar()
    {
        animando = true;
        bool golpeo = false, tiro = false;
        float T = 0.9f, t = 0f;
        while (t < T)
        {
            t += Time.deltaTime;
            float k = t / T;
            Movimiento(k, false);
            if (!golpeo && k >= 0.42f)
            {
                golpeo = true;
                Sonar(golpes % 2 == 0 ? sonidoCavar1 : sonidoCavar2, 1f);
                Tierra(centroHoyo, 22, 2.2f);
                golpes++;
                ActualizarHoyo(golpes / (float)golpesParaCavar, 0f);
            }
            if (!tiro && k >= 0.7f)
            {
                tiro = true;
                Sonar(sonidoTierra, 0.8f);
                Tierra(posMonton + normalHoyo * 0.2f, 18, 1.2f);
            }
            yield return null;
        }
        Movimiento(1f, false);
        animando = false;

        if (golpes >= golpesParaCavar)
        {
            fase = Fase.Marionetas;
            Pista("");
            Objetivo(objetivoMarionetas);
            MostrarContador();
        }
    }

    // 3) echar marionetas: misma mecánica que guardar las marionetas en la escena 2 (E, contador, sonido)
    IEnumerator EcharMarioneta()
    {
        animando = true;
        int i = echadas;
        echadas++;
        var gestor = GestorCanastaDevolver.instancia;
        if (gestor != null) gestor.Depositar();
        ActualizarContador();

        GameObject m = marionetas.Length > 0 ? marionetas[i % marionetas.Length] : null;
        if (m != null)
        {
            if (i >= marionetas.Length) m = Instantiate(m);         // por si hay más marionetas que modelos
            bool enCanasta = m.activeInHierarchy;                    // la sacas de la canasta
            Quaternion rotIni = m.transform.rotation;               // así está parada (en la canasta o en su lugar)
            Vector3 desde = enCanasta
                ? m.transform.position
                : (jugador != null
                    ? Vector3.Lerp(centroHoyo, PosJugador(), 0.45f) + Vector3.up * 1.25f
                    : centroHoyo + Vector3.up * 1.4f);
            m.transform.SetParent(transform, true);
            m.SetActive(true);
            Vector3 escIni = m.transform.localScale;

            // tamaño parejo (que quepan en el hoyo)
            Bounds b = Limites(m);
            float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            Vector3 escFin = mayor > 0.85f ? escIni * (0.85f / mayor) : escIni;

            // pose final: acostada dentro del hoyo
            Vector3 lado = Quaternion.AngleAxis(i * 72f + Random.Range(-15f, 15f), normalHoyo) * Vector3.Cross(normalHoyo, Vector3.right).normalized;
            Quaternion rotFin = Quaternion.FromToRotation(Vector3.up, normalHoyo)
                              * Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up)
                              * Quaternion.AngleAxis(-82f, Vector3.right) * rotIni;          // acostada
            m.transform.localScale = escFin;
            m.transform.SetPositionAndRotation(centroHoyo + lado * Random.Range(0.05f, 0.22f), rotFin);
            Bounds bf = Limites(m);
            Vector3 fin = m.transform.position + normalHoyo * (centroHoyo.y - 0.1f + 0.035f * i - bf.min.y);
            m.transform.localScale = escIni;

            // sale de la canasta, sube un poco y cae al hoyo
            float T = enCanasta ? 0.85f : 0.6f, t = 0f;
            while (t < T)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / T);
                Vector3 pos = Vector3.Lerp(desde, fin, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * (enCanasta ? 0.6f : 0.35f));
                m.transform.SetPositionAndRotation(pos, Quaternion.Slerp(rotIni, rotFin, k * k));
                m.transform.localScale = Vector3.Lerp(escIni, escFin, Suave(k));
                yield return null;
            }
            m.transform.localScale = escFin;
            m.transform.SetPositionAndRotation(fin, rotFin);
            Sonar(sonidoMarioneta, 1f);
            Tierra(centroHoyo, 10, 1f);
        }
        else Sonar(sonidoMarioneta, 1f);

        yield return new WaitForSeconds(0.25f);
        animando = false;

        if (echadas >= totalMarionetas)
        {
            fase = Fase.Enterrar;
            Pista("");
            yield return new WaitForSeconds(0.6f);
            OcultarContador();
            Objetivo(objetivoEnterrar);
        }
    }

    // 4) enterrar
    IEnumerator Enterrar()
    {
        animando = true;
        bool tiro = false;
        float T = 0.9f, t = 0f;
        while (t < T)
        {
            t += Time.deltaTime;
            float k = t / T;
            Movimiento(k, true);
            if (!tiro && k >= 0.62f)
            {
                tiro = true;
                Sonar(sonidoEnterrar, 1f);
                Tierra(centroHoyo + normalHoyo * 0.15f, 26, 1.4f);
                paladas++;
                ActualizarHoyo(1f, paladas / (float)golpesParaEnterrar);
            }
            yield return null;
        }
        Movimiento(1f, true);
        animando = false;

        if (paladas >= golpesParaEnterrar)
        {
            fase = Fase.Final;
            StartCoroutine(Final());
        }
    }

    // movimiento de la pala: levantar, clavar, sacar y aventar
    void Movimiento(float k, bool haciaHoyo)
    {
        if (mano == null) return;
        float pitch, yaw, adelante, arriba;
        if (k < 0.3f)
        {
            float a = Suave(k / 0.3f);
            pitch = -18f * a; yaw = 0f; adelante = -0.08f * a; arriba = 0.1f * a;
        }
        else if (k < 0.45f)
        {
            float a = Suave((k - 0.3f) / 0.15f);
            pitch = Mathf.Lerp(-18f, 32f, a); yaw = 0f; adelante = Mathf.Lerp(-0.08f, 0.22f, a); arriba = Mathf.Lerp(0.1f, -0.12f, a);
        }
        else if (k < 0.75f)
        {
            float a = Suave((k - 0.45f) / 0.3f);
            pitch = Mathf.Lerp(32f, -8f, a); yaw = Mathf.Lerp(0f, haciaHoyo ? -20f : 28f, a);
            adelante = Mathf.Lerp(0.22f, 0.05f, a); arriba = Mathf.Lerp(-0.12f, 0.12f, a);
        }
        else
        {
            float a = Suave((k - 0.75f) / 0.25f);
            pitch = Mathf.Lerp(-8f, 0f, a); yaw = Mathf.Lerp(haciaHoyo ? -20f : 28f, 0f, a);
            adelante = Mathf.Lerp(0.05f, 0f, a); arriba = Mathf.Lerp(0.12f, 0f, a);
        }
        mano.localRotation = manoRotBase * Quaternion.Euler(pitch, yaw, 0f);
        mano.localPosition = manoBase + Vector3.Scale(new Vector3(0f, arriba, adelante), mano.localScale);
    }

    // ───────────────────────── final ─────────────────────────

    IEnumerator Final()
    {
        Pista("");
        if (jugador != null)
        {
            jugador.enabled = false;
            var pasos = jugador.GetComponent<AudioSource>();
            if (pasos != null) pasos.Stop();
        }
        foreach (var m in marionetas) if (m != null) m.SetActive(false);   // ya quedaron bajo la tierra

        // la pala se queda clavada junto a la tumba
        if (mano != null) Destroy(mano.gameObject);
        ClavarPala();

        Sonar(sonidoSuspiro, 0.9f);
        yield return new WaitForSeconds(0.8f);
        Sonar(sonidoAmanecer, 0.9f);
        if (musicaFinal != null)
        {
            srcMusica.clip = musicaFinal;
            srcMusica.volume = 0f;
            srcMusica.Play();
        }

        // mira la tumba mientras el sol se pone cálido
        Transform cuerpo = jugador != null ? jugador.transform : null;
        Quaternion cuerpo0 = cuerpo != null ? cuerpo.rotation : Quaternion.identity;
        Quaternion cam0 = cam != null ? cam.localRotation : Quaternion.identity;
        Quaternion cuerpo1 = cuerpo0, cam1 = cam0;
        if (cuerpo != null && cam != null)
        {
            Vector3 hacia = centroHoyo - cam.position;
            Vector3 plano = new Vector3(hacia.x, 0f, hacia.z);
            if (plano.sqrMagnitude > 0.001f) cuerpo1 = Quaternion.LookRotation(plano.normalized, Vector3.up);
            float pitch = Mathf.Clamp(-Mathf.Atan2(hacia.y, plano.magnitude) * Mathf.Rad2Deg, -10f, 60f);
            cam1 = Quaternion.Euler(pitch, 0f, 0f);
        }
        float i0 = luzSol != null ? luzSol.intensity : 1f;
        Color c0 = luzSol != null ? luzSol.color : Color.white;
        float t = 0f;
        while (t < 6f)
        {
            t += Time.deltaTime;
            float k = Suave(Mathf.Clamp01(t / 2.8f));
            if (cuerpo != null) cuerpo.rotation = Quaternion.Slerp(cuerpo0, cuerpo1, k);
            if (cam != null) cam.localRotation = Quaternion.Slerp(cam0, cam1, k);
            float s = Suave(Mathf.Clamp01(t / 5f));
            if (luzSol != null)
            {
                luzSol.intensity = Mathf.Lerp(i0, i0 * 1.35f, s);
                luzSol.color = Color.Lerp(c0, new Color(1f, 0.92f, 0.78f), s);
            }
            if (srcMusica.isPlaying) srcMusica.volume = Mathf.MoveTowards(srcMusica.volume, volumenMusica, Time.deltaTime / 4f);
            srcAmbiente.volume = Mathf.Lerp(volumenAmbiente, volumenAmbiente * 1.2f, s);
            yield return null;
        }

        // la pantalla se aclara y sale el FINAL BUENO
        t = 0f;
        while (t < 2.5f)
        {
            t += Time.deltaTime;
            grupoBlanco.alpha = Suave(Mathf.Clamp01(t / 2.5f));
            if (srcMusica.isPlaying) srcMusica.volume = Mathf.MoveTowards(srcMusica.volume, volumenMusica, Time.deltaTime / 4f);
            yield return null;
        }
        grupoFinal.alpha = 1f;
        t = 0f;
        bool puede = false;
        while (true)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            if (srcMusica.isPlaying) srcMusica.volume = Mathf.MoveTowards(srcMusica.volume, volumenMusica, dt / 4f);
            srcAmbiente.volume = Mathf.MoveTowards(srcAmbiente.volume, volumenAmbiente * 0.5f, dt / 4f);

            txtFinal.alpha = Suave(Mathf.Clamp01((t - 0.3f) / 2f));
            txtFinal.characterSpacing = Mathf.Lerp(40f, 14f, Suave(Mathf.Clamp01((t - 0.3f) / 4f)));
            txtFrase.alpha = Suave(Mathf.Clamp01((t - 1.8f) / 1.6f));
            txtGracias.alpha = Suave(Mathf.Clamp01((t - 3.4f) / 1.6f)) * 0.85f;

            if (!puede && t >= esperaParaContinuar)
            {
                puede = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (puede)
            {
                grupoContinuar.alpha = Mathf.MoveTowards(grupoContinuar.alpha, 0.55f + 0.35f * Mathf.Sin(t * 2f), dt * 1.5f);
                if (PresionoContinuar()) break;
            }
            yield return null;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (GestorTransicion.instancia != null) GestorTransicion.instancia.CambiarEscena(escenaMenu);
        else SceneManager.LoadScene(escenaMenu);
    }

    static bool PresionoContinuar()
    {
        if (MenuPausa.EstaPausado) return false;
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;
        var m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame) return true;
        var g = Gamepad.current;
        if (g != null && g.buttonSouth.wasPressedThisFrame) return true;
        var ts = Touchscreen.current;
        return ts != null && ts.primaryTouch.press.wasPressedThisFrame;
    }

    void ClavarPala()
    {
        if (palaEnElSuelo == null) return;
        var t = palaEnElSuelo.transform;
        // el eje de la pala apunta hacia abajo, un poco inclinado, clavada en el montón de tierra
        Vector3 haciaAbajo = Quaternion.AngleAxis(18f, Vector3.Cross(normalHoyo, (posMonton - centroHoyo).normalized)) * -normalHoyo;
        Quaternion rotLocal = Quaternion.LookRotation(palaEje.normalized, palaNormal.normalized);
        Quaternion rot = Quaternion.LookRotation(haciaAbajo, (posMonton - centroHoyo).normalized) * Quaternion.Inverse(rotLocal);
        t.rotation = rot;
        // la punta de la hoja queda 20 cm adentro del montón
        Vector3 punta = palaMango + palaEje.normalized * palaLargo;
        Vector3 puntaMundo = t.TransformPoint(punta) - t.position;
        Vector3 destino = posMonton + normalHoyo * 0.08f;
        t.position = destino + haciaAbajo * 0.2f - puntaMundo;
        palaEnElSuelo.SetActive(true);
    }

    // ───────────────────────── objetivos, pistas y contador ─────────────────────────

    void Objetivo(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return;
        if (GestorObjetivos.instancia != null) GestorObjetivos.instancia.MostrarObjetivo(texto);
        else StartCoroutine(ObjetivoRespaldo(texto));
    }

    IEnumerator ObjetivoRespaldo(string texto)
    {
        txtObjetivoRespaldo.text = texto;
        float t = 0f;
        while (t < 4f)
        {
            t += Time.unscaledDeltaTime;
            txtObjetivoRespaldo.alpha = Mathf.Clamp01(Mathf.Min(t / 0.5f, (4f - t) / 0.5f));
            yield return null;
        }
        txtObjetivoRespaldo.alpha = 0f;
    }

    void Pista(string texto)
    {
        if (texto == pistaActual) return;
        pistaActual = texto;
        if (GestorPistas.instancia != null)
        {
            if (string.IsNullOrEmpty(texto)) GestorPistas.instancia.Ocultar();
            else GestorPistas.instancia.MostrarPista(texto);
        }
        else txtPistaRespaldo.text = texto;
    }

    void MostrarContador()
    {
        if (GestorCanastaDevolver.instancia != null) GestorCanastaDevolver.instancia.MostrarContador();
        ActualizarContador();
    }

    void OcultarContador()
    {
        if (GestorCanastaDevolver.instancia != null) GestorCanastaDevolver.instancia.OcultarContador();
        txtContadorRespaldo.text = "";
    }

    void ActualizarContador()
    {
        string texto = "Marionetas en el hoyo: " + echadas + "/" + totalMarionetas;
        var g = GestorCanastaDevolver.instancia;
        if (g != null && g.textoContador != null) g.textoContador.text = texto;
        else txtContadorRespaldo.text = texto;
    }

    // ───────────────────────── el hoyo (se dibuja por código) ─────────────────────────

    void ConstruirHoyo()
    {
        Vector3 basePos = puntoHoyo != null ? puntoHoyo.position : transform.position;
        float y = AlturaSuelo(basePos, out normalHoyo);
        if (Vector3.Dot(normalHoyo, Vector3.up) < 0.5f) normalHoyo = Vector3.up;
        centroHoyo = new Vector3(basePos.x, y, basePos.z);

        hoyo = new GameObject("Hoyo").transform;
        hoyo.SetParent(transform, false);
        hoyo.SetPositionAndRotation(centroHoyo, Quaternion.FromToRotation(Vector3.up, normalHoyo));
        ignorar.Add(hoyo);

        // el montón de tierra va del lado contrario al jugador
        Vector3 lado = jugador != null ? (centroHoyo - PosJugador()) : Vector3.forward;
        lado.y = 0f;
        if (lado.sqrMagnitude < 0.01f) lado = Vector3.forward;
        lado = Quaternion.AngleAxis(70f, Vector3.up) * lado.normalized;
        posMonton = centroHoyo + lado * (radioHoyo + 0.55f);
        posMonton.y = AlturaSuelo(posMonton, out _);

        matTierra = materialTierra != null ? new Material(materialTierra) : MaterialRespaldo();
        matTierra.SetTexture("_BaseMap", TexturaTierra(false));
        matTierra.SetColor("_BaseColor", Color.white);
        matHoyo = new Material(matTierra);
        matHoyo.SetTexture("_BaseMap", TexturaTierra(true));

        discoHoyo = Malla("Hueco", hoyo, MallaDisco(1f, 40), matHoyo);
        discoHoyo.localPosition = Vector3.up * 0.03f;
        borde = Malla("Borde", hoyo, MallaAnillo(1f, 1.45f, 40, 0.11f), matTierra);
        monton = Malla("Monton", null, MallaDomo(0.55f, 1f, 24, 8, 7), matTierra);
        monton.SetParent(transform, true);
        monton.SetPositionAndRotation(posMonton, Quaternion.FromToRotation(Vector3.up, normalHoyo));
        tumba = Malla("Tumba", hoyo, MallaDomo(radioHoyo * 1.25f, 1f, 28, 9, 13), matTierra);

        ActualizarHoyo(0f, 0f);

        if (materialParticulas != null)
        {
            var go = new GameObject("TierraVolando");
            go.transform.SetParent(transform, false);
            tierraPS = go.AddComponent<ParticleSystem>();
            tierraPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = tierraPS.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.32f, 0.22f, 0.13f), new Color(0.2f, 0.13f, 0.07f));
            main.gravityModifier = 1.4f;
            var em = tierraPS.emission;
            em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = materialParticulas;
            tierraPS.Play();
        }
    }

    // cavado: 0..1 ; enterrado: 0..1
    void ActualizarHoyo(float cavado, float enterrado)
    {
        bool hayHoyo = cavado > 0f;
        float rHoyo = Mathf.Lerp(0.35f, radioHoyo, cavado);
        discoHoyo.localScale = new Vector3(rHoyo, 1f, rHoyo) * (1f - 0.6f * enterrado);
        discoHoyo.gameObject.SetActive(enterrado < 0.99f);
        // antes de cavar: solo una mancha de tierra suelta; ya cavado: el hueco oscuro
        discoHoyo.GetComponent<MeshRenderer>().sharedMaterial = hayHoyo ? matHoyo : matTierra;

        borde.gameObject.SetActive(hayHoyo && enterrado < 0.99f);
        borde.localScale = new Vector3(rHoyo, Mathf.Lerp(0.3f, 1f, cavado) * (1f - enterrado), rHoyo);

        float hMonton = Mathf.Lerp(0f, 0.38f, cavado) * Mathf.Lerp(1f, 0.25f, enterrado);
        monton.gameObject.SetActive(hMonton > 0.005f);
        monton.localScale = new Vector3(Mathf.Lerp(0.5f, 1f, cavado), Mathf.Max(0.001f, hMonton), Mathf.Lerp(0.5f, 1f, cavado));

        float hTumba = 0.24f * Suave(enterrado);
        tumba.gameObject.SetActive(hTumba > 0.005f);
        tumba.localScale = new Vector3(1f, Mathf.Max(0.001f, hTumba), 1f);

        // las marionetas se van quedando bajo la tierra
        if (enterrado > 0f)
            foreach (var m in marionetas)
                if (m != null && m.activeSelf) m.transform.position -= normalHoyo * 0.03f;
    }

    void Tierra(Vector3 donde, int cuantas, float fuerza)
    {
        if (tierraPS == null) return;
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < cuantas; i++)
        {
            ep.position = donde + Random.insideUnitSphere * 0.15f;
            Vector3 v = (normalHoyo + Random.insideUnitSphere * 0.7f).normalized * Random.Range(0.6f, 1f) * fuerza;
            ep.velocity = v;
            tierraPS.Emit(ep, 1);
        }
    }

    // ───────────────────────── utilidades ─────────────────────────

    float AlturaSuelo(Vector3 p, out Vector3 normal)
    {
        normal = Vector3.up;
        var hits = Physics.RaycastAll(p + Vector3.up * 30f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue;
        float y = float.NaN;
        foreach (var h in hits)
        {
            if (Ignorado(h.collider.transform)) continue;
            if (h.distance < mejor)
            {
                mejor = h.distance;
                y = h.point.y;
                normal = h.normal;
            }
        }
        if (!float.IsNaN(y)) return y;
        var ter = Terrain.activeTerrain;
        if (ter != null)
        {
            return ter.SampleHeight(p) + ter.transform.position.y;
        }
        return p.y;
    }

    bool Ignorado(Transform t)
    {
        foreach (var r in ignorar)
            if (r != null && t.IsChildOf(r)) return true;
        return false;
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.3f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    void Sonar(AudioClip clip, float vol)
    {
        if (clip != null) src2D.PlayOneShot(clip, vol);
    }

    static float Suave(float x) => x * x * (3f - 2f * x);

    void ConstruirAudio()
    {
        src2D = gameObject.AddComponent<AudioSource>();
        src2D.playOnAwake = false;
        src2D.spatialBlend = 0f;
        srcAmbiente = gameObject.AddComponent<AudioSource>();
        srcAmbiente.playOnAwake = false;
        srcAmbiente.loop = true;
        srcAmbiente.spatialBlend = 0f;
        srcAmbiente.volume = volumenAmbiente;
        srcAmbiente.clip = ambienteBosque;
        if (ambienteBosque != null) srcAmbiente.Play();
        srcMusica = gameObject.AddComponent<AudioSource>();
        srcMusica.playOnAwake = false;
        srcMusica.loop = true;
        srcMusica.spatialBlend = 0f;
    }

    static Material MaterialRespaldo()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        var m = new Material(s);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        return m;
    }

    // tierra con ruido; la del hoyo es más oscura al centro
    static Texture2D TexturaTierra(bool hoyo)
    {
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        var px = new Color32[N * N];
        float sx = Random.value * 50f, sy = Random.value * 50f;
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)N, v = y / (float)N;
                float n = 0.55f * Mathf.PerlinNoise(sx + u * 6f, sy + v * 6f) + 0.3f * Mathf.PerlinNoise(sx + u * 18f, sy + v * 18f) + 0.15f * Random.value;
                Color c = Color.Lerp(new Color(0.2f, 0.13f, 0.08f), new Color(0.36f, 0.25f, 0.15f), n);
                if (hoyo)
                {
                    float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                    float hondo = 1f - Mathf.SmoothStep(0.15f, 0.95f, d);
                    c = Color.Lerp(c, new Color(0.05f, 0.03f, 0.02f), hondo * 0.85f);
                }
                px[y * N + x] = c;
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    Transform Malla(string nombre, Transform padre, Mesh malla, Material mat)
    {
        var go = new GameObject(nombre);
        if (padre != null) go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        return go.transform;
    }

    static Mesh MallaDisco(float radio, int lados)
    {
        var v = new List<Vector3> { Vector3.zero };
        var uv = new List<Vector2> { new Vector2(0.5f, 0.5f) };
        var tri = new List<int>();
        for (int i = 0; i <= lados; i++)
        {
            float a = i / (float)lados * Mathf.PI * 2f;
            v.Add(new Vector3(Mathf.Cos(a) * radio, 0f, Mathf.Sin(a) * radio));
            uv.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(a), 0.5f + 0.5f * Mathf.Sin(a)));
            if (i > 0) { tri.Add(0); tri.Add(i + 1); tri.Add(i); }
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // anillo de tierra levantada alrededor del hoyo
    static Mesh MallaAnillo(float r0, float r1, int lados, float alto)
    {
        const int pasos = 5;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int i = 0; i <= lados; i++)
        {
            float a = i / (float)lados * Mathf.PI * 2f;
            float ruido = 0.75f + 0.5f * Mathf.PerlinNoise(i * 0.4f, 1.7f);
            for (int j = 0; j <= pasos; j++)
            {
                float k = j / (float)pasos;
                float r = Mathf.Lerp(r0, r1, k);
                float h = Mathf.Sin(k * Mathf.PI) * alto * ruido;
                v.Add(new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r));
                uv.Add(new Vector2(i / (float)lados * 4f, k));
            }
        }
        int fila = pasos + 1;
        for (int i = 0; i < lados; i++)
            for (int j = 0; j < pasos; j++)
            {
                int a = i * fila + j, b = (i + 1) * fila + j;
                tri.Add(a); tri.Add(b); tri.Add(a + 1);
                tri.Add(b); tri.Add(b + 1); tri.Add(a + 1);
            }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // domo irregular de tierra (alto = 1, se escala en Y)
    static Mesh MallaDomo(float radio, float alto, int lados, int anillos, int semilla)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        v.Add(new Vector3(0f, alto, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
        for (int j = 1; j <= anillos; j++)
        {
            float k = j / (float)anillos;
            for (int i = 0; i < lados; i++)
            {
                float a = i / (float)lados * Mathf.PI * 2f;
                float ruido = 0.85f + 0.3f * Mathf.PerlinNoise(semilla + Mathf.Cos(a) * 1.5f, semilla + Mathf.Sin(a) * 1.5f + k);
                float r = radio * k * ruido;
                float h = alto * Mathf.Pow(Mathf.Max(0f, 1f - k * k), 0.8f);
                v.Add(new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r));
                uv.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(a) * k, 0.5f + 0.5f * Mathf.Sin(a) * k));
            }
        }
        for (int i = 0; i < lados; i++) { tri.Add(0); tri.Add(1 + (i + 1) % lados); tri.Add(1 + i); }
        for (int j = 1; j < anillos; j++)
        {
            int a0 = 1 + (j - 1) * lados, b0 = 1 + j * lados;
            for (int i = 0; i < lados; i++)
            {
                int i1 = (i + 1) % lados;
                tri.Add(a0 + i); tri.Add(a0 + i1); tri.Add(b0 + i);
                tri.Add(b0 + i); tri.Add(a0 + i1); tri.Add(b0 + i1);
            }
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // ───────────────────────── interfaz ─────────────────────────

    void ConstruirInterfaz()
    {
        var go = new GameObject("FinalBueno_UI", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        Transform raiz = go.transform;

        // respaldo por si la escena no trae los objetivos / pistas de las otras escenas
        txtObjetivoRespaldo = Texto("Objetivo", raiz, 40f, new Color(0.95f, 0.92f, 0.85f), "", new Vector2(0f, 420f), new Vector2(1500f, 60f));
        txtObjetivoRespaldo.alpha = 0f;
        txtPistaRespaldo = Texto("Pista", raiz, 32f, Color.white, "", new Vector2(0f, -380f), new Vector2(1400f, 50f));
        txtContadorRespaldo = Texto("Contador", raiz, 32f, Color.white, "", new Vector2(-700f, 440f), new Vector2(480f, 50f));

        // pantalla final (clara, cálida)
        var blanco = Rect("Blanco", raiz);
        Estirar(blanco);
        grupoBlanco = blanco.gameObject.AddComponent<CanvasGroup>();
        grupoBlanco.alpha = 0f;
        var img = blanco.gameObject.AddComponent<Image>();
        img.color = new Color(0.98f, 0.95f, 0.88f);
        img.raycastTarget = false;

        var fin = Rect("Final", raiz);
        Estirar(fin);
        grupoFinal = fin.gameObject.AddComponent<CanvasGroup>();
        grupoFinal.alpha = 0f;
        txtFinal = Texto("FinalBueno", fin, 130f, new Color(0.55f, 0.38f, 0.12f), textoFinal, new Vector2(0f, 90f), new Vector2(1700f, 180f));
        txtFrase = Texto("Frase", fin, 46f, new Color(0.33f, 0.26f, 0.2f), frase, new Vector2(0f, -40f), new Vector2(1600f, 70f));
        txtFrase.fontStyle = FontStyles.Italic;
        txtGracias = Texto("Gracias", fin, 28f, new Color(0.45f, 0.4f, 0.35f), gracias, new Vector2(0f, -160f), new Vector2(1400f, 50f));
        foreach (var t in new[] { txtFinal, txtFrase, txtGracias }) t.alpha = 0f;
        var cont = Rect("Continuar", fin);
        cont.anchorMin = cont.anchorMax = new Vector2(0.5f, 0f);
        cont.pivot = new Vector2(0.5f, 0f);
        cont.anchoredPosition = new Vector2(0f, 70f);
        cont.sizeDelta = new Vector2(1200f, 50f);
        grupoContinuar = cont.gameObject.AddComponent<CanvasGroup>();
        grupoContinuar.alpha = 0f;
        Texto("Texto", cont, 26f, new Color(0.4f, 0.33f, 0.27f), textoContinuar, Vector2.zero, new Vector2(1200f, 50f));
    }

    TextMeshProUGUI Texto(string nombre, Transform padre, float tam, Color color, string texto, Vector2 pos, Vector2 tamRect)
    {
        var tx = Rect(nombre, padre).gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) tx.font = fuente;
        tx.fontSize = tam;
        tx.color = color;
        tx.alignment = TextAlignmentOptions.Center;
        tx.raycastTarget = false;
        tx.text = texto;
        var r = tx.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tamRect;
        return tx;
    }

    static RectTransform Rect(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        var r = (RectTransform)go.transform;
        r.SetParent(padre, false);
        return r;
    }

    static void Estirar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
