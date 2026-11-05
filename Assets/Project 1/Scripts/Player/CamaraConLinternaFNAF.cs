using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraConLinternaFNAF : MonoBehaviour
{
    [Header("Configuracion de Rotacion de Camara")]
    public float velocidadMouseCamara = 15f;
    public float limiteIzquierdo = -35f;
    public float limiteDerecho = 35f;
    public float limiteInferior = -15f;
    public float limiteSuperior = 15f;

    [Header("Configuracion de Respiracion")]
    public float amplitudRespiracion = 0.03f;
    public float velocidadRespiracion = 1.5f;

    [Header("Configuracion de la Luz / Linterna 3D")]
    public Transform objetoLuz;
    public float distanciaFrenteLuz = 2f;
    public float velocidadSuavizadoLuz = 10f;
    public float desplazamientoMaximoLuz = 0.5f;

    private float rotacionYaw = 0f;
    private float rotacionPitch = 0f;
    private Vector3 posicionInicialCamara;
    private Camera camBase;

    void Start()
    {
        camBase = GetComponent<Camera>();
        if (camBase == null) camBase = Camera.main;

        posicionInicialCamara = transform.position;
        rotacionYaw = 0f;
        rotacionPitch = 0f;
    }

    void Update()
    {
        if (Mouse.current == null || camBase == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        float pantallaAncho = Screen.width;
        float pantallaAlto = Screen.height;

        float direccionX = 0f;
        float direccionY = 0f;

        if (mousePos.x < pantallaAncho * 0.3f)
        {
            direccionX = -1f;
        }
        else if (mousePos.x > pantallaAncho * 0.7f)
        {
            direccionX = 1f;
        }

        if (mousePos.y < pantallaAlto * 0.3f)
        {
            direccionY = -1f;
        }
        else if (mousePos.y > pantallaAlto * 0.7f)
        {
            direccionY = 1f;
        }

        rotacionYaw += direccionX * velocidadMouseCamara * Time.deltaTime;
        rotacionYaw = Mathf.Clamp(rotacionYaw, limiteIzquierdo, limiteDerecho);

        rotacionPitch -= direccionY * velocidadMouseCamara * Time.deltaTime;
        rotacionPitch = Mathf.Clamp(rotacionPitch, limiteInferior, limiteSuperior);

        float offsetRespiracion = Mathf.Sin(Time.time * velocidadRespiracion) * amplitudRespiracion;
        float nuevaY = posicionInicialCamara.y + offsetRespiracion;

        transform.rotation = Quaternion.Euler(rotacionPitch, rotacionYaw, 0f);
        transform.position = new Vector3(posicionInicialCamara.x, nuevaY, posicionInicialCamara.z);

        if (objetoLuz != null)
        {
            Ray ray = camBase.ScreenPointToRay(mousePos);
            Vector3 posicionObjetivoLuz = ray.GetPoint(distanciaFrenteLuz);

            Vector3 localPos = transform.InverseTransformPoint(posicionObjetivoLuz);
            localPos.x = Mathf.Clamp(localPos.x, -desplazamientoMaximoLuz, desplazamientoMaximoLuz);
            localPos.y = Mathf.Clamp(localPos.y, -desplazamientoMaximoLuz, desplazamientoMaximoLuz);
            localPos.z = distanciaFrenteLuz;

            Vector3 posicionFinalLuz = transform.TransformPoint(localPos);

            objetoLuz.position = Vector3.Lerp(objetoLuz.position, posicionFinalLuz, velocidadSuavizadoLuz * Time.deltaTime);
            objetoLuz.rotation = Quaternion.Slerp(objetoLuz.rotation, transform.rotation, velocidadSuavizadoLuz * Time.deltaTime);
        }
    }
}