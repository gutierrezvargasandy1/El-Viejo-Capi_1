using UnityEngine;

public class LamparaSound : MonoBehaviour
{
    public Transform personaje;
    private AudioSource audioSource;
    public float distanciaMinima = 2f;
    public float distanciaMaxima = 10f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (personaje == null || audioSource == null) return;

        float distancia = Vector3.Distance(transform.position, personaje.position);

        if (distancia <= distanciaMinima)
        {
            audioSource.volume = 1f; // Volumen máximo cuando está muy cerca
        }
        else if (distancia >= distanciaMaxima)
        {
            audioSource.volume = 0f; // Silencio cuando está lejos
        }
        else
        {
            // Calcula el volumen de forma gradual entre la distancia mínima y máxima
            float volumen = 1f - ((distancia - distanciaMinima) / (distanciaMaxima - distanciaMinima));
            audioSource.volume = volumen;
        }
    }
}