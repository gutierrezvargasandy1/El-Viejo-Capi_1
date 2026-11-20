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
    SceneAsset escenaBueno, escenaMarionetas;
    [Range(0f, 1f)] float probabilidadBueno = 0.5f;
    float alturaVista = 1.6f, alturaArboles = 10f;
    const float LargoPala = 1.3f;
    bool ajustarArboles = true;
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
        escenaBueno = AssetDatabase.LoadAssetAtPath<SceneAsset>(Base + "Nivel 2/FinalBuenoScene.unity");
        escenaMarionetas = AssetDatabase.LoadAssetAtPath<SceneAsset>(Base + "Nivel 1/Esena 2/Esena 2 Noche.unity");
        probabilidadBueno = EditorPrefs.GetFloat("CapiProbFinalBueno", 0.5f);
        alturaVista = EditorPrefs.GetFloat("CapiVistaFinalBueno", 1.6f);
        alturaArboles = EditorPrefs.GetFloat("CapiArbolesFinalBueno", 10f);
        ajustarArboles = EditorPrefs.GetBool("CapiAjustarArbolesFB", true);
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

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3. Final bueno (bosque)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Arma el final bueno en tu escena del bosque: copia de la escena de las marionetas al jugador, los objetivos, " +
            "las pistas, la canasta con su contador y las 5 marionetas; pone el hoyo junto a la pala y todo el juego " +
            "(tomar la pala → cavar → echar las marionetas → enterrar → FINAL BUENO).\n" +
            "Al ganar la noche sale al azar el final bueno o el malo. En la noche: F7 = final bueno, F8 = final malo (pruebas).",
            MessageType.Info);
        escenaBueno = (SceneAsset)EditorGUILayout.ObjectField("Escena del bosque", escenaBueno, typeof(SceneAsset), false);
        escenaMarionetas = (SceneAsset)EditorGUILayout.ObjectField("Copiar marionetas de", escenaMarionetas, typeof(SceneAsset), false);
        EditorGUI.BeginChangeCheck();
        probabilidadBueno = EditorGUILayout.Slider("Probabilidad final bueno", probabilidadBueno, 0f, 1f);
        alturaVista = EditorGUILayout.Slider("Altura de la vista del Capi (m)", alturaVista, 1.3f, 1.9f);
        ajustarArboles = EditorGUILayout.Toggle("Árboles a tamaño real", ajustarArboles);
        using (new EditorGUI.DisabledScope(!ajustarArboles))
            alturaArboles = EditorGUILayout.Slider("Altura de los árboles (m)", alturaArboles, 5f, 20f);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetFloat("CapiProbFinalBueno", probabilidadBueno);
            EditorPrefs.SetFloat("CapiVistaFinalBueno", alturaVista);
            EditorPrefs.SetFloat("CapiArbolesFinalBueno", alturaArboles);
            EditorPrefs.SetBool("CapiAjustarArbolesFB", ajustarArboles);
        }
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            if (GUILayout.Button("Armar final bueno", GUILayout.Height(28))) ArmarFinalBueno(true);

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
        bool bueno = escenaBueno != null && ArmarFinalBueno(false);
        Volver(volver);
        EditorUtility.DisplayDialog("Capítulos y final",
            "Listo:\n• Título de capítulo en " + n + " escenas\n" +
            (final ? "• Escena final armada, agregada al Build y conectada al ganar la noche\n" : "• La escena final NO se armó (revisa el video)\n") +
            (bueno ? "• Final bueno armado en el bosque (sale al azar al ganar; F7 lo fuerza)\n" : "") +
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

    // ───────────────────────── final bueno ─────────────────────────

    const string Sufijo = " (Final Bueno)";
    const string CarpetaBueno = "Assets/Prefabs/FinalBueno";

    bool ArmarFinalBueno(bool solo)
    {
        if (escenaBueno == null || escenaMarionetas == null)
        {
            EditorUtility.DisplayDialog("Final bueno", "Falta la escena del bosque o la escena de donde se copian las marionetas.", "OK");
            return false;
        }
        if (solo && !GuardarAbiertas()) return false;
        string volver = SceneManager.GetActiveScene().path;
        string rutaBueno = AssetDatabase.GetAssetPath(escenaBueno);
        string rutaFuente = AssetDatabase.GetAssetPath(escenaMarionetas);

        var destino = EditorSceneManager.OpenScene(rutaBueno, OpenSceneMode.Single);
        foreach (var r in destino.GetRootGameObjects())
            if (r.name == "FINAL BUENO" || r.name.EndsWith(Sufijo)) Object.DestroyImmediate(r);

        // la pala de la escena
        GameObject pala = null;
        foreach (var r in destino.GetRootGameObjects())
        {
            string n = r.name.ToLowerInvariant();
            if (n.Contains("shovel") || n.Contains("pala")) { pala = r; break; }
        }
        if (pala == null)
        {
            EditorUtility.DisplayDialog("Final bueno", "No encontré la pala en la escena del bosque (un objeto con 'Shovel' o 'Pala' en el nombre).", "OK");
            if (solo) Volver(volver);
            return false;
        }

        // ---- copiar de la escena de las marionetas (jugador, objetivos, pistas, canasta, contador, marionetas) ----
        EditorUtility.DisplayProgressBar("Final bueno", "Copiando al Capi, los objetivos y las marionetas...", 0.2f);
        ControladorPrimeraPersona jugador = null;
        GestorCanastaDevolver gestorCanasta = null;
        var marionetas = new List<GameObject>();
        try
        {
            var fuente = EditorSceneManager.OpenScene(rutaFuente, OpenSceneMode.Additive);
            var jugadorF = Primero<ControladorPrimeraPersona>(fuente);
            var objetivosF = Primero<GestorObjetivos>(fuente);
            var pistasF = Primero<GestorPistas>(fuente);
            var canastaF = Primero<GestorCanastaDevolver>(fuente);
            var depositosF = Todos<MarionetaDeposito>(fuente);

            var raices = new List<GameObject>();
            AgregarRaiz(raices, jugadorF); AgregarRaiz(raices, objetivosF); AgregarRaiz(raices, pistasF); AgregarRaiz(raices, canastaF);
            if (objetivosF != null) { AgregarRaiz(raices, objetivosF.textoObjetivo); AgregarRaiz(raices, objetivosF.grupoObjetivo); }
            if (pistasF != null) { AgregarRaiz(raices, pistasF.textoPista); AgregarRaiz(raices, pistasF.grupoPista); }
            if (canastaF != null)
            {
                AgregarRaiz(raices, canastaF.textoContador);
                if (canastaF.canvasContador != null) AgregarRaiz(raices, canastaF.canvasContador.transform);
            }

            SceneManager.SetActiveScene(destino);
            var mapa = new Dictionary<Object, Object>();
            var clones = new List<GameObject>();
            foreach (var r in raices)
            {
                var c = (GameObject)Object.Instantiate(r);
                c.name = r.name + Sufijo;
                SceneManager.MoveGameObjectToScene(c, destino);
                Mapear(r.transform, c.transform, mapa);
                clones.Add(c);
            }
            Remapear(clones, mapa, destino);

            if (jugadorF != null) jugador = mapa[jugadorF] as ControladorPrimeraPersona;
            if (canastaF != null) gestorCanasta = mapa[canastaF] as GestorCanastaDevolver;

            // las marionetas que vienen DENTRO de la canasta (así las sacas de ahí y las echas al hoyo)
            if (gestorCanasta != null)
            {
                Transform dentro = BuscarHijo(gestorCanasta.transform, "Marionetas");
                if (dentro != null)
                    for (int i = 0; i < dentro.childCount; i++) marionetas.Add(dentro.GetChild(i).gameObject);
            }
            // si la canasta no traía, se copian las de los lugares de la escena (empiezan escondidas)
            if (marionetas.Count == 0)
            {
                var sueltas = new List<GameObject>();
                foreach (var d in depositosF)
                {
                    var c = (GameObject)Object.Instantiate(d.gameObject);
                    c.name = d.gameObject.name + Sufijo;
                    SceneManager.MoveGameObjectToScene(c, destino);
                    Mapear(d.transform, c.transform, mapa);
                    sueltas.Add(c);
                    c.SetActive(false);
                }
                Remapear(sueltas, mapa, destino);
                marionetas.AddRange(sueltas);
            }

            EditorSceneManager.CloseScene(fuente, true);
        }
        finally { EditorUtility.ClearProgressBar(); }

        if (jugador == null)
        {
            EditorUtility.DisplayDialog("Final bueno", "No encontré al jugador (ControladorPrimeraPersona) en '" + escenaMarionetas.name + "'.", "OK");
            if (solo) Volver(volver);
            return false;
        }

        var raiz = new GameObject("FINAL BUENO");
        var fb = raiz.AddComponent<FinalBueno>();

        // marionetas: solo el modelo (sin depósito ni colisiones)
        for (int i = 0; i < marionetas.Count; i++)
        {
            var m = marionetas[i];
            foreach (var d in m.GetComponentsInChildren<MarionetaDeposito>(true)) Object.DestroyImmediate(d);
            foreach (var d in m.GetComponentsInChildren<MarionetaRecolectable>(true)) Object.DestroyImmediate(d);
            foreach (var c in m.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in m.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            if (m.transform.parent == null)
            {
                m.transform.SetParent(raiz.transform, true);
                m.SetActive(false);
            }
        }

        // la canasta: te sigue y empieza llena, el contador sale hasta que echas las marionetas
        CanastaSigue sigue = null;
        if (gestorCanasta != null)
        {
            gestorCanasta.mostrarContadorAlInicio = false;
            gestorCanasta.totalMarionetas = Mathf.Max(1, marionetas.Count);
            LimpiarEvento(gestorCanasta, "alCompletar");
            sigue = gestorCanasta.GetComponent<CanastaSigue>();
            if (sigue != null)
            {
                sigue.seguirDesdeInicio = true;
                sigue.lugarFinal = null;
                sigue.alturaFija = 0f;
                sigue.jugador = jugador.transform;
                LimpiarEvento(sigue, "alLlegarASuLugar");
            }
        }

        // ---- acomodar: el Capi enfrente de la pala y el hoyo junto a ella ----
        var terreno = Terrain.activeTerrain;
        ignorarSuelo.Clear();
        ignorarSuelo.Add(pala); ignorarSuelo.Add(jugador.gameObject); ignorarSuelo.Add(raiz);
        if (gestorCanasta != null) ignorarSuelo.Add(gestorCanasta.gameObject);
        Physics.SyncTransforms();
        string notaTamanos = AjustarTamanos(jugador, gestorCanasta, pala, raiz, destino, terreno);
        Physics.SyncTransforms();
        Bounds bPala = Limites(pala);
        float suelo = Suelo(terreno, new Vector3(bPala.center.x, bPala.min.y, bPala.center.z));   // si no hay suelo debajo, se queda donde está
        if (Mathf.Abs(bPala.min.y - suelo) > 0.3f) pala.transform.position += Vector3.up * (suelo - bPala.min.y);   // estaba flotando
        bPala = Limites(pala);
        Vector3 s = bPala.center; s.y = suelo;

        var obst = Obstaculos(destino, pala, raiz, jugador.gameObject, gestorCanasta != null ? gestorCanasta.gameObject : null);
        Vector3 posJugador = s + Vector3.back * 5f, posHoyo = s + Vector3.right * 1.7f;
        float mejor = float.MinValue;
        for (int a = 0; a < 16; a++)
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float ang = a * 22.5f * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                var p = s + dir * 5f;
                var h = s + Quaternion.Euler(0f, 90f * lado, 0f) * dir * 1.7f;
                float puntos = Mathf.Min(Libre(obst, p) - 1.2f, Libre(obst, h) - 1.4f)
                             + 0.3f * Mathf.Min(Libre(obst, Vector3.Lerp(p, s, 0.5f)), 3f)
                             - Fuera(terreno, p) - Fuera(terreno, h) - Inclinacion(terreno, h) * 0.05f;
                if (puntos > mejor) { mejor = puntos; posJugador = p; posHoyo = h; }
            }

        var punto = new GameObject("PuntoHoyo (muévelo si quieres)").transform;
        punto.SetParent(raiz.transform, false);
        punto.position = new Vector3(posHoyo.x, Suelo(terreno, posHoyo), posHoyo.z);

        var cc = jugador.GetComponent<CharacterController>();
        float esc = jugador.transform.lossyScale.y;
        float pie = cc != null ? (cc.center.y - cc.height * 0.5f) * esc : 0f;
        jugador.transform.position = new Vector3(posJugador.x, Suelo(terreno, posJugador) - pie + 0.08f, posJugador.z);
        Vector3 mira = s - posJugador; mira.y = 0f;
        jugador.transform.rotation = Quaternion.LookRotation(mira.normalized, Vector3.up);
        if (jugador.camara != null) jugador.camara.localRotation = Quaternion.identity;
        var pasos = Clip("Pasos_Pasto");
        if (pasos != null) jugador.sonidoPasos = pasos;

        if (gestorCanasta != null)
        {
            var c = gestorCanasta.transform;
            Bounds tamCanasta = Limites(c.gameObject);
            float detras = Mathf.Max(1.5f, Mathf.Max(tamCanasta.extents.x, tamCanasta.extents.z) + 1f);   // que no se te encime
            if (sigue != null) sigue.distancia = detras;
            Vector3 atras = posJugador - mira.normalized * detras;
            c.position = new Vector3(atras.x, c.position.y, atras.z);
            Bounds bc = Limites(c.gameObject);
            c.position += Vector3.up * (Suelo(terreno, atras) - bc.min.y);
        }

        // las otras cámaras de la escena se apagan (la del Capi es la que vale)
        foreach (var camara in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (camara.gameObject.scene == destino && !camara.transform.IsChildOf(jugador.transform))
                camara.gameObject.SetActive(false);

        // ---- el controlador del final bueno ----
        fb.jugador = jugador;
        fb.palaEnElSuelo = pala;
        fb.puntoHoyo = punto;
        fb.marionetas = marionetas.ToArray();
        fb.canasta = sigue;
        fb.luzSol = Sol(destino);
        MedirPala(pala, out fb.palaEje, out fb.palaNormal, out fb.palaMango, out fb.palaLargo);
        fb.materialTierra = MaterialTierra();
        fb.materialParticulas = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Noche/Mat_Particula_Polvo.mat");
        fb.ambienteBosque = Clip("Bosque_Ambiente");
        fb.sonidoTomarPala = Clip("Pala_Tomar");
        fb.sonidoCavar1 = Clip("Pala_Cavar_1");
        fb.sonidoCavar2 = Clip("Pala_Cavar_2");
        fb.sonidoTierra = Clip("Tierra_Cae");
        fb.sonidoMarioneta = Clip("Marioneta_Hoyo");
        fb.sonidoEnterrar = Clip("Enterrar");
        fb.sonidoSuspiro = Clip("Capi_Suspiro");
        fb.sonidoAmanecer = Clip("FinalBueno_Amanecer");
        fb.musicaFinal = Clip("FinalBueno_Musica");
        EditorUtility.SetDirty(fb);

        PonerTitulo(capitulos[3]);                 // también es el capítulo 4 (no se sabe cuál final te tocó)
        EditorSceneManager.MarkSceneDirty(destino);
        EditorSceneManager.SaveScene(destino);
        AgregarAlBuild(rutaBueno);

        // al ganar la noche: final bueno o malo al azar
        bool conectado = false;
        if (escenaNoche != null)
        {
            var noche = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(escenaNoche), OpenSceneMode.Single);
            foreach (var g in Object.FindObjectsByType<GestorNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                g.escenaFinalBueno = Path.GetFileNameWithoutExtension(rutaBueno);
                g.probabilidadFinalBueno = probabilidadBueno;
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
            EditorUtility.DisplayDialog("Final bueno",
                "Listo: final bueno armado en '" + escenaBueno.name + "' y agregado al Build.\n" +
                "• Copié al Capi, los objetivos, las pistas, la canasta y " + marionetas.Count + " marionetas.\n" +
                "• El hoyo quedó junto a la pala (objeto 'PuntoHoyo': muévelo si lo quieres en otro lado).\n" +
                (notaTamanos.Length > 0 ? "• Tamaños: " + notaTamanos + ".\n" : "") +
                (conectado ? "• Al ganar la noche sale al azar (" + Mathf.RoundToInt(probabilidadBueno * 100f) + "% final bueno). F7 = final bueno, F8 = final malo.\n" : "• OJO: no encontré GestorNoche en la escena de la noche.\n") +
                "\nPruébalo: abre la escena del bosque y dale Play.", "OK");
        }
        return true;
    }

    // El Capi de la escena 2 está hecho a otra escala (mide como 7 m) y su cuerpo está lejos de su punto de origen:
    // aquí se centra, se deja de tamaño de persona y la canasta, la pala y los árboles quedan en proporción.
    string AjustarTamanos(ControladorPrimeraPersona jugador, GestorCanastaDevolver canasta, GameObject pala, GameObject raiz, Scene escena, Terrain terreno)
    {
        var notas = new List<string>();
        var t = jugador.transform;
        var cc = jugador.GetComponent<CharacterController>();
        if (cc != null)
        {
            // 1) el punto de origen del Capi va donde está su cuerpo (así girar no lo "orbita" y todo lo encuentra)
            Vector3 c = cc.center;
            Vector3 corrimiento = new Vector3(c.x, 0f, c.z);
            if (corrimiento.sqrMagnitude > 0.0001f)
            {
                Vector3 enMundo = t.TransformVector(corrimiento);
                for (int i = 0; i < t.childCount; i++) t.GetChild(i).localPosition -= corrimiento;
                cc.center = new Vector3(0f, c.y, 0f);
                t.position += enMundo;
                notas.Add("el Capi quedó centrado en su cuerpo (estaba " + enMundo.magnitude.ToString("0") + " m chueco)");
            }

            // 2) tamaño de persona (los ojos a ~1.6 m del suelo); la canasta con sus marionetas en la misma proporción que en la escena 2
            float escY = Mathf.Abs(t.lossyScale.y);
            float pies = t.TransformPoint(cc.center).y - cc.height * 0.5f * escY;
            float vista = jugador.camara != null ? jugador.camara.position.y - pies : cc.height * escY * 0.92f;
            if (vista < 0.05f) vista = cc.height * escY * 0.92f;
            if (vista > 0.01f)
            {
                float f = alturaVista / vista;
                if (Mathf.Abs(f - 1f) > 0.03f)
                {
                    t.localScale *= f;
                    if (canasta != null) canasta.transform.localScale *= f;
                    notas.Add("el Capi medía " + (cc.height * escY).ToString("0.0") + " m y ahora " + (cc.height * escY * f).ToString("0.0") +
                              " m, con la vista a " + alturaVista.ToString("0.0") + " m (la canasta y las marionetas también se ajustaron)");
                }
            }
        }
        jugador.velocidad = 3.5f;          // velocidades de persona (las de la escena 2 eran para el Capi gigante)
        jugador.velocidadCorrer = 6f;
        jugador.alturaSalto = 1f;
        if (jugador.camara != null)
        {
            var camara = jugador.camara.GetComponent<Camera>();
            if (camara != null && camara.nearClipPlane > 0.08f) camara.nearClipPlane = 0.08f;
        }

        // 3) la pala de tamaño normal
        Bounds bp = Limites(pala);
        float largo = Mathf.Max(bp.size.x, Mathf.Max(bp.size.y, bp.size.z));
        if (largo > 0.01f && (largo < 0.9f || largo > 1.9f))
        {
            float k = LargoPala / largo;
            Vector3 ancla = new Vector3(bp.center.x, bp.min.y, bp.center.z);
            pala.transform.localScale *= k;
            pala.transform.position = ancla + (pala.transform.position - ancla) * k;
            notas.Add("la pala medía " + largo.ToString("0.0") + " m y ahora " + LargoPala.ToString("0.0") + " m");
        }

        // 4) los árboles a tamaño real (cada uno crece desde su base, sin moverse de su lugar)
        if (ajustarArboles)
        {
            var arboles = new List<Transform>();
            foreach (var r in escena.GetRootGameObjects())
            {
                if (r == pala || r == raiz || r == jugador.gameObject || (canasta != null && r == canasta.gameObject)) continue;
                if (r.GetComponentInChildren<Renderer>(true) == null) continue;
                string n = r.name.ToLowerInvariant();
                if (n.Contains("tree") || n.Contains("arbol") || n.Contains("árbol") || r.GetComponentInChildren<LODGroup>(true) != null)
                    arboles.Add(r.transform);
            }
            var ks = new List<float>();
            float sumaAlto = 0f;
            foreach (var a in arboles)
            {
                Bounds b = Limites(a.gameObject);
                ignorarSuelo.Add(a.gameObject);
                float suelo = Suelo(terreno, a.position);
                ignorarSuelo.Remove(a.gameObject);
                float arriba = b.max.y - a.position.y;          // de la base del árbol a la punta
                float visible = b.max.y - suelo;                 // lo que se ve arriba del suelo
                if (arriba < 0.1f || visible < 0.1f) continue;
                ks.Add((alturaArboles + suelo - a.position.y) / arriba);
                sumaAlto += visible;
            }
            if (ks.Count > 0)
            {
                ks.Sort();
                float k = ks[ks.Count / 2];
                if (k > 1.12f || k < 0.88f)
                {
                    foreach (var a in arboles) a.localScale *= k;
                    notas.Add(arboles.Count + " árboles medían como " + (sumaAlto / ks.Count).ToString("0.0") + " m y ahora como " + alturaArboles.ToString("0") + " m");
                }
            }
        }
        return string.Join("; ", notas.ToArray());
    }

    static void AgregarRaiz(List<GameObject> raices, Component c)
    {
        if (c == null) return;
        var r = c.transform.root.gameObject;
        if (!raices.Contains(r)) raices.Add(r);
    }

    static Transform BuscarHijo(Transform t, string nombre)
    {
        foreach (var h in t.GetComponentsInChildren<Transform>(true))
            if (h != t && h.name == nombre) return h;
        return null;
    }

    static T Primero<T>(Scene escena) where T : Component
    {
        foreach (var r in escena.GetRootGameObjects())
        {
            var c = r.GetComponentInChildren<T>(true);
            if (c != null) return c;
        }
        return null;
    }

    static List<T> Todos<T>(Scene escena) where T : Component
    {
        var lista = new List<T>();
        foreach (var r in escena.GetRootGameObjects()) lista.AddRange(r.GetComponentsInChildren<T>(true));
        return lista;
    }

    // original → copia, objeto por objeto (para arreglar las referencias entre lo copiado)
    static void Mapear(Transform a, Transform b, Dictionary<Object, Object> mapa)
    {
        mapa[a.gameObject] = b.gameObject;
        var ca = a.GetComponents<Component>();
        var cb = b.GetComponents<Component>();
        for (int i = 0; i < ca.Length && i < cb.Length; i++)
            if (ca[i] != null && cb[i] != null) mapa[ca[i]] = cb[i];
        for (int i = 0; i < a.childCount && i < b.childCount; i++) Mapear(a.GetChild(i), b.GetChild(i), mapa);
    }

    static void Remapear(List<GameObject> clones, Dictionary<Object, Object> mapa, Scene destino)
    {
        foreach (var raiz in clones)
            foreach (var c in raiz.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var so = new SerializedObject(c);
                var it = so.GetIterator();
                bool cambio = false;
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
                    var valor = it.objectReferenceValue;
                    Object nuevo;
                    if (mapa.TryGetValue(valor, out nuevo)) { it.objectReferenceValue = nuevo; cambio = true; }
                    else if (DeOtraEscena(valor, destino)) { it.objectReferenceValue = null; cambio = true; }
                }
                if (cambio) so.ApplyModifiedPropertiesWithoutUndo();
            }
    }

    static bool DeOtraEscena(Object o, Scene destino)
    {
        GameObject go = o as GameObject;
        var comp = o as Component;
        if (go == null && comp != null) go = comp.gameObject;
        if (go == null) return false;
        var e = go.scene;
        return e.IsValid() && e != destino;
    }

    static void LimpiarEvento(Object o, string campo)
    {
        var so = new SerializedObject(o);
        var p = so.FindProperty(campo + ".m_PersistentCalls.m_Calls");
        if (p != null) { p.arraySize = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    static readonly List<GameObject> ignorarSuelo = new List<GameObject>();   // la pala, el Capi y la canasta no son "suelo"

    static float Suelo(Terrain t, Vector3 p)
    {
        if (t != null) return t.SampleHeight(p) + t.transform.position.y;
        var hits = Physics.RaycastAll(p + Vector3.up * 50f, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue, y = p.y;
        foreach (var h in hits)
        {
            bool saltar = false;
            foreach (var g in ignorarSuelo)
                if (g != null && h.collider.transform.IsChildOf(g.transform)) { saltar = true; break; }
            if (saltar || h.distance >= mejor) continue;
            mejor = h.distance; y = h.point.y;
        }
        return y;
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.2f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // árboles, rocas y lo que estorbe (para no poner al Capi ni el hoyo encima)
    static List<Vector4> Obstaculos(Scene escena, params GameObject[] excluir)
    {
        var lista = new List<Vector4>();
        foreach (var r in escena.GetRootGameObjects())
            foreach (var c in r.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger || c is TerrainCollider) continue;
                bool fuera = false;
                foreach (var e in excluir) if (e != null && c.transform.IsChildOf(e.transform)) fuera = true;
                if (fuera) continue;
                var b = c.bounds;
                lista.Add(new Vector4(b.center.x, 0f, b.center.z, Mathf.Max(0.3f, Mathf.Min(b.extents.x, b.extents.z))));
            }
        return lista;
    }

    static float Libre(List<Vector4> obst, Vector3 p)
    {
        float m = 10f;
        foreach (var o in obst)
        {
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(o.x, o.z)) - o.w;
            if (d < m) m = d;
        }
        return m;
    }

    static float Fuera(Terrain t, Vector3 p)
    {
        if (t == null) return 0f;
        Vector3 o = t.transform.position, tam = t.terrainData.size;
        bool dentro = p.x > o.x + 3f && p.x < o.x + tam.x - 3f && p.z > o.z + 3f && p.z < o.z + tam.z - 3f;
        return dentro ? 0f : 50f;
    }

    static float Inclinacion(Terrain t, Vector3 p)
    {
        if (t == null) return 0f;
        Vector3 o = t.transform.position, tam = t.terrainData.size;
        return t.terrainData.GetSteepness((p.x - o.x) / tam.x, (p.z - o.z) / tam.z);
    }

    static Light Sol(Scene escena)
    {
        if (RenderSettings.sun != null && RenderSettings.sun.gameObject.scene == escena) return RenderSettings.sun;
        Light mejor = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.gameObject.scene == escena && (mejor == null || l.intensity > mejor.intensity)) mejor = l;
        return mejor;
    }

    // mide la pala: hacia dónde va el mango → hoja, de qué lado es la hoja y qué tan larga es
    static void MedirPala(GameObject pala, out Vector3 eje, out Vector3 normal, out Vector3 mango, out float largo)
    {
        eje = Vector3.down; normal = Vector3.forward; mango = Vector3.up; largo = 1f;
        var raiz = pala.transform;
        var pts = new List<Vector3>();
        // primero que se puedan leer (puede reimportar el modelo), luego se leen
        foreach (var mf in pala.GetComponentsInChildren<MeshFilter>(true)) HacerLegible(mf.sharedMesh);
        foreach (var sk in pala.GetComponentsInChildren<SkinnedMeshRenderer>(true)) HacerLegible(sk.sharedMesh);
        var mallas = new List<KeyValuePair<Mesh, Matrix4x4>>();
        foreach (var mf in pala.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) mallas.Add(new KeyValuePair<Mesh, Matrix4x4>(mf.sharedMesh, raiz.worldToLocalMatrix * mf.transform.localToWorldMatrix));
        foreach (var sk in pala.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (sk.sharedMesh != null) mallas.Add(new KeyValuePair<Mesh, Matrix4x4>(sk.sharedMesh, raiz.worldToLocalMatrix * sk.transform.localToWorldMatrix));
        foreach (var kv in mallas)
        {
            var vs = kv.Key.vertices;
            if (vs == null) continue;
            int paso = Mathf.Max(1, vs.Length / 40000);
            for (int i = 0; i < vs.Length; i += paso) pts.Add(kv.Value.MultiplyPoint3x4(vs[i]));
        }
        if (pts.Count < 10) return;

        Vector3 min = pts[0], max = pts[0];
        foreach (var p in pts) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        Vector3 tam = max - min;
        int a = tam.x >= tam.y && tam.x >= tam.z ? 0 : (tam.y >= tam.z ? 1 : 2);
        int u = (a + 1) % 3, v = (a + 2) % 3;
        largo = tam[a];

        // en cada punta: qué tan ancha es (la hoja es la punta ancha)
        float lo = min[a] + largo * 0.15f, hi = max[a] - largo * 0.15f;
        Vector2 minLo = new Vector2(float.MaxValue, float.MaxValue), maxLo = -minLo, minHi = minLo, maxHi = -minLo;
        Vector3 sumaLo = Vector3.zero, sumaHi = Vector3.zero; int nLo = 0, nHi = 0;
        foreach (var p in pts)
        {
            var q = new Vector2(p[u], p[v]);
            if (p[a] <= lo) { minLo = Vector2.Min(minLo, q); maxLo = Vector2.Max(maxLo, q); sumaLo += p; nLo++; }
            else if (p[a] >= hi) { minHi = Vector2.Min(minHi, q); maxHi = Vector2.Max(maxHi, q); sumaHi += p; nHi++; }
        }
        Vector2 anchoLo = maxLo - minLo, anchoHi = maxHi - minHi;
        bool hojaArriba = anchoHi.x * anchoHi.y > anchoLo.x * anchoLo.y;
        Vector3 vEje = Vector3.zero; vEje[a] = 1f;
        eje = hojaArriba ? vEje : -vEje;

        Vector2 anchoHoja = hojaArriba ? anchoHi : anchoLo;
        Vector3 vN = Vector3.zero; vN[anchoHoja.x < anchoHoja.y ? u : v] = 1f;
        normal = vN;

        Vector3 centroMango = hojaArriba ? (nLo > 0 ? sumaLo / nLo : min) : (nHi > 0 ? sumaHi / nHi : max);
        centroMango[a] = hojaArriba ? min[a] : max[a];
        mango = centroMango;
    }

    static Material MaterialTierra()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaBueno))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "FinalBueno");
        }
        string ruta = CarpetaBueno + "/Mat_Tierra_FinalBueno.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(sh != null ? sh : Shader.Find("Standard"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static void HacerLegible(Mesh m)
    {
        if (m == null || m.isReadable) return;
        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m)) as ModelImporter;
        if (imp != null && !imp.isReadable)
        {
            imp.isReadable = true;
            imp.SaveAndReimport();
        }
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
