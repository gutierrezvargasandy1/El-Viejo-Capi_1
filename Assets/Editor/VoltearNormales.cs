#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Herramientas > Voltear normales del seleccionado
/// Para telas o planos que se ven oscuros porque su cara "buena" apunta hacia el otro lado.
/// Crea una copia del mesh con las normales al revés (el original no se toca) y la asigna.
/// Funciona con Cloth: no cambia el número ni el orden de los vértices.
/// Si queda peor, presiónalo otra vez y regresa como estaba.
/// </summary>
public static class VoltearNormales
{
    const string Carpeta = "Assets/Prefabs/Noche";

    [MenuItem("Herramientas/Voltear normales del seleccionado")]
    static void Voltear()
    {
        var seleccion = Selection.gameObjects;
        if (seleccion == null || seleccion.Length == 0)
        {
            EditorUtility.DisplayDialog("Voltear normales", "Selecciona en la Hierarchy la tela (ej. Plane y Plane.001).", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/Prefabs", "Noche");

        int hechos = 0;
        foreach (var go in seleccion)
        {
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var nuevo = Copiar(smr.sharedMesh);
                if (nuevo == null) continue;
                Undo.RecordObject(smr, "Voltear normales");
                smr.sharedMesh = nuevo;
                EditorUtility.SetDirty(smr);
                hechos++;
            }
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var nuevo = Copiar(mf.sharedMesh);
                if (nuevo == null) continue;
                Undo.RecordObject(mf, "Voltear normales");
                mf.sharedMesh = nuevo;
                EditorUtility.SetDirty(mf);
                hechos++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Voltear normales",
            hechos > 0 ? $"Listo: {hechos} mesh volteado(s). Dale Play y apunta la linterna a la tela." : "No encontré meshes en lo seleccionado.",
            "OK");
    }

    static Mesh Copiar(Mesh original)
    {
        if (original == null) return null;

        // Si ya es una copia volteada, la regresamos a su original volteándola otra vez
        var copia = Object.Instantiate(original);
        string nombre = original.name.EndsWith("_Volteado")
            ? original.name.Substring(0, original.name.Length - "_Volteado".Length)
            : original.name + "_Volteado";
        copia.name = nombre;

        // normales al revés
        var normales = copia.normals;
        for (int i = 0; i < normales.Length; i++) normales[i] = -normales[i];
        copia.normals = normales;

        // triángulos al revés (el orden de los vértices no cambia: el Cloth sigue funcionando)
        for (int s = 0; s < copia.subMeshCount; s++)
        {
            var tris = copia.GetTriangles(s);
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int t = tris[i + 1];
                tris[i + 1] = tris[i + 2];
                tris[i + 2] = t;
            }
            copia.SetTriangles(tris, s);
        }
        copia.RecalculateTangents();

        string ruta = AssetDatabase.GenerateUniqueAssetPath(Carpeta + "/" + nombre + ".asset");
        AssetDatabase.CreateAsset(copia, ruta);
        return copia;
    }
}
#endif
