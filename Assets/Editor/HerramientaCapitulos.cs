#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Herramientas > Capítulos y final
/// 1) Pone el título de capítulo (TituloCapitulo) en las escenas de cada capítulo.
/// 2) Arma la escena final: capítulo 4 + la cinemática con su sonido + pantalla de FIN,
///    la agrega al Build y hace que al ganar la noche se vaya a ella.
/// No mueve ni cambia nada de tus escenas: solo agrega un objeto "TituloCapitulo" (y en la final "CinematicaFinal").
/// </summary>
public class HerramientaCapitulos : EditorWindow
{
    const string Base = "Assets/Project 1/Scenes/Levels/";

    class Capitulo
    {
        public SceneAsset escena;
        public int numero;
        public string titulo, subtitulo;
    }

    List<Capitulo> capitulos;
    VideoClip video;
    SceneAsset escenaNoche;
    Vector2 scroll;

    [MenuItem("Herramientas/Capítulos y final")]
    static void Abrir() => GetWindow<HerramientaCapitulos>("Capítulos y final");

    void OnEnable()
    {
        capitulos = new List<Capitulo>
        {
            Nuevo(1, "Un día normal", "Celaya, Guanajuato", Base + "Nivel 1/Esenas 1/Esenas 1.unity"),
            Nuevo(2, "Cada una en su lugar", "La noche llega a casa", Base + "Nivel 1/Esena 2/Esena 2 Noche.unity"),
            Nuevo(3, "Sobrevive", "Hasta las 6 de la mañana", Base + "Nivel 2/HabitacionScene.unity"),
            Nuevo(4, "El final", "", Base + "Nivel 2/FinalJuegoScene.unity"),
        };
        escenaNoche = AssetDatabase.LoadAssetAtPath<SceneAsset>(Base + "Nivel 2/HabitacionScene.unity");
        video = BuscarVideo();
    }

    static Capitulo Nuevo(int n, string titulo, string sub, string ruta)
    {
        string clave = "CapiCapitulo" + n;
        return new Capitulo
        {
            numero = n,
            titulo = EditorPrefs.GetString(clave + "Titulo", titulo),
            subtitulo = EditorPrefs.GetString(clave + "Sub", sub),
            escena = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorPrefs.GetString(clave + "Escena", ruta)),
        };
    }

    void Guardar(Capitulo c)
    {
        string clave = "CapiCapitulo" + c.numero;
        EditorPrefs.SetString(clave + "Titulo", c.titulo);
        EditorPrefs.SetString(clave + "Sub", c.subtitulo);
        EditorPrefs.SetString(clave + "Escena", c.escena != null ? AssetDatabase.GetAssetPath(c.escena) : "");
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Presiona 'HACER TODO' y listo. Pone el título de cada capítulo en su escena, arma la escena final " +
            "(capítulo 4 + cinemática con sonido + pantalla de FIN), la agrega al Build y hace que al ganar la noche se vaya ahí.\n" +
            "No mueve nada de tus escenas: solo agrega objetos nuevos.", MessageType.Info);
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("HACER TODO", GUILayout.Height(40))) HacerTodo();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("1. Capítulos", EditorStyles.boldLabel);
        foreach (var c in capitulos)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Capítulo " + c.numero, EditorStyles.miniBoldLabel);
            c.escena = (SceneAsset)EditorGUILayout.ObjectField("Escena", c.escena, typeof(SceneAsset), false);
            c.titulo = EditorGUILayout.TextField("Título", c.titulo);
            c.subtitulo = EditorGUILayout.TextField("Subtítulo", c.subtitulo);
            if (EditorGUI.EndChangeCheck()) Guardar(c);
            using (new EditorGUI.DisabledScope(c.escena == null || Application.isPlaying))
                if (GUILayout.Button("Instalar solo este")) InstalarVarios(new List<Capitulo> { c });
            EditorGUILayout.EndVertical();
        }
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("Instalar los 4 capítulos", GUILayout.Height(28))) InstalarVarios(capitulos);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2. Escena final (capítulo 4)", EditorStyles.boldLabel);
        video = (VideoClip)EditorGUILayout.ObjectField("Video de la cinemática", video, typeof(VideoClip), false);
        escenaNoche = (SceneAsset)EditorGUILayout.ObjectField("Escena de la noche", escenaNoche, typeof(SceneAsset), false);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("Armar escena final", GUILayout.Height(28))) ArmarFinal(true);

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Quitar el título de capítulo de la escena abierta"))
        {
            foreach (var tc in Object.FindObjectsByType<TituloCapitulo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Undo.DestroyObjectImmediate(tc.gameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        EditorGUILayout.EndScrollView();
    }

    // ───────────────────────── capítulos ─────────────────────────

    void HacerTodo()
    {
        if (!GuardarAbiertas()) return;
        string volver = SceneManager.GetActiveScene().path;
        int n = 0;
        foreach (var c in capitulos) if (InstalarEn(c)) n++;
        bool final = ArmarFinal(false);
        Volver(volver);
        EditorUtility.DisplayDialog("Capítulos y final",
            "Listo:\n• Título de capítulo en " + n + " escenas\n" +
            (final ? "• Escena final armada, agregada al Build y conectada al ganar la noche\n" : "• La escena final NO se armó (revisa el video)\n") +
            "\nPara probar el final: abre la escena final y dale Play.\n" +
            "Para probar todo: juega la noche y gana (F4 te adelanta a las 5:50 AM).", "OK");
    }

    void InstalarVarios(List<Capitulo> lista)
    {
        if (!GuardarAbiertas()) return;
        string volver = SceneManager.GetActiveScene().path;
        int n = 0;
        foreach (var c in lista) if (InstalarEn(c)) n++;
        Volver(volver);
        EditorUtility.DisplayDialog("Capítulos", "Listo: título de capítulo en " + n + " escena(s).", "OK");
    }

    bool InstalarEn(Capitulo c)
    {
        if (c.escena == null) return false;
        string ruta = AssetDatabase.GetAssetPath(c.escena);
        var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        PonerTitulo(c);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        return true;
    }

    static TituloCapitulo PonerTitulo(Capitulo c)
    {
        TituloCapitulo tc = null;
        foreach (var t in Object.FindObjectsByType<TituloCapitulo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (tc == null) tc = t;
            else Object.DestroyImmediate(t.gameObject);                 // solo uno por escena
        if (tc == null)
        {
            var go = new GameObject("TituloCapitulo");
            tc = go.AddComponent<TituloCapitulo>();
        }
        tc.numero = c.numero;
        tc.titulo = c.titulo;
        tc.subtitulo = c.subtitulo;
        tc.sonido = Clip("Capitulo_Golpe");
        EditorUtility.SetDirty(tc);
        return tc;
    }

    // ───────────────────────── escena final ─────────────────────────

    bool ArmarFinal(bool solo)
    {
        var cap = capitulos[3];
        if (video == null) video = BuscarVideo();
        if (video == null)
        {
            EditorUtility.DisplayDialog("Escena final", "No encontré el video 'Escena_Final'. Ponlo en el campo 'Video de la cinemática'.", "OK");
            return false;
        }
        if (cap.escena == null)
        {
            EditorUtility.DisplayDialog("Escena final", "Falta la escena del capítulo 4 (FinalJuegoScene).", "OK");
            return false;
        }
        if (solo && !GuardarAbiertas()) return false;
        string volver = SceneManager.GetActiveScene().path;

        string rutaFinal = AssetDatabase.GetAssetPath(cap.escena);
        var escena = EditorSceneManager.OpenScene(rutaFinal, OpenSceneMode.Single);

        foreach (var viejo in Object.FindObjectsByType<CinematicaFinal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(viejo.gameObject);
        var go = new GameObject("CinematicaFinal");
        var cf = go.AddComponent<CinematicaFinal>();
        cf.video = video;
        cf.pista = Clip("Final_Pista");
        cf.musicaFin = Clip("Final_Musica");
        EditorUtility.SetDirty(cf);

        // cámara en negro (el video va encima) y que haya quien escuche
        Camera cam = Camera.main;
        if (cam == null)
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) { cam = c; break; }
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        EditorUtility.SetDirty(cam);
        if (Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0)
            cam.gameObject.AddComponent<AudioListener>();

        PonerTitulo(cap);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        AgregarAlBuild(rutaFinal);
        string nombreFinal = Path.GetFileNameWithoutExtension(rutaFinal);

        // al ganar la noche → escena final
        bool conectado = false;
        if (escenaNoche != null)
        {
            var noche = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(escenaNoche), OpenSceneMode.Single);
            foreach (var g in Object.FindObjectsByType<GestorNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                g.escenaAlGanar = nombreFinal;
                EditorUtility.SetDirty(g);
                conectado = true;
            }
            if (conectado)
            {
                EditorSceneManager.MarkSceneDirty(noche);
                EditorSceneManager.SaveScene(noche);
            }
        }

        if (solo)
        {
            Volver(volver);
            EditorUtility.DisplayDialog("Escena final",
                "Listo: escena final armada (capítulo 4 + cinemática con sonido + FIN) y agregada al Build.\n" +
                (conectado ? "Al ganar la noche ahora se va a '" + nombreFinal + "'." : "OJO: no encontré GestorNoche en la escena de la noche.") +
                "\n\nPruébala: abre la escena final y dale Play.", "OK");
        }
        return true;
    }

    static void AgregarAlBuild(string ruta)
    {
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool esta = false;
        for (int i = 0; i < lista.Count; i++)
            if (lista[i].path == ruta)
            {
                esta = true;
                if (!lista[i].enabled) lista[i] = new EditorBuildSettingsScene(ruta, true);
            }
        if (!esta) lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ───────────────────────── utilidades ─────────────────────────

    static bool GuardarAbiertas()
    {
        var abiertas = new Scene[SceneManager.sceneCount];
        for (int i = 0; i < abiertas.Length; i++) abiertas[i] = SceneManager.GetSceneAt(i);
        return EditorSceneManager.SaveModifiedScenesIfUserWantsTo(abiertas);
    }

    static void Volver(string ruta)
    {
        if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta) && SceneManager.GetActiveScene().path != ruta)
            EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
    }

    static VideoClip BuscarVideo()
    {
        foreach (var guid in AssetDatabase.FindAssets("Escena_Final t:VideoClip"))
        {
            var v = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (v != null) return v;
        }
        return null;
    }

    static AudioClip Clip(string nombre)
    {
        foreach (var guid in AssetDatabase.FindAssets(nombre + " t:AudioClip"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) == nombre)
                return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }
        return null;
    }
}
#endif
