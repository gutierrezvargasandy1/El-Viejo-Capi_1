using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraFNAF : MonoBehaviour
{
    [Header("Configuracion de Movimiento por Mouse")]
    public float velocidadMouse = 4.05f;

    [Header("Limites Horizontales (Eje X)")]
    public float limiteIzquierdo = -4.24f;
    public float limiteDerecho = 3f;

    [Header("Limites Verticales (Eje Y)")]
    public float limiteInferior = -1.5f;
    public float limiteSuperior = 1.5f;

    [Header("Configuracion de Respiracion")]
    public float amplitudRespiracion = 0.03f; 
    public float velocidadRespiracion = 1.5f; 

    private float posYBase;

    void Start()
    {
        // Guardamos la altura inicial de la camara
        posYBase = transform.position.y;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        float pantallaAncho = Screen.width;
        float pantallaAlto = Screen.height;

        float direccionX = 0f;
        float direccionY = 0f;

        // --- 1. Deteccion de bordes Horizontales (Eje X) ---
        if (mousePos.x < pantallaAncho * 0.3f)
        {
            direccionX = -1f;
        }
        else if (mousePos.x > pantallaAncho * 0.7f)
        {
            direccionX = 1f;
        }

        // --- 2. Deteccion de bordes Verticales (Eje Y) ---
        if (mousePos.y < pantallaAlto * 0.3f)
        {
            direccionY = -1f;
        }
        else if (mousePos.y > pantallaAlto * 0.7f)
        {
            direccionY = 1f;
        }

        // --- 3. Calcular nueva posicion X con limites ---
        float nuevaX = transform.position.x + (direccionX * velocidadMouse * Time.deltaTime);
        nuevaX = Mathf.Clamp(nuevaX, limiteIzquierdo, limiteDerecho);

        // --- 4. Calcular nueva posicion Y base con limites ---
        posYBase += direccionY * velocidadMouse * Time.deltaTime;
        posYBase = Mathf.Clamp(posYBase, limiteInferior, limiteSuperior);

        // --- 5. Calcular la respiracion sobre el eje Y ---
        float offsetRespiracion = Mathf.Sin(Time.time * velocidadRespiracion) * amplitudRespiracion;
        float nuevaY = posYBase + offsetRespiracion;

        // --- 6. Aplicar la posicion final ---
        transform.position = new Vector3(nuevaX, nuevaY, transform.position.z);
    }
}