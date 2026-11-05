using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Menú de pausa con ESC. Pon el prefab MenuPausa en cada escena jugable
/// (NO en la escena del menú principal).
/// Otros scripts pueden revisar MenuPausa.EstaPausado para no moverse mientras está en pausa.
/// </summary>
public class MenuPausa : MonoBehaviour
{
    /// <summary>Úsalo en tus scripts: if (MenuPausa.EstaPausado) return;</summary>
    public static bool EstaPausado { get; private set; }

    [Header("Paneles")]
    [Tooltip("Objeto con el fondo oscuro y los paneles. Se activa al pausar.")]
    public GameObject contenido;
    public GameObject panelPrincipal;
    public GameObject panelAjustes;

    [Header("Botones")]
    public Button botonReanudar;
    public Button botonAjustes;
    public Button botonMenuPrincipal;
    public Button botonSalirWindows;
    public Button botonVolver;

    [Header("Escenas")]
    [Tooltip("Nombre EXACTO de la escena del menú principal (debe estar en Build Profiles)")]
    public string escenaMenuPrincipal = "MenuPrincipal";

    [Header("Opcional")]
    [Tooltip("Scripts que se apagan durante la pausa (ej. el que mueve la cámara con el mouse)")]
    public Behaviour[] desactivarAlPausar;

    CursorLockMode cursorAntes = CursorLockMode.Locked;
    bool cursorVisibleAntes;

    void Awake()
    {
        EstaPausado = false;
        if (contenido) contenido.SetActive(false);
        AsegurarEventSystem();

        if (botonReanudar) botonReanudar.onClick.AddListener(Reanudar);
        if (botonAjustes) botonAjustes.onClick.AddListener(MostrarAjustes);
        if (botonMenuPrincipal) botonMenuPrincipal.onClick.AddListener(SalirAlMenu);
        if (botonSalirWindows) botonSalirWindows.onClick.AddListener(SalirAWindows);
        if (botonVolver) botonVolver.onClick.AddListener(MostrarPrincipal);
    }

    void Update()
    {
        if (!PresionoEsc()) return;

        if (!EstaPausado) Pausar();
        else if (panelAjustes && panelAjustes.activeSelf) MostrarPrincipal(); // ESC en ajustes = volver
        else Reanudar();
    }

    // ------------------------------------------------------------ Acciones
    public void Pausar()
    {
        EstaPausado = true;
        Time.timeScale = 0f;          // Congela física, animaciones y Timeline
        AudioListener.pause = true;   // Pausa todos los sonidos del juego

        cursorAntes = Cursor.lockState;
        cursorVisibleAntes = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (var b in desactivarAlPausar) if (b) b.enabled = false;

        if (contenido) contenido.SetActive(true);
        MostrarPrincipal();
    }

    public void Reanudar()
    {
        RestaurarJuego();
        Cursor.lockState = cursorAntes;
        Cursor.visible = cursorVisibleAntes;
        foreach (var b in desactivarAlPausar) if (b) b.enabled = true;
        if (contenido) contenido.SetActive(false);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    public void MostrarAjustes()
    {
        if (panelPrincipal) panelPrincipal.SetActive(false);
        if (panelAjustes) panelAjustes.SetActive(true);
    }

    public void MostrarPrincipal()
    {
        if (panelAjustes) panelAjustes.SetActive(false);
        if (panelPrincipal) panelPrincipal.SetActive(true);
    }

    public void SalirAlMenu()
    {
        RestaurarJuego();
        SceneManager.LoadScene(escenaMenuPrincipal);
    }

    public void SalirAWindows()
    {
        RestaurarJuego();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ------------------------------------------------------------ Utilidades
    static void RestaurarJuego()
    {
        EstaPausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void OnDestroy()
    {
        // Por si se cambia de escena estando en pausa
        if (EstaPausado) RestaurarJuego();
    }

    static bool PresionoEsc()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    static void AsegurarEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}
