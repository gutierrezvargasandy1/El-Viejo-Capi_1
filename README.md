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

<!-- Ajusta este texto a la historia real del juego -->

Todo empieza con una cinemática: el protagonista despierta en su habitación. Lo que parece una mañana cualquiera —desayunar, juntar las marionetas en la canasta, salir a la calle— se va torciendo poco a poco, mientras el recorrido lo lleva por las calles de **Celaya**, de día y de noche.

**El Viejo Capi** mezcla exploración, objetivos guiados y una ambientación oscura con distintos estilos de cámara: tramos de desplazamiento lateral en 2.5D, exploración en primera persona y momentos de cámara fija con linterna, al estilo de las cámaras de vigilancia.

---

## 📸 Capturas

<!--
Agrega tus capturas en una carpeta docs/screenshots/ y descomenta estas líneas:

<p align="center">
  <img src="docs/screenshots/menu.png" width="45%" />
  <img src="docs/screenshots/habitacion.png" width="45%" />
  <img src="docs/screenshots/celaya-noche.png" width="45%" />
  <img src="docs/screenshots/linterna.png" width="45%" />
</p>
-->

*Próximamente.*

---

## 🎮 Controles

| Acción | Tecla / entrada | Dónde aplica |
|---|---|---|
| Moverse | `W` `A` `S` `D` | Primera persona |
| Correr | `Shift izquierdo` (mantener) | Primera persona |
| Saltar | `Espacio` | Primera persona |
| Mirar | Mouse | Primera persona |
| Moverse a los lados | `A` / `D` o `←` / `→` | Escenas laterales 2.5D |
| Girar cámara y apuntar la linterna | Llevar el mouse hacia los bordes de la pantalla | Escenas de cámara fija |
| Interactuar / recoger / abrir puertas | `E` | Todo el juego |

> Todos los controles usan el **nuevo Input System** de Unity.

---

## ✨ Características

- **Tres estilos de control** según la escena: lateral 2.5D, primera persona y cámara fija con linterna.
- **Progresión por fases** (`GestorSecuencia`): desayunar → reunir las marionetas → salir.
- **Recolección de marionetas**: se depositan en una canasta con contador en pantalla (`Marionetas: X/5`).
- **Sistema de objetivos** activado por zonas y **pistas contextuales** en pantalla (*"Presiona E para recoger"*).
- **Puertas interactivas** con animación suavizada y efectos de sonido.
- **Guardado por puntos de control** en JSON, con banderas para objetivos cumplidos, llaves y puertas abiertas.
- **Menú de pausa global** que se crea solo en cada escena de juego.
- **Transiciones con fundido** entre escenas y **cinemáticas** hechas con Timeline y Cinemachine.
- **Ambientación**: versiones de día y de noche de las escenas, faroles que parpadean, fuego, pasos que cambian de tono al correr y un leve efecto de respiración en la cámara.

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
| uGUI | 2.6.0 |
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

### Compilar

`File` → `Build Profiles` → elige tu plataforma → **Build**. Las escenas ya están agregadas en el orden del juego.

---

## 🗺️ Escenas

| Grupo | Escena | Ruta |
|---|---|---|
| Menús | Logotipo | `Assets/Project 1/Scenes/Menus/LogotipoScene.unity` |
| Menús | Advertencia | `Assets/Project 1/Scenes/Menus/WarningScene.unity` |
| Menús | Menú principal | `Assets/Project 1/Scenes/Menus/MainMenu.unity` |
| Nivel 1 | Escena 1 | `Assets/Project 1/Scenes/Levels/Nivel 1/Esenas 1/Esenas 1.unity` |
| Nivel 1 | Escena 2 (día / noche) | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 2/` |
| Nivel 1 | Calles de Celaya (día / noche) | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 3/` |
| Nivel 1 | Escena 4 | `Assets/Project 1/Scenes/Levels/Nivel 1/Esena 4/Esena 4.unity` |
| Nivel 2 | Habitación | `Assets/Project 1/Scenes/Levels/Nivel 2/HabitacionScene.unity` |

---

## 📁 Estructura del proyecto

```text
Assets/
├── Project 1/                  # Contenido propio del juego
│   ├── Animations/
│   ├── Audio/
│   ├── Materials/
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── Menus/              # Logotipo, advertencia y menú principal
│   │   └── Levels/
│   │       ├── Nivel 1/        # Escenas 1–4 y calles de Celaya
│   │       └── Nivel 2/        # Habitación
│   ├── Scripts/
│   │   ├── Audio/
│   │   ├── Cinematics/
│   │   ├── Core/               # Cambio de escena, transiciones, guardado y menús
│   │   ├── Items/              # Canasta, marionetas, puertas y efectos del escenario
│   │   ├── Player/             # Controladores de movimiento y cámaras
│   │   └── UI/                 # Objetivos, pistas, pausa y secuencia de fases
│   ├── Sprites/
│   └── UI/
├── Resources/                  # Prefab MenuPausa (se carga en tiempo de ejecución)
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
| `GestorTransicion` / `CambioEscena` | Cambio de escena con fundido. |
| `PausaGlobal` | Agrega el menú de pausa a cada escena de juego. |

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
- [ ] Correcciones de menús
- [ ] Capítulos 1 y 2 jugables, con audio

Consulta los [issues](https://github.com/gutierrezvargasandy1/El-Viejo-Capi_1/issues) para ver el detalle de cada tarea.

---

## 👥 Equipo

| Integrante | GitHub |
|---|---|
| Jose Andres Gutierrez Vargas | [@gutierrezvargasandy1](https://github.com/gutierrezvargasandy1) |
| *Nombre de tu compañera* | *@usuario* |

---

## 🙏 Créditos

El proyecto incluye recursos de terceros. Cada uno pertenece a sus autores y conserva su propia licencia:

- **Amplify Color** — corrección de color y LUTs
- **Day-Night Skyboxes** y **SkySeries Freebie** — cielos
- **Old Floor Linoleum FREE** (ALP Assets) — materiales de piso
- **Raw Wooden Furniture Free** — muebles
- **StylArts**, **GeniusCrate Games** y **Free Pack** — modelos y recursos del escenario
- **Unity Technologies** — recursos de ejemplo

---

## 📄 Licencia

Este repositorio no tiene una licencia pública: todos los derechos del contenido original están reservados por sus autores. Los recursos de terceros se rigen por sus propias licencias.

<div align="center">

Hecho con 🖤 en Guanajuato, México .....

</div>
