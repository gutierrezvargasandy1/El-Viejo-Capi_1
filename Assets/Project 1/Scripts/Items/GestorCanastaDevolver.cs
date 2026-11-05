using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Versión inversa de GestorCanasta:
/// la canasta arranca LLENA y se va vaciando conforme el jugador
/// devuelve cada marioneta a su lugar.
/// </summary>
public class GestorCanastaDevolver : MonoBehaviour
{
    public static GestorCanastaDevolver instancia;

    [Header("Cuántas marionetas trae de regreso")]
    public int totalMarionetas = 5;

    [Header("UI - Canvas del contador")]
    public GameObject canvasContador;
    public TMP_Text textoContador;

    [Tooltip("Si está marcado, el contador se ve desde que arranca la escena")]
    public bool mostrarContadorAlInicio = true;

    [Tooltip("Marcado: muestra las que faltan (5,4,3...). Sin marcar: muestra las guardadas (0/5,1/5...)")]
    public bool contarHaciaAtras = true;

    [Header("Marionetas que vienen DENTRO de la canasta")]
    [Tooltip("Arrastra aquí las marionetas/cubos que están DENTRO de la canasta")]
    public GameObject[] marionetasEnCanasta;

    [Header("Qué pasa al devolverlas todas")]
    public UnityEvent alCompletar;

    private int devueltas = 0;

    public bool Completado { get; private set; } = false;
    public int Restantes { get { return totalMarionetas - devueltas; } }
    public bool QuedanMarionetas { get { return Restantes > 0; } }

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        // La canasta empieza LLENA (al revés que en la escena de recolectar)
        for (int i = 0; i < marionetasEnCanasta.Length; i++)
        {
            if (marionetasEnCanasta[i] != null)
                marionetasEnCanasta[i].SetActive(i < totalMarionetas);
        }

        if (canvasContador != null)
            canvasContador.SetActive(mostrarContadorAlInicio);

        ActualizarTexto();
    }

    public void MostrarContador()
    {
        if (canvasContador != null)
            canvasContador.SetActive(true);

        ActualizarTexto();
    }

    public void OcultarContador()
    {
        if (canvasContador != null)
            canvasContador.SetActive(false);
    }

    /// <summary>
    /// Saca una marioneta de la canasta. Devuelve false si ya no trae ninguna.
    /// </summary>
    public bool Depositar()
    {
        if (Completado || !QuedanMarionetas)
            return false;

        // Apaga la última marioneta visible dentro de la canasta
        int indice = Restantes - 1;
        if (indice >= 0 && indice < marionetasEnCanasta.Length && marionetasEnCanasta[indice] != null)
            marionetasEnCanasta[indice].SetActive(false);

        devueltas++;
        ActualizarTexto();

        if (devueltas >= totalMarionetas)
        {
            Completado = true;

            // Avanza a la siguiente fase de la secuencia
            if (GestorSecuencia.instancia != null)
                GestorSecuencia.instancia.AvanzarFase();

            alCompletar.Invoke();
        }

        return true;
    }

    void ActualizarTexto()
    {
        if (textoContador == null) return;

        if (contarHaciaAtras)
            textoContador.text = "Marionetas por guardar: " + Restantes;
        else
            textoContador.text = "Marionetas guardadas: " + devueltas + "/" + totalMarionetas;
    }
}