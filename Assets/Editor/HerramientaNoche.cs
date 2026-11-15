#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
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

    Camera camara;
    Vector2 scroll;
    static bool cortinasCerradasVista;

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
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
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
