#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramientas > Pantalla de Carga
/// Crea la configuración y arma la pantalla de carga en una escena o en todas las del Build.
/// Ponlo en una carpeta llamada Editor (ej. Assets/Editor/).
/// </summary>
public class HerramientaPantallaCarga : EditorWindow
{
    const string RutaConfig = "Assets/Resources/ConfigPantallaCarga.asset";

    ConfigPantallaCarga config;
    bool vistaVisible;

    [MenuItem("Herramientas/Pantalla de Carga")]
    static void Abrir()
    {
        var ventana = GetWindow<HerramientaPantallaCarga>("Pantalla de Carga");
        ventana.minSize = new Vector2(360f, 340f);
    }

    void OnEnable()
    {
        if (config == null) config = AssetDatabase.LoadAssetAtPath<ConfigPantallaCarga>(RutaConfig);
    }

    void OnGUI()
    {
        GUILayout.Space(6);
        EditorGUILayout.LabelField("1. Configuración", EditorStyles.boldLabel);
        config = (ConfigPantallaCarga)EditorGUILayout.ObjectField("Config", config, typeof(ConfigPantallaCarga), false);

        if (config == null)
        {
            EditorGUILayout.HelpBox("Primero crea la configuración. Ahí van los consejos, la historia del Capi, los tiempos, el audio y los colores.", MessageType.Info);
            if (GUILayout.Button("Crear configuración", GUILayout.Height(28)))
                config = CrearConfig();
            return;
        }

        if (GUILayout.Button("Editar consejos, historia, tiempos y colores"))
        {
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        int nFondos = config.imagenesFondo == null ? 0 : config.imagenesFondo.Length;
        if (GUILayout.Button($"Usar imágenes de una carpeta como fondos ({nFondos} ahora)"))
            CargarFondosDeCarpeta(config);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("2. Instalar", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Instalar en la escena abierta", GUILayout.Height(28)))
                InstalarEnEscenaAbierta();

            if (GUILayout.Button($"Instalar en TODAS las escenas del Build ({ContarEscenasBuild()})", GUILayout.Height(28)))
                InstalarEnTodas();
        }
        EditorGUILayout.HelpBox(
            "Busca el 'Gestor de Transiciones' de la escena (si no hay, lo crea), borra la pantalla negra vieja " +
            "y arma la pantalla de carga nueva. No toca CambioEscena ni ControlCinematica.",
            MessageType.None);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("3. Revisar", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button(vistaVisible ? "Ocultar vista previa" : "Ver vista previa en la ventana Game"))
                AlternarVista();
        }
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Probar transición (recarga esta escena)"))
                Probar();
        }
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("'Probar transición' se activa con el juego en Play.", MessageType.None);
    }

    // ---------------------------------------------------------------

    static ConfigPantallaCarga CrearConfig()
    {
        var existente = AssetDatabase.LoadAssetAtPath<ConfigPantallaCarga>(RutaConfig);
        if (existente != null) return existente;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var cfg = ScriptableObject.CreateInstance<ConfigPantallaCarga>();
        AssetDatabase.CreateAsset(cfg, RutaConfig);
        AssetDatabase.SaveAssets();
        Selection.activeObject = cfg;
        EditorGUIUtility.PingObject(cfg);
        return cfg;
    }

    // Elige una carpeta dentro de Assets, convierte sus imágenes a Sprite y las pone como fondos
    static void CargarFondosDeCarpeta(ConfigPantallaCarga config)
    {
        string absoluta = EditorUtility.OpenFolderPanel("Carpeta con las imágenes de fondo", Application.dataPath, "");
        if (string.IsNullOrEmpty(absoluta)) return;

        absoluta = absoluta.Replace('\\', '/');
        string datos = Application.dataPath.Replace('\\', '/');
        if (!absoluta.StartsWith(datos))
        {
            EditorUtility.DisplayDialog("Pantalla de carga", "La carpeta tiene que estar dentro de Assets del proyecto.", "OK");
            return;
        }
        string carpeta = "Assets" + absoluta.Substring(datos.Length);

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta });
        var sprites = new System.Collections.Generic.List<Sprite>();
        foreach (var guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) continue;

            bool cambiar = imp.textureType != TextureImporterType.Sprite
                || imp.spriteImportMode != SpriteImportMode.Single
                || imp.mipmapEnabled || imp.maxTextureSize != 2048;
            if (cambiar)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (sp != null) sprites.Add(sp);
        }

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Pantalla de carga", $"No encontré imágenes en {carpeta}.", "OK");
            return;
        }

        Undo.RecordObject(config, "Fondos de carga");
        config.imagenesFondo = sprites.ToArray();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Pantalla de carga", $"Listo: {sprites.Count} imagen(es) de fondo.", "OK");
    }

    static GestorTransicion BuscarGestor(Scene escena)
    {
        foreach (var raiz in escena.GetRootGameObjects())
        {
            var g = raiz.GetComponentInChildren<GestorTransicion>(true);
            if (g != null) return g;
        }
        return null;
    }

    static void Instalar(Scene escena, ConfigPantallaCarga config)
    {
        var gestor = BuscarGestor(escena);

        if (gestor == null)
        {
            var go = new GameObject("Gestor de Transiciones");
            SceneManager.MoveGameObjectToScene(go, escena);
            Undo.RegisterCreatedObjectUndo(go, "Crear Gestor de Transiciones");
            gestor = go.AddComponent<GestorTransicion>();
        }
        else
        {
            // Si es parte de un prefab, hay que desempaquetarlo para poder cambiar sus hijos
            var raizPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(gestor.gameObject);
            if (raizPrefab != null)
                PrefabUtility.UnpackPrefabInstance(raizPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        gestor.config = config;
        gestor.ConstruirUI();
        gestor.Ocultar();

        EditorUtility.SetDirty(gestor);
        EditorSceneManager.MarkSceneDirty(escena);
    }

    void InstalarEnEscenaAbierta()
    {
        var escena = SceneManager.GetActiveScene();
        Instalar(escena, config);
        vistaVisible = false;
        EditorUtility.DisplayDialog("Pantalla de carga",
            $"Instalada en '{escena.name}'.\nGuarda la escena con Ctrl+S.", "OK");
    }

    void InstalarEnTodas()
    {
        // Pregunta si quieres guardar los cambios de las escenas abiertas antes de empezar
        var abiertas = new Scene[SceneManager.sceneCount];
        for (int i = 0; i < abiertas.Length; i++)
            abiertas[i] = SceneManager.GetSceneAt(i);
        if (!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(abiertas)) return;

        string escenaOriginal = SceneManager.GetActiveScene().path;
        var escenas = EditorBuildSettings.scenes;
        int instaladas = 0;

        try
        {
            for (int i = 0; i < escenas.Length; i++)
            {
                var e = escenas[i];
                if (!e.enabled || AssetDatabase.LoadAssetAtPath<SceneAsset>(e.path) == null) continue;

                EditorUtility.DisplayProgressBar("Pantalla de carga", e.path, (float)i / escenas.Length);
                var escena = EditorSceneManager.OpenScene(e.path, OpenSceneMode.Single);
                Instalar(escena, config);
                EditorSceneManager.SaveScene(escena);
                instaladas++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (!string.IsNullOrEmpty(escenaOriginal))
            EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);

        vistaVisible = false;
        EditorUtility.DisplayDialog("Pantalla de carga",
            $"Listo: instalada y guardada en {instaladas} escena(s) del Build.", "OK");
    }

    void AlternarVista()
    {
        var gestor = BuscarGestor(SceneManager.GetActiveScene());
        if (gestor == null || !gestor.UIValida())
        {
            EditorUtility.DisplayDialog("Pantalla de carga", "Primero instala la pantalla de carga en esta escena.", "OK");
            return;
        }

        vistaVisible = !vistaVisible;
        gestor.VistaPrevia(vistaVisible);
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    static void Probar()
    {
        if (GestorTransicion.instancia == null)
        {
            Debug.LogWarning("No hay GestorTransicion activo.");
            return;
        }
        GestorTransicion.instancia.CambiarEscena(SceneManager.GetActiveScene().name);
    }

    static int ContarEscenasBuild()
    {
        int n = 0;
        foreach (var e in EditorBuildSettings.scenes)
            if (e.enabled) n++;
        return n;
    }
}
#endif
