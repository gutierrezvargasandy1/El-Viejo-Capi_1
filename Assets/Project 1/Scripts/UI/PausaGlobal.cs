using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea el menú de pausa automáticamente en TODAS las escenas.
/// No se pone en ningún GameObject: Unity lo ejecuta solo al iniciar el juego.
///
/// Requisito: el prefab debe estar en  Assets/Resources/MenuPausa.prefab
/// </summary>
public static class PausaGlobal
{
    // Escenas donde NO debe aparecer el menú de pausa (nombre exacto)
    static readonly string[] EscenasExcluidas = { "MainMenu" };

    const string NombrePrefab = "MenuPausa";
    static GameObject prefab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        prefab = Resources.Load<GameObject>(NombrePrefab);
        if (prefab == null)
        {
            Debug.LogWarning("[PausaGlobal] No encontré Assets/Resources/MenuPausa.prefab");
            return;
        }
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (modo == LoadSceneMode.Additive) return;                              // escenas extra cargadas encima
        if (System.Array.IndexOf(EscenasExcluidas, escena.name) >= 0) return;  // menú principal, etc.
        if (Object.FindFirstObjectByType<MenuPausa>() != null) return;          // ya hay uno en la escena

        var menu = Object.Instantiate(prefab);
        menu.name = NombrePrefab;
    }
}