#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramientas > Noche (Sobrevive hasta las 6 AM)
/// 1) Crea el monstruo provisional.  2) Arma toda la noche en la escena abierta.
/// 3) Asigna los sonidos por nombre.  4) Botones para ir a cada pieza y acomodarla.
/// </summary>
public class HerramientaNoche : EditorWindow
{
    const string Carpeta = "Assets/Prefabs/Noche";
    const string RutaPrefab = Carpeta + "/Monstruo_Provisional.prefab";
    const string RutaFinal = Carpeta + "/Monstruo_Final.prefab";

    Camera camara;
    Vector2 scroll;
    static bool cortinasCerradasVista;
    static float alturaMonstruo = 2.0f;
    static float corteCuello = 0.84f;

    [MenuItem("Herramientas/Noche (Sobrevive hasta las 6 AM)")]
    static void Abrir()
    {
        var v = GetWindow<HerramientaNoche>("Noche 6 AM");
        v.minSize = new Vector2(380f, 520f);
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (camara == null) camara = Camera.main;

        Titulo("0. TU monstruo y efectos (estilo Joy of Creation)");
        EditorGUILayout.HelpBox(
            "1) En la ventana Project da clic en el FBX de tu monstruo (o en el modelo dentro de la Hierarchy).\n" +
            "2) Presiona 'Usar MI monstruo'. Crea Monstruo_Final con su material y lo pone en la ventana, la puerta, la chimenea (de cabeza) y en el jumpscare.\n" +
            "3) Presiona 'Agregar efectos dinámicos'.\n" +
            "4) Presiona 'Aplicar animación: asomarse + fade' (aparece con fade in, se asoma y solo tiembla).",
            MessageType.Info);
        var selObj = Selection.activeObject as GameObject;
        EditorGUILayout.LabelField("Seleccionado:", selObj != null ? selObj.name : "(nada)", EditorStyles.boldLabel);
        alturaMonstruo = EditorGUILayout.FloatField("Altura del monstruo (m)", alturaMonstruo);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            using (new EditorGUI.DisabledScope(selObj == null))
            {
                if (GUILayout.Button("Usar MI monstruo = seleccionado", GUILayout.Height(30))) UsarMiMonstruo(selObj);
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Girar mi monstruo 180°")) GirarMonstruo();
            if (GUILayout.Button("Reacomodar entradas (MUEVE)"))
                if (EditorUtility.DisplayDialog("Noche", "Esto MUEVE la ventana, la puerta y la chimenea a la posición automática. ¿Seguro?", "Sí, mover", "No"))
                    ReacomodarMonstruo();
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Agregar efectos dinámicos", GUILayout.Height(30))) AgregarEfectos(true);
            if (GUILayout.Button("Aplicar animación: asomarse + fade", GUILayout.Height(30))) AplicarAsomarse();
        }

        Titulo("0b. Chimenea: solo la cabeza");
        EditorGUILayout.HelpBox(
            "Deja SOLO LA CABEZA en el monstruo de la chimenea para que el cuerpo no atraviese el tiro.\n" +
            "No mueve ni escala nada: solo cambia la malla. 0.84 deja la cabeza con el pelo y la barbilla completa; más alto = menos cuello.",
            MessageType.Info);
        corteCuello = EditorGUILayout.Slider("Corte del cuello (alto)", corteCuello, 0.6f, 0.95f);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Chimenea: solo la cabeza", GUILayout.Height(28))) SoloCabezaChimenea();
            if (GUILayout.Button("Regresar cuerpo completo", GUILayout.Height(28))) CuerpoCompletoChimenea();
            EditorGUILayout.EndHorizontal();
        }

        Titulo("1. Monstruo provisional");
        if (GUILayout.Button("Crear / actualizar monstruo provisional", GUILayout.Height(26)))
        {
            CrearMonstruo();
            EditorUtility.DisplayDialog("Noche", "Monstruo provisional creado en\n" + RutaPrefab, "OK");
        }

        Titulo("2. Armar la noche en esta escena");
        camara = (Camera)EditorGUILayout.ObjectField("Cámara del jugador", camara, typeof(Camera), true);
        EditorGUILayout.HelpBox(
            "Antes de armar: pon la cámara donde está la cabeza del Capi en la cama, mirando hacia la puerta. " +
            "Todo se acomoda a partir de ahí (ventana a la izquierda, puerta enfrente, chimenea a la derecha).",
            MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying || camara == null))
        {
            if (GUILayout.Button("Armar la noche", GUILayout.Height(30))) Armar(camara);
        }

        Titulo("3. Pegar a TUS objetos (lo más importante)");
        EditorGUILayout.HelpBox(
            "Haz los pasos en este orden:\n" +
            "A) En la ventana Scene, colócate como si fueras el Capi acostado en la cama mirando a la puerta y presiona 'Cama = vista del Scene'.\n" +
            "B) En la Hierarchy da clic en TU ventana y presiona 'Ventana = seleccionado'.\n" +
            "C) Clic en TU puerta y 'Puerta = seleccionado'.\n" +
            "D) Clic en TU chimenea y 'Chimenea = seleccionado'.",
            MessageType.Info);
        var sel = Selection.activeGameObject;
        EditorGUILayout.LabelField("Seleccionado ahora:", sel != null ? sel.name : "(nada)", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("A) Cama = vista del Scene", GUILayout.Height(26))) CamaDesdeScene();
            if (GUILayout.Button("Cama = Main Camera", GUILayout.Height(26))) CamaDesdeCamara(camara);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(sel == null))
            {
                if (GUILayout.Button("B) Ventana = seleccionado", GUILayout.Height(26))) Alinear(sel, TipoEntrada.Ventana);
                if (GUILayout.Button("C) Puerta = seleccionado", GUILayout.Height(26))) Alinear(sel, TipoEntrada.Puerta);
                if (GUILayout.Button("D) Chimenea = seleccionado", GUILayout.Height(26))) Alinear(sel, TipoEntrada.Chimenea);
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Revisar choques con tu cámara y tu linterna")) RevisarConflictos(true);
        }

        Titulo("3a. Usar TUS cortinas y TU fuego");
        EditorGUILayout.HelpBox(
            "E) Clic en tu objeto 'Cortinas' y presiona 'Mis cortinas = seleccionado' (haz antes el paso B).\n" +
            "F) Clic en tu objeto 'Fuego' y presiona 'Mi fuego = seleccionado'. Toma también la Point Light más cercana.",
            MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(sel == null))
            {
                if (GUILayout.Button("E) Mis cortinas = seleccionado", GUILayout.Height(26))) UsarMisCortinas(sel);
                if (GUILayout.Button("F) Mi fuego = seleccionado", GUILayout.Height(26))) UsarMiFuego(sel);
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button(cortinasCerradasVista ? "Ver cortinas ABIERTAS" : "Ver cortinas CERRADAS (vista previa)"))
            {
                cortinasCerradasVista = !cortinasCerradasVista;
                foreach (var c in Object.FindObjectsByType<CortinasNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    c.Previsualizar(cortinasCerradasVista ? 1f : 0f);
                SceneView.RepaintAll();
            }
        }

        Titulo("3b. Sonidos");
        EditorGUILayout.HelpBox("Busca en todo el proyecto los sonidos por su nombre (Ventana_Intento, Puerta_Intento, etc.).", MessageType.None);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Asignar sonidos")) AsignarSonidos(true);
        }

        Titulo("4. Ajuste fino (opcional): selecciona y mueve a mano");
        Fila("Vista_Cama", "Ventana_NOCHE", "Puerta_NOCHE");
        Fila("Chimenea_NOCHE", "Entrada_Ventana", "Entrada_Puerta");
        Fila("Entrada_Chimenea", "Cortinas", "Fuego_NOCHE");
        Fila("Linterna_NOCHE", "NOCHE", null);

        Titulo("5. Probar (con Play)");
        EditorGUILayout.HelpBox("F1 = ataque por la ventana   F2 = por la puerta   F3 = por la chimenea   F4 = adelantar a las 5:50 AM", MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    void OnSelectionChange() => Repaint();

    static void Titulo(string t)
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField(t, EditorStyles.boldLabel);
    }

    static void Fila(string a, string b, string c)
    {
        EditorGUILayout.BeginHorizontal();
        foreach (var n in new[] { a, b, c })
        {
            if (string.IsNullOrEmpty(n)) { GUILayout.FlexibleSpace(); continue; }
            if (GUILayout.Button(n.Replace("_NOCHE", ""), GUILayout.MinWidth(100)))
            {
                var go = Buscar(n);
                if (go != null)
                {
                    Selection.activeGameObject = go;
                    SceneView.FrameLastActiveSceneView();
                }
                else EditorUtility.DisplayDialog("Noche", "No encontré '" + n + "'. ¿Ya armaste la noche?", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    static GameObject Buscar(string nombre)
    {
        foreach (var raiz in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (raiz.name == nombre) return raiz;
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t.gameObject;
        }
        return null;
    }

    // =====================================================================
    // 1. MONSTRUO PROVISIONAL
    // =====================================================================

    static GameObject CrearMonstruo()
    {
        AsegurarCarpeta(Carpeta);
        var piel = CrearMaterial("Mat_Monstruo_Piel", new Color(0.42f, 0.06f, 0.05f), Color.black);
        var oscuro = CrearMaterial("Mat_Monstruo_Oscuro", new Color(0.08f, 0.01f, 0.01f), Color.black);
        var ojos = CrearMaterial("Mat_Monstruo_Ojos", Color.white, new Color(1f, 0.95f, 0.85f) * 3f);
        var dientes = CrearMaterial("Mat_Monstruo_Dientes", new Color(0.9f, 0.88f, 0.8f), Color.black);
        var peluca = CrearMaterial("Mat_Monstruo_Peluca", new Color(0.55f, 0.55f, 0.5f), Color.black);

        var raiz = new GameObject("Monstruo_Provisional");
        var mv = raiz.AddComponent<MonstruoVisual>();

        // cuerpo flaco y jorobado
        Parte(PrimitiveType.Capsule, "Cuerpo", raiz.transform, new Vector3(0f, 1.05f, 0f), new Vector3(10f, 0f, 0f), new Vector3(0.5f, 0.8f, 0.38f), piel);
        foreach (int lado in new[] { -1, 1 })
            Parte(PrimitiveType.Capsule, "Pierna", raiz.transform, new Vector3(lado * 0.15f, 0.42f, 0f), Vector3.zero, new Vector3(0.14f, 0.42f, 0.14f), oscuro);

        // cabeza (pivote sin escala para que nada se deforme)
        var cabeza = new GameObject("Cabeza").transform;
        cabeza.SetParent(raiz.transform, false);
        cabeza.localPosition = new Vector3(0f, 1.95f, 0.08f);
        mv.cabeza = cabeza;

        Parte(PrimitiveType.Sphere, "Craneo", cabeza, Vector3.zero, Vector3.zero, new Vector3(0.4f, 0.5f, 0.42f), piel);
        Parte(PrimitiveType.Sphere, "OjoIzq", cabeza, new Vector3(-0.085f, 0.07f, 0.18f), Vector3.zero, Vector3.one * 0.085f, ojos);
        Parte(PrimitiveType.Sphere, "OjoDer", cabeza, new Vector3(0.085f, 0.07f, 0.18f), Vector3.zero, Vector3.one * 0.085f, ojos);
        Parte(PrimitiveType.Sphere, "PupilaIzq", cabeza, new Vector3(-0.085f, 0.07f, 0.222f), Vector3.zero, Vector3.one * 0.025f, oscuro);
        Parte(PrimitiveType.Sphere, "PupilaDer", cabeza, new Vector3(0.085f, 0.07f, 0.222f), Vector3.zero, Vector3.one * 0.025f, oscuro);

        // boca enorme con dientes
        Parte(PrimitiveType.Cube, "Boca", cabeza, new Vector3(0f, -0.12f, 0.17f), Vector3.zero, new Vector3(0.26f, 0.18f, 0.06f), oscuro);
        for (int i = 0; i < 7; i++)
        {
            float x = -0.11f + i * 0.037f;
            Parte(PrimitiveType.Cube, "DienteArriba", cabeza, new Vector3(x, -0.05f, 0.2f), new Vector3(0f, 0f, 45f), new Vector3(0.025f, 0.045f, 0.02f), dientes);
            Parte(PrimitiveType.Cube, "DienteAbajo", cabeza, new Vector3(x + 0.018f, -0.2f, 0.2f), new Vector3(0f, 0f, 45f), new Vector3(0.025f, 0.045f, 0.02f), dientes);
        }

        // peluca rizada a los lados (como el monstruo de la imagen)
        Parte(PrimitiveType.Sphere, "PelucaArriba", cabeza, new Vector3(0f, 0.2f, -0.05f), Vector3.zero, new Vector3(0.44f, 0.2f, 0.42f), peluca);
        foreach (int lado in new[] { -1, 1 })
            for (int j = 0; j < 4; j++)
                Parte(PrimitiveType.Sphere, "Rizo", cabeza, new Vector3(lado * 0.21f, 0.13f - j * 0.1f, -0.06f + j * 0.015f), Vector3.zero, Vector3.one * 0.12f, peluca);

        // brazos largos con garras
        foreach (int lado in new[] { -1, 1 })
        {
            var hombro = new GameObject(lado < 0 ? "BrazoIzq" : "BrazoDer").transform;
            hombro.SetParent(raiz.transform, false);
            hombro.localPosition = new Vector3(lado * 0.32f, 1.45f, 0.05f);
            hombro.localRotation = Quaternion.Euler(-25f, 0f, lado * 12f);
            Parte(PrimitiveType.Capsule, "Brazo", hombro, new Vector3(0f, -0.45f, 0f), Vector3.zero, new Vector3(0.1f, 0.48f, 0.1f), piel);
            for (int k = 0; k < 4; k++)
                Parte(PrimitiveType.Cube, "Garra", hombro, new Vector3((k - 1.5f) * 0.03f, -0.98f, 0.03f), new Vector3(15f, 0f, 0f), new Vector3(0.015f, 0.12f, 0.015f), dientes);
        }

        // luz de los ojos
        var luz = new GameObject("LuzOjos").AddComponent<Light>();
        luz.transform.SetParent(cabeza, false);
        luz.transform.localPosition = new Vector3(0f, 0.05f, 0.35f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.85f, 0.8f);
        luz.range = 1.6f;
        luz.intensity = 1.2f;
        luz.shadows = LightShadows.None;
        mv.lucesOjos = new[] { luz };

        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    static GameObject Parte(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 rot, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        var col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);      // que no estorbe a los clics
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // =====================================================================
    // 2. ARMAR LA NOCHE
    // =====================================================================

    static void Armar(Camera camara)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        if (prefab == null) prefab = CrearMonstruo();

        var existente = Object.FindAnyObjectByType<GestorNoche>(FindObjectsInactive.Include);
        if (existente != null)
        {
            if (!EditorUtility.DisplayDialog("Noche", "Ya hay una noche armada ('" + existente.name + "'). ¿Borrarla y armarla de nuevo?", "Sí, rehacer", "Cancelar"))
                return;
            Undo.DestroyObjectImmediate(existente.gameObject);
        }
        var linternaVieja = Object.FindAnyObjectByType<LinternaNoche>(FindObjectsInactive.Include);
        if (linternaVieja != null) Undo.DestroyObjectImmediate(linternaVieja.gameObject);

        Transform cam = camara.transform;
        Vector3 adelante = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        if (adelante.sqrMagnitude < 0.01f) adelante = Vector3.forward;
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
        Vector3 ojo = cam.position;

        var raiz = new GameObject("NOCHE");
        Undo.RegisterCreatedObjectUndo(raiz, "Armar noche");
        var gestor = raiz.AddComponent<GestorNoche>();
        gestor.ui = raiz.AddComponent<UINoche>();
        var control = raiz.AddComponent<ControlJugadorNoche>();
        control.camara = camara;
        gestor.jugador = control;
        gestor.prefabJumpscare = prefab;

        // ---------- Cama ----------
        var vCama = Nuevo("Vista_Cama", raiz.transform, ojo, Quaternion.LookRotation(adelante)).AddComponent<PuntoVista>();
        vCama.tipo = VistaNoche.Cama;
        vCama.nombre = "cama";
        vCama.limiteYaw = new Vector2(-80f, 80f);
        vCama.limitePitch = new Vector2(-35f, 30f);
        control.vistaCama = vCama;

        // ---------- Linterna (hija de la cámara) ----------
        var goL = new GameObject("Linterna_NOCHE");
        Undo.RegisterCreatedObjectUndo(goL, "Linterna");
        goL.transform.SetParent(cam, false);
        goL.transform.localPosition = new Vector3(0.15f, -0.1f, 0f);
        var luzL = goL.AddComponent<Light>();
        luzL.type = LightType.Spot;
        luzL.spotAngle = 40f;
        luzL.innerSpotAngle = 20f;
        luzL.range = 14f;
        luzL.intensity = 6f;
        luzL.color = new Color(1f, 0.96f, 0.85f);
        luzL.shadows = LightShadows.Soft;
        luzL.enabled = false;
        var linterna = goL.AddComponent<LinternaNoche>();
        linterna.luz = luzL;
        gestor.linterna = linterna;

        // ---------- Ventana (izquierda) ----------
        Vector3 posV = ojo - derecha * 2.6f + adelante * 0.6f;
        var ventana = Nuevo("Ventana_NOCHE", raiz.transform, posV, MirandoA(posV, ojo));
        var pvV = ventana.AddComponent<PuntoVista>();
        pvV.tipo = VistaNoche.Ventana;
        pvV.nombre = "ventana";
        pvV.limiteYaw = new Vector2(-25f, 25f);
        pvV.limitePitch = new Vector2(-20f, 20f);
        Caja(ventana, new Vector3(1.6f, 1.4f, 0.3f), Vector3.zero);
        Vector3 dentroV = ventana.transform.forward;
        pvV.poseCamara = Nuevo("PoseCamara", ventana.transform, posV + dentroV * 1.0f, Quaternion.LookRotation(-dentroV)).transform;

        var cortinas = CrearCortinas(ventana.transform, dentroV);

        var eV = CrearEntrada("Entrada_Ventana", ventana.transform, TipoEntrada.Ventana,
            posV - Vector3.up * 1.85f - dentroV * 0.45f, ventana.transform.rotation, prefab,
            new Vector3(0f, -1.3f, -0.9f));
        eV.cortinas = cortinas;
        eV.tiempoParaAhuyentar = 4f;

        // ---------- Puerta (enfrente) ----------
        Vector3 posP = ojo + adelante * 4.5f;
        var puerta = Nuevo("Puerta_NOCHE", raiz.transform, posP, MirandoA(posP, ojo));
        Vector3 dentroP = puerta.transform.forward;
        var mirar = Nuevo("PuntoMirar", puerta.transform, posP, puerta.transform.rotation).transform;
        var eP = CrearEntrada("Entrada_Puerta", puerta.transform, TipoEntrada.Puerta,
            posP - Vector3.up * 1.75f - dentroP * 0.5f, puerta.transform.rotation, prefab,
            new Vector3(0f, 0f, -1.5f));
        eP.puntoMirar = mirar;
        eP.tiempoParaAhuyentar = 2.5f;

        // ---------- Chimenea (derecha) ----------
        Vector3 posC = ojo + derecha * 2.6f + adelante * 0.6f - Vector3.up * 0.9f;
        var chimenea = Nuevo("Chimenea_NOCHE", raiz.transform, posC, MirandoA(posC, ojo));
        var pvC = chimenea.AddComponent<PuntoVista>();
        pvC.tipo = VistaNoche.Chimenea;
        pvC.nombre = "chimenea";
        pvC.limiteYaw = new Vector2(-25f, 25f);
        pvC.limitePitch = new Vector2(-25f, 20f);
        Caja(chimenea, new Vector3(1.6f, 1.3f, 0.6f), new Vector3(0f, 0.5f, 0f));
        Vector3 dentroC = chimenea.transform.forward;
        Vector3 posPoseC = posC + dentroC * 1.3f + Vector3.up * 0.75f;
        pvC.poseCamara = Nuevo("PoseCamara", chimenea.transform, posPoseC, Quaternion.LookRotation(posC + Vector3.up * 0.3f - posPoseC)).transform;

        var fuego = CrearFuego(chimenea.transform);
        gestor.fuego = fuego;

        var eC = CrearEntrada("Entrada_Chimenea", chimenea.transform, TipoEntrada.Chimenea,
            posC - Vector3.up * 0.9f - dentroC * 0.15f, chimenea.transform.rotation, prefab,
            new Vector3(0f, 2.6f, 0f));
        eC.fuego = fuego;
        eC.tiempoParaAhuyentar = 3f;

        gestor.entradas = new[] { eV, eP, eC };

        AgregarEfectos(false);
        AsignarSonidos(false);
        RevisarConflictos(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = raiz;
        EditorUtility.DisplayDialog("Noche",
            "¡Listo! Se creó 'NOCHE' con la cama, la ventana, la puerta y la chimenea.\n\n" +
            "Ahora sigue la sección 3 de esta ventana (A, B, C y D) para pegarlo a tu cama, ventana, puerta y chimenea. Luego guarda la escena (Ctrl+S).",
            "OK");
    }

    static Quaternion MirandoA(Vector3 desde, Vector3 hacia)
    {
        Vector3 d = Vector3.ProjectOnPlane(hacia - desde, Vector3.up);
        return Quaternion.LookRotation(d.sqrMagnitude > 0.001f ? d.normalized : Vector3.forward);
    }

    static GameObject Nuevo(string nombre, Transform padre, Vector3 pos, Quaternion rot)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.SetPositionAndRotation(pos, rot);
        return go;
    }

    static void Caja(GameObject go, Vector3 tam, Vector3 centro)
    {
        var c = go.AddComponent<BoxCollider>();
        c.isTrigger = true;
        c.size = tam;
        c.center = centro;
    }

    static EntradaMonstruo CrearEntrada(string nombre, Transform padre, TipoEntrada tipo, Vector3 pos, Quaternion rot,
                                        GameObject prefab, Vector3 desplazamiento)
    {
        var go = Nuevo(nombre, padre, pos, rot);
        var e = go.AddComponent<EntradaMonstruo>();
        e.tipo = tipo;
        e.desplazamientoInicial = desplazamiento;

        var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
        m.name = "Monstruo";
        m.transform.localPosition = Vector3.zero;
        m.transform.localRotation = Quaternion.identity;
        m.SetActive(false);
        e.monstruo = m.GetComponent<MonstruoVisual>();
        return e;
    }

    static CortinasNoche CrearCortinas(Transform ventana, Vector3 dentro)
    {
        var tela = CrearMaterial("Mat_Cortina", new Color(0.25f, 0.05f, 0.06f), Color.black);
        var madera = CrearMaterial("Mat_Varilla", new Color(0.18f, 0.11f, 0.06f), Color.black);

        var raiz = Nuevo("Cortinas", ventana, ventana.position + dentro * 0.18f, ventana.rotation);
        var c = raiz.AddComponent<CortinasNoche>();

        var izq = Nuevo("Pivote_Izquierda", raiz.transform, raiz.transform.TransformPoint(new Vector3(-0.85f, 0f, 0f)), raiz.transform.rotation);
        Parte(PrimitiveType.Cube, "Tela", izq.transform, new Vector3(0.425f, 0f, 0f), Vector3.zero, new Vector3(0.85f, 1.7f, 0.04f), tela);
        var der = Nuevo("Pivote_Derecha", raiz.transform, raiz.transform.TransformPoint(new Vector3(0.85f, 0f, 0f)), raiz.transform.rotation);
        Parte(PrimitiveType.Cube, "Tela", der.transform, new Vector3(-0.425f, 0f, 0f), Vector3.zero, new Vector3(0.85f, 1.7f, 0.04f), tela);
        Parte(PrimitiveType.Cylinder, "Varilla", raiz.transform, new Vector3(0f, 0.88f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.03f, 0.95f, 0.03f), madera);

        c.izquierda = izq.transform;
        c.derecha = der.transform;
        izq.transform.localScale = new Vector3(c.escalaAbierta, 1f, 1f);
        der.transform.localScale = new Vector3(c.escalaAbierta, 1f, 1f);
        return c;
    }

    static FuegoChimenea CrearFuego(Transform chimenea)
    {
        var lenaMat = CrearMaterial("Mat_Lena", new Color(0.22f, 0.12f, 0.06f), new Color(0.001f, 0.001f, 0.001f));

        var raiz = Nuevo("Fuego_NOCHE", chimenea, chimenea.position, chimenea.rotation);
        var f = raiz.AddComponent<FuegoChimenea>();

        var t1 = Parte(PrimitiveType.Cylinder, "Tronco1", raiz.transform, new Vector3(0f, 0.07f, 0f), new Vector3(0f, 25f, 90f), new Vector3(0.12f, 0.32f, 0.12f), lenaMat);
        var t2 = Parte(PrimitiveType.Cylinder, "Tronco2", raiz.transform, new Vector3(0f, 0.07f, 0.05f), new Vector3(0f, -25f, 90f), new Vector3(0.12f, 0.32f, 0.12f), lenaMat);
        var t3 = Parte(PrimitiveType.Cylinder, "Tronco3", raiz.transform, new Vector3(0f, 0.17f, 0.02f), new Vector3(0f, 90f, 90f), new Vector3(0.1f, 0.28f, 0.1f), lenaMat);
        f.brasas = new[] { t1.GetComponent<Renderer>(), t2.GetComponent<Renderer>(), t3.GetComponent<Renderer>() };

        var luz = new GameObject("LuzFuego").AddComponent<Light>();
        luz.transform.SetParent(raiz.transform, false);
        luz.transform.localPosition = new Vector3(0f, 0.45f, 0.3f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.55f, 0.2f);
        luz.range = 6f;
        luz.intensity = 0f;
        luz.shadows = LightShadows.Soft;
        luz.enabled = false;
        f.luz = luz;

        f.particulas = CrearParticulas(raiz.transform);
        return f;
    }

    static ParticleSystem CrearParticulas(Transform padre)
    {
        var go = new GameObject("Particulas_Fuego");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // que salgan hacia arriba

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f), new Color(1f, 0.4f, 0.1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;

        var emision = ps.emission;
        emision.rateOverTime = 35f;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 10f;
        forma.radius = 0.22f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.5f), new GradientColorKey(new Color(0.3f, 0.05f, 0.02f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = new ParticleSystem.MinMaxGradient(g);

        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = MaterialParticula();
        return ps;
    }

    // =====================================================================
    // 0. MONSTRUO REAL Y EFECTOS
    // =====================================================================

    static GameObject ModeloDe(GameObject sel)
    {
        if (sel == null) return null;
        if (AssetDatabase.Contains(sel)) return sel;                                    // el FBX en Project
        var fuente = PrefabUtility.GetCorrespondingObjectFromOriginalSource(sel);       // el modelo en la escena
        return fuente != null && AssetDatabase.Contains(fuente) ? fuente : null;
    }

    static Texture2D Textura(string carpeta, string nombre, string sufijo)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(carpeta + "/" + nombre + sufijo + ".png");
    }

    static Material MaterialDelModelo(string rutaModelo)
    {
        string carpeta = Path.GetDirectoryName(rutaModelo).Replace('\\', '/');
        string nombre = Path.GetFileNameWithoutExtension(rutaModelo);
        var color = Textura(carpeta, nombre, "");
        if (color == null) return null;
        var normal = Textura(carpeta, nombre, "_normal");
        if (normal != null)
        {
            var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal)) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.NormalMap)
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.SaveAndReimport();
                normal = Textura(carpeta, nombre, "_normal");
            }
        }

        var mat = CrearMaterial("Mat_Monstruo_Final", Color.white, Color.black);
        mat.mainTexture = color;
        mat.SetTexture("_BaseMap", color);
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.35f);      // piel húmeda: brilla con la linterna
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void UsarMiMonstruo(GameObject sel)
    {
        var g = Gestor();
        if (g == null) return;
        var modelo = ModeloDe(sel);
        if (modelo == null)
        {
            EditorUtility.DisplayDialog("Noche", "Selecciona el FBX de tu monstruo en la ventana Project (o su modelo en la Hierarchy).", "OK");
            return;
        }
        if (modelo.GetComponentInChildren<MonstruoVisual>(true) != null && AssetDatabase.GetAssetPath(modelo).EndsWith(".prefab"))
        {
            EditorUtility.DisplayDialog("Noche", "Seleccionaste un prefab de NOCHE. Selecciona el FBX original de tu monstruo.", "OK");
            return;
        }

        string info;
        var prefab = CrearMonstruoFinal(modelo, out info);
        if (prefab == null) return;
        PonerMonstruoEnEntradas(prefab);
        EditorUtility.DisplayDialog("Noche", "Listo: Monstruo_Final está en la ventana, la puerta, la chimenea y el jumpscare.\n\n" + info +
            "\n\nPruébalo con Play (F1, F2, F3). Si sale de espaldas, presiona 'Girar mi monstruo 180°'.", "OK");
    }

    static GameObject CrearMonstruoFinal(GameObject modeloAsset, out string info)
    {
        info = "";
        AsegurarCarpeta(Carpeta);
        string rutaModelo = AssetDatabase.GetAssetPath(modeloAsset);
        var mat = MaterialDelModelo(rutaModelo);

        var raiz = new GameObject("Monstruo_Final");
        var mv = raiz.AddComponent<MonstruoVisual>();
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloAsset);
        modelo.name = "Modelo";
        modelo.transform.SetParent(raiz.transform, false);
        modelo.transform.localPosition = Vector3.zero;
        // OJO: se conserva la rotación y escala con las que viene el FBX (Meshy lo exporta girado y con escala 100)

        int triangulos = 0;
        foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            if (mat != null)
            {
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            r.shadowCastingMode = ShadowCastingMode.On;
        }
        Material matOpaco = mat;
        if (matOpaco == null)
            foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterial != null) { matOpaco = r.sharedMaterial; break; }
        mv.materialFade = CrearMaterialFade(matOpaco);

        // puntos del modelo (en el espacio de la raíz) para medirlo
        var puntos = new List<Vector3>();
        foreach (var mf in modelo.GetComponentsInChildren<MeshFilter>(true))
            AgregarPuntos(mf.sharedMesh, mf.transform, puntos, ref triangulos);
        foreach (var smr in modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            AgregarPuntos(smr.sharedMesh, smr.transform, puntos, ref triangulos);

        bool deEspaldas = false;
        if (puntos.Count > 100)
        {
            // 1) DE PIE: el lado más largo es la altura. La cabeza (con la peluca) es el extremo con más detalle.
            Bounds b0 = Medir(puntos);
            Vector3 tam = b0.size;
            int eje = tam.x >= tam.y && tam.x >= tam.z ? 0 : (tam.y >= tam.z ? 1 : 2);
            float mitad = b0.center[eje];
            int ladoPositivo = 0, ladoNegativo = 0;
            foreach (var p in puntos) { if (p[eje] > mitad) ladoPositivo++; else ladoNegativo++; }
            Vector3 haciaCabeza = Vector3.zero;
            haciaCabeza[eje] = ladoPositivo >= ladoNegativo ? 1f : -1f;
            Quaternion dePie = Quaternion.FromToRotation(haciaCabeza, Vector3.up);
            if (Quaternion.Angle(dePie, Quaternion.identity) > 1f)
            {
                info += "Venía acostado: lo puse de pie. ";
                Girar(modelo.transform, puntos, dePie);
            }

            // 2) DE FRENTE: lo más delgado (pecho-espalda) va hacia Z
            Bounds b1 = Medir(puntos);
            if (b1.size.x < b1.size.z) Girar(modelo.transform, puntos, Quaternion.Euler(0f, 90f, 0f));

            // 3) ¿hacia dónde mira? los dedos de los pies apuntan hacia adelante
            Bounds b2 = Medir(puntos);
            float sPie = 0f, sTob = 0f; int nPie = 0, nTob = 0;
            foreach (var p in puntos)
            {
                float h = (p.y - b2.min.y) / Mathf.Max(0.0001f, b2.size.y);
                if (h < 0.03f) { sPie += p.z; nPie++; }
                else if (h > 0.06f && h < 0.12f) { sTob += p.z; nTob++; }
            }
            deEspaldas = nPie > 0 && nTob > 0 && (sPie / nPie - sTob / nTob) < 0f;
            if (deEspaldas) Girar(modelo.transform, puntos, Quaternion.Euler(0f, 180f, 0f));

            // escala a la altura pedida
            Bounds b3 = Medir(puntos);
            float k = Mathf.Max(0.1f, alturaMonstruo) / Mathf.Max(0.0001f, b3.size.y);
            modelo.transform.localScale = modelo.transform.localScale * k;
            for (int i = 0; i < puntos.Count; i++) puntos[i] *= k;

            // pies en el piso y centrado
            Bounds b4 = Medir(puntos);
            Vector3 mover = new Vector3(-b4.center.x, -b4.min.y, -b4.center.z);
            modelo.transform.localPosition += mover;
            for (int i = 0; i < puntos.Count; i++) puntos[i] += mover;
        }
        else
        {
            info += "No pude leer los vértices del modelo: revisa a mano que mire hacia la flecha azul.\n";
        }

        // cabeza: la parte de arriba, un poco hacia adelante (la cara)
        Bounds bf = puntos.Count > 0 ? Medir(puntos) : new Bounds(Vector3.up * alturaMonstruo * 0.5f, new Vector3(0.6f, alturaMonstruo, 0.4f));
        Vector3 cabezaPos = new Vector3(0f, bf.min.y + bf.size.y * 0.875f, 0.1f);
        if (puntos.Count > 0)
        {
            float sx = 0f, sz = 0f, maxZ = float.MinValue; int n = 0;
            foreach (var p in puntos)
            {
                float h = (p.y - bf.min.y) / bf.size.y;
                if (h < 0.8f || h > 0.95f) continue;
                sx += p.x; sz += p.z; n++;
                if (p.z > maxZ) maxZ = p.z;
            }
            if (n > 0) cabezaPos = new Vector3(sx / n, cabezaPos.y, sz / n + (maxZ - sz / n) * 0.5f);
        }
        var cabeza = new GameObject("Cabeza").transform;
        cabeza.SetParent(raiz.transform, false);
        cabeza.localPosition = cabezaPos;
        mv.cabeza = cabeza;
        mv.cuerpo = modelo.transform;

        // luz tenue frente a la cara para que brillen los dientes y los ojos
        var luz = new GameObject("LuzCara").AddComponent<Light>();
        luz.transform.SetParent(cabeza, false);
        luz.transform.localPosition = new Vector3(0f, 0.05f, 0.45f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.55f, 0.5f);
        luz.range = 1.6f;
        luz.intensity = 0.8f;
        luz.shadows = LightShadows.None;
        mv.lucesOjos = new[] { luz };
        mv.intensidadOjos = 0.8f;

        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaFinal);
        DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();

        info += "Altura: " + alturaMonstruo.ToString("0.0") + " m. " + (deEspaldas ? "Venía de espaldas: lo giré." : "Mira hacia adelante.");
        if (mat == null) info += "\nNo encontré sus texturas junto al FBX: se queda con su material original.";
        if (triangulos > 150000)
            info += "\n\nOjo: tiene " + (triangulos / 1000) + " mil triángulos. En PC está bien; para Android conviene bajarlo en Meshy (Remesh) a unos 50 mil.";
        return prefab;
    }

    static void AgregarPuntos(Mesh m, Transform t, List<Vector3> puntos, ref int triangulos)
    {
        if (m == null) return;
        for (int s = 0; s < m.subMeshCount; s++) triangulos += (int)(m.GetIndexCount(s) / 3);
        Vector3[] vs;
        try { vs = m.vertices; }
        catch { return; }
        if (vs == null || vs.Length == 0) return;
        Matrix4x4 a = t.localToWorldMatrix;           // la raíz está en el origen: mundo = raíz
        int paso = Mathf.Max(1, vs.Length / 60000);
        for (int i = 0; i < vs.Length; i += paso) puntos.Add(a.MultiplyPoint3x4(vs[i]));
    }

    // Gira el modelo alrededor del origen de la raíz y también los puntos medidos
    static void Girar(Transform modelo, List<Vector3> puntos, Quaternion giro)
    {
        modelo.localRotation = giro * modelo.localRotation;
        for (int i = 0; i < puntos.Count; i++) puntos[i] = giro * puntos[i];
    }

    static Bounds Medir(List<Vector3> puntos)
    {
        var b = new Bounds(puntos[0], Vector3.zero);
        foreach (var p in puntos) b.Encapsulate(p);
        return b;
    }

    static void PonerMonstruoEnEntradas(GameObject prefab)
    {
        bool habiaFinal = false;
        var g = Object.FindAnyObjectByType<GestorNoche>(FindObjectsInactive.Include);
        foreach (var e in Object.FindObjectsByType<EntradaMonstruo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(e, "Monstruo final");

            // quitar los monstruos anteriores de esta entrada (si era Monstruo_Final se conserva dónde y de qué tamaño lo dejaste)
            bool conservar = false;
            Vector3 lp = Vector3.zero, ls = Vector3.one;
            Quaternion lr = Quaternion.identity;
            foreach (var viejo in e.GetComponentsInChildren<MonstruoVisual>(true))
            {
                if (viejo == null) continue;
                var raizVieja = PrefabUtility.GetOutermostPrefabInstanceRoot(viejo.gameObject) ?? viejo.gameObject;
                var fuenteVieja = PrefabUtility.GetCorrespondingObjectFromSource(raizVieja);
                if (!conservar && fuenteVieja != null && AssetDatabase.GetAssetPath(fuenteVieja) == RutaFinal && raizVieja.transform.parent == e.transform)
                {
                    conservar = true;
                    habiaFinal = true;
                    lp = raizVieja.transform.localPosition;
                    lr = raizVieja.transform.localRotation;
                    ls = raizVieja.transform.localScale;
                }
                Undo.DestroyObjectImmediate(raizVieja);
            }
            if (e.monstruo != null && !e.monstruo.transform.IsChildOf(e.transform))
            {
                // el que pusiste a mano fuera de la entrada: se apaga (no se borra)
                Undo.RecordObject(e.monstruo.gameObject, "Monstruo final");
                e.monstruo.gameObject.SetActive(false);
                if (!e.monstruo.name.Contains("(VIEJO")) e.monstruo.name += " (VIEJO - puedes borrarlo)";
            }

            var nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, e.transform);
            Undo.RegisterCreatedObjectUndo(nuevo, "Monstruo final");
            nuevo.name = "Monstruo";
            nuevo.transform.localPosition = lp;
            nuevo.transform.localRotation = lr;
            nuevo.transform.localScale = ls;
            nuevo.SetActive(false);
            e.monstruo = nuevo.GetComponent<MonstruoVisual>();
            EditorUtility.SetDirty(e);
        }

        if (g != null)
        {
            Undo.RecordObject(g, "Monstruo final");
            g.prefabJumpscare = prefab;
            EditorUtility.SetDirty(g);
        }
        if (!habiaFinal) ReacomodarMonstruo();     // solo la primera vez: después no se mueve nada
    }

    // Pone cada entrada para que la CABEZA del monstruo quede en el lugar correcto
    static void ReacomodarMonstruo()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFinal);
        var mvPrefab = prefab != null ? prefab.GetComponent<MonstruoVisual>() : null;
        Vector3 cabeza = mvPrefab != null && mvPrefab.cabeza != null ? mvPrefab.cabeza.localPosition : new Vector3(0f, 1.9f, 0.1f);

        foreach (var e in Object.FindObjectsByType<EntradaMonstruo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(e.transform, "Reacomodar");
            Undo.RecordObject(e, "Reacomodar");
            Vector3 adentro = e.transform.forward;
            Vector3 meta;
            Quaternion rot;
            switch (e.tipo)
            {
                case TipoEntrada.Ventana:
                {
                    var v = Vista(VistaNoche.Ventana);
                    if (v != null) adentro = v.transform.forward;
                    Vector3 centro = v != null ? v.transform.position : e.transform.TransformPoint(cabeza);
                    rot = Quaternion.LookRotation(Plano(adentro));
                    meta = centro - Plano(adentro) * 0.45f;                    // pegado al vidrio, por fuera
                    e.desplazamientoInicial = new Vector3(0f, -1.4f, -0.6f);  // se asoma subiendo desde abajo del marco
                    break;
                }
                case TipoEntrada.Puerta:
                {
                    Vector3 vidrio = e.puntoMirar != null ? e.puntoMirar.position : e.transform.TransformPoint(cabeza);
                    rot = Quaternion.LookRotation(Plano(adentro));
                    meta = vidrio - Plano(adentro) * 0.45f;                    // detrás del vidrio de la puerta
                    e.desplazamientoInicial = new Vector3(0f, -0.7f, -0.5f);   // se asoma desde abajo del vidrio
                    break;
                }
                default:
                {
                    var v = Vista(VistaNoche.Chimenea);
                    if (v != null) adentro = v.transform.forward;
                    Vector3 piso = v != null ? v.transform.position : e.transform.position;
                    rot = Quaternion.LookRotation(Plano(adentro)) * Quaternion.Euler(0f, 0f, 180f);   // DE CABEZA
                    meta = piso + Vector3.up * 0.8f + Plano(adentro) * 0.05f;  // la cabeza asoma por arriba de la boca
                    e.desplazamientoInicial = new Vector3(0f, -1.0f, 0f);      // (de cabeza) se asoma bajando del tiro
                    break;
                }
            }
            e.transform.SetPositionAndRotation(meta - rot * cabeza, rot);
            EditorUtility.SetDirty(e);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // Material transparente (copia del normal) para el fade in / fade out del monstruo.
    // Se guarda como asset para que el shader transparente también entre en el build de Android.
    static Material CrearMaterialFade(Material opaco)
    {
        if (opaco == null) return null;
        if (opaco.name.EndsWith("_Fade")) return opaco;
        AsegurarCarpeta(Carpeta);
        string ruta = Carpeta + "/" + opaco.name + "_Fade.mat";
        var fade = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (fade == null)
        {
            fade = new Material(opaco);
            AssetDatabase.CreateAsset(fade, ruta);
        }
        else
        {
            fade.shader = opaco.shader;
            fade.CopyPropertiesFromMaterial(opaco);
        }
        MonstruoVisual.HacerTransparente(fade);
        EditorUtility.SetDirty(fade);
        return fade;
    }

    static Material MaterialOpacoDe(GameObject raiz)
    {
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial != null && !(r is ParticleSystemRenderer)) return r.sharedMaterial;
        return null;
    }

    static void AplicarAsomarse()
    {
        int listos = 0;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaFinal) != null)
        {
            var raiz = PrefabUtility.LoadPrefabContents(RutaFinal);
            var mv = raiz.GetComponent<MonstruoVisual>();
            if (mv != null)
            {
                mv.materialFade = CrearMaterialFade(MaterialOpacoDe(raiz));
                listos++;
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaFinal);
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        // monstruos de la escena que no vienen de Monstruo_Final (por ejemplo el provisional)
        foreach (var mv in Object.FindObjectsByType<MonstruoVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(mv) != null) continue;
            Undo.RecordObject(mv, "Asomarse + fade");
            mv.materialFade = CrearMaterialFade(MaterialOpacoDe(mv.gameObject));
            EditorUtility.SetDirty(mv);
            listos++;
        }
        AssetDatabase.SaveAssets();

        if (listos == 0)
        {
            EditorUtility.DisplayDialog("Noche", "No encontré ningún monstruo. Primero presiona 'Usar MI monstruo'.", "OK");
            return;
        }
        EditorUtility.DisplayDialog("Noche",
            "Listo. Ahora en cada entrada el monstruo:\n" +
            "• aparece con fade in, ya asomándose un poco\n" +
            "• se termina de asomar por pasos (a veces se esconde y vuelve)\n" +
            "• no gira ni se mueve de lado a lado: solo tiembla\n" +
            "• al ahuyentarlo se va con fade out\n\n" +
            "Ajustes en el componente MonstruoVisual del prefab Monstruo_Final.\nPruébalo con Play (F1, F2, F3).", "OK");
    }

    // ───────────────────────── Chimenea: solo la cabeza ─────────────────────────

    static EntradaMonstruo EntradaChimenea()
    {
        foreach (var e in Object.FindObjectsByType<EntradaMonstruo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (e.tipo == TipoEntrada.Chimenea) return e;
        return null;
    }

    // La malla original (la del FBX), aunque ya se haya cortado antes
    static Mesh MallaOriginal(Component c, Mesh actual)
    {
        Mesh m = null;
        var fuente = PrefabUtility.GetCorrespondingObjectFromOriginalSource(c);
        var fmf = fuente as MeshFilter;
        var fsk = fuente as SkinnedMeshRenderer;
        if (fmf != null) m = fmf.sharedMesh;
        else if (fsk != null) m = fsk.sharedMesh;
        return m != null ? m : actual;
    }

    static Mesh MallaDe(Component c)
    {
        var mf = c as MeshFilter;
        if (mf != null) return mf.sharedMesh;
        var sk = c as SkinnedMeshRenderer;
        return sk != null ? sk.sharedMesh : null;
    }

    static void PonerMalla(Component c, Mesh m)
    {
        Undo.RecordObject(c, "Solo cabeza");
        var mf = c as MeshFilter;
        if (mf != null) mf.sharedMesh = m;
        var sk = c as SkinnedMeshRenderer;
        if (sk != null) sk.sharedMesh = m;
        PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }

    static Renderer RenderDe(Component c)
    {
        var r = c as Renderer;
        return r != null ? r : c.GetComponent<Renderer>();
    }

    static void HacerLegible(Mesh m)
    {
        if (m == null || m.isReadable) return;
        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m)) as ModelImporter;
        if (imp != null && !imp.isReadable)
        {
            imp.isReadable = true;           // solo para poder leer los vértices (no cambia tamaño ni posición)
            imp.SaveAndReimport();
        }
    }

    // Junta los vértices que se quedan y re-numera los triángulos
    class Recorte
    {
        public Vector3[] v, n;
        public Vector4[] tg;
        public Color[] col;
        public List<Vector2>[] uv = new List<Vector2>[4];
        public int[] mapa;
        public List<Vector3> nv = new List<Vector3>(), nn = new List<Vector3>();
        public List<Vector4> ntg = new List<Vector4>();
        public List<Color> ncol = new List<Color>();
        public List<Vector2>[] nuv = new List<Vector2>[4];

        public int Indice(int i)
        {
            if (mapa[i] >= 0) return mapa[i];
            mapa[i] = nv.Count;
            nv.Add(v[i]);
            if (n != null) nn.Add(n[i]);
            if (tg != null) ntg.Add(tg[i]);
            if (col != null) ncol.Add(col[i]);
            for (int c = 0; c < 4; c++) if (uv[c] != null) nuv[c].Add(uv[c][i]);
            return mapa[i];
        }
    }

    static Mesh CortarCabeza(Mesh src, Vector3[] enRaiz, float corteY, Vector3 cabeza, float radio, string ruta, out int triangulos)
    {
        triangulos = 0;
        var r = new Recorte();
        r.v = src.vertices;
        if (r.v == null || r.v.Length == 0) return null;
        int nVert = r.v.Length;
        var normales = src.normals; r.n = normales != null && normales.Length == nVert ? normales : null;
        var tangentes = src.tangents; r.tg = tangentes != null && tangentes.Length == nVert ? tangentes : null;
        var colores = src.colors; r.col = colores != null && colores.Length == nVert ? colores : null;
        for (int c = 0; c < 4; c++)
        {
            var l = new List<Vector2>();
            src.GetUVs(c, l);
            if (l.Count == nVert) { r.uv[c] = l; r.nuv[c] = new List<Vector2>(); }
        }
        r.mapa = new int[nVert];
        for (int i = 0; i < nVert; i++) r.mapa[i] = -1;

        float radio2 = radio * radio;
        var subs = new List<int[]>();
        for (int s = 0; s < src.subMeshCount; s++)
        {
            int[] t = src.GetTriangles(s);
            var lista = new List<int>();
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                int a = t[i], b = t[i + 1], c = t[i + 2];
                Vector3 centro = (enRaiz[a] + enRaiz[b] + enRaiz[c]) / 3f;
                if (centro.y < corteY) continue;                                  // abajo del cuello: fuera
                float dx = centro.x - cabeza.x, dz = centro.z - cabeza.z;
                if (dx * dx + dz * dz > radio2) continue;                          // brazos o cosas lejos de la cabeza: fuera
                lista.Add(r.Indice(a));
                lista.Add(r.Indice(b));
                lista.Add(r.Indice(c));
            }
            triangulos += lista.Count / 3;
            subs.Add(lista.ToArray());
        }
        if (triangulos == 0) return null;

        var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        bool nueva = malla == null;
        if (nueva) malla = new Mesh();
        else malla.Clear();
        malla.name = "Cabeza_" + src.name;
        malla.indexFormat = r.nv.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        malla.SetVertices(r.nv);
        if (r.n != null) malla.SetNormals(r.nn);
        if (r.tg != null) malla.SetTangents(r.ntg);
        if (r.col != null) malla.SetColors(r.ncol);
        for (int c = 0; c < 4; c++) if (r.nuv[c] != null) malla.SetUVs(c, r.nuv[c]);
        malla.subMeshCount = subs.Count;
        for (int s = 0; s < subs.Count; s++) malla.SetTriangles(subs[s], s);
        malla.RecalculateBounds();
        if (nueva) AssetDatabase.CreateAsset(malla, ruta);
        else EditorUtility.SetDirty(malla);
        return malla;
    }

    static void SoloCabezaChimenea()
    {
        var e = EntradaChimenea();
        if (e == null || e.monstruo == null)
        {
            EditorUtility.DisplayDialog("Noche", "No encontré el monstruo de la chimenea (EntradaMonstruo tipo Chimenea con su Monstruo).", "OK");
            return;
        }
        var mv = e.monstruo;
        Transform raiz = mv.transform;

        // las piezas con malla del monstruo de la chimenea
        var piezas = new List<Component>();
        foreach (var mf in mv.GetComponentsInChildren<MeshFilter>(true)) piezas.Add(mf);
        foreach (var sk in mv.GetComponentsInChildren<SkinnedMeshRenderer>(true)) piezas.Add(sk);
        if (piezas.Count == 0)
        {
            EditorUtility.DisplayDialog("Noche", "El monstruo de la chimenea no tiene mallas.", "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("Noche", "Leyendo el modelo...", 0.1f);
        try
        {
            foreach (var p in piezas) HacerLegible(MallaOriginal(p, MallaDe(p)));

            // vértices en el espacio del monstruo (de pie, aunque la chimenea esté de cabeza)
            var originales = new List<Mesh>();
            var puntos = new List<Vector3[]>();
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in piezas)
            {
                var m = MallaOriginal(p, MallaDe(p));
                originales.Add(m);
                Vector3[] vs = m != null ? m.vertices : null;
                if (vs == null) vs = new Vector3[0];
                Matrix4x4 aRaiz = raiz.worldToLocalMatrix * p.transform.localToWorldMatrix;
                var enRaiz = new Vector3[vs.Length];
                for (int i = 0; i < vs.Length; i++)
                {
                    enRaiz[i] = aRaiz.MultiplyPoint3x4(vs[i]);
                    if (enRaiz[i].y < minY) minY = enRaiz[i].y;
                    if (enRaiz[i].y > maxY) maxY = enRaiz[i].y;
                }
                puntos.Add(enRaiz);
            }
            if (maxY <= minY)
            {
                EditorUtility.DisplayDialog("Noche", "No pude leer los vértices del modelo. Activa Read/Write en el FBX y vuelve a intentar.", "OK");
                return;
            }

            float alto = maxY - minY;
            float corteY = minY + alto * corteCuello;
            Vector3 cabeza = mv.cabeza != null ? raiz.InverseTransformPoint(mv.cabeza.position) : new Vector3(0f, minY + alto * 0.875f, 0f);
            float radio = alto * 0.2f;

            int total = 0;
            for (int i = 0; i < piezas.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Noche", "Cortando la cabeza...", 0.3f + 0.6f * i / piezas.Count);
                int tris = 0;
                Mesh cabezaMalla = originales[i] != null
                    ? CortarCabeza(originales[i], puntos[i], corteY, cabeza, radio, Carpeta + "/Cabeza_Chimenea_" + i + ".asset", out tris)
                    : null;
                var rend = RenderDe(piezas[i]);
                if (cabezaMalla != null)
                {
                    PonerMalla(piezas[i], cabezaMalla);
                    if (rend != null && !rend.enabled) { Undo.RecordObject(rend, "Solo cabeza"); rend.enabled = true; }
                }
                else if (rend != null)
                {
                    Undo.RecordObject(rend, "Solo cabeza");
                    rend.enabled = false;                 // esta pieza no tiene nada de la cabeza
                    PrefabUtility.RecordPrefabInstancePropertyModifications(rend);
                }
                total += tris;
            }

            if (total == 0)
            {
                CuerpoCompletoChimenea(false);
                EditorUtility.DisplayDialog("Noche", "Con ese corte no quedó nada. Baja el 'Corte del cuello' y vuelve a intentar.", "OK");
                return;
            }

            // la cabeza sola no "respira" (si no, sube y baja como si flotara); solo tiembla
            Undo.RecordObject(mv, "Solo cabeza");
            mv.respiracion = 0f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(mv);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(e.gameObject.scene);
            EditorUtility.DisplayDialog("Noche",
                "Listo: el monstruo de la chimenea ahora es SOLO LA CABEZA (" + (total / 1000) + " mil triángulos).\n" +
                "No moví ni escalé nada.\n\n" +
                "Si quedó mucho cuello, sube el 'Corte del cuello'; si se cortó la barbilla, bájalo. Y vuelve a presionar el botón.\n" +
                "Pruébalo con Play + F3.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void CuerpoCompletoChimenea() => CuerpoCompletoChimenea(true);

    static void CuerpoCompletoChimenea(bool avisar)
    {
        var e = EntradaChimenea();
        if (e == null || e.monstruo == null) return;
        var mv = e.monstruo;
        foreach (var mf in mv.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = MallaOriginal(mf, null);
            if (m != null && mf.sharedMesh != m) PonerMalla(mf, m);
        }
        foreach (var sk in mv.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var m = MallaOriginal(sk, null);
            if (m != null && sk.sharedMesh != m) PonerMalla(sk, m);
        }
        foreach (var r in mv.GetComponentsInChildren<Renderer>(true))
            if (!r.enabled) { Undo.RecordObject(r, "Cuerpo completo"); r.enabled = true; }
        Undo.RecordObject(mv, "Cuerpo completo");
        mv.respiracion = 0.015f;
        EditorSceneManager.MarkSceneDirty(e.gameObject.scene);
        if (avisar) EditorUtility.DisplayDialog("Noche", "Listo: el monstruo de la chimenea tiene otra vez el cuerpo completo.", "OK");
    }

    static void GirarMonstruo()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaFinal) == null)
        {
            EditorUtility.DisplayDialog("Noche", "Primero presiona 'Usar MI monstruo'.", "OK");
            return;
        }
        var raiz = PrefabUtility.LoadPrefabContents(RutaFinal);
        var mv = raiz.GetComponent<MonstruoVisual>();
        Quaternion giro = Quaternion.Euler(0f, 180f, 0f);
        if (mv != null && mv.cuerpo != null)
        {
            mv.cuerpo.localPosition = giro * mv.cuerpo.localPosition;
            mv.cuerpo.localRotation = giro * mv.cuerpo.localRotation;
        }
        if (mv != null && mv.cabeza != null)
        {
            var c = mv.cabeza.localPosition;
            mv.cabeza.localPosition = new Vector3(-c.x, c.y, -c.z);
        }
        PrefabUtility.SaveAsPrefabAsset(raiz, RutaFinal);
        PrefabUtility.UnloadPrefabContents(raiz);
        EditorUtility.DisplayDialog("Noche", "Listo: lo giré 180° (no moví las entradas). Pruébalo otra vez.", "OK");
    }

    static void AgregarEfectos(bool avisar)
    {
        var g = Object.FindAnyObjectByType<GestorNoche>(FindObjectsInactive.Include);
        if (g == null)
        {
            if (avisar) EditorUtility.DisplayDialog("Noche", "Primero presiona 'Armar la noche'.", "OK");
            return;
        }
        var ef = g.GetComponent<EfectosNoche>();
        if (ef == null) ef = Undo.AddComponent<EfectosNoche>(g.gameObject);
        Undo.RecordObject(ef, "Efectos");
        ef.gestor = g;
        ef.jugador = g.jugador;
        ef.linterna = g.linterna;
        ef.ui = g.ui;
        ef.materialAditivo = MaterialParticula();
        ef.materialPolvo = MaterialPolvo();
        EditorUtility.SetDirty(ef);

        // que la cámara tenga post-procesado
        if (g.jugador != null && g.jugador.camara != null)
        {
            var datos = g.jugador.camara.GetUniversalAdditionalCameraData();
            if (datos != null)
            {
                Undo.RecordObject(datos, "Efectos");
                datos.renderPostProcessing = true;
                EditorUtility.SetDirty(datos);
            }
        }

        if (avisar)
        {
            AsignarSonidos(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Noche",
                "Efectos agregados a NOCHE:\n" +
                "- Post-procesado que reacciona al peligro (viñeta que late, grano, aberración, distorsión)\n" +
                "- Temblor de cámara con cada golpe y campo de visión que se cierra\n" +
                "- Linterna con inercia que parpadea con el miedo\n" +
                "- Latido, tensión y respiración del Capi\n" +
                "- Relámpagos con la silueta del monstruo en la ventana\n" +
                "- Sustos: pasos en el techo, golpes, rechinidos, susurros\n" +
                "- Polvo, hollín y chispas\n\n" +
                "Ajusta todo en NOCHE > Efectos Noche.", "OK");
        }
    }

    static Material MaterialPolvo()
    {
        AsegurarCarpeta(Carpeta);
        string ruta = Carpeta + "/Mat_Particula_Polvo.mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;
        var aditivo = MaterialParticula();                  // crea también la textura suave
        var textura = aditivo != null ? aditivo.mainTexture : null;

        Material mat;
        var urp = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Particles/Unlit") : null;
        if (urp != null)
        {
            mat = new Material(urp);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);                      // alfa normal
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            if (textura != null) mat.SetTexture("_BaseMap", textura);
        }
        else
        {
            mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = textura;
        }
        mat.color = Color.white;
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    // =====================================================================
    // 3. PEGAR A LOS OBJETOS DEL USUARIO
    // =====================================================================

    static GestorNoche Gestor()
    {
        var g = Object.FindAnyObjectByType<GestorNoche>(FindObjectsInactive.Include);
        if (g == null) EditorUtility.DisplayDialog("Noche", "Primero presiona 'Armar la noche'.", "OK");
        return g;
    }

    static PuntoVista Vista(VistaNoche tipo)
    {
        foreach (var p in Object.FindObjectsByType<PuntoVista>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p.tipo == tipo) return p;
        return null;
    }

    static EntradaMonstruo Entrada(TipoEntrada tipo)
    {
        foreach (var e in Object.FindObjectsByType<EntradaMonstruo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (e.tipo == tipo) return e;
        return null;
    }

    static Vector3 Plano(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
    }

    static void PonerCama(Vector3 pos, Vector3 adelante)
    {
        var g = Gestor();
        if (g == null) return;
        var cama = g.jugador != null && g.jugador.vistaCama != null ? g.jugador.vistaCama : Vista(VistaNoche.Cama);
        if (cama == null) return;
        Undo.RecordObject(cama.transform, "Mover cama");
        cama.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Plano(adelante)));
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = cama.gameObject;
    }

    static void CamaDesdeScene()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
        {
            EditorUtility.DisplayDialog("Noche", "Abre la ventana Scene primero.", "OK");
            return;
        }
        PonerCama(sv.camera.transform.position, sv.camera.transform.forward);
    }

    static void CamaDesdeCamara(Camera cam)
    {
        if (cam == null) return;
        PonerCama(cam.transform.position, cam.transform.forward);
    }

    static bool EsDeLaNoche(Component c)
    {
        return EsDeLaNoche(c.transform);
    }

    static bool EsDeLaNoche(Transform c)
    {
        return c.GetComponentInParent<GestorNoche>() != null || c.GetComponentInParent<CortinasNoche>() != null
            || c.GetComponentInParent<FuegoChimenea>() != null || c.GetComponentInParent<MonstruoVisual>() != null
            || c.GetComponentInParent<EntradaMonstruo>() != null || c.GetComponentInParent<PuntoVista>() != null;
    }

    static Bounds Limites(GameObject go)
    {
        bool hay = false;
        var b = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || EsDeLaNoche(r)) continue;
            if (!hay) { b = r.bounds; hay = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!hay)
            foreach (var c in go.GetComponentsInChildren<Collider>())
            {
                if (EsDeLaNoche(c)) continue;
                if (!hay) { b = c.bounds; hay = true; }
                else b.Encapsulate(c.bounds);
            }
        if (!hay) b = new Bounds(go.transform.position, Vector3.one);
        return b;
    }

    // Hacia dónde está el cuarto: si el objeto es delgado (ventana, puerta) usa su lado más delgado
    static Vector3 HaciaCuarto(Bounds b, Vector3 cama)
    {
        Vector3 haciaCama = Plano(cama - b.center);
        float corto = Mathf.Min(b.extents.x, b.extents.z), largo = Mathf.Max(b.extents.x, b.extents.z);
        if (largo < 0.001f || corto / largo > 0.6f) return haciaCama;
        Vector3 normal = b.extents.x < b.extents.z ? Vector3.right : Vector3.forward;
        return Vector3.Dot(normal, haciaCama) >= 0f ? normal : -normal;
    }

    static float Ancho(Bounds b, Vector3 eje)
    {
        return 2f * (b.extents.x * Mathf.Abs(eje.x) + b.extents.z * Mathf.Abs(eje.z));
    }

    static void Alinear(GameObject real, TipoEntrada tipo)
    {
        if (real == null) return;
        var g = Gestor();
        if (g == null) return;
        if (real.GetComponentInParent<GestorNoche>() != null || real.GetComponent<PuntoVista>() != null || real.GetComponent<EntradaMonstruo>() != null)
        {
            EditorUtility.DisplayDialog("Noche", "Seleccionaste una pieza de NOCHE. Selecciona TU objeto (el modelo de tu ventana, puerta o chimenea) en la Hierarchy.", "OK");
            return;
        }
        var cama = g.jugador != null && g.jugador.vistaCama != null ? g.jugador.vistaCama : Vista(VistaNoche.Cama);
        if (cama == null) return;

        Bounds b = Limites(real);
        Vector3 adentro = HaciaCuarto(b, cama.Pose.position);
        Vector3 derecha = Vector3.Cross(Vector3.up, adentro);
        float ancho = Mathf.Clamp(Ancho(b, derecha), 0.4f, 4f);
        float fondo = Mathf.Clamp(Ancho(b, adentro), 0.05f, 3f);
        float alto = Mathf.Clamp(b.size.y, 0.4f, 4f);
        Quaternion rot = Quaternion.LookRotation(adentro);
        var entrada = Entrada(tipo);
        GameObject resultado = null;

        switch (tipo)
        {
            case TipoEntrada.Ventana:
            {
                var vista = Vista(VistaNoche.Ventana);
                if (vista == null) return;
                Regresar(vista.transform, g.transform);
                Undo.RegisterFullObjectHierarchyUndo(vista.gameObject, "Pegar ventana");
                vista.transform.SetPositionAndRotation(b.center, rot);
                var col = vista.GetComponent<BoxCollider>();
                if (col != null) { col.center = Vector3.zero; col.size = new Vector3(ancho + 0.2f, alto + 0.2f, fondo + 0.3f); }
                if (vista.poseCamara != null)
                    vista.poseCamara.SetPositionAndRotation(b.center + adentro * (fondo * 0.5f + 0.9f), Quaternion.LookRotation(-adentro));

                var cort = vista.GetComponentInChildren<CortinasNoche>(true);
                if (cort != null)
                {
                    cort.transform.SetPositionAndRotation(b.center + adentro * (fondo * 0.5f + 0.12f), rot);
                    AjustarCortina(cort.izquierda, -1, ancho, alto);
                    AjustarCortina(cort.derecha, 1, ancho, alto);
                    var varilla = cort.transform.Find("Varilla");
                    if (varilla != null)
                    {
                        varilla.localPosition = new Vector3(0f, alto * 0.5f + 0.1f, 0f);
                        varilla.localScale = new Vector3(0.03f, ancho * 0.5f + 0.2f, 0.03f);
                    }
                }
                if (entrada != null)
                {
                    Undo.RecordObject(entrada.transform, "Pegar ventana");
                    entrada.transform.SetPositionAndRotation(b.center - Vector3.up * 1.95f - adentro * (fondo * 0.5f + 0.35f), rot);
                }
                resultado = vista.gameObject;
                break;
            }

            case TipoEntrada.Puerta:
            {
                if (entrada == null) return;
                var raizPuerta = entrada.transform.parent != null ? entrada.transform.parent.gameObject : entrada.gameObject;
                Regresar(raizPuerta.transform, g.transform);
                Undo.RegisterFullObjectHierarchyUndo(raizPuerta, "Pegar puerta");
                Undo.RecordObject(entrada, "Pegar puerta");
                raizPuerta.transform.SetPositionAndRotation(b.center, rot);
                // el vidrio de la puerta: parte de arriba, cara que da al cuarto
                Vector3 vidrio = new Vector3(b.center.x, b.min.y + b.size.y * 0.72f, b.center.z) + adentro * (fondo * 0.5f);
                if (entrada.puntoMirar != null) entrada.puntoMirar.position = vidrio;
                entrada.transform.SetPositionAndRotation(vidrio - Vector3.up * 1.95f - adentro * (fondo + 0.45f), rot);
                entrada.puertaQueTiembla = real.transform;
                EditorUtility.SetDirty(entrada);
                resultado = raizPuerta;
                break;
            }

            case TipoEntrada.Chimenea:
            {
                var vista = Vista(VistaNoche.Chimenea);
                if (vista == null) return;
                Regresar(vista.transform, g.transform);
                Undo.RegisterFullObjectHierarchyUndo(vista.gameObject, "Pegar chimenea");
                Vector3 piso = new Vector3(b.center.x, b.min.y, b.center.z);
                vista.transform.SetPositionAndRotation(piso, rot);
                var col = vista.GetComponent<BoxCollider>();
                if (col != null)
                {
                    float altoCol = Mathf.Min(alto, 1.6f);
                    col.center = new Vector3(0f, altoCol * 0.5f, 0f);
                    col.size = new Vector3(ancho + 0.2f, altoCol, fondo + 0.3f);
                }
                if (vista.poseCamara != null)
                {
                    Vector3 pose = piso + adentro * (fondo * 0.5f + 1.2f) + Vector3.up * 1.25f;
                    vista.poseCamara.SetPositionAndRotation(pose, Quaternion.LookRotation(piso + Vector3.up * 0.35f - pose));
                }
                var fuego = vista.GetComponentInChildren<FuegoChimenea>(true);
                if (fuego != null) fuego.transform.SetPositionAndRotation(piso + Vector3.up * 0.02f, rot);
                if (entrada != null)
                {
                    Undo.RecordObject(entrada.transform, "Pegar chimenea");
                    entrada.transform.SetPositionAndRotation(piso - Vector3.up * 0.9f, rot);
                }
                resultado = vista.gameObject;
                break;
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (resultado != null)
        {
            Selection.activeGameObject = resultado;
            SceneView.FrameLastActiveSceneView();
        }
    }

    // Si la pieza quedó dentro de otro objeto (que puede estar escalado), la regresa a NOCHE
    static void Regresar(Transform pieza, Transform noche)
    {
        if (pieza.parent != noche) Undo.SetTransformParent(pieza, noche, "Regresar a NOCHE");
        pieza.localScale = Vector3.one;
    }

    static void AjustarCortina(Transform pivote, int lado, float ancho, float alto)
    {
        if (pivote == null) return;
        float mitad = ancho * 0.5f + 0.1f;
        pivote.localPosition = new Vector3(lado * mitad, 0f, 0f);
        var tela = pivote.Find("Tela");
        if (tela != null)
        {
            tela.localPosition = new Vector3(-lado * mitad * 0.5f, 0f, 0f);
            tela.localScale = new Vector3(mitad, alto + 0.25f, 0.04f);
        }
    }

    // ---------------- Tus cortinas ----------------
    static int EjeLocal(Transform t, Vector3 d)
    {
        float ax = Mathf.Abs(Vector3.Dot(t.right, d));
        float ay = Mathf.Abs(Vector3.Dot(t.up, d));
        float az = Mathf.Abs(Vector3.Dot(t.forward, d));
        if (ax >= ay && ax >= az) return 0;
        return ay >= az ? 1 : 2;
    }

    static void UsarMisCortinas(GameObject sel)
    {
        var g = Gestor();
        if (g == null) return;
        var entrada = Entrada(TipoEntrada.Ventana);
        var vista = Vista(VistaNoche.Ventana);
        if (entrada == null || vista == null) return;
        if (EsDeLaNoche(sel.transform))
        {
            EditorUtility.DisplayDialog("Noche", "Selecciona TU objeto 'Cortinas', no el de NOCHE.", "OK");
            return;
        }

        // las telas son las piezas altas (la varilla y las bolas son bajitas)
        var piezas = new List<Renderer>();
        float maxAlto = 0f;
        foreach (var r in sel.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            piezas.Add(r);
            maxAlto = Mathf.Max(maxAlto, r.bounds.size.y);
        }

        Vector3 centro = vista.transform.position;                         // centro de la ventana (paso B)
        Vector3 d = Vector3.Cross(Vector3.up, vista.transform.forward).normalized;   // horizontal de la ventana
        var lista = new List<CortinasNoche.Panel>();
        var nombres = new List<string>();

        foreach (var r in piezas)
        {
            if (r.bounds.size.y < maxAlto * 0.6f) continue;
            Transform t = r.transform;
            Bounds b = r.bounds;
            float cProj = Vector3.Dot(b.center - centro, d);
            float ext = b.extents.x * Mathf.Abs(d.x) + b.extents.y * Mathf.Abs(d.y) + b.extents.z * Mathf.Abs(d.z);
            float lo = cProj - ext, hi = cProj + ext;
            if (hi - lo < 0.01f || (lo < 0f && hi > 0f && Mathf.Abs(cProj) < ext * 0.3f)) continue;   // no es una tela de un lado

            float lado = cProj >= 0f ? 1f : -1f;
            float exterior = lado > 0f ? hi : lo;
            float interiorMeta = -lado * 0.03f;                                // que se encimen un poquito al centro
            float k = Mathf.Max(1f, Mathf.Abs(exterior - interiorMeta) / (hi - lo));

            int eje = EjeLocal(t, d);
            Vector3 escA = t.localScale;
            Vector3 escC = escA;
            escC[eje] *= k;

            float pivote = Vector3.Dot(t.position - centro, d);
            float exteriorNuevo = pivote + (exterior - pivote) * k;
            Vector3 mover = d * (exterior - exteriorNuevo);                    // que el borde de afuera no se mueva
            Vector3 posA = t.localPosition;
            Vector3 posC = posA + (t.parent != null ? t.parent.InverseTransformVector(mover) : mover);

            lista.Add(new CortinasNoche.Panel { panel = t, posicionAbierta = posA, posicionCerrada = posC, escalaAbierta = escA, escalaCerrada = escC });
            nombres.Add(t.name);
        }

        if (lista.Count == 0)
        {
            EditorUtility.DisplayDialog("Noche", "No encontré las telas dentro de '" + sel.name + "'. ¿Seleccionaste el objeto Cortinas?", "OK");
            return;
        }

        var cort = sel.GetComponent<CortinasNoche>();
        if (cort == null) cort = Undo.AddComponent<CortinasNoche>(sel);
        Undo.RecordObject(cort, "Mis cortinas");
        cort.paneles = lista.ToArray();
        cort.izquierda = null;
        cort.derecha = null;
        cort.sonidoCerrar = Clip("Cortina_Cerrar");
        cort.sonidoAbrir = Clip("Cortina_Abrir");
        EditorUtility.SetDirty(cort);

        // quitar las cortinas provisionales
        foreach (var vieja in vista.GetComponentsInChildren<CortinasNoche>(true))
            if (vieja != cort) Undo.DestroyObjectImmediate(vieja.gameObject);

        Undo.RecordObject(entrada, "Mis cortinas");
        entrada.cortinas = cort;
        EditorUtility.SetDirty(entrada);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Noche",
            "Listo: tus cortinas se cierran con Espacio.\nTelas: " + string.Join(", ", nombres) +
            "\n\nUsa 'Ver cortinas CERRADAS' para ver cómo quedan cerradas.", "OK");
    }

    // ---------------- Tu fuego ----------------
    static void UsarMiFuego(GameObject sel)
    {
        var fuego = Object.FindAnyObjectByType<FuegoChimenea>(FindObjectsInactive.Include);
        if (fuego == null)
        {
            EditorUtility.DisplayDialog("Noche", "Primero presiona 'Armar la noche'.", "OK");
            return;
        }
        var ps = sel.GetComponent<ParticleSystem>();
        if (ps == null) ps = sel.GetComponentInChildren<ParticleSystem>(true);
        if (ps == null || EsDeLaNoche(ps))
        {
            EditorUtility.DisplayDialog("Noche", "Selecciona TU objeto 'Fuego' (el que tiene el Particle System).", "OK");
            return;
        }

        // que tu fuego empiece apagado
        foreach (var p in sel.GetComponentsInChildren<ParticleSystem>(true))
        {
            Undo.RecordObject(p, "Mi fuego");
            var main = p.main;
            main.playOnAwake = false;
            EditorUtility.SetDirty(p);
        }

        Undo.RecordObject(fuego, "Mi fuego");

        // quitar las partículas y troncos provisionales
        if (fuego.particulas != null && fuego.particulas != ps && fuego.particulas.GetComponentInParent<FuegoChimenea>() == fuego)
            Undo.DestroyObjectImmediate(fuego.particulas.gameObject);
        if (fuego.brasas != null)
            foreach (var r in fuego.brasas)
                if (r != null && r.GetComponentInParent<FuegoChimenea>() == fuego) Undo.DestroyObjectImmediate(r.gameObject);
        fuego.brasas = new Renderer[0];
        fuego.particulas = ps;

        // tu luz del fuego: la Point Light más cercana que no sea de NOCHE
        Light mejor = null;
        float dMin = 4f;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.type != LightType.Point || EsDeLaNoche(l) || l.GetComponentInParent<LinternaNoche>() != null) continue;
            float dist = Vector3.Distance(l.transform.position, sel.transform.position);
            if (dist < dMin) { dMin = dist; mejor = l; }
        }
        string textoLuz = "No encontré una Point Light cerca: se queda la luz provisional.";
        if (mejor != null)
        {
            if (fuego.luz != null && fuego.luz != mejor && fuego.luz.GetComponentInParent<FuegoChimenea>() == fuego)
                Undo.DestroyObjectImmediate(fuego.luz.gameObject);
            fuego.luz = mejor;
            fuego.intensidadLuz = mejor.intensity;
            textoLuz = "Luz del fuego: '" + mejor.name + "' (se apaga y prende con el fuego).";
        }

        Undo.RecordObject(fuego.transform, "Mi fuego");
        fuego.transform.position = sel.transform.position;     // de ahí sale el sonido del fuego
        EditorUtility.SetDirty(fuego);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Noche",
            "Listo: tu fuego '" + sel.name + "' empieza apagado y se prende con Espacio en la chimenea.\n" + textoLuz, "OK");
    }

    // Apaga los scripts que ya movían tu cámara (ej. Camara Con Linterna FNAF) y esconde tu linterna anterior
    static void RevisarConflictos(bool avisar)
    {
        var g = Object.FindAnyObjectByType<GestorNoche>(FindObjectsInactive.Include);
        if (g == null || g.jugador == null || g.jugador.camara == null)
        {
            if (avisar) EditorUtility.DisplayDialog("Noche", "Primero presiona 'Armar la noche'.", "OK");
            return;
        }
        var control = g.jugador;
        Undo.RecordObject(control, "Revisar choques");

        var apagar = new List<Behaviour>();
        if (control.desactivarDuranteNoche != null)
            foreach (var b in control.desactivarDuranteNoche) if (b != null) apagar.Add(b);
        var nombres = new List<string>();

        foreach (var mb in control.camara.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb is ControlJugadorNoche || mb is LinternaNoche) continue;
            if (!string.IsNullOrEmpty(mb.GetType().Namespace)) continue;   // URP, Cinemachine, etc. se quedan
            if (!apagar.Contains(mb)) { apagar.Add(mb); nombres.Add(mb.GetType().Name); }
        }
        control.desactivarDuranteNoche = apagar.ToArray();

        var esconder = new List<GameObject>();
        if (control.ocultarDuranteNoche != null)
            foreach (var go in control.ocultarDuranteNoche) if (go != null) esconder.Add(go);

        foreach (var luz in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (EsDeLaNoche(luz) || luz.GetComponentInParent<LinternaNoche>() != null) continue;
            for (var t = luz.transform; t != null; t = t.parent)
            {
                if (!t.name.ToLower().Contains("linterna")) continue;
                if (!esconder.Contains(t.gameObject)) { esconder.Add(t.gameObject); nombres.Add(t.name + " (se esconde)"); }
                break;
            }
        }
        control.ocultarDuranteNoche = esconder.ToArray();

        EditorUtility.SetDirty(control);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (avisar)
            EditorUtility.DisplayDialog("Noche",
                nombres.Count > 0 ? "Durante la noche se apagan:\n- " + string.Join("\n- ", nombres) : "No encontré nada que choque.",
                "OK");
    }

    // =====================================================================
    // 3b. SONIDOS
    // =====================================================================

    static int asignados, faltan;

    static AudioClip S(string nombre)
    {
        var c = Clip(nombre);
        if (c != null) asignados++; else faltan++;
        return c;
    }

    static void AsignarSonidos(bool avisar)
    {
        asignados = 0;
        faltan = 0;

        foreach (var g in Object.FindObjectsByType<GestorNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(g, "Sonidos");
            g.ambiente = S("Noche_Ambiente");
            g.sonidoHora = S("Noche_Hora");
            g.sonidoVictoria = S("Noche_6AM");
            g.sonidoJumpscare = S("Noche_Jumpscare");
            g.musicaGameOver = S("GameOver_Musica");
            g.golpeGameOver = S("GameOver_Golpe");
            var imagen = ImagenGameOver();
            if (imagen != null) g.imagenGameOver = imagen;
            EditorUtility.SetDirty(g);
        }
        foreach (var c in Object.FindObjectsByType<ControlJugadorNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(c, "Sonidos");
            c.sonidoPasos = S("Noche_Pasos");
            c.sonidoCama = S("Noche_Cama");
            EditorUtility.SetDirty(c);
        }
        foreach (var l in Object.FindObjectsByType<LinternaNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(l, "Sonidos");
            l.sonidoClick = S("Linterna_Click");
            l.sonidoSinPila = S("Linterna_SinPila");
            EditorUtility.SetDirty(l);
        }
        foreach (var c in Object.FindObjectsByType<CortinasNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(c, "Sonidos");
            c.sonidoCerrar = S("Cortina_Cerrar");
            c.sonidoAbrir = S("Cortina_Abrir");
            EditorUtility.SetDirty(c);
        }
        foreach (var f in Object.FindObjectsByType<FuegoChimenea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(f, "Sonidos");
            f.sonidoEncender = S("Fuego_Encender");
            f.sonidoApagar = S("Fuego_Apagar");
            f.sonidoLoop = S("Fuego_Loop");
            f.sonidoSinLena = S("Fuego_SinLena");
            EditorUtility.SetDirty(f);
        }
        foreach (var ef in Object.FindObjectsByType<EfectosNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(ef, "Sonidos");
            ef.latido = S("Latido_Loop");
            ef.tension = S("Tension_Loop");
            ef.respiracion = S("Respiracion_Capi");
            ef.trueno = S("Trueno");
            ef.stingerAparicion = S("Stinger_Aparicion");
            ef.monstruoHuye = S("Monstruo_Huye");
            ef.estatica = S("Estatica");
            ef.estaticaLoop = S("Estatica_Loop");
            ef.respiracionAgitada = S("Respiracion_Agitada");
            var lista = new List<AudioClip>();
            foreach (var n in new[] { "Pasos_Techo", "Golpe_Lejano", "Rechinido_Casa", "Susurro_Fantasma", "Marioneta_GiraCabeza" })
            {
                var c = Clip(n);
                if (c != null) lista.Add(c);
            }
            ef.sustos = lista.ToArray();
            EditorUtility.SetDirty(ef);
        }
        foreach (var e in Object.FindObjectsByType<EntradaMonstruo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(e, "Sonidos");
            e.sonidoSeVa = S("Monstruo_SeVa");
            switch (e.tipo)
            {
                case TipoEntrada.Ventana:
                    e.sonidoIntento = S("Ventana_Intento");
                    e.sonidoEntra = S("Ventana_Rompe");
                    break;
                case TipoEntrada.Puerta:
                    e.sonidoIntento = S("Puerta_Intento");
                    e.sonidoEntra = S("Puerta_Revienta");
                    break;
                case TipoEntrada.Chimenea:
                    e.sonidoIntento = S("Chimenea_Intento");
                    e.sonidoEntra = S("Chimenea_Cae");
                    break;
            }
            EditorUtility.SetDirty(e);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (avisar || faltan > 0)
            EditorUtility.DisplayDialog("Noche",
                $"Sonidos asignados: {asignados}" + (faltan > 0 ? $"\nNo encontrados: {faltan} (¿ya importaste la carpeta Sonidos_Noche?)" : ""),
                "OK");
    }

    // Busca la imagen GameOver_Capi y la deja lista (sin reescalar, sin mipmaps) para la pantalla de Game Over
    static Texture2D ImagenGameOver()
    {
        foreach (var guid in AssetDatabase.FindAssets("GameOver_Capi t:Texture2D"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) != "GameOver_Capi") continue;
            var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Default || imp.npotScale != TextureImporterNPOTScale.None || imp.mipmapEnabled))
            {
                imp.textureType = TextureImporterType.Default;
                imp.npotScale = TextureImporterNPOTScale.None;     // que no la estire a 2048x1024
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
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

    // =====================================================================
    // Materiales
    // =====================================================================

    static void AsegurarCarpeta(string ruta)
    {
        string[] partes = ruta.Split('/');
        string actual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = actual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(siguiente)) AssetDatabase.CreateFolder(actual, partes[i]);
            actual = siguiente;
        }
    }

    static Shader ShaderLit()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s != null) return s;
            s = Shader.Find("HDRP/Lit");
            if (s != null) return s;
        }
        return Shader.Find("Standard");
    }

    static Material CrearMaterial(string nombre, Color color, Color emision)
    {
        AsegurarCarpeta(Carpeta);
        string ruta = Carpeta + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = mat == null;
        if (nuevo) mat = new Material(ShaderLit());

        mat.color = color;
        if (emision != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emision);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (nuevo) AssetDatabase.CreateAsset(mat, ruta);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material MaterialParticula()
    {
        AsegurarCarpeta(Carpeta);
        string rutaMat = Carpeta + "/Mat_Particula_Fuego.mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
        if (existente != null) return existente;

        // textura: círculo suave
        string rutaTex = Carpeta + "/Particula_Suave.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex) == null)
        {
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            File.WriteAllBytes(rutaTex, tex.EncodeToPNG());
            DestroyImmediate(tex);
            AssetDatabase.ImportAsset(rutaTex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(rutaTex);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);

        Material mat;
        var urp = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Particles/Unlit") : null;
        if (urp != null)
        {
            mat = new Material(urp);
            mat.SetFloat("_Surface", 1f);                       // transparente
            mat.SetFloat("_Blend", 2f);                         // aditivo
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetTexture("_BaseMap", textura);
        }
        else
        {
            var s = Shader.Find("Legacy Shaders/Particles/Additive");
            if (s == null) s = Shader.Find("Sprites/Default");
            mat = new Material(s);
            mat.mainTexture = textura;
        }
        mat.color = Color.white;
        AssetDatabase.CreateAsset(mat, rutaMat);
        return mat;
    }
}
#endif
