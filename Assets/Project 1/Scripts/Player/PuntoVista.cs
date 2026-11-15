using UnityEngine;

/// <summary>
/// Un lugar donde se pone la cámara: la cama, la ventana o la chimenea.
/// Para que se pueda dar clic, este objeto (o un hijo) necesita un Collider.
/// </summary>
public class PuntoVista : MonoBehaviour
{
    public VistaNoche tipo = VistaNoche.Ventana;
    [Tooltip("Cómo aparece en la pista: \"[Clic] Ir a la ...\"")]
    public string nombre = "ventana";
    [Tooltip("Dónde se pone la cámara. Vacío = este mismo objeto")]
    public Transform poseCamara;
    [Tooltip("Cuánto se puede voltear a los lados (grados)")]
    public Vector2 limiteYaw = new Vector2(-20f, 20f);
    [Tooltip("Cuánto se puede voltear arriba / abajo (grados)")]
    public Vector2 limitePitch = new Vector2(-15f, 15f);

    public Transform Pose => poseCamara != null ? poseCamara : transform;

    void OnDrawGizmos()
    {
        var p = Pose;
        Gizmos.color = tipo == VistaNoche.Cama ? Color.cyan : new Color(1f, 0.85f, 0.2f);
        Gizmos.DrawWireSphere(p.position, 0.1f);
        var m = Gizmos.matrix;
        Gizmos.matrix = p.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, 50f, 0.7f, 0.05f, 1.7f);
        Gizmos.matrix = m;
        if (p != transform) Gizmos.DrawLine(p.position, transform.position);
    }
}
