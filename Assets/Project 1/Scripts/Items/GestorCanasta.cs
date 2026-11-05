using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class GestorCanasta : MonoBehaviour
{
    public static GestorCanasta instancia;

    [Header("Cuántas marionetas hay que juntar")]
    public int totalMarionetas = 5;

    [Header("UI - Canvas del contador (se oculta al inicio)")]
    public GameObject canvasContador;
    public TMP_Text textoContador;

    [Tooltip("Marcado: el contador se esconde solo al juntar todas")]
    public bool ocultarAlCompletar = true;

    [Header("Marionetas que aparecen en la canasta")]
    [Tooltip("Arrastra aquí las marionetas/cubos que están DENTRO de la canasta")]
    public GameObject[] marionetasEnCanasta;

    [Header("Qué pasa al juntarlas todas")]
    public UnityEvent alCompletar;

    private int recolectadas = 0;
    public bool Completado { get; private set; } = false;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        if (canvasContador != null)
            canvasContador.SetActive(false);

        foreach (GameObject m in marionetasEnCanasta)
        {
            if (m != null)
                m.SetActive(false);
        }

        ActualizarTexto();
    }

    public void MostrarContador()
    {
        if (canvasContador != null)
            canvasContador.SetActive(true);

        ActualizarTexto();
    }

    /// <summary>
    /// Esconde el contador. Se puede llamar desde cualquier UnityEvent.
    /// </summary>
    public void OcultarContador()
    {
        if (canvasContador != null)
            canvasContador.SetActive(false);
    }

    /// <summary>
    /// Lo prende o lo apaga según como esté.
    /// </summary>
    public void AlternarContador()
    {
        if (canvasContador == null) return;

        if (canvasContador.activeSelf)
            OcultarContador();
        else
            MostrarContador();
    }

    public void Recolectar()
    {
        if (recolectadas < marionetasEnCanasta.Length && marionetasEnCanasta[recolectadas] != null)
            marionetasEnCanasta[recolectadas].SetActive(true);

        recolectadas++;
        ActualizarTexto();

        if (recolectadas >= totalMarionetas)
        {
            Completado = true;

            if (ocultarAlCompletar)
                OcultarContador();

            // Avanza a fase 2 (salir)
            if (GestorSecuencia.instancia != null)
                GestorSecuencia.instancia.AvanzarFase();

            alCompletar.Invoke();
        }
    }

    void ActualizarTexto()
    {
        if (textoContador != null)
            textoContador.text = "Marionetas: " + recolectadas + "/" + totalMarionetas;
    }
}