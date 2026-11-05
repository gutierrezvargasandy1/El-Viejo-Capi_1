using UnityEngine;
using UnityEngine.InputSystem;

public class SeguirMouse : MonoBehaviour
{
    [Header("Configuracion de Seguimiento 3D")]
    public float distanciaFrente = 2f;
    public float velocidadSuavizado = 10f;

    private Camera camBase;

    void Start()
    {
        camBase = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null || camBase == null) return;

        // Obtenemos la posicion actual del mouse en la pantalla
        Vector2 mousePosScreen = Mouse.current.position.ReadValue();

        // Lanzamos un rayo 3D desde la camara hacia la posicion del cursor en la pantalla
        Ray ray = camBase.ScreenPointToRay(mousePosScreen);

        // Calculamos la posicion objetivo manteniendo la distancia especificada frente a la camara
        Vector3 posicionObjetivo = ray.GetPoint(distanciaFrente);

        // Movemos el objeto suavemente hacia la posicion calculada en el espacio tridimensional
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, velocidadSuavizado * Time.deltaTime);
    }
}