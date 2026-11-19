<div align="center">

# 🕯️ El Viejo Capi

**Videojuego de terror en 2.5D y primera persona, hecho en Unity 6**

![Unity](https://img.shields.io/badge/Unity-6000.6.0f1-000000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![URP](https://img.shields.io/badge/URP-17.6-4B5563?style=for-the-badge)
![Git LFS](https://img.shields.io/badge/Git_LFS-requerido-F05032?style=for-the-badge&logo=git&logoColor=white)
![Estado](https://img.shields.io/badge/estado-en_desarrollo-orange?style=for-the-badge)

</div>

---

## 📖 Sinopsis

Inspirado en la leyenda del **Capi de Celaya**.

Todo empieza con una cinemática: el Capi despierta en su habitación. Lo que parece un día cualquiera —desayunar, juntar sus marionetas en la canasta, salir a las calles de **Celaya**— se va torciendo poco a poco. Al caer la noche regresa a casa, deja cada marioneta en su lugar y se va a dormir… pero algo intenta entrar.

Tiene que **sobrevivir hasta las 6 de la mañana** cuidando la ventana, la puerta y la chimenea. Y cuando por fin amanece, el final no es el que espera.

**El Viejo Capi** mezcla exploración, objetivos guiados y una ambientación oscura con distintos estilos de cámara: tramos de desplazamiento lateral en 2.5D, exploración en primera persona, momentos de cámara fija con linterna y una noche de supervivencia al estilo *Five Nights at Freddy's*.

---

## 📚 Capítulos

Cada capítulo empieza con su título en pantalla.

| # | Título | Empieza en | Qué pasa |
|---|---|---|---|
| 1 | **Un día normal** | `Esenas 1` | El Capi despierta, desayuna, junta sus marionetas y recorre Celaya de día y de noche. |
| 2 | **Cada una en su lugar** | `Esena 2 Noche` | Regresa a casa de noche, guarda las marionetas, cena y se va a dormir. |
| 3 | **Sobrevive** | `HabitacionScene` | La noche: algo intenta entrar. Hay que aguantar hasta las 6 AM. |
| 4 | **El final** | `FinalJuegoScene` | Cinemática final (mal final). |

---

## 📸 Capturas

<!--
Agrega tus capturas en una carpeta docs/screenshots/ y descomenta estas líneas:

<p align="center">
  <img src="docs/screenshots/menu.png" width="45%" />
  <img src="docs/screenshots/habitacion.png" width="45%" />
  <img src="docs/screenshots/celaya-noche.png" width="45%" />
  <img src="docs/screenshots/noche-fnaf.png" width="45%" />
</p>
-->

*Próximamente.*

---

## 🎮 Controles

### Exploración (capítulos 1 y 2)

| Acción | Tecla / entrada | Dónde aplica |
|---|---|---|
| Moverse | `W` `A` `S` `D` | Primera persona |
| Correr | `Shift izquierdo` (mantener) | Primera persona |
| Saltar | `Espacio` | Primera persona |
| Mirar | Mouse | Primera persona |
| Moverse a los lados | `A` / `D` o `←` / `→` | Escenas laterales 2.5D |
| Girar cámara y apuntar la linterna | Llevar el mouse hacia los bordes de la pantalla | Escenas de cámara fija |
| Interactuar / recoger / abrir puertas | `E` | Todo el juego |

### La noche (capítulo 3)

| Acción | Tecla / entrada | Dónde |
|---|---|---|
| Mirar alrededor | Mouse | Siempre |
| Ir a la ventana o a la chimenea | Apuntar y `Clic` | Desde la cama |
| Volver a la cama | `S` o `Clic derecho` | Ventana / chimenea |
| Prender o apagar la linterna (para la puerta) | `F` | Desde la cama |
| Cerrar las cortinas | Mantener `Espacio` | Ventana |
| Encender o apagar el fuego | `Espacio` | Chimenea |

### En todo el juego

| Acción | Tecla / entrada |
|---|---|
| Pausa | `Esc` |
| Saltar el título de capítulo | `Clic` / `Espacio` |
| Game Over: intentar otra vez | `Clic` / `Espacio` (en Android: tocar la pantalla) |
| Game Over: menú principal | `Esc` (en Android: botón Atrás) |
| Pantalla de FIN: volver al menú | `Clic` / `Espacio` |

> Todos los controles usan el **nuevo Input System** de Unity.

---

## 🌙 Cómo se juega la noche

- El reloj avanza de las **12 AM a las 6 AM**. Si llegas a las 6, sobrevives.
- El monstruo intenta entrar por tres lados, y cada uno se defiende distinto:
  - **Ventana** → cierra las cortinas (mantén `Espacio`).
  - **Puerta** → alúmbrala con la linterna. La pila se acaba.
  - **Chimenea** → enciende el fuego. La leña se acaba.
- Cada entrada tiene su propio sonido, así sabes por dónde viene. Si lo defiendes a tiempo, se va; si no, entra: **jumpscare** y **Game Over**.
- Cada hora es más difícil: el monstruo entra más rápido y, desde las 2 AM, **pueden llegar dos a la vez**. Desde las 4 AM, **hasta tres**.
- Señales de peligro: el latido y la tensión suben, la imagen se llena de **estática** cuando está a punto de entrar y el Capi **respira agitado** cuando lo tiene enfrente.

---

## ✨ Características

**Exploración**
- **Tres estilos de control** según la escena: lateral 2.5D, primera persona y cámara fija con linterna.
- **Progresión por fases** (`GestorSecuencia`): desayunar → reunir las marionetas → salir.
- **Recolección de marionetas**: se depositan en una canasta con contador en pantalla (`Marionetas: X/5`).
- **Sistema de objetivos** activado por zonas y **pistas contextuales** en pantalla (*"Presiona E para recoger"*).
- **Puertas interactivas** con animación suavizada y efectos de sonido.
- **Guardado por puntos de control** en JSON, con banderas para objetivos cumplidos, llaves y puertas abiertas.

**Presentación**
- **Pantalla de carga** entre escenas, con barra de progreso, fondos ilustrados y consejos o fragmentos de la leyenda del Capi. El sonido del juego se desvanece en cada cambio de escena.
- **Títulos de capítulo** al empezar cada capítulo: las letras aparecen una por una con un golpe de piano. Mientras están en pantalla, la escena espera.
- **Menú de pausa global** que se crea solo en cada escena de juego.
- **Cinemáticas** hechas con Timeline y Cinemachine.

**La noche (capítulo 3)**
- **Noche de supervivencia** estilo FNAF: tres entradas, linterna con pila, leña limitada y dificultad que sube cada hora.
- **El monstruo**: modelo 3D sin huesos animado por código. Aparece con un *fade*, se asoma poco a poco y tiembla. En la chimenea solo se asoma la cabeza, de cabeza.
- **Efectos dinámicos** (estilo *The Joy of Creation*):
  - **Imagen:** post-procesado que cambia con el peligro (viñeta, aberración cromática, grano y distorsión).
  - **Cámara:** temblor de miedo y visión de túnel cuando el Capi tiene al monstruo enfrente.
  - **Ambiente:** relámpagos que dejan ver la silueta del monstruo en la ventana, partículas (polvo, hollín, chispas) y sustos ambientales (pasos en el techo, golpes, rechinidos).
- **Jumpscare** y pantalla de **Game Over** con imagen propia y música.

**El final (capítulo 4)**
- **Cinemática final** en video, con sonido hecho a la medida y efectos encima: la imagen tiembla con cada golpe y late con el corazón del Capi.
- **Pantalla de FIN** (mal final) y regreso al menú principal.

**Ambientación**
- Versiones de día y de noche de las escenas, faroles que parpadean, fuego, pasos que cambian de tono al correr y un leve efecto de respiración en la cámara.
- **Efectos de sonido y música originales**: la noche, el Game Over, los capítulos y el final tienen sonidos hechos por síntesis para este juego.

---

## 🛠️ Tecnologías

| Herramienta | Versión |
|---|---|
| Unity | **6000.6.0f1** (Unity 6) |
| Universal Render Pipeline (URP) | 17.6.0 |
| Input System | 1.20.0 |
| Cinemachine | 6.6.0 |
| Timeline | 6.6.0 |
| AI Navigation | 2.0.14 |
| uGUI + TextMeshPro | 2.6.0 |
| Video | Unity Video Player (H.264) |
| Lenguaje | C# |
| Control de versiones | Git + Git LFS |

---

## 🚀 Cómo abrir el proyecto

### Requisitos

- [Unity Hub](https://unity.com/download) con **Unity 6000.6.0f1** instalado
- [Git](https://git-scm.com/)
- [Git LFS](https://git-lfs.com/) — **obligatorio**: los modelos, texturas, audio y video se guardan con LFS. Sin él, el proyecto se abre sin esos archivos.

### Pasos

```bash
# 1. Activa Git LFS (solo la primera vez en tu equipo)
git lfs install

# 2. Clona el repositorio
git clone https://github.com/gutierrezvargasandy1/El-Viejo-Capi_1.git
cd El-Viejo-Capi_1

# 3. Descarga los archivos pesados
git lfs pull
```

4. Abre **Unity Hub** → **Add** → **Add project from disk** y selecciona la carpeta `El-Viejo-Capi_1`.
5. Ábrelo con **Unity 6000.6.0f1**. La primera importación tarda unos minutos.
6. Abre la escena `Assets/Project 1/Scenes/Menus/LogotipoScene.unity` y presiona **Play**.

> Para probar directo una parte: abre `HabitacionScene` (la noche) o `FinalJuegoScene` (el final) y presiona **Play**.

### Compilar

`File` → `Build Profiles` → elige tu plataforma → **Build**. Las escenas del juego ya están agregadas. La escena final la agrega la herramienta **Capítulos y final** (ver abajo).

---

## 🗺️ Escenas

En el orden en que se juegan:

| Capítulo | Escena | Ruta |
|---|---|---|
| Menús | Logotipo | `Assets/Project 1/Scenes/Menus/LogotipoScene.unity` |
| Menús | Advertencia | `Assets/Project 1/Scenes/Menus/WarningScene.unity` |
| Menús | Menú principal | `Assets/Project 1/Scenes/Menus/MainMenu.unity` |
| 1 | Escena 1 (despertar) | `Assets/Project 1/Scenes/Levels/Nivel 1/Esenas 1/Esenas 1.unity` |
| 1 | Escena 2 (día) | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 2/Esena 2.unity` |
| 1 | Calles de Celaya (día) | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 3/Caminata en las Calles de Celaya Dia.unity` |
| 1 | Escena 4 | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 4/Esena 4.unity` |
| 1 | Calles de Celaya (noche) | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 3/Caminata en las Calles de Celaya Noche.unity` |
| 2 | Escena 2 (noche) | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 2/Esena 2 Noche.unity` |
| 2 | Cinemática: el Capi se duerme | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 2/Cinematica Dormir Capi.unity` |
| 3 | La noche (habitación) | `Assets/Project 1/Scenes/Levels/Nivel 2/HabitacionScene.unity` |
| 4 | El final | `Assets/Project 1/Scenes/Levels/Nivel 2/FinalJuegoScene.unity` |

---

## 📁 Estructura del proyecto

```text
Assets/
├── Editor/                     # Herramientas del editor (noche, pantalla de carga, capítulos)
├── Project 1/                  # Contenido propio del juego
│   ├── Animations/
│   ├── Audio/
│   │   └── SFX/                # Sonidos y música (noche, Game Over, capítulos, final)
│   ├── FondosCarga/            # Fondos de la pantalla de carga
│   ├── Materials/
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── Menus/              # Logotipo, advertencia y menú principal
│   │   └── Levels/
│   │       ├── Nivel 1/        # Escenas 1–4 y calles de Celaya
│   │       └── Nivel 2/        # La noche (habitación) y el final
│   ├── Scripts/
│   │   ├── Audio/
│   │   ├── Cinematics/
│   │   ├── Core/               # Cambio de escena, pantalla de carga, capítulos, final, guardado y menús
│   │   ├── Items/              # Canasta, marionetas, puertas y efectos del escenario
│   │   ├── Player/             # Controladores, cámaras y todo el sistema de la noche
│   │   └── UI/                 # Objetivos, pistas, pausa y secuencia de fases
│   ├── Sprites/
│   ├── UI/                     # Incluye la imagen del Game Over
│   └── Video/                  # Cinemática final
├── Prefabs/
│   └── Noche/                  # Monstruo final y sus materiales (los crea la herramienta de la noche)
├── Resources/                  # Prefab MenuPausa y configuración de la pantalla de carga
├── Settings/                   # Configuración de URP
├── InputSystem_Actions.inputactions
└── ...                         # Paquetes de terceros (ver Créditos)
Packages/                       # Dependencias de Unity
ProjectSettings/                # Configuración del proyecto
```

### Scripts principales

| Script | Función |
|---|---|
| `ControladorPrimeraPersona` | Movimiento en primera persona: caminar, correr, saltar, mirar con el mouse y sonido de pasos. |
| `ControladorLateral` | Movimiento lateral 2.5D sobre el eje X o Z, con giro hacia la dirección de avance. |
| `CamaraConLinternaFNAF` | Cámara fija que gira según la posición del cursor, con linterna que sigue al puntero. |
| `GestorSecuencia` | Controla las fases de la escena (desayunar → marionetas → salir). |
| `GestorCanasta` / `MarionetaRecolectable` | Recolección de marionetas y contador en pantalla. |
| `GestorObjetivos` / `ZonaObjetivo` | Objetivos que se activan al entrar en zonas. |
| `GestorPistas` / `ZonaInteraccion` | Mensajes de interacción en pantalla. |
| `Oportera` | Puertas interactivas con animación y sonido. |
| `DatosGuardado` / `PuntoDeGuardado` | Guardado y carga de la partida en JSON. |
| `GestorTransicion` / `ConfigPantallaCarga` | Cambio de escena con pantalla de carga: barra, fondos, consejos e historia, y fundido del sonido. |
| `CambioEscena` | Dispara el cambio de escena. |
| `PausaGlobal` | Agrega el menú de pausa a cada escena de juego. |
| `TituloCapitulo` | Título de capítulo al empezar la escena (no se repite al reintentar). |
| `GestorNoche` | La noche: reloj de 12 a 6 AM, dificultad por hora, ataques (uno o varios a la vez), jumpscare, Game Over y victoria. |
| `ControlJugadorNoche` / `PuntoVista` | Mirar y moverse entre la cama, la ventana y la chimenea. |
| `EntradaMonstruo` | Cada entrada (ventana, puerta, chimenea): ataque, defensa y sonido propio. |
| `MonstruoVisual` | Animación del monstruo por código: aparece con *fade*, se asoma y tiembla. |
| `LinternaNoche` / `CortinasNoche` / `FuegoChimenea` | Las tres defensas: linterna con pila, cortinas y fuego con leña. |
| `EfectosNoche` | Efectos dinámicos: post-procesado, temblor de cámara, latido, respiración, estática, relámpagos, partículas y sustos. |
| `UINoche` | HUD de la noche (hora, pila, leña, pistas) y pantalla de Game Over. |
| `CinematicaFinal` | Reproduce la cinemática final con su sonido sincronizado y efectos, y muestra la pantalla de FIN. |

### Herramientas del editor

Todas están en el menú **Herramientas** de Unity:

| Menú | Para qué sirve |
|---|---|
| **Pantalla de Carga** | Crea la configuración, carga los fondos e instala la pantalla de carga en las escenas. |
| **Noche (Sobrevive hasta las 6 AM)** | Arma la noche: usa tu propio monstruo, agrega los efectos dinámicos, asigna los sonidos, aplica la animación de asomarse y deja solo la cabeza en la chimenea. |
| **Capítulos y final** | Pone el título de cada capítulo en su escena y arma la escena final (botón **HACER TODO**). |
| **Voltear normales del seleccionado** | Hace que la luz ilumine las cortinas por los dos lados. |

### Atajos de prueba (solo en el editor, durante la noche)

| Tecla | Qué hace |
|---|---|
| `F1` / `F2` / `F3` | El monstruo ataca la ventana / la puerta / la chimenea |
| `F4` | Adelanta el reloj a las 5:50 AM |
| `F5` / `F6` | Llegan dos / tres monstruos a la vez |

---

## 💾 Partidas guardadas

La partida se guarda en `partida.json`, dentro de la carpeta de datos persistentes de Unity:

| Sistema | Ruta |
|---|---|
| Windows | `%USERPROFILE%\AppData\LocalLow\DefaultCompany\El Viejo Capi V2\` |
| macOS | `~/Library/Application Support/DefaultCompany/El Viejo Capi V2/` |
| Linux | `~/.config/unity3d/DefaultCompany/El Viejo Capi V2/` |

Borra ese archivo para empezar desde cero.

---

## 📌 Avance

- [x] Creación del proyecto inicial
- [x] Escena 1 terminada
- [x] Sistema de guardado
- [x] Menú de pausa
- [x] Pantalla de carga con consejos e historia
- [x] Títulos de capítulo (1 al 4)
- [x] Capítulo 3: la noche estilo FNAF
- [x] Monstruo final integrado
- [x] Efectos dinámicos, jumpscare y Game Over
- [x] Capítulo 4: cinemática final y pantalla de FIN
- [ ] Correcciones de menús
- [ ] Capítulos 1 y 2 jugables, con audio

Consulta los [issues](https://github.com/gutierrezvargasandy1/El-Viejo-Capi_1/issues) para ver el detalle de cada tarea.

---

## 👥 Equipo

| Integrante | GitHub |
|---|---|
| Jose Andres Gutierrez Vargas | [@gutierrezvargasandy1](https://github.com/gutierrezvargasandy1) |
| Ana Barrientos | *@danonino25 |

---

## 🙏 Créditos

El proyecto incluye recursos de terceros. Cada uno pertenece a sus autores y conserva su propia licencia:

- **Amplify Color** — corrección de color y LUTs
- **Day-Night Skyboxes** y **SkySeries Freebie** — cielos
- **Old Floor Linoleum FREE** (ALP Assets) — materiales de piso
- **Raw Wooden Furniture Free** — muebles
- **StylArts**, **GeniusCrate Games** y **Free Pack** — modelos y recursos del escenario
- **Unity Technologies** — recursos de ejemplo
- **Meshy** — modelo 3D del monstruo (generado con IA)
- **Gemini** — ilustraciones de la pantalla de carga (generadas con IA)

Los efectos de sonido y la música de la noche, el Game Over, los capítulos y el final son originales de este proyecto.

---

## 📄 Licencia

Este repositorio no tiene una licencia pública: todos los derechos del contenido original están reservados por sus autores. Los recursos de terceros se rigen por sus propias licencias.

<div align="center">

Hecho con 🖤 en Guanajuato, México .

</div>