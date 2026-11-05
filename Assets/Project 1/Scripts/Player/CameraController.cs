using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("El transform del personaje al que la cámara debe seguir")]
    public Transform target;

    [Header("Configuración de Seguimiento")]
    public Vector3 offset = new Vector3(0f, 2f, -10f);
    public float smoothSpeed = 5.0f;

    [Header("Fundido a Negro (Opcional)")]
    [Tooltip("Marca para usar el eje X. Desmarca para el eje Z.")]
    public bool usarEjeX = true;     // <-- en la calle lateral suele ser X
    public float limiteMin = -20f;
    public float limiteMax = 20f;
    public float fadeDistance = 3.0f;
    public Image fadeImage;

    void LateUpdate()
    {
        if (target == null) return;

        // Seguir al personaje
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Fundido según el eje elegido
        if (fadeImage != null)
        {
            // Toma la posición del eje correcto
            float pos = usarEjeX ? target.position.x : target.position.z;

            float alpha = 0f;

            if (pos < limiteMin + fadeDistance)
            {
                float distance = limiteMin + fadeDistance - pos;
                alpha = Mathf.Clamp01(distance / fadeDistance);
            }
            else if (pos > limiteMax - fadeDistance)
            {
                float distance = pos - (limiteMax - fadeDistance);
                alpha = Mathf.Clamp01(distance / fadeDistance);
            }

            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
}