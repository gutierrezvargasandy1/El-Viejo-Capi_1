using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Herramientas → Celular (Android)
/// Deja el juego listo para el teléfono: pantalla horizontal, IL2CPP ARM64, texturas y audios
/// más ligeros para Android (en la PC se quedan igual), y crea el APK.
/// Los controles táctiles (ControlesTactiles.cs) salen solos en el celular: no hay que poner nada en las escenas.
/// </summary>
public class HerramientaAndroid : EditorWindow
{
    const string ClaveId = "CapiAndroidId";
    const string ClaveForzar = "ForzarControlesTactiles";
    const string RutaApk = "Builds/Android/ElViejoCapi.apk";

    string idPaquete;
    Vector2 scroll;

    [MenuItem("Herramientas/Celular (Android)")]
    static void Abrir() => GetWindow<HerramientaAndroid>("Celular (Android)");

    void OnEnable()
    {
        idPaquete = EditorPrefs.GetString(ClaveId, "com.gearmat.elviejocapi");
    }

    static bool ModuloAndroid => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
    static bool EnAndroid => EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Los controles táctiles salen solos en el celular (joystick, arrastrar para mirar, tocar = clic y botones " +
            "USAR / SALTAR / LINTERNA / ACCIÓN / VOLVER / OMITIR / PAUSA según la escena). En la PC todo sigue igual.\n\n" +
            "Pasos: 1) Preparar  →  2) Cambiar a Android  →  3) Crear el APK (o instalarlo directo en tu celular por USB).",
            MessageType.Info);

        // estado
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Estado", EditorStyles.boldLabel);
        Estado("Módulo de Android instalado", ModuloAndroid,
            "Falta: abre Unity Hub → Installs → tu Unity 6 → Add modules → 'Android Build Support' (con OpenJDK y Android SDK & NDK).");
        Estado("Plataforma actual: " + EditorUserBuildSettings.activeBuildTarget, EnAndroid, "Todavía está en PC; usa el paso 2.");
        Estado("Pantalla horizontal", !PlayerSettings.allowedAutorotateToPortrait, "Se arregla en el paso 1.");
        Estado("IL2CPP + ARM64 (lo pide Google Play)",
            PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP &&
            (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0, "Se arregla en el paso 1.");

        // 1
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("1. Preparar el proyecto", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        idPaquete = EditorGUILayout.TextField("Nombre del paquete", idPaquete);
        if (EditorGUI.EndChangeCheck()) EditorPrefs.SetString(ClaveId, idPaquete);
        EditorGUILayout.LabelField("Pantalla horizontal, IL2CPP ARM64, texturas máx. 2048 (mapas 1024) y audios largos en streaming. Solo para Android.",
            EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("Preparar para Android", GUILayout.Height(30))) Preparar();

        // 2
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2. Cambiar la plataforma a Android", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Tarda: Unity vuelve a importar las texturas para el celular (la primera vez puede ser un buen rato).",
            EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(Application.isPlaying || EnAndroid || !ModuloAndroid))
            if (GUILayout.Button(EnAndroid ? "Ya está en Android" : "Cambiar a Android", GUILayout.Height(30))) CambiarAAndroid();

        // 3
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3. Crear el juego para el celular", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(Application.isPlaying || !ModuloAndroid))
        {
            if (GUILayout.Button("Crear el APK", GUILayout.Height(30))) Construir(false);
            if (GUILayout.Button("Crear el APK e instalarlo en el celular conectado por USB", GUILayout.Height(30))) Construir(true);
        }
        EditorGUILayout.LabelField("El APK queda en la carpeta del proyecto: " + RutaApk +
            ". Pásalo al celular y ábrelo (acepta 'instalar apps de origen desconocido'). " +
            "Para instalar por USB: en el celular activa Opciones de desarrollador → Depuración USB.", EditorStyles.wordWrappedMiniLabel);

        // pruebas
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Probar en la PC", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "La mejor forma: Window → General → Device Simulator, escoge un celular y dale Play: salen los controles y el mouse hace de dedo.",
            MessageType.None);
        bool forzar = PlayerPrefs.GetInt(ClaveForzar, 0) == 1;
        bool nuevo = EditorGUILayout.ToggleLeft("Mostrar los controles táctiles también en la ventana Game normal", forzar);
        if (nuevo != forzar) { PlayerPrefs.SetInt(ClaveForzar, nuevo ? 1 : 0); PlayerPrefs.Save(); }

        EditorGUILayout.EndScrollView();
    }

    static void Estado(string texto, bool bien, string ayuda)
    {
        EditorGUILayout.BeginHorizontal();
        var color = GUI.color;
        GUI.color = bien ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.75f, 0.4f);
        EditorGUILayout.LabelField(bien ? "✓" : "!", GUILayout.Width(16));
        GUI.color = color;
        EditorGUILayout.LabelField(texto);
        EditorGUILayout.EndHorizontal();
        if (!bien && !string.IsNullOrEmpty(ayuda)) EditorGUILayout.LabelField("    " + ayuda, EditorStyles.wordWrappedMiniLabel);
    }

    // ------------------------------------------------------------------ 1. preparar
    void Preparar()
    {
        var notas = new List<string>();

        // pantalla y compilación
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        if (!string.IsNullOrEmpty(idPaquete)) PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, idPaquete.Trim());
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        EditorUserBuildSettings.buildAppBundle = false;
        notas.Add("pantalla horizontal, IL2CPP ARM64, texturas ASTC, paquete " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));

        int texturas = 0, audios = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            texturas = AjustarTexturas();
            audios = AjustarAudios();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }
        notas.Add(texturas + " texturas más ligeras para el celular");
        notas.Add(audios + " audios ajustados (los largos en streaming)");

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Celular (Android)",
            "Listo:\n• " + string.Join("\n• ", notas.ToArray()) +
            "\n\nEn la PC no cambia nada. Ahora usa el paso 2 (Cambiar a Android).", "OK");
    }

    static bool EsDelJuego(string ruta)
    {
        if (!ruta.StartsWith("Assets/")) return false;
        if (ruta.Contains("/Editor/") || ruta.StartsWith("Assets/TutorialInfo")) return false;
        return true;
    }

    static int AjustarTexturas()
    {
        int n = 0;
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (i % 20 == 0) EditorUtility.DisplayProgressBar("Celular (Android)", "Texturas: " + Path.GetFileName(ruta), (float)i / Mathf.Max(1, guids.Length));
            if (!EsDelJuego(ruta)) continue;
            var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) continue;
            if (imp.textureType == TextureImporterType.Sprite || imp.textureType == TextureImporterType.GUI) continue;   // menús e interfaz: igual
            if (imp.textureType == TextureImporterType.Lightmap || imp.textureType == TextureImporterType.DirectionalLightmap) continue;

            string nombre = Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant();
            bool secundario = imp.textureType == TextureImporterType.NormalMap || nombre.Contains("normal") ||
                              nombre.Contains("rough") || nombre.Contains("metal") || nombre.Contains("_ao") || nombre.Contains("height");
            int objetivo = secundario ? 1024 : 2048;
            int maximo = Mathf.Min(imp.maxTextureSize, objetivo);

            var ps = imp.GetPlatformTextureSettings("Android");
            if (ps.overridden && ps.maxTextureSize <= maximo) continue;      // ya estaba
            if (!ps.overridden && imp.maxTextureSize <= maximo) continue;    // ya es chica
            ps.name = "Android";
            ps.overridden = true;
            ps.maxTextureSize = maximo;
            ps.format = TextureImporterFormat.Automatic;
            ps.textureCompression = TextureImporterCompression.Compressed;
            ps.compressionQuality = 50;
            imp.SetPlatformTextureSettings(ps);
            imp.SaveAndReimport();
            n++;
        }
        return n;
    }

    static int AjustarAudios()
    {
        int n = 0;
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (i % 10 == 0) EditorUtility.DisplayProgressBar("Celular (Android)", "Audios: " + Path.GetFileName(ruta), (float)i / Mathf.Max(1, guids.Length));
            if (!EsDelJuego(ruta)) continue;
            var imp = AssetImporter.GetAtPath(ruta) as AudioImporter;
            if (imp == null) continue;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            if (clip == null) continue;

            var s = imp.GetOverrideSampleSettings("Android");
            AudioClipLoadType carga = clip.length > 20f ? AudioClipLoadType.Streaming
                                    : clip.length > 4f ? AudioClipLoadType.CompressedInMemory
                                    : AudioClipLoadType.DecompressOnLoad;
            if (s.loadType == carga && s.compressionFormat == AudioCompressionFormat.Vorbis) continue;
            s.loadType = carga;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            imp.SetOverrideSampleSettings("Android", s);
            imp.SaveAndReimport();
            n++;
        }
        return n;
    }

    // ------------------------------------------------------------------ 2. plataforma
    static void CambiarAAndroid()
    {
        if (!ModuloAndroid)
        {
            EditorUtility.DisplayDialog("Celular (Android)",
                "Falta el módulo de Android.\n\nUnity Hub → Installs → tu Unity 6 → (engrane) Add modules → marca 'Android Build Support' " +
                "con 'OpenJDK' y 'Android SDK & NDK Tools' → Install. Luego reabre el proyecto.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Celular (Android)",
            "Unity va a cambiar a Android y a volver a importar las texturas. Puede tardar varios minutos.\n\n" +
            "Guarda tu escena antes. ¿Seguimos?", "Cambiar", "Cancelar")) return;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    // ------------------------------------------------------------------ 3. construir
    static void Construir(bool instalar)
    {
        if (!ModuloAndroid) { CambiarAAndroid(); return; }
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var escenas = new List<string>();
        foreach (var e in EditorBuildSettings.scenes)
            if (e.enabled && File.Exists(e.path)) escenas.Add(e.path);
        if (escenas.Count == 0)
        {
            EditorUtility.DisplayDialog("Celular (Android)", "No hay escenas en File → Build Profiles / Build Settings.", "OK");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(RutaApk));
        PlayerSettings.Android.bundleVersionCode = PlayerSettings.Android.bundleVersionCode + 1;

        var opciones = new BuildPlayerOptions
        {
            scenes = escenas.ToArray(),
            locationPathName = RutaApk,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = instalar ? BuildOptions.AutoRunPlayer : BuildOptions.None,
        };
        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        var r = reporte.summary;
        if (r.result == BuildResult.Succeeded)
        {
            float mb = (float)(r.totalSize / (1024.0 * 1024.0));
            EditorUtility.DisplayDialog("Celular (Android)",
                "¡Listo! APK de " + mb.ToString("0") + " MB en " + RutaApk +
                (instalar ? "\n\nSe instaló y abrió en tu celular." : "\n\nPásalo a tu celular y ábrelo para instalarlo."), "OK");
            if (!instalar) EditorUtility.RevealInFinder(RutaApk);
        }
        else
        {
            EditorUtility.DisplayDialog("Celular (Android)",
                "No se pudo crear el APK (" + r.result + ", " + r.totalErrors + " errores). Revisa la Consola: ahí dice qué falló.", "OK");
        }
    }
}
