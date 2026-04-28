# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 (URP 17.3.0) vision therapy application for patients using a Tobii Pro eye tracker. Patients log in by DNI, calibrate their eyes, then play gaze-controlled mini-games. All UI is Canvas-based (uGUI + TextMesh Pro). The New Input System is active alongside the legacy `Input` class.

## Running Tests


```
# From project root (requires Unity on PATH)
Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath . -logFile -
```

Test files are in `Assets/Tests/EditMode/Fase1_NavegacionUI/`. Tests parse script source files directly (with `File.ReadAllLines`) and open scenes additively via `EditorSceneManager` — no Play Mode required.

## Scene Navigation Flow

```
Login → Home → Calibracion (optional, required to unlock Activities)
                         ↓
                    Activities
                    ↙  ↓  ↓  ↘  ↘
  MeteoroZigzag  CometaCuadrado  EstrellaLineal  LaberintoVisual  CarreraOcular  ExplosionGlobos
                    ↘  ↓  ↓  ↙
                      History
```

All scenes must be registered and enabled in **Build Settings**. `ActivitiesManager` wires `SceneManager.LoadScene` calls at runtime in `Start()` — the Inspector has no persistent listeners.

## Core Architecture

### `GestorPaciente` (singleton, DontDestroyOnLoad)
The only cross-scene state carrier. Holds `DatosPaciente pacienteActual` (DNI, name, session timestamp, game history). Persists to `Application.persistentDataPath/pacientes_data.json` via `JsonUtility`. Auto-creates itself if not present (`Instance` getter instantiates a new GameObject). Activities call `GestorPaciente.Instance.GuardarPartida(...)` before navigating to `History`.

### `BaseActividad` (abstract MonoBehaviour)
All activity managers inherit from this. Provides:
- Common UI fields: `botonIniciar`, `botonPausar`, `botonSalir`, `botonReiniciar`, `botonInfo`, `overlayInicio`, `textoPuntuacion`
- Auto-binding in `Start()`: searches by `GameObject.Find("StartButton")` etc. if Inspector refs are null
- Tobii eye-detection gate on the start button (`RutinaValidacionOjosInicio` coroutine). Can be bypassed by setting `usarValidacionOjos = false` in a subclass constructor area
- Standard flow: `IniciarJuego()` → `AlternarPausa()` → `ReiniciarJuego()` / `SalirAlMenu()` → `FinalizarActividad()`
- `FinalizarActividad(...)` saves to `GestorPaciente` then loads `History`

### `TobiiGazeProvider` (singleton, DontDestroyOnLoad)
Wraps Tobii Pro Unity SDK (`Assets/TobiiPro`). Falls back to `Input.mousePosition` when `useMouseFallback = true` or no eye tracker is connected. Activities can use `TobiiGazeProvider.Instance.GazePositionScreen` or access `EyeTracker.Instance.LatestGazeData` directly.

### Auto-binding pattern
Every manager has a `VincularElementosManual()` / `MapearJerarquia()` / `AutoVincular()` method called in `Awake` or `Start`. It uses `GameObject.Find`, `FindObjectsByType<T>(FindObjectsInactive.Include, ...)`, or Canvas hierarchy traversal. **Do not rely on Inspector serialization** — always ensure the expected GameObject name exists in the scene hierarchy.

## Activity Implementations

| Scene | Manager | Input method |
|---|---|---|
| MeteoroZigzag | `ZigzagMovementController` | Mouse / Tobii |
| CometaCuadrado | `SquareMovementController` | Mouse / Tobii |
| EstrellaLineal | `EstrellaLinealManager` | Mouse / Tobii |
| LaberintoVisual | `LaberintoManager` + `GeneradorLaberinto` | Mouse / Tobii gaze cursor |
| CarreraOcular | `CarreraOcularManager` | Single/double click → up/down lanes |
| ExplosionGlobos | `GlobosManager` + `GloboComponente` | Click numbered balloons in order |

`LaberintoManager` drives `GeneradorLaberinto` which procedurally builds a grid maze (BFS solution path). The player cursor follows gaze/mouse and must trace the valid path sequentially (`_indiceValidadoActual`).

## Key Conventions

- **Scoring** calls `FinalizarActividad(gameName, score, level, precision, success, time)` which routes to `History`; activities that skip this (e.g. `CarreraOcularManager`) call `GestorPaciente.Instance.GuardarPartida(...)` directly.
- **Scene names are exact strings** — typos silently cause `MissingSceneException` at runtime. The known unfixed case is `"PlanetaCircular"` referenced in `ActivitiesManager` but not present in Build Settings (documented in `NavigationTests`).
- **`Time.timeScale`** must always be reset to `1` before any `SceneManager.LoadScene` call — `BaseActividad.SalirAlMenu()` and `ReiniciarJuego()` do this; keep that invariant when overriding.
- **Input System duality**: New Input System is used for pointer detection in `BaseActividad.Update()`, but legacy `Input.GetMouseButtonDown` remains in `LoginManager` and `CarreraOcularManager`. Both must work.
