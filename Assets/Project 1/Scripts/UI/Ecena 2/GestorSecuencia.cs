using UnityEngine;

public class GestorSecuencia : MonoBehaviour
{
    public static GestorSecuencia instancia;

    [Tooltip("0 = desayunar, 1 = marionetas, 2 = salir")]
    public int faseActual = 0;

    void Awake()
    {
        instancia = this;
    }

    public bool EsFase(int fase)
    {
        return faseActual == fase;
    }

    public void AvanzarFase()
    {
        faseActual++;
        Debug.Log("Avanzó a la fase: " + faseActual);
    }
}