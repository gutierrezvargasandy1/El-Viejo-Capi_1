using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ZonaInteraccion : MonoBehaviour
{
    [Header("Texto de la pista")]
    public string mensaje = "Presiona E para abrir";

    [Header("Tecla de interacción")]
    public KeyCode tecla = KeyCode.E;

    [Header("Qué pasa al pulsar E")]
    public UnityEvent alInteractuar;   // aquí arrastras la acción (abrir puerta, etc.)

    private bool jugadorCerca = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (GestorPistas.instancia != null)
                GestorPistas.instancia.MostrarPista(mensaje);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (GestorPistas.instancia != null)
                GestorPistas.instancia.Ocultar();
        }
    }

    void Update()
    {
        if (MenuPausa.EstaPausado) return;   // no interactuar con el juego en pausa

        if (jugadorCerca && TeclaPresionada(tecla))
        {
            alInteractuar?.Invoke();   // ejecuta la acción asignada
        }
    }

    // Funciona con el Input System nuevo (y con el viejo si algún día lo cambias)
    static bool TeclaPresionada(KeyCode k)
    {
#if ENABLE_INPUT_SYSTEM
        // Botones del mouse
        if (k == KeyCode.Mouse0) return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (k == KeyCode.Mouse1) return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

        var teclado = Keyboard.current;
        if (teclado == null) return false;

        // Convertir el nombre de KeyCode al de Input System (E -> E, Alpha1 -> Digit1, Return -> Enter...)
        string nombre = k.ToString();
        if (nombre.StartsWith("Alpha")) nombre = "Digit" + nombre.Substring(5);
        else if (nombre.StartsWith("Keypad") && nombre.Length == 7 && char.IsDigit(nombre[6])) nombre = "Numpad" + nombre[6];
        else if (k == KeyCode.Return) nombre = "Enter";

        if (System.Enum.TryParse(nombre, out Key key) && key != Key.None)
            return teclado[key].wasPressedThisFrame;
        return false;
#else
        return Input.GetKeyDown(k);
#endif
    }
}