using UnityEngine;
using TMPro;

public class GestorPistas : MonoBehaviour
{
    public static GestorPistas instancia;

    [Header("Referencia UI")]
    public TMP_Text textoPista;
    public CanvasGroup grupoPista;   // CanvasGroup del Canvas Pistas

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        Ocultar();
    }

    public void MostrarPista(string mensaje)
    {
        textoPista.text = mensaje;
        if (grupoPista != null)
            grupoPista.alpha = 1f;
    }

    public void Ocultar()
    {
        if (grupoPista != null)
            grupoPista.alpha = 0f;
    }
}