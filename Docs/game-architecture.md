# game-architecture.md — ExplosionGlobos

> Este archivo define la arquitectura completa del minijuego ExplosionGlobos.
> Claude Code debe leerlo completo antes de escribir cualquier script.

## State Machine

```csharp
public enum GameState { Inicio, Countdown, Playing, Results }
```

Solo un estado activo a la vez. Nada se ejecuta fuera de su estado.

## Scripts y responsabilidades

| Script | Responsabilidad única |
|---|---|
| `GameManager.cs` | State machine, coordinación, eventos globales |
| `BalloonSpawner.cs` | Generación con grid+jitter, sin solapamiento |
| `BalloonController.cs` | Comportamiento individual, dwell time Tobii |
| `GameTimer.cs` | Countdown, penalización, eventos de tiempo |
| `SessionScorer.cs` | Cálculo de 3 componentes de puntuación |
| `SessionData.cs` | Clase serializable de sesión |
| `PatientDataManager.cs` | Lectura/escritura JSON del paciente |
| `UIManager.cs` | Paneles, animaciones, feedback visual |
| `GazeCursorController.cs` | Cursor visual Tobii (opcional) |

## Eventos del sistema

```csharp
// GameManager
public static event Action<GameState> OnStateChanged;
public static event Action<int>       OnBalloonPopped;   // número del globo
public static event Action            OnWrongBalloon;
public static event Action<SessionData> OnGameOver;

// GameTimer
public static event Action<float> OnTimerUpdated;   // tiempo restante
public static event Action        OnTimeUp;
public static event Action<float> OnPenaltyApplied; // segundos restados
```

## Flujo completo

```
[Inicio]     → PanelInicio visible, sin globos, timer estático
     ↓ click BtnIniciar
[Countdown]  → 3→2→1→¡Ya! animado, globos aún no existen
     ↓ al terminar
[Playing]    → SpawnBalloons() + timer.Start() simultáneos
     ↓ todos explotados O tiempo=0
[Results]    → SaveSession() → LoadHistory() → ShowResults()
```

## Spawner — algoritmo grid+jitter

```csharp
// Tamaño según cantidad
float size = count <= 5 ? 120f : count <= 8 ? 100f : 85f;

// Grid NxM donde N*M >= count
int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
int rows = Mathf.CeilToInt((float)count / cols);

// Área segura con márgenes: top 80px, resto 40px
// Celda mínima: size * 1.8f en ambos ejes
// Jitter: máximo 20% del tamaño de celda
// Shuffle Fisher-Yates de celdas antes de asignar
```

## Fórmula de scoring

```csharp
// Precisión (40%) — penaliza errores sobre intentos totales
int   intentos  = globosExplotados + errores;
float precision = (globosExplotados / (float)intentos) * 100f;

// Velocidad (40%) — zonas de tiempo
float ratio      = tiempoUsado / tiempoLimite;
float multiplier = ratio <= 0.33f ? 1.0f : ratio <= 0.66f ? 0.65f : 0.35f;
string zona      = ratio <= 0.33f ? "Rápida" : ratio <= 0.66f ? "Media" : "Lenta";
float velocidad  = completado ? 100f * multiplier
                 : (globosExplotados / (float)total) * 50f * multiplier;

// Consistencia (20%) — 15 puntos por error
float consistencia = Mathf.Clamp(100f - (errores * 15f), 0f, 100f);

// Final
float final = precision*0.40f + velocidad*0.40f + consistencia*0.20f;
final = Mathf.Round(Mathf.Clamp(final, 0f, 100f) * 10f) / 10f;
```

## Rangos de puntuación

| Puntuación | Rango | Color | Mensaje |
|---|---|---|---|
| 90–100 | ★★★ Excelente | Verde | "¡Rendimiento excepcional!" |
| 70–89  | ★★ Bien | Azul | "¡Buen trabajo!" |
| 50–69  | ★ Regular | Amarillo | "¡Sigue practicando!" |
| 0–49   | Iniciando | Naranja | "¡Cada sesión cuenta!" |

NUNCA usar rojo ni mensajes negativos — contexto terapéutico.

## Tobii Eye Tracking

```csharp
[Header("Tobii")]
[SerializeField] bool  useEyeTracking = false;  // auto-detecta si Tobii disponible
[SerializeField] float onsetDelayMs   = 175f;   // antes de mostrar feedback
[SerializeField] float dwellTimeMs    = 600f;   // para activar globo

// Dos fases:
// 1. Onset (175ms): mirada entra → esperar → si sale, cancelar
// 2. Dwell (600ms): mostrar anillo de progreso → al completar, activar globo

// Cursor: TobiiGazeProvider.Instance.GazePositionScreen
//         → RectTransformUtility.ScreenPointToLocalPointInRectangle
//         → Lerp factor 0.2f para suavizar
```

## Parámetros configurables en Inspector

```csharp
[Header("Configuración de sesión")]
[SerializeField] int   cantidadGlobos         = 5;    // 3–10
[SerializeField] float tiempoLimiteSegundos   = 60f;
[SerializeField] float penalizacionSegundos   = 5f;

[Header("Tobii")]
[SerializeField] bool  useEyeTracking  = false;
[SerializeField] float onsetDelayMs    = 175f;
[SerializeField] float dwellTimeMs     = 600f;
```

## Jerarquía Canvas

```
Canvas (Screen Space - Overlay)
├── PanelInicio        → BtnIniciar (primario) + BtnVolver (secundario)
├── PanelCountdown     → FondoOscuro + TextoContador
├── PanelGame          → HUD (timer + instrucción) + BalloonsContainer
├── PanelResults       → FondoOscuro + ScrollRect > Tarjeta > [contenido]
└── GazeCursor         → siempre encima, visible solo con Tobii activo
```

## Panel de resultados — orden de contenido

```
Tarjeta (VerticalLayoutGroup + ContentSizeFitter Preferred)
├── TituloResultado
├── FilaMetricas (HorizontalLayoutGroup)
├── Separador
├── FilaBarra_Precision   (Label 130px | Barra flexible | Valor 45px)
├── FilaBarra_Velocidad
├── FilaBarra_Consistencia
├── Separador
├── FilaPuntuacionFinal   (★★ | "82,6" font 40pt | "/ 100")
├── SeccionEvolucion      (solo si historial >= 2 sesiones)
│   ├── GraficaBarras     (LayoutElement MinHeight=120, FIJO)
│   ├── TablaUltimas5
│   └── FilaEstadisticas  (mejor | promedio | tendencia)
├── TextoPrimeraVez       (solo si es la primera sesión)
└── FilaBotones           (BtnReintentar | BtnVolver)
```

## Animación de entrada del panel de resultados

```
t=0.0s  FadeIn fondo oscuro
t=0.3s  SlideUp tarjeta
t=0.6s  FadeIn título + métricas
t=0.9s  AnimateFill barras (secuencial, 0.5s cada una)
t=1.6s  CountUp puntuación (0→valor, 0.6s)
t=2.3s  SlideUp sección evolución
t=2.7s  FadeIn botones
```
