using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Herramientas → Arreglar Canvas
///
/// Los Canvas de las escenas estaban en "Constant Pixel Size": se ven bien solo en 1920x1080.
/// En otra resolución (la ventana Game, otro monitor, el celular) el párpado y las pantallas negras
/// no tapan todo y el banner de OBJETIVO se corta.
///
/// Esto los cambia a "Scale With Screen Size" (1920x1080) para que se vean igual en cualquier pantalla:
///   • Las imágenes de pantalla completa (párpados, pantalla negra, fondo negro) se estiran a toda la pantalla.
///   • Lo demás se ancla a su lado (izquierda / centro / derecha) sin moverse de donde lo pusiste en 1920x1080.
/// Solo toca los Canvas que estaban en "Constant Pixel Size".
/// </summary>
public static class ArreglarCanvas
{
    static readonly Vector2 Referencia = new Vector2(1920f, 1080f);

    [MenuItem("Herramientas/Arreglar Canvas/En todas las escenas del juego")]
    static void EnTodas()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Arreglar Canvas", "Sal del modo Play primero.", "OK"); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var rutas = new List<string>();
        foreach (var e in EditorBuildSettings.scenes)
            if (e.enabled && File.Exists(e.path) && !rutas.Contains(e.path)) rutas.Add(e.path);
        if (rutas.Count == 0) { EditorUtility.DisplayDialog("Arreglar Canvas", "No hay escenas en el Build.", "OK"); return; }

        var configuracion = EditorSceneManager.GetSceneManagerSetup();
        var reporte = new List<string>();
        int escenasCambiadas = 0;
        try
        {
            for (int i = 0; i < rutas.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Arreglar Canvas", Path.GetFileNameWithoutExtension(rutas[i]), (float)i / rutas.Count);
                var escena = EditorSceneManager.OpenScene(rutas[i], OpenSceneMode.Single);
                int n = ArreglarEscena(escena, reporte);
                if (n > 0)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                    escenasCambiadas++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (configuracion != null && configuracion.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(configuracion);
        }

        EditorUtility.DisplayDialog("Arreglar Canvas",
            reporte.Count == 0
                ? "Todos los Canvas ya se ajustaban a la pantalla. No cambié nada."
                : "Listo, " + escenasCambiadas + " escenas guardadas:\n\n• " + string.Join("\n• ", reporte.ToArray()) +
                  "\n\nAhora se ven igual en cualquier resolución (PC y celular).",
            "OK");
    }

    [MenuItem("Herramientas/Arreglar Canvas/Solo en la escena abierta")]
    static void EnLaAbierta()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Arreglar Canvas", "Sal del modo Play primero.", "OK"); return; }
        var reporte = new List<string>();
        var escena = SceneManager.GetActiveScene();
        int n = ArreglarEscena(escena, reporte);
        if (n > 0) EditorSceneManager.MarkSceneDirty(escena);
        EditorUtility.DisplayDialog("Arreglar Canvas",
            n == 0 ? "Los Canvas de esta escena ya se ajustaban a la pantalla."
                   : "Listo:\n\n• " + string.Join("\n• ", reporte.ToArray()) + "\n\nGuarda la escena con Ctrl+S (Ctrl+Z lo deshace).",
            "OK");
    }

    static int ArreglarEscena(Scene escena, List<string> reporte)
    {
        int n = 0;
        foreach (var raiz in escena.GetRootGameObjects())
        {
            foreach (var canvas in raiz.GetComponentsInChildren<Canvas>(true))
            {
                // solo los Canvas principales (los de adentro de otro Canvas siguen al de afuera)
                var padre = canvas.transform.parent;
                if (padre != null && padre.GetComponentInParent<Canvas>(true) != null) continue;
                if (canvas.renderMode == RenderMode.WorldSpace) continue;

                var escalador = canvas.GetComponent<CanvasScaler>();
                if (escalador == null) escalador = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
                if (escalador.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize) continue;   // ya escalaba bien

                Undo.RecordObject(escalador, "Arreglar Canvas");
                escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                escalador.referenceResolution = Referencia;
                escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                escalador.matchWidthOrHeight = 1f;          // el alto manda: en pantallas más anchas solo hay más espacio a los lados
                Registrar(escalador);

                int estirados = 0, anclados = 0;
                foreach (Transform hijo in canvas.transform)
                {
                    var rt = hijo as RectTransform;
                    if (rt == null) continue;
                    int r = Reanclar(rt);
                    if (r == 1) estirados++;
                    else if (r == 2) anclados++;
                }

                string detalle = estirados > 0 ? " (" + estirados + " a pantalla completa)" : "";
                reporte.Add(escena.name + ": " + canvas.name + detalle);
                n++;
            }
        }
        return n;
    }

    // 0 = no se tocó, 1 = se estiró a toda la pantalla, 2 = se ancló a su lado
    static int Reanclar(RectTransform rt)
    {
        // solo lo que está anclado a un punto en el centro (como quedan por defecto)
        if (rt.anchorMin != rt.anchorMax) return 0;
        if (Mathf.Abs(rt.anchorMin.x - 0.5f) > 0.001f || Mathf.Abs(rt.anchorMin.y - 0.5f) > 0.001f) return 0;

        Vector2 tam = Vector2.Scale(rt.sizeDelta, new Vector2(Mathf.Abs(rt.localScale.x), Mathf.Abs(rt.localScale.y)));
        Vector2 pos = rt.anchoredPosition;
        Undo.RecordObject(rt, "Arreglar Canvas");

        bool pantallaCompleta = tam.x >= Referencia.x * 0.9f && tam.y >= Referencia.y * 0.9f &&
                                Mathf.Abs(pos.x) < 60f && Mathf.Abs(pos.y) < 60f;
        if (pantallaCompleta)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            Registrar(rt);
            return 1;
        }

        // dónde está su centro en la pantalla de 1920x1080
        Vector2 centro = new Vector2(Referencia.x * 0.5f + pos.x + (0.5f - rt.pivot.x) * tam.x,
                                     Referencia.y * 0.5f + pos.y + (0.5f - rt.pivot.y) * tam.y);
        float ax = centro.x < Referencia.x / 3f ? 0f : (centro.x > Referencia.x * 2f / 3f ? 1f : 0.5f);
        float ay = centro.y < Referencia.y / 3f ? 0f : (centro.y > Referencia.y * 2f / 3f ? 1f : 0.5f);
        if (ax == 0.5f && ay == 0.5f) return 0;   // ya está bien en el centro

        var ancla = new Vector2(ax, ay);
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        // misma posición en 1920x1080, pero medida desde su nueva ancla
        rt.anchoredPosition = pos + new Vector2((0.5f - ax) * Referencia.x, (0.5f - ay) * Referencia.y);
        Registrar(rt);
        return 2;
    }

    static void Registrar(Object o)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(o)) PrefabUtility.RecordPrefabInstancePropertyModifications(o);
        EditorUtility.SetDirty(o);
    }
}
