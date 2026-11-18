using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Va en la raíz del monstruo (Monstruo_Final o el provisional).
/// El modelo no trae huesos ni animaciones, así que todo se hace por código:
/// - Aparece con FADE IN (de transparente a sólido) ya asomándose un poco.
/// - Se ASOMA poco a poco: avanza, se detiene, a veces se esconde un poquito y vuelve a asomarse.
/// - Al final se pega más (hacia el cuarto).
/// - NO gira ni se mueve de lado a lado: solo TIEMBLA (con espasmos de vez en cuando).
/// - Cuando lo ahuyentas tiembla fuerte y se va con FADE OUT.
/// </summary>
public class MonstruoVisual : MonoBehaviour
{
    [Tooltip("La cabeza: en el jumpscare se pone justo frente a la cámara")]
    public Transform cabeza;
    [Tooltip("El modelo (hijo). Respira sin mover la raíz")]
    public Transform cuerpo;
    [Tooltip("Luces de la cara / ojos (opcional). También hacen fade")]
    public Light[] lucesOjos;
    public float intensidadOjos = 1.2f;

    [Header("Aparición (fade in)")]
    [Tooltip("Segundos que tarda en pasar de invisible a sólido")]
    public float duracionFade = 1.4f;
    [Tooltip("Segundos del fade out cuando lo ahuyentas")]
    public float duracionFadeSalida = 0.6f;
    [Tooltip("Material transparente para el fade (lo crea la herramienta: 'Aplicar animación: asomarse + fade'). Si está vacío se crea solo al jugar")]
    public Material materialFade;

    [Header("Asomarse")]
    [Tooltip("Qué tanto se asoma al aparecer. 0 = escondido, 1 = en su lugar final")]
    [Range(0f, 1f)] public float asomoInicial = 0.6f;
    [Tooltip("En cuántos pasos se termina de asomar (avanza, se detiene, avanza...)")]
    [Range(1, 6)] public int pasos = 3;
    [Tooltip("Parte del tiempo de ataque que tarda en asomarse completo")]
    [Range(0.2f, 1f)] public float llegadaAsomo = 0.75f;
    [Tooltip("A veces se esconde un poquito y vuelve a asomarse")]
    public bool dudas = true;
    [Range(0f, 0.3f)] public float fuerzaDuda = 0.1f;
    [Tooltip("Al final se pega más hacia el cuarto (metros)")]
    [Range(0f, 0.4f)] public float presion = 0.12f;

    [Header("Temblor (sin girar)")]
    [Tooltip("Temblor al aparecer (metros)")]
    [Range(0f, 0.05f)] public float temblor = 0.006f;
    [Tooltip("Temblor cuando ya está por entrar (metros)")]
    [Range(0f, 0.08f)] public float temblorFinal = 0.022f;
    [Tooltip("Rachas cortas de temblor fuerte")]
    public bool espasmos = true;
    [Tooltip("Respiración (se infla un poquito)")]
    [Range(0f, 0.06f)] public float respiracion = 0.015f;

    [Tooltip("Opcional: Animator de un modelo con animaciones. Recibe el float Progreso (0 a 1)")]
    public Animator animator;

    Vector3 desde;
    Coroutine retirada;
    float semilla, inicio, duda, proximaDuda, finDuda, proximoEspasmo, finEspasmo, ultimoProgreso;
    bool modoJumpscare;
    Vector3 cuerpoPos0, cuerpoEsc0 = Vector3.one;
    Quaternion cuerpoRot0 = Quaternion.identity;
    // donde TÚ lo dejaste en la escena: es su lugar final (no se cambia ni se escala)
    Vector3 posFinal;
    Quaternion rotFinal = Quaternion.identity;
    static readonly int ParamProgreso = Animator.StringToHash("Progreso");
    static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int IdColor = Shader.PropertyToID("_Color");

    // materiales para el fade
    Renderer[] renders;
    Material[][] matsOriginales;
    Material[][] matsFade;
    ShadowCastingMode[] sombras;
    readonly List<Material> fadesCreados = new List<Material>();
    readonly List<Color> coloresBase = new List<Color>();
    bool enFade;
    float alfa = 1f;

    bool preparado;

    void Awake() => Preparar();

    // Guarda cómo está el modelo la primera vez (antes de moverlo), aunque el objeto siga apagado
    void Preparar()
    {
        if (preparado) return;
        preparado = true;
        posFinal = transform.localPosition;
        rotFinal = transform.localRotation;
        if (cuerpo != null)
        {
            cuerpoPos0 = cuerpo.localPosition;
            cuerpoRot0 = cuerpo.localRotation;
            cuerpoEsc0 = cuerpo.localScale;
        }
        renders = GetComponentsInChildren<Renderer>(true);
        matsOriginales = new Material[renders.Length][];
        sombras = new ShadowCastingMode[renders.Length];
        for (int i = 0; i < renders.Length; i++)
        {
            matsOriginales[i] = renders[i].sharedMaterials;
            sombras[i] = renders[i].shadowCastingMode;
        }
    }

    // ───────────────────────── aparecer / asomarse ─────────────────────────

    public void Aparecer(Vector3 desplazamiento) => Aparecer(desplazamiento, TipoEntrada.Ventana);

    public void Aparecer(Vector3 desplazamiento, TipoEntrada tipo)
    {
        Preparar();
        DetenerRetirada();
        desde = desplazamiento;
        semilla = Random.value * 100f;
        inicio = Time.time;
        duda = 0f;
        finDuda = 0f;
        proximaDuda = Time.time + duracionFade + Random.Range(1.5f, 3f);
        finEspasmo = 0f;
        proximoEspasmo = Time.time + duracionFade + Random.Range(0.8f, 2f);
        ultimoProgreso = 0f;
        modoJumpscare = false;
        RestaurarCuerpo();

        transform.localPosition = posFinal + Vector3.Lerp(desde, Vector3.zero, asomoInicial * 0.85f);
        transform.localRotation = rotFinal;
        PonerAlfa(0f);
        LucesOjos(0f);
        gameObject.SetActive(true);
    }

    /// <summary>Para los relámpagos: aparece quieto en su lugar final (solo un instante).</summary>
    public void MostrarSilueta()
    {
        Preparar();
        DetenerRetirada();
        transform.localPosition = posFinal;
        transform.localRotation = rotFinal;
        PonerAlfa(1f);
        gameObject.SetActive(true);
    }

    /// <param name="progreso">0 = recién llegó, 1 = está por entrar</param>
    /// <param name="empujado">0..1 cuánto lo estás ahuyentando</param>
    public void Acercar(float progreso, float empujado)
    {
        Preparar();
        if (!gameObject.activeInHierarchy || retirada != null) return;
        progreso = Mathf.Clamp01(progreso);
        ultimoProgreso = progreso;
        float ahora = Time.time;
        float dt = Time.deltaTime;

        // 1) fade in
        float tAparicion = ahora - inicio;
        float aparicion = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tAparicion / Mathf.Max(0.05f, duracionFade)));
        PonerAlfa(aparicion);

        // 2) se asoma por pasos: avanza, se detiene, avanza...
        float avance = Escalones(Mathf.Clamp01(progreso / Mathf.Max(0.01f, llegadaAsomo)), pasos);
        float asomo = Mathf.Lerp(asomoInicial, 1f, avance);
        asomo = Mathf.Lerp(asomoInicial * 0.85f, asomo, aparicion);       // mientras aparece sube un poquito

        // 3) dudas: se esconde un poco y vuelve a asomarse
        if (dudas && progreso < llegadaAsomo && ahora >= proximaDuda)
        {
            finDuda = ahora + Random.Range(0.35f, 0.8f);
            proximaDuda = ahora + Random.Range(2.5f, 5.5f);
        }
        float metaDuda = ahora < finDuda ? fuerzaDuda : 0f;
        duda = Mathf.Lerp(duda, metaDuda, 1f - Mathf.Exp(-(metaDuda > duda ? 9f : 4f) * dt));
        asomo -= duda;

        Vector3 pos = posFinal + Vector3.Lerp(desde, Vector3.zero, asomo);

        // 4) al final se pega más hacia el cuarto (hacia donde mira)
        float pegado = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progreso - llegadaAsomo) / Mathf.Max(0.01f, 1f - llegadaAsomo)));
        pos += rotFinal * Vector3.forward * (presion * pegado);

        // 5) lo empujas hacia atrás mientras te defiendes
        if (desde.sqrMagnitude > 0.0001f) pos += desde.normalized * (0.3f * empujado);

        // 6) solo tiembla (nada de girar)
        pos += Temblor(progreso, empujado);

        transform.localPosition = pos;
        transform.localRotation = rotFinal;

        Respirar(ahora + semilla, 1f + progreso);
        LucesOjos(progreso);
        if (animator != null) animator.SetFloat(ParamProgreso, progreso);
    }

    // avanza en tramos y se queda quieto un rato en cada uno
    static float Escalones(float x, int n)
    {
        n = Mathf.Max(1, n);
        if (x >= 1f) return 1f;
        float s = x * n;
        float i = Mathf.Floor(s);
        float f = s - i;
        float mover = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((f - 0.4f) / 0.6f));
        return (i + mover) / n;
    }

    Vector3 Temblor(float progreso, float empujado)
    {
        float ahora = Time.time;
        float amp = Mathf.Lerp(temblor, temblorFinal, progreso * progreso);
        amp *= 1f + empujado * 0.8f;                                // tiembla más cuando lo estás ahuyentando

        if (espasmos)
        {
            if (ahora >= proximoEspasmo)
            {
                finEspasmo = ahora + Random.Range(0.15f, 0.45f);
                proximoEspasmo = ahora + Random.Range(1.5f, 4f) * Mathf.Lerp(1.2f, 0.5f, progreso);
            }
            if (ahora < finEspasmo) amp *= 3f;
        }

        float t = ahora * 24f + semilla;
        var ruido = new Vector3(
            Mathf.PerlinNoise(t, 1.3f) - 0.5f,
            Mathf.PerlinNoise(2.7f, t) - 0.5f,
            (Mathf.PerlinNoise(t * 0.8f, 5.1f) - 0.5f) * 0.5f) * 2f;
        return ruido * amp + Random.insideUnitSphere * (amp * 0.25f);
    }

    void Respirar(float t, float fuerza)
    {
        if (cuerpo == null || respiracion <= 0f) return;
        float r = Mathf.Sin(t * 2.2f) * respiracion * fuerza;
        cuerpo.localScale = new Vector3(cuerpoEsc0.x * (1f + r * 0.6f), cuerpoEsc0.y * (1f + r * 0.3f), cuerpoEsc0.z * (1f + r));
    }

    void LucesOjos(float progreso)
    {
        if (lucesOjos == null) return;
        float t = Time.time + semilla;
        foreach (var l in lucesOjos)
            if (l != null)
                l.intensity = intensidadOjos * (0.6f + 0.6f * progreso) * (0.85f + 0.3f * Mathf.PerlinNoise(t * 8f, 0f)) * alfa;
    }

    void Update()
    {
        if (!modoJumpscare || cuerpo == null) return;
        // sacudida violenta durante el jumpscare
        float t = Time.time * 40f;
        cuerpo.localRotation = cuerpoRot0 * Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(-6f, 6f), Random.Range(-7f, 7f));
        cuerpo.localScale = cuerpoEsc0 * (1f + 0.05f * Mathf.Sin(t));
        cuerpo.localPosition = cuerpoPos0 + Random.insideUnitSphere * 0.01f;
    }

    // ───────────────────────── irse ─────────────────────────

    public void Retirarse(Vector3 hacia)
    {
        if (!gameObject.activeInHierarchy) return;
        DetenerRetirada();
        retirada = StartCoroutine(RutinaRetirada(hacia));
    }

    IEnumerator RutinaRetirada(Vector3 hacia)
    {
        transform.localRotation = rotFinal;
        Vector3 a = transform.localPosition;
        float alfa0 = alfa;

        // tiembla fuerte un instante (sin girar)
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            transform.localPosition = a + Random.insideUnitSphere * 0.03f;
            yield return null;
        }

        // se esconde por donde llegó mientras se desvanece
        Vector3 b = posFinal + hacia * 1.15f;
        float d = Mathf.Max(0.1f, duracionFadeSalida);
        t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float e = Mathf.Clamp01(t / d);
            PonerAlfa(alfa0 * (1f - Mathf.SmoothStep(0f, 1f, e)));
            LucesOjos(ultimoProgreso);
            transform.localPosition = Vector3.Lerp(a, b, e * e) + Random.insideUnitSphere * (0.012f * (1f - e));
            yield return null;
        }
        retirada = null;
        PonerAlfa(1f);
        transform.localPosition = posFinal;
        gameObject.SetActive(false);
    }

    public void Ocultar()
    {
        Preparar();
        DetenerRetirada();
        modoJumpscare = false;
        RestaurarCuerpo();
        PonerAlfa(1f);
        transform.localPosition = posFinal;
        transform.localRotation = rotFinal;
        gameObject.SetActive(false);
    }

    /// <summary>Lo usa el jumpscare: sólido, ojos al máximo y sacudida.</summary>
    public void ModoJumpscare()
    {
        Preparar();
        DetenerRetirada();
        PonerAlfa(1f);
        modoJumpscare = true;
        if (lucesOjos != null)
            foreach (var l in lucesOjos)
                if (l != null) l.intensity = intensidadOjos * 2.5f;
        if (animator != null) animator.SetFloat(ParamProgreso, 1f);
    }

    void RestaurarCuerpo()
    {
        Preparar();
        if (cuerpo == null) return;
        cuerpo.localPosition = cuerpoPos0;
        cuerpo.localRotation = cuerpoRot0;
        cuerpo.localScale = cuerpoEsc0;
    }

    void DetenerRetirada()
    {
        if (retirada != null)
        {
            StopCoroutine(retirada);
            retirada = null;
        }
    }

    // ───────────────────────── fade (transparencia) ─────────────────────────

    /// <summary>0 = invisible, 1 = sólido. En 1 vuelve a su material normal (opaco).</summary>
    void PonerAlfa(float a)
    {
        Preparar();
        alfa = Mathf.Clamp01(a);
        if (renders == null || renders.Length == 0) return;

        if (alfa >= 0.999f)
        {
            if (!enFade) return;
            for (int i = 0; i < renders.Length; i++)
            {
                if (renders[i] == null) continue;
                renders[i].sharedMaterials = matsOriginales[i];
                renders[i].shadowCastingMode = sombras[i];
            }
            enFade = false;
            return;
        }

        CrearMaterialesFade();
        if (!enFade)
        {
            for (int i = 0; i < renders.Length; i++)
            {
                if (renders[i] == null) continue;
                renders[i].sharedMaterials = matsFade[i];
                renders[i].shadowCastingMode = ShadowCastingMode.Off;   // un fantasma no hace sombra completa
            }
            enFade = true;
        }
        for (int i = 0; i < fadesCreados.Count; i++)
        {
            var m = fadesCreados[i];
            Color c = coloresBase[i];
            c.a *= alfa;
            if (m.HasProperty(IdBaseColor)) m.SetColor(IdBaseColor, c);
            if (m.HasProperty(IdColor)) m.SetColor(IdColor, c);
        }
    }

    void CrearMaterialesFade()
    {
        if (matsFade != null) return;
        var copias = new Dictionary<Material, Material>();
        var unicos = new HashSet<Material>();
        foreach (var orig in matsOriginales)
            foreach (var o in orig)
                if (o != null) unicos.Add(o);
        matsFade = new Material[renders.Length][];
        for (int i = 0; i < renders.Length; i++)
        {
            var orig = matsOriginales[i];
            matsFade[i] = new Material[orig.Length];
            for (int j = 0; j < orig.Length; j++)
            {
                var o = orig[j];
                if (o == null) continue;
                Material f;
                if (!copias.TryGetValue(o, out f))
                {
                    // si la herramienta ya creó el material transparente se usa ese (así no falla en Android)
                    bool usarAsset = materialFade != null && (unicos.Count == 1 || materialFade.name == o.name + "_Fade");
                    f = new Material(usarAsset ? materialFade : o);
                    f.name = o.name + " (fade)";
                    HacerTransparente(f);
                    Color c = Color.white;
                    if (o.HasProperty(IdBaseColor)) c = o.GetColor(IdBaseColor);
                    else if (o.HasProperty(IdColor)) c = o.GetColor(IdColor);
                    copias[o] = f;
                    fadesCreados.Add(f);
                    coloresBase.Add(c);
                }
                matsFade[i][j] = f;
            }
        }
    }

    /// <summary>Pone un material URP Lit en modo transparente (alpha). Lo usa también la herramienta.</summary>
    public static void HacerTransparente(Material m)
    {
        if (m == null) return;
        m.SetFloat("_Surface", 1f);                                    // Transparent
        m.SetFloat("_Blend", 0f);                                      // Alpha
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 1f);                                     // así no se le ve "por dentro" a medio fade
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.DisableKeyword("_ALPHAMODULATE_ON");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
    }

    void OnDestroy()
    {
        foreach (var m in fadesCreados)
            if (m != null) Destroy(m);
        fadesCreados.Clear();
    }
}
