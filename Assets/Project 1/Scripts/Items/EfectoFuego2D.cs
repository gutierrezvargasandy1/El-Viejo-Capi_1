using UnityEngine;

public class EfectoFuego3D : MonoBehaviour
{
    private Light luzFuego;

    [Header("Configuracion de Intensidad")]
    public float intensidadMinima = 1.2f;
    public float intensidadMaxima = 1.8f;
    public float velocidadParpadeo = 8f;

    [Header("Configuracion de Rango (Opcional)")]
    public bool variarRango = true;
    public float rangoMinimo = 3.5f;
    public float rangoMaximo = 4.0f;

    void Start()
    {
        luzFuego = GetComponent<Light>();
    }

    void Update()
    {
        if (luzFuego == null) return;

        // Calculamos un valor suave usando PerlinNoise basado en el tiempo
        float ruido = Mathf.PerlinNoise(Time.time * velocidadParpadeo, 0f);

        // Variamos la intensidad entre el mínimo y el máximo
        luzFuego.intensity = Mathf.Lerp(intensidadMinima, intensidadMaxima, ruido);

        // Variamos el rango de la luz para simular el alcance variable de la llama
        if (variarRango)
        {
            luzFuego.range = Mathf.Lerp(rangoMinimo, rangoMaximo, ruido);
        }
    }
}