using System.Collections;
using UnityEngine;

/// <summary>
/// Va en la raíz del monstruo (el provisional o tu modelo final).
/// Hace que aparezca, se acerque, se balancee, se retire y desaparezca.
/// Para cambiar al modelo final: pon tu modelo como hijo de este objeto
/// (con los pies en 0,0,0 y mirando hacia Z azul) y borra las piezas del provisional.
/// </summary>
public class MonstruoVisual : MonoBehaviour
{
    [Tooltip("La cabeza: en el jumpscare se pone justo frente a la cámara")]
    public Transform cabeza;
    [Tooltip("Luces de los ojos (opcional)")]
    public Light[] lucesOjos;
    public float intensidadOjos = 1.2f;
    [Tooltip("Cuánto se balancea mientras intenta entrar")]
    public float balanceo = 0.04f;
    [Tooltip("Parte del tiempo de ataque que tarda en llegar a su lugar (0.35 = llega rápido y luego se queda insistiendo)")]
    [Range(0.1f, 1f)] public float llegada = 0.35f;
    [Tooltip("Opcional: Animator de tu modelo final. Se le manda el parámetro float \"Progreso\"")]
    public Animator animator;

    Vector3 desde;
    Coroutine retirada;
    float semilla;
    static readonly int ParamProgreso = Animator.StringToHash("Progreso");

    public void Aparecer(Vector3 desplazamiento)
    {
        DetenerRetirada();
        desde = desplazamiento;
        semilla = Random.value * 10f;
        transform.localPosition = desplazamiento;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive(true);
    }

    /// <param name="progreso">0 = recién llegó, 1 = está por entrar</param>
    /// <param name="empujado">0..1 cuánto lo estás ahuyentando</param>
    public void Acercar(float progreso, float empujado)
    {
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progreso / Mathf.Max(0.01f, llegada)));
        Vector3 pos = Vector3.Lerp(desde, Vector3.zero, k);
        if (desde.sqrMagnitude > 0.0001f) pos += desde.normalized * (0.35f * empujado);   // lo empujas hacia atrás

        float t = Time.time + semilla;
        pos += new Vector3(Mathf.Sin(t * 1.7f), Mathf.Sin(t * 2.3f) * 0.5f, 0f) * balanceo * (0.5f + progreso);
        transform.localPosition = pos;
        transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.9f) * 6f, Mathf.Sin(t * 1.3f) * 5f * progreso);

        if (lucesOjos != null)
            foreach (var l in lucesOjos)
                if (l != null) l.intensity = intensidadOjos * (0.6f + 0.6f * progreso) * (0.85f + 0.3f * Mathf.PerlinNoise(t * 8f, 0f));

        if (animator != null) animator.SetFloat(ParamProgreso, progreso);
    }

    public void Retirarse(Vector3 hacia)
    {
        if (!gameObject.activeInHierarchy) return;
        DetenerRetirada();
        retirada = StartCoroutine(RutinaRetirada(hacia));
    }

    IEnumerator RutinaRetirada(Vector3 hacia)
    {
        Vector3 a = transform.localPosition;
        Vector3 b = hacia * 1.2f;
        float t = 0f;
        const float d = 0.7f;
        while (t < d)
        {
            t += Time.deltaTime;
            float e = t / d;
            transform.localPosition = Vector3.Lerp(a, b, e * e);
            yield return null;
        }
        retirada = null;
        gameObject.SetActive(false);
    }

    public void Ocultar()
    {
        DetenerRetirada();
        gameObject.SetActive(false);
    }

    /// <summary>Lo usa el jumpscare: ojos al máximo y sin animación de entrada.</summary>
    public void ModoJumpscare()
    {
        DetenerRetirada();
        if (lucesOjos != null)
            foreach (var l in lucesOjos)
                if (l != null) l.intensity = intensidadOjos * 2.5f;
        if (animator != null) animator.SetFloat(ParamProgreso, 1f);
    }

    void DetenerRetirada()
    {
        if (retirada != null)
        {
            StopCoroutine(retirada);
            retirada = null;
        }
    }
}
