using UnityEngine;

public class ParpadeoFaroCalle : MonoBehaviour
{
    private Light luzFaro;

    [Header("Configuracion de Intensidad")]
    public float intensidadBase = 2.0f;
    public float intensidadMinima = 0.5f;
    public float intensidadMaxima = 3.0f;

    [Header("Control de Parpadeo")]
    public float velocidadParpadeo = 10f;
    [Range(0f, 1f)]
    public float probabilidadFalla = 0.1f;

    void Start()
    {
        luzFaro = GetComponent<Light>();
        if (luzFaro == null)
        {
            luzFaro = gameObject.AddComponent<Light>();
        }
    }

    void Update()
    {
        if (luzFaro == null) return;

        // Simulamos un parpadeo aleatorio usando ruido y probabilidad
        float ruido = Mathf.PerlinNoise(Time.time * velocidadParpadeo, 0f);

        if (Random.value < probabilidadFalla)
        {
            // Apagón o parpadeo fuerte repentino
            luzFaro.intensity = Random.Range(0f, intensidadMinima);
        }
        else
        {
            // Variación suave de la luz del faro
            luzFaro.intensity = Mathf.Lerp(intensidadMinima, intensidadMaxima, ruido);
        }
    }
}