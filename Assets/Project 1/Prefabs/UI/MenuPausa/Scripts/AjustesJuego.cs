using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Ajustes de volumen y gráficos. Se guardan en PlayerPrefs y se aplican solos al abrir el juego.
/// </summary>
public class AjustesJuego : MonoBehaviour
{
    [Header("Volumen")]
    public Slider sliderGeneral;
    public Slider sliderMusica;
    public Slider sliderEfectos;

    [Tooltip("Opcional. AudioMixer con parámetros expuestos para música y efectos. " +
             "Si lo dejas vacío, solo funciona el Volumen General.")]
    public AudioMixer mezclador;
    public string parametroMusica = "VolMusica";
    public string parametroEfectos = "VolEfectos";

    [Tooltip("Filas de Música y Efectos: se ocultan si no hay mezclador")]
    public GameObject filaMusica;
    public GameObject filaEfectos;

    [Header("Gráficos")]
    public TMP_Dropdown dropdownCalidad;
    public TMP_Dropdown dropdownResolucion;
    public Toggle togglePantallaCompleta;
    public Toggle toggleVSync;

    // Claves de PlayerPrefs
    const string K_GENERAL = "aj_vol_general";
    const string K_MUSICA = "aj_vol_musica";
    const string K_EFECTOS = "aj_vol_efectos";
    const string K_CALIDAD = "aj_calidad";
    const string K_RES_W = "aj_res_w";
    const string K_RES_H = "aj_res_h";
    const string K_COMPLETA = "aj_pantalla_completa";
    const string K_VSYNC = "aj_vsync";

    readonly List<Resolution> resoluciones = new List<Resolution>();

    static readonly Dictionary<string, string> NombresCalidad = new Dictionary<string, string>
    {
        { "Very Low", "Muy baja" }, { "Low", "Baja" }, { "Medium", "Media" },
        { "High", "Alta" }, { "Very High", "Muy alta" }, { "Ultra", "Ultra" },
        { "Performant", "Rendimiento" }, { "Balanced", "Equilibrada" }, { "High Fidelity", "Alta fidelidad" },
        { "Mobile", "Móvil" }, { "PC", "PC" },
    };

    // ------------------------------------------------------------ Aplicar al arrancar el juego
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AplicarAlIniciar()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(K_GENERAL, 1f);

        if (PlayerPrefs.HasKey(K_CALIDAD))
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(K_CALIDAD), true);
        if (PlayerPrefs.HasKey(K_VSYNC))
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(K_VSYNC); // va después de la calidad

        if (PlayerPrefs.HasKey(K_RES_W))
        {
            bool completa = PlayerPrefs.GetInt(K_COMPLETA, 1) == 1;
            Screen.SetResolution(PlayerPrefs.GetInt(K_RES_W), PlayerPrefs.GetInt(K_RES_H),
                completa ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }

    // ------------------------------------------------------------ UI
    void Start()
    {
        PrepararVolumen();
        PrepararCalidad();
        PrepararResolucion();
        PrepararToggles();
    }

    void PrepararVolumen()
    {
        Configurar(sliderGeneral, PlayerPrefs.GetFloat(K_GENERAL, 1f), CambiarGeneral);

        bool hayMezclador = mezclador != null;
        if (filaMusica) filaMusica.SetActive(hayMezclador);
        if (filaEfectos) filaEfectos.SetActive(hayMezclador);
        if (!hayMezclador) return;

        float m = PlayerPrefs.GetFloat(K_MUSICA, 1f), e = PlayerPrefs.GetFloat(K_EFECTOS, 1f);
        Configurar(sliderMusica, m, CambiarMusica);
        Configurar(sliderEfectos, e, CambiarEfectos);
        mezclador.SetFloat(parametroMusica, ADecibeles(m));
        mezclador.SetFloat(parametroEfectos, ADecibeles(e));
    }

    static void Configurar(Slider s, float valor, UnityEngine.Events.UnityAction<float> accion)
    {
        if (!s) return;
        s.minValue = 0f; s.maxValue = 1f;
        s.SetValueWithoutNotify(valor);
        s.onValueChanged.AddListener(accion);
    }

    void PrepararCalidad()
    {
        if (!dropdownCalidad) return;
        var opciones = new List<string>();
        foreach (var n in QualitySettings.names)
            opciones.Add(NombresCalidad.TryGetValue(n, out var es) ? es : n);
        dropdownCalidad.ClearOptions();
        dropdownCalidad.AddOptions(opciones);
        dropdownCalidad.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
        dropdownCalidad.RefreshShownValue();
        dropdownCalidad.onValueChanged.AddListener(CambiarCalidad);
    }

    void PrepararResolucion()
    {
        if (!dropdownResolucion) return;
        resoluciones.Clear();
        var opciones = new List<string>();
        int actual = 0;
        foreach (var r in Screen.resolutions)
        {
            // Quitar repetidas (misma resolución con distinta frecuencia)
            if (resoluciones.Exists(x => x.width == r.width && x.height == r.height)) continue;
            resoluciones.Add(r);
            opciones.Add($"{r.width} x {r.height}");
            if (r.width == Screen.width && r.height == Screen.height) actual = resoluciones.Count - 1;
        }
        dropdownResolucion.ClearOptions();
        dropdownResolucion.AddOptions(opciones);
        dropdownResolucion.SetValueWithoutNotify(actual);
        dropdownResolucion.RefreshShownValue();
        dropdownResolucion.onValueChanged.AddListener(CambiarResolucion);
    }

    void PrepararToggles()
    {
        if (togglePantallaCompleta)
        {
            togglePantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreenMode != FullScreenMode.Windowed);
            togglePantallaCompleta.onValueChanged.AddListener(CambiarPantallaCompleta);
        }
        if (toggleVSync)
        {
            toggleVSync.SetIsOnWithoutNotify(QualitySettings.vSyncCount > 0);
            toggleVSync.onValueChanged.AddListener(CambiarVSync);
        }
    }

    // ------------------------------------------------------------ Cambios
    public void CambiarGeneral(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(K_GENERAL, v);
    }

    public void CambiarMusica(float v)
    {
        if (mezclador) mezclador.SetFloat(parametroMusica, ADecibeles(v));
        PlayerPrefs.SetFloat(K_MUSICA, v);
    }

    public void CambiarEfectos(float v)
    {
        if (mezclador) mezclador.SetFloat(parametroEfectos, ADecibeles(v));
        PlayerPrefs.SetFloat(K_EFECTOS, v);
    }

    public void CambiarCalidad(int i)
    {
        QualitySettings.SetQualityLevel(i, true);
        if (toggleVSync) QualitySettings.vSyncCount = toggleVSync.isOn ? 1 : 0; // la calidad puede cambiar el VSync
        PlayerPrefs.SetInt(K_CALIDAD, i);
    }

    public void CambiarResolucion(int i)
    {
        if (i < 0 || i >= resoluciones.Count) return;
        var r = resoluciones[i];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
        PlayerPrefs.SetInt(K_RES_W, r.width);
        PlayerPrefs.SetInt(K_RES_H, r.height);
    }

    public void CambiarPantallaCompleta(bool completa)
    {
        Screen.fullScreenMode = completa ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        PlayerPrefs.SetInt(K_COMPLETA, completa ? 1 : 0);
    }

    public void CambiarVSync(bool activo)
    {
        QualitySettings.vSyncCount = activo ? 1 : 0;
        PlayerPrefs.SetInt(K_VSYNC, QualitySettings.vSyncCount);
    }

    void OnDisable() => PlayerPrefs.Save();

    static float ADecibeles(float v) => Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;
}
