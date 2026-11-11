using UnityEngine;
using TMPro;

/// <summary>
/// Configuración de la pantalla de carga: consejos, historia del Capi,
/// tiempos, audio y colores. Se edita en UN solo lugar y la usan todas las escenas.
/// Créala con Herramientas > Pantalla de Carga > Crear configuración.
/// </summary>
[CreateAssetMenu(fileName = "ConfigPantallaCarga", menuName = "El Viejo Capi/Config Pantalla de Carga")]
public class ConfigPantallaCarga : ScriptableObject
{
    [System.Serializable]
    public class Texto
    {
        [Tooltip("Marcado = pedazo de la historia/leyenda. Sin marcar = consejo de juego")]
        public bool esHistoria;
        [TextArea(2, 4)] public string texto;

        public Texto(bool esHistoria, string texto)
        {
            this.esHistoria = esHistoria;
            this.texto = texto;
        }
    }

    [Header("Tiempos (en segundos; no les afecta la pausa)")]
    [Tooltip("Lo que tarda en oscurecer / aclarar la pantalla (y en bajar / subir el volumen)")]
    public float duracionFade = 1f;
    [Tooltip("Segundos en negro ANTES de que aparezcan la imagen y el consejo, y DESPUÉS de que desaparecen (antes de ver la escena nueva)")]
    public float esperaEnNegro = 1f;
    [Tooltip("Lo que tardan la imagen, el consejo y la barra en aparecer / desaparecer sobre el negro")]
    public float duracionFadeContenido = 0.6f;
    [Tooltip("Tiempo mínimo que se ve la pantalla de carga antes de cambiar de escena (antes 'Tiempo En Negro')")]
    public float tiempoMinimo = 4f;
    [Tooltip("Cada cuánto cambia el consejo si la carga dura mucho")]
    public float cambiarTextoCada = 6f;
    [Tooltip("Cuánto se queda la barra en 100 % antes de aclarar")]
    public float esperaAlLlegar100 = 0.4f;

    [Header("Audio")]
    [Tooltip("Baja TODO el volumen del juego al salir y lo sube al entrar a la escena nueva")]
    public bool bajarVolumen = true;
    [Tooltip("Opcional: sonido en loop mientras carga (ej. Drone_Terror). Suena aunque el volumen general esté en 0")]
    public AudioClip musicaCarga;
    [Range(0f, 1f)] public float volumenMusicaCarga = 0.4f;

    [Header("Al entrar a la escena nueva")]
    [Tooltip("Quita la pausa (Time.timeScale = 1 y audio sin pausa). Útil al salir al menú desde el menú de pausa")]
    public bool reanudarTiempo = true;

    [Header("Aspecto")]
    [Tooltip("Imágenes de fondo: en cada carga sale una al azar (sin repetir la anterior). Vacío = solo color")]
    public Sprite[] imagenesFondo;
    [Tooltip("Acerca la imagen poco a poco mientras carga")]
    public bool zoomLento = true;
    [Range(1f, 1.2f)] public float zoomMaximo = 1.08f;
    public Color colorFondo = Color.black;
    [Tooltip("Opcional: fuente TextMeshPro. Si está vacío, usa la de TMP por defecto")]
    public TMP_FontAsset fuente;
    public Color colorTexto = new Color(0.86f, 0.82f, 0.74f);
    public Color colorEtiqueta = new Color(0.72f, 0.13f, 0.10f);
    public Color colorBarraFondo = new Color(0.10f, 0.08f, 0.08f);
    public Color colorBarra = new Color(0.62f, 0.09f, 0.07f);
    public string etiquetaConsejo = "CONSEJO";
    public string etiquetaHistoria = "LA LEYENDA DEL CAPI";
    public string textoCargando = "Cargando";

    [Header("Consejos e historia (salen al azar, sin repetir el anterior)")]
    public Texto[] textos = new Texto[]
    {
        // ---- Consejos de juego ----
        new Texto(false, "Junta todas las marionetas antes de salir de la casa. El Capi nunca da una función incompleta."),
        new Texto(false, "Cuando veas \"Presiona E\", puedes interactuar con ese objeto."),
        new Texto(false, "Si no sabes qué hacer, revisa el objetivo en pantalla."),
        new Texto(false, "Juega con audífonos: muchos sustos se escuchan antes de verse."),
        new Texto(false, "Puedes saltar las cinemáticas con Espacio o Enter."),
        new Texto(false, "Un buen desayuno es el inicio de un buen show."),

        // ---- Pedazos de la leyenda ----
        new Texto(true, "Dicen en Celaya que el Viejo Capi llegó con una maleta llena de marionetas. Nadie lo vio hacerlas, y nadie lo vio comprarlas."),
        new Texto(true, "Los cartoneros de Celaya juran que una de sus marionetas no es de cartón, y que pesa como alguien dormido."),
        new Texto(true, "Cada función del Capi termina con aplausos. Lo que nadie cuenta es quién sigue aplaudiendo cuando la plaza ya está vacía."),
        new Texto(true, "El Capi cierra la maleta con tres cerrojos. No es para que nadie se las robe; es para que no salgan."),
        new Texto(true, "Los vecinos dicen que de noche se oyen pasos de madera en la casa del Capi, aunque él esté dormido."),
        new Texto(true, "El Capi les habla a sus marionetas durante la cena. Eso no es lo raro. Lo raro es cuando una le contesta."),
        new Texto(true, "La puerta del cuarto del Capi tiene arañazos por fuera. Él dice que es el gato. El Capi no tiene gato."),
        new Texto(true, "Nunca dejes una marioneta mirando hacia tu cama."),
    };
}