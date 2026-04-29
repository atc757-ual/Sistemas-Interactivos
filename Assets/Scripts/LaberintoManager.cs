using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using Tobii.Research.Unity;

/// <summary>
/// Gestiona la lógica del laberinto: movimiento del cursor, validación de pasos y condiciones de victoria.
/// Implementa gestión de estados visuales según la jerarquía del usuario.
/// 
/// RESUMEN DE LÓGICA DE MÉTRICAS ACTUALIZADA:
/// - Tiempo "Top" (10s): Los primeros 10 segundos no penalizan puntos.
/// - Contador de Errores: Cada error (pared roja) resta 5 puntos.
/// - Cálculo de Puntuación: 100 - (Errores * 5) - ((Tiempo - 10s) * 0.8). Mínimo 0.
/// - Rangos de Mensajes:
///     - 91-100: "¡ERES UN CRACK!" (Botón Reload desaparece al llegar a 100).
///     - 81-90:  "¡INCREÍBLE!"
///     - 71-80:  "¡GENIAL!"
///     - 50-70:  "¡BIEN HECHO!"
/// - Botón Dinámico: Si puntaje < 100, el botón es VERDE y dice "MEJORAR PUNTUACIÓN".
/// </summary>
public class LaberintoManager : BaseActividad
{
    [Header("Referencias de Escena (Auto-mapeadas)")]
    public GameObject mazeContainer;
    public RectTransform playerCursor; 
    public Image imageFlashDano;      
    public TMP_Text timerText;        
    public GameObject overlayFinal;   
    public GameObject overlayRetry;   // Nuevo: Para reintentar
    public TMP_Text textResult; 
    public TMP_Text textMessage;
    public TMP_Text textTiempoValue;  // Texto para el valor del tiempo
    public TMP_Text textErroresValue; // Texto para el valor de errores
    public TMP_Text textScoreValue;   // Texto para el valor de puntuación
    public TMP_Text textCountdown; 
    public TMP_Text textBienvenida; 
    public GameObject overlayInstructions; // NUEVO: Separado del Counter
    public GameObject counterInicio; // EXCLUSIVO para la cuenta atrás (3, 2, 1)
    public GameObject counterFinal;  
    public GameObject botonReload; 
    public GameObject botonVolver; // Nuevo: Botón para volver al menú
    public Image progressBar;      // Barra de progreso visual
    public Image barScore;   // Barra de Puntuación (Verde)
    public Image barTime;    // Barra de Tiempo (Azul)
    public Image barErrors;  // Barra de Errores (Amarillo)
    public ParticleSystem estrellaConfeti; // Nuevo: Confeti de estrellas

    [Header("Ajustes Laberinto")]
    public float velocidadSuavizado = 15f;
    public RectTransform puntoInicio; 
    public RectTransform puntoMeta;
    public Color colorRastro = Color.yellow;
    public Color colorCorrecto = Color.green;
    public Color colorError = Color.red;
    public float tiempoLimite = 60f;
    public float tiempoMinimoResolucion = 10f; // Tiempo "Top" que no resta puntos

    private int _conteoErrores = 0; // Contador de fallos (casillas rojas)
    private Vector2 _posicionActual;
    private Vector2 _gazeDebugPos;
    private List<Vector2Int> _nodosValidados = new List<Vector2Int>(); 
    private GeneradorLaberinto _generador;
    private bool _enMeta = false;
    private float _tiempoRestante;
    private bool _esperandoInicioPosicion = false;
    private bool _enCuentaRegresiva = false;

    // Control de parpadeo (estilo Estrella Lineal)
    private float _tiempoOjosCerrados = 0f;
    private bool _ojosEstablesParaIniciar = false;

    protected override void Start()
    {
        // 1. Mapeamos todos los objetos por nombre según la jerarquía
        MapearJerarquia();
        base.Start(); // Configura botones básicos y overlayInicio
        
        _tiempoRestante = tiempoLimite;

        // 2. ESTADO INICIAL: Solo fondo y OverlayInicio visibles
        ConfigurarVisibilidad(inicio: true, juego: false, final: false);
        
        _generador = GetComponent<GeneradorLaberinto>();
        if (_generador != null) {
            // Aseguramos que el generador dibuje dentro del contenedor correcto
            if (mazeContainer != null) _generador.contenedor = mazeContainer.GetComponent<RectTransform>();
            _generador.Generar();
        }
        
        _nodosValidados.Clear();
        _nodosValidados.Add(new Vector2Int(0, 1)); 
        
        // Personalizar nombre del paciente
        if (textBienvenida != null && GestorPaciente.Instance != null) {
            string nombre = GestorPaciente.Instance.GetNombrePacienteFormateado();
            textBienvenida.text = "¡HOLA, " + nombre + "!";
        }
        
        ReiniciarPosicion();
    }

    /// <summary>
    /// Busca y asigna automáticamente los objetos según los nombres exactos de tu jerarquía.
    /// </summary>
    void MapearJerarquia()
    {
        // Contenedores principales
        if (mazeContainer == null) mazeContainer = GameObject.Find("MazeContainer");
        if (playerCursor == null) playerCursor = GameObject.Find("PlayerCursor")?.GetComponent<RectTransform>();
        if (imageFlashDano == null) imageFlashDano = GameObject.Find("FlashDano")?.GetComponent<Image>();
        if (timerText == null) timerText = GameObject.Find("TimerText")?.GetComponent<TMP_Text>();
        if (botonVolver == null) botonVolver = GameObject.Find("VolverBtn");
        
        // Overlays (Búsqueda más agresiva por si están desactivados)
        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null) mainCanvas = GameObject.FindFirstObjectByType<Canvas>();

        if (mainCanvas != null) {
            if (overlayInicio == null) overlayInicio = mainCanvas.transform.Find("OverlayInicio")?.gameObject;
            if (overlayFinal == null) overlayFinal = mainCanvas.transform.Find("OverlayFinal")?.gameObject;
            if (overlayRetry == null) overlayRetry = mainCanvas.transform.Find("OverlayRetry")?.gameObject;
        }

        // Búsqueda del botón y mensaje (OverlayInstructions) de forma robusta
        if (overlayInicio != null) {
            if (textBienvenida == null) textBienvenida = overlayInicio.transform.Find("OverlayTitle")?.GetComponent<TMP_Text>();
            
            if (botonIniciar == null) {
                // Buscamos el botón "BotonInicio" dentro de los hijos del overlay
                Button[] todosLosBotones = overlayInicio.GetComponentsInChildren<Button>(true);
                foreach (var b in todosLosBotones) {
                    if (b.name.Contains("Inicio") || b.name.Contains("Start")) {
                        botonIniciar = b;
                        break;
                    }
                }
            }

            if (botonIniciar != null) {
                botonIniciar.onClick.RemoveAllListeners();
                botonIniciar.onClick.AddListener(() => {
                    Debug.Log("🚀 [TOBII] ¡BOTÓN INICIAR PULSADO!");
                    IniciarJuego();
                });
                botonIniciar.interactable = true; // Forzamos interactuable
            }
        }

        if (overlayInstructions == null && overlayInicio != null) {
            foreach (Transform t in overlayInicio.GetComponentsInChildren<Transform>(true)) {
                if (t.name.Contains("Instruction") || t.name.Contains("Intstruction")) {
                    overlayInstructions = t.gameObject;
                    break;
                }
            }
        }

        if (counterInicio == null && overlayInicio != null) {
            foreach (Transform t in overlayInicio.GetComponentsInChildren<Transform>(true)) {
                if (t.name == "Counter" || t.name.Contains("Contador")) {
                    counterInicio = t.gameObject;
                    textCountdown = counterInicio.GetComponent<TMP_Text>() ?? counterInicio.GetComponentInChildren<TMP_Text>();
                    break;
                }
            }
            if (counterInicio != null) counterInicio.SetActive(false); // Apagado por defecto
        }
        
        // Elementos del OverlayFinal (Hijos y Nietos)
        if (overlayFinal != null) {
            if (textResult == null) textResult = overlayFinal.transform.Find("OverlayResult")?.GetComponent<TMP_Text>();
            if (textMessage == null) textMessage = overlayFinal.transform.Find("OverlayMessage")?.GetComponent<TMP_Text>();
            
            // Búsqueda de métricas en el Panel
            Transform panel = overlayFinal.transform.Find("Panel");
            if (panel != null) {
                // Buscamos los textos de los valores (buscando el componente TMP_Text en los hijos correspondientes)
                textTiempoValue = panel.Find("GameObject/OverlayMetricaTiempo")?.GetComponentInChildren<TMP_Text>();
                textErroresValue = panel.Find("GameObject/OverlayMetricaErrores")?.GetComponentInChildren<TMP_Text>();
                textScoreValue = panel.Find("GameObject/OverlayMetricaScore")?.GetComponentInChildren<TMP_Text>();

                // Buscamos las barras
                if (barTime == null) barTime = panel.Find("GameObject/OverlayBarTime")?.GetComponent<Image>();
                if (barErrors == null) barErrors = panel.Find("GameObject/OverlayBarErrors")?.GetComponent<Image>();
                if (barScore == null) barScore = panel.Find("GameObject/OverlayBarScore")?.GetComponent<Image>();
            }

            if (estrellaConfeti == null) {
                GameObject confetiObj = GameObject.Find("ConfetiEstelar");
                if (confetiObj != null) {
                    estrellaConfeti = confetiObj.GetComponent<ParticleSystem>();
                    
                    // FORZAR RENDERIZADO PARA QUE SE VEA EN EL CANVAS
                    var renderer = estrellaConfeti.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null) {
                        renderer.sortingLayerName = "UI";
                        renderer.sortingOrder = 9999; // Máxima prioridad
                    }

                    // TRUCO FINAL: Moverlo al final de la jerarquía del Canvas para que esté "delante"
                    if (mainCanvas != null) {
                        estrellaConfeti.transform.SetParent(mainCanvas.transform, false);
                        estrellaConfeti.transform.SetAsLastSibling();
                    }

                    Debug.Log("<color=green>LABERINTO: ¡Confeti Estelar forzado al frente!</color>");
                } else {
                    Debug.LogWarning("<color=orange>LABERINTO: No se encontró el objeto 'ConfetiEstelar' en la escena.</color>");
                }
            }

            if (counterFinal == null) {
                counterFinal = overlayFinal.transform.Find("CounterRetry")?.gameObject;
                if (counterFinal != null) counterFinal.SetActive(false);
            }
            
            // Mapeo del Botón Reload
            Transform btnTr = overlayFinal.transform.Find("BotonReload");
            if (btnTr != null) {
                botonReload = btnTr.gameObject;
                Button btnComp = btnTr.GetComponent<Button>();
                if (btnComp != null) {
                    btnComp.onClick.RemoveAllListeners();
                    btnComp.onClick.AddListener(ReiniciarJuego);
                }
            }
        }

        // Puntos de control (Nombres exactos de tu foto)
        if (puntoInicio == null) puntoInicio = GameObject.Find("StartPoint")?.GetComponent<RectTransform>();
        if (puntoMeta == null) puntoMeta = GameObject.Find("Goal_Point")?.GetComponent<RectTransform>();
    }

    /// <summary>
    /// Alterna la visibilidad de los grupos de UI según la fase de la actividad.
    /// </summary>
    void ConfigurarVisibilidad(bool inicio, bool juego, bool final)
    {
        if (overlayInicio != null) overlayInicio.SetActive(inicio);
        
        if (mazeContainer != null) mazeContainer.SetActive(juego);
        if (playerCursor != null) playerCursor.gameObject.SetActive(juego);
        if (timerText != null) timerText.gameObject.SetActive(juego);
        if (imageFlashDano != null) imageFlashDano.gameObject.SetActive(juego);
        
        if (overlayFinal != null) {
            overlayFinal.SetActive(final);
            if (final) {
                overlayFinal.transform.SetAsLastSibling();
                if (mazeContainer != null) mazeContainer.SetActive(false);
                if (playerCursor != null) playerCursor.gameObject.SetActive(false);
            }
        }
        if (overlayRetry != null) overlayRetry.SetActive(false);

        // ... el botón Volver se pone después del overlay para que SIEMPRE esté encima
        if (botonVolver != null) {
            botonVolver.SetActive(!juego); 
            if (!juego) botonVolver.transform.SetAsLastSibling(); 
        }
    }

    public override void IniciarJuego()
    {
        ConfigurarVisibilidad(inicio: true, juego: true, final: false);
        _esperandoInicioPosicion = true;
        juegoIniciado = false;
        
        // Limpiamos la pantalla de inicio dejando solo el Counter visible
        if (overlayInicio != null) {
            foreach (Transform child in overlayInicio.transform) {
                if (child.gameObject != counterInicio) {
                    child.gameObject.SetActive(false);
                }
            }
        }
        if (botonIniciar != null) botonIniciar.gameObject.SetActive(false);

        // Usamos EL COUNTER para dar la instrucción de moverse al START
        if (counterInicio != null) {
            counterInicio.SetActive(true);
            TMP_Text cText = counterInicio.GetComponentInChildren<TMP_Text>();
            if (cText != null) {
                cText.text = "¡Lleva al astronauta al START\npara comenzar la misión!";
                cText.color = Color.white;
            }
        }

        // RESET de posición del astronauta al centro desplazado para evitar que toque el START por error
        if (playerCursor != null) playerCursor.anchoredPosition = new Vector2(0, -30f);
        _posicionActual = new Vector2(0, -30f);
    }

    public override void ReiniciarJuego()
    {
        _enMeta = false;
        _tiempoRestante = tiempoLimite;
        puntuacion = 0;
        _indiceValidadoActual = 0;
        _conteoErrores = 0;
        _nodosValidados.Clear();
        _nodosValidados.Add(new Vector2Int(0, 1));
        
        if (_generador != null) _generador.Generar();
        ReiniciarPosicion();

        // IMPORTANTE: Volvemos al estado visual de Inicio
        ConfigurarVisibilidad(inicio: true, juego: true, final: false);

        // Limpiamos la pantalla de inicio dejando solo el Counter visible
        if (overlayInicio != null) {
            foreach (Transform child in overlayInicio.transform) {
                if (child.gameObject != counterInicio) {
                    child.gameObject.SetActive(false);
                }
            }
        }
        if (botonIniciar != null) botonIniciar.gameObject.SetActive(false);

        // Usamos EL COUNTER para dar la instrucción de moverse al START
        if (counterInicio != null) {
            counterInicio.SetActive(true);
            TMP_Text cText = counterInicio.GetComponentInChildren<TMP_Text>();
            if (cText != null) {
                cText.text = "¡Lleva al astronauta al START\npara comenzar la misión!";
                cText.color = Color.white;
            }
        }

        // El botón se apaga porque ya ha cumplido su función (vamos directo al juego)
        if (botonIniciar != null) botonIniciar.gameObject.SetActive(false);
        
        _esperandoInicioPosicion = true; // <--- Vamos directo a la fase de posicionamiento
        juegoIniciado = false;

        // RESET visual del TimerText
        if (timerText != null) {
            timerText.text = tiempoLimite.ToString("F0") + "s";
            timerText.color = Color.white;
            timerText.alpha = 1f;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (_enMeta || juegoPausado) return;

        // FASE 0: Antes de siquiera estar en el modo "Llevar al START", buscamos los ojos
        if (!juegoIniciado && !_esperandoInicioPosicion && !_enCuentaRegresiva) {
            ManejarInstrucciones();
            return;
        }

        // ESTADO 1: Esperando a que el usuario se ponga en el START
        if (_esperandoInicioPosicion && !_enCuentaRegresiva) {
            ManejarMovimientoCursor();
            
            float dist = Vector2.Distance(playerCursor.anchoredPosition, puntoInicio.anchoredPosition);
            if (dist < _generador.anchoCelda * 0.7f) {
                StartCoroutine(RoutineCuentaRegresiva());
            }
            return;
        }

        if (!juegoIniciado) return;

        ProcesarSeguimientoOcular();

        // Cronómetro con efectos visuales
        _tiempoRestante -= Time.deltaTime;
        if (timerText != null) {
            timerText.text = Mathf.Max(0, _tiempoRestante).ToString("F0") + "s";
            
            // LÓGICA DE COLORES
            if (_tiempoRestante <= 10f) {
                // ROJO Y PARPADEO (Efecto alarma)
                float alpha = (Mathf.Sin(Time.time * 15f) + 1f) / 2f;
                timerText.color = Color.red;
                timerText.alpha = alpha;
            } else if (_tiempoRestante <= 25f) {
                // NARANJA/AMARILLO
                timerText.color = new Color(1f, 0.5f, 0f); // Naranja
                timerText.alpha = 1f;
            } else {
                // COLOR NORMAL (Blanco o el que tenga el prefab)
                timerText.color = Color.white;
                timerText.alpha = 1f;
            }
        }

        if (_tiempoRestante <= 0) {
            FinalizarPorTiempo();
            return;
        }

        ManejarMovimientoCursor();
        ChequearColisiones();
    }

    System.Collections.IEnumerator RoutineCuentaRegresiva()
    {
        _enCuentaRegresiva = true;
        
        // Ya no apagamos el counterInicio aquí, porque se usa para el número 3, 2, 1
        // Aseguramos que esté encendido por si acaso
        if (counterInicio != null) counterInicio.SetActive(true);

        if (textCountdown != null) {
            textCountdown.text = "3";
            yield return new WaitForSeconds(1f);
            textCountdown.text = "2";
            yield return new WaitForSeconds(1f);
            textCountdown.text = "1";
            yield return new WaitForSeconds(1f);
            textCountdown.text = "¡Empezar!";
            yield return new WaitForSeconds(0.8f);
            textCountdown.gameObject.SetActive(false);
        }
        
        _esperandoInicioPosicion = false;
        _enCuentaRegresiva = false;
        
        // APAGAR OVERLAYS para que no se vea opaco
        if (overlayInicio != null) overlayInicio.SetActive(false);
        if (overlayFinal != null) overlayFinal.SetActive(false);
        if (overlayRetry != null) overlayRetry.SetActive(false); // Se maneja en MostrarFeedback
        
        juegoIniciado = true;
    }

    protected virtual void ProcesarSeguimientoOcular()
    {
        if (EyeTracker.Instance == null) return;

        var gaze = EyeTracker.Instance.LatestGazeData;
        if (gaze != null && (gaze.Left.GazePointValid || gaze.Right.GazePointValid))
        {
            // Promedio de ambos ojos (0.0 a 1.0)
            Vector2 viewPos = new Vector2(
                (gaze.Left.GazePointOnDisplayArea.x + gaze.Right.GazePointOnDisplayArea.x) / 2f,
                (gaze.Left.GazePointOnDisplayArea.y + gaze.Right.GazePointOnDisplayArea.y) / 2f
            );

            // Convertir de ratio (0-1) a píxeles de pantalla
            Vector2 screenPoint = new Vector2(viewPos.x * Screen.width, (1f - viewPos.y) * Screen.height);

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mazeContainer.GetComponent<RectTransform>(), 
                screenPoint, 
                null, 
                out localPoint
            );

            // Suavizado
            _posicionActual = Vector2.Lerp(_posicionActual, localPoint, 0.15f);
            
            if (playerCursor != null) {
                playerCursor.anchoredPosition = _posicionActual;
            }
        }
    }

    void ManejarInstrucciones()
    {
        bool tobiiDisponible = (EyeTracker.Instance != null);

        if (usarValidacionOjos && tobiiDisponible) {
            var gaze = EyeTracker.Instance.LatestGazeData;
            bool ojosDetectados = gaze != null && (gaze.Left.GazePointValid || gaze.Right.GazePointValid);

            // 1. El mensaje (OverlayInstructions) SOLO aparece si se detectan los ojos
            if (overlayInstructions != null) {
                if (overlayInstructions.activeSelf != ojosDetectados) {
                    overlayInstructions.SetActive(ojosDetectados);
                }
            }

            // 2. El botón aparece si hay ojos detectados
            if (botonIniciar != null) {
                if (botonIniciar.gameObject.activeSelf != ojosDetectados) {
                    botonIniciar.gameObject.SetActive(ojosDetectados);
                }
            }

            // Lógica de parpadeo (se mantiene porque es interna)
            if (ojosDetectados) {
                _tiempoOjosCerrados = 0f;
                float distMedia = (gaze.Left.GazeOriginInUserCoordinates.z + gaze.Right.GazeOriginInUserCoordinates.z) / 20f;
                _ojosEstablesParaIniciar = (distMedia >= 35 && distMedia <= 95);
            } 
            else if (_ojosEstablesParaIniciar) {
                _tiempoOjosCerrados += Time.deltaTime;
                if (_tiempoOjosCerrados >= 0.10f && _tiempoOjosCerrados <= 0.70f) {
                    _ojosEstablesParaIniciar = false;
                    IniciarJuego();
                }
            }
        } else {
            // MODO FALLBACK (Sin Tobii o desactivado)
            if (botonIniciar != null) {
                botonIniciar.gameObject.SetActive(true);
                botonIniciar.interactable = true;
            }
            if (overlayInstructions != null) overlayInstructions.SetActive(false); // Oculto por defecto
            
            // ¡CLAVE! Le decimos a la clase padre que deje de bloquear el botón
            usarValidacionOjos = false;
        }
    }

    void ManejarMovimientoCursor()
    {
        if (playerCursor == null) return;

        // PRIORIDAD: Si está activada la validación de ojos, usamos Tobii. 
        // Si no, usamos el Mouse (perfecto para pruebas).
        if (usarValidacionOjos) {
            ProcesarSeguimientoOcular();
        } else {
            ProcesarMovimientoMouse();
        }
    }

    void ProcesarMovimientoMouse()
    {
        Vector3 inputPos = Input.mousePosition;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mazeContainer.GetComponent<RectTransform>(), 
            inputPos, 
            null, 
            out localPoint
        );

        // Suavizado también para el mouse para que no se sienta "brusco"
        _posicionActual = Vector2.Lerp(_posicionActual, localPoint, 0.25f);
        playerCursor.anchoredPosition = _posicionActual;
    }

    void OnGUI()
    {
        if (juegoIniciado && _gazeDebugPos != Vector2.zero) {
            GUI.color = new Color(1, 0, 0, 0.7f);
            Rect gazeRect = new Rect(_gazeDebugPos.x - 10, Screen.height - _gazeDebugPos.y - 10, 20, 20);
            GUI.DrawTexture(gazeRect, Texture2D.whiteTexture);
        }
    }

    private int _indiceValidadoActual = 0; // Índice de la última casilla del camino feliz validada

    void ChequearColisiones()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = playerCursor.position;
        List<RaycastResult> results = new List<RaycastResult>();
        
        if (EventSystem.current != null) {
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results) {
                if (r.gameObject.name.Contains("Path")) {
                    Vector2Int coords = EncontrarCoordenadas(r.gameObject);
                    if (coords.x != -1) {
                        List<Vector2Int> camino = _generador.GetCaminoOrdenado();
                        
                        // VALIDACIÓN ESTRICTA: ¿Es la siguiente casilla que toca?
                        if (_indiceValidadoActual + 1 < camino.Count && coords == camino[_indiceValidadoActual + 1]) {
                            _indiceValidadoActual++;
                            if (!_nodosValidados.Contains(coords)) _nodosValidados.Add(coords);
                            r.gameObject.GetComponent<Image>().color = colorCorrecto;
                        } 
                        // Si es una casilla que ya validamos, la ignoramos (sigue verde)
                        else if (camino.GetRange(0, _indiceValidadoActual + 1).Contains(coords)) {
                            // No hacemos nada, ya es verde
                        }
                        // Si es parte del camino pero está demasiado lejos (salto), no se pinta
                        else if (_generador.EsParteDeLaSolucion(coords.x, coords.y)) {
                            // No se pinta de verde todavía (se queda transparente/blanco)
                        }
                        // Si es pared o camino fuera de la solución, rojo
                        else {
                            if (r.gameObject.GetComponent<Image>().color != colorError) {
                                _conteoErrores++;
                                r.gameObject.GetComponent<Image>().color = colorError;
                            }
                        }
                    }
                }
                
                if (r.gameObject.name == "Goal_Point") {
                    // Verificamos si ha llegado al FINAL de la lista del camino feliz
                    if (_indiceValidadoActual >= _generador.GetCantidadNodosSolucion() - 1) {
                        Ganar();
                    }
                }
            }
        }
    }

    Vector2Int EncontrarCoordenadas(GameObject obj)
    {
        if (_generador == null) return new Vector2Int(-1, -1);
        for (int x = 0; x < _generador.columnas; x++)
            for (int y = 0; y < _generador.filas; y++)
                if (_generador.GetObjetoEn(x, y) == obj) return new Vector2Int(x, y);
        return new Vector2Int(-1, -1);
    }

    void ReiniciarPosicion()
    {
        // En lugar de ir al START, vamos al CENTRO DESPLAZADO para que el usuario tenga que moverse
        _posicionActual = new Vector2(0, -30f);
        if (playerCursor != null) {
            playerCursor.anchoredPosition = new Vector2(0, -30f);
        }
    }

    void FinalizarPorTiempo()
    {
        if (_enMeta) return;
        _enMeta = true; juegoIniciado = false;
        
        if (GestorPaciente.Instance != null) {
            int puntos = Mathf.FloorToInt((_nodosValidados.Count / (float)(_generador.columnas * _generador.filas)) * 100);
            GestorPaciente.Instance.GuardarPartida("Laberinto", puntos, 1, 0, false, tiempoLimite, _conteoErrores);
        }

        ConfigurarVisibilidad(inicio: false, juego: false, final: true);
        if (botonReload != null) botonReload.SetActive(true); // Permitir reintento
        MostrarFeedbackFinal("Has recorrido gran parte del laberinto. \n¡Sigue practicando para ser un maestro!");
    }

    void Ganar()
    {
        if (_enMeta) return;
        _enMeta = true; juegoIniciado = false;
        
        float tiempoUsado = tiempoLimite - _tiempoRestante;
        if (GestorPaciente.Instance != null) {
            GestorPaciente.Instance.GuardarPartida("Laberinto", 100, 1, 100, true, tiempoUsado, _conteoErrores);
        }

        ConfigurarVisibilidad(inicio: false, juego: false, final: true);
        if (botonReload != null) botonReload.SetActive(true); // Siempre mostrar
        
        // LANZAR CONFETI UI (Garantizado)
        IniciarConfetiGarantizado();
        
        MostrarFeedbackFinal($"Has logrado salir del laberinto en {tiempoUsado:F0} segundos.");
    }
 
    void MostrarFeedbackFinal(string mensaje)
    {
        float tiempoUsado = tiempoLimite - _tiempoRestante;
        
        // CÁLCULO DE PUNTUACIÓN
        float penalizacionTiempo = Mathf.Max(0, tiempoUsado - tiempoMinimoResolucion) * 0.8f;
        float puntaje = 100f - (_conteoErrores * 5f) - penalizacionTiempo;
        puntaje = Mathf.Clamp(puntaje, 0, 100);
        int finalScore = Mathf.FloorToInt(puntaje);

        // DETERMINAR TÍTULO Y MENSAJE DINÁMICO
        string tituloDinamico = "¡Misión Cumplida!";
        string mensajeDinamico = mensaje;

        if (finalScore >= 91) {
            tituloDinamico = "¡Extraordinario!";
            mensajeDinamico = mensaje + " \n¡Eres un auténtico CRACK!";
        } else if (finalScore >= 81) {
            tituloDinamico = "¡Increíble!";
            mensajeDinamico = mensaje + "\n ¡Tienes vista de lince!";
        } else if (finalScore >= 71) {
            tituloDinamico = "¡Genial!";
            mensajeDinamico = mensaje + " \n¡Vas por muy buen camino!";
        } else if (finalScore >= 50) {
            tituloDinamico = "¡Bien hecho!";
            mensajeDinamico = mensaje;
        } else {
            tituloDinamico = "¡Sigue intentándolo!";
            mensajeDinamico = "Has estado cerca, ¡la próxima vez lo lograrás!";
        }

        if (textResult != null) {
            textResult.text = tituloDinamico;
        }
        if (textMessage != null) {
            textMessage.text = mensajeDinamico;
        }

        // Mostrar Métricas en sus elementos individuales
        int minutos = Mathf.FloorToInt(tiempoUsado / 60);
        int segundos = Mathf.FloorToInt(tiempoUsado % 60);

        if (textTiempoValue != null) textTiempoValue.text = $"{minutos:00}:{segundos:00}";
        if (textErroresValue != null) textErroresValue.text = _conteoErrores.ToString();
        if (textScoreValue != null) textScoreValue.text = $"{finalScore}";

        // Actualizar Barras de Progreso Visuales (Solo el llenado)
        if (barScore != null) {
            barScore.fillAmount = finalScore / 100f;
        }
        if (barTime != null) {
            barTime.fillAmount = _tiempoRestante / tiempoLimite;
        }
        if (barErrors != null) {
            float precision = Mathf.Max(0, 10 - _conteoErrores) / 10f;
            barErrors.fillAmount = precision;
        }

        // Lógica del Botón Mejorar Puntuación (Siempre visible)
        if (botonReload != null) {
            botonReload.SetActive(true);
            // Solo mostrar mensaje de pestañeo si estamos usando Tobii activamente
            if (overlayRetry != null) overlayRetry.SetActive(usarValidacionOjos);
        }
    }

    // --- CONFETI UI (SIEMPRE VISIBLE) ---
    private void IniciarConfetiGarantizado() {
        StartCoroutine(RoutineConfetiUI());
    }

    private System.Collections.IEnumerator RoutineConfetiUI() {
        for (int i = 0; i < 100; i++) { // ¡100 elementos para una explosión galáctica!
            CrearEstrellaUI();
            yield return new WaitForSeconds(0.02f); // Más rápido
        }
    }

    void CrearEstrellaUI() {
        GameObject star = new GameObject("StarUI");
        star.layer = 5; // CAPA 5 = UI (Vital para que se vea)
        star.transform.SetParent(overlayFinal.transform, false);
        star.transform.SetAsLastSibling();

        var txt = star.AddComponent<TextMeshProUGUI>();
        txt.text = "*"; // Solo asteriscos para evitar las "x"
        txt.fontSize = Random.Range(40, 90); 
        txt.color = Random.value > 0.3f ? Color.yellow : Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform rt = star.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(Random.Range(-800, 800), 600); // Más dispersión
        
        StartCoroutine(AnimarEstrella(rt));
    }

    private System.Collections.IEnumerator AnimarEstrella(RectTransform rt) {
        float vy = Random.Range(-300, -600);
        float vr = Random.Range(100, 300);
        while (rt != null && rt.anchoredPosition.y > -600) {
            rt.anchoredPosition += new Vector2(0, vy) * Time.deltaTime;
            rt.Rotate(0, 0, vr * Time.deltaTime);
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }
}
