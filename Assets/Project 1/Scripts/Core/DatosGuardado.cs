using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Datos que se escriben en el archivo de guardado.
/// Agrega aquí lo que quieras recordar (vida, inventario, etc.).
/// </summary>
[System.Serializable]
public class DatosGuardado
{
    public string escena;               // Nombre de la escena donde se guardó
    public string puntoId;              // ID del PuntoDeGuardado
    public Vector3 posicion;            // Respaldo por si no se encuentra el punto
    public float rotacionY;
    public List<string> banderas = new List<string>(); // Objetivos cumplidos, llaves, puertas abiertas...
}

/// <summary>
/// Sistema de guardado estático. No necesitas ponerlo en ningún GameObject.
/// El archivo queda en Application.persistentDataPath/partida.json
/// </summary>
public static class SistemaGuardado
{
    static string Ruta => Path.Combine(Application.persistentDataPath, "partida.json");

    static DatosGuardado datos = new DatosGuardado();

    /// <summary>True cuando venimos de "Continuar" y hay que colocar al jugador en su checkpoint.</summary>
    public static bool PendienteDeCargar { get; private set; }

    public static DatosGuardado Datos => datos;

    public static bool HayPartida() => File.Exists(Ruta);

    // ------------------------------------------------------------ Guardar
    public static void Guardar(string puntoId, Transform jugador)
    {
        datos.escena = SceneManager.GetActiveScene().name;
        datos.puntoId = puntoId;
        datos.posicion = jugador.position;
        datos.rotacionY = jugador.eulerAngles.y;
        Escribir();
        Debug.Log($"[Guardado] Escena: {datos.escena} | Punto: {puntoId}");
    }

    static void Escribir()
    {
        File.WriteAllText(Ruta, JsonUtility.ToJson(datos, true));
    }

    // ------------------------------------------------------------ Cargar
    /// <summary>Lee el archivo y abre la escena guardada. Úsalo en el botón Continuar.</summary>
    public static bool CargarPartida()
    {
        if (!HayPartida()) return false;
        datos = JsonUtility.FromJson<DatosGuardado>(File.ReadAllText(Ruta)) ?? new DatosGuardado();
        if (string.IsNullOrEmpty(datos.escena)) return false;
        PendienteDeCargar = true;
        SceneManager.LoadScene(datos.escena);
        return true;
    }

    /// <summary>Lo llama el PuntoDeGuardado cuando ya colocó al jugador.</summary>
    public static void MarcarCargado() => PendienteDeCargar = false;

    // ------------------------------------------------------------ Nueva partida
    public static void BorrarPartida()
    {
        if (File.Exists(Ruta)) File.Delete(Ruta);
        datos = new DatosGuardado();
        PendienteDeCargar = false;
    }

    // ------------------------------------------------------------ Banderas (objetivos, objetos, puertas)
    public static void ActivarBandera(string nombre)
    {
        if (!datos.banderas.Contains(nombre)) datos.banderas.Add(nombre);
    }

    public static bool TieneBandera(string nombre) => datos.banderas.Contains(nombre);

    // ------------------------------------------------------------ Mover al jugador
    /// <summary>Teletransporta al jugador. Desactiva el CharacterController un momento para que no lo regrese.</summary>
    public static void Teletransportar(Transform jugador, Vector3 pos, float rotY)
    {
        var cc = jugador.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        var rb = jugador.GetComponent<Rigidbody>();
        if (rb) { rb.position = pos; rb.linearVelocity = Vector3.zero; }

        jugador.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
        Physics.SyncTransforms();

        if (cc) cc.enabled = true;
    }
}