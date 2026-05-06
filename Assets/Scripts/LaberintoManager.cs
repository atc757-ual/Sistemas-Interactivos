using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
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
    private bool _permitirReintento = false;
    private float _tiempoRestante;
    private bool _esperandoInicioPosicion = false;
    private bool _enCuentaRegresiva = false;

    // Control de parpadeo (estandarizado)
    private float _blinkTimer = 0f;
    private bool _eyesWereDetected = false;
    
    // Control de flujo inicial (1s o mirada)
    private bool _instruccionesMostradas = false;
    private bool _inicioHabilitado = false;

    protected override void Start()
    {
        if (GestorPaciente.Instance == null || !GestorPaciente.Instance.EsSesionValida()) return;

        usarValidacionOjos = false; // Desactivamos el control automático de la Base para manejarlo nosotros
        base.Start(); // Configura botones básicos y overlayInicio
        
        // 1. Mapeamos todos los objetos por nombre según la jerarquía
        MapearJerarquia();
        _tiempoRestante = tiempoLimite;

        // 2. ESTADO INICIAL: Solo fondo y OverlayInicio visibles
        ConfigurarVisibilidad(inicio: true, juego: false, final: false);
        
        // Iniciamos el delay de 1s para habilitar el botón/instrucciones
        StartCoroutine(RoutineInicioDelayed());
        
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
        if (mazeContainer == null) mazeContainer = BuscarObjetoInactivo("MazeContainer");
        if (playerCursor == null) {
            var go = BuscarObjetoInactivo("PlayerCursor");
            if (go == null) go = BuscarObjetoInactivo("Astronauta");
            if (go != null) playerCursor = go.GetComponent<RectTransform>();
        }
        if (imageFlashDano == null) {
            var go = BuscarObjetoInactivo("FlashDano");
            if (go != null) imageFlashDano = go.GetComponent<Image>();
        }
        if (timerText == null) {
            var go = BuscarObjetoInactivo("TimerText");
            if (go != null) timerText = go.GetComponent<TMP_Text>();
        }
        
        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null) mainCanvas = GameObject.FindFirstObjectByType<Canvas>();

        if (mainCanvas != null) {
            if (overlayInicio == null) overlayInicio = mainCanvas.transform.Find("OverlayInicio")?.gameObject;
            if (overlayFinal == null) overlayFinal = mainCanvas.transform.Find("OverlayFinal")?.gameObject;
            if (overlayRetry == null) {
                overlayRetry = mainCanvas.transform.Find("OverlayRetry")?.gameObject;
                if (overlayRetry == null && overlayFinal != null) {
                    overlayRetry = overlayFinal.transform.Find("OverlayRetry")?.gameObject;
                }
            }

            if (botonVolver == null) {
                Transform vt = mainCanvas.transform.Find("VolverBtn") ?? mainCanvas.transform.Find("BotonVolver") ?? mainCanvas.transform.Find("BackBtn");
                if (vt != null) {
                    botonVolver = vt.gameObject;
                    Button btnVolver = vt.GetComponent<Button>();
                    if (btnVolver != null) {
                        btnVolver.onClick.RemoveAllListeners();
                        btnVolver.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Activities"));
                    }
                }
            }
        }

        if (overlayInicio != null) {
            if (textBienvenida == null) textBienvenida = overlayInicio.transform.Find("OverlayTitle")?.GetComponent<TMP_Text>();
            
            if (botonIniciar == null) {
                Transform tInicio = overlayInicio.transform.Find("BotonInicio");
                if (tInicio != null) botonIniciar = tInicio.GetComponent<Button>();
            }

            if (botonIniciar != null) {
                botonIniciar.onClick.RemoveAllListeners();
                botonIniciar.onClick.AddListener(() => {
                    Debug.Log("🚀 [TOBII] ¡BOTÓN INICIAR PULSADO!");
                    IniciarJuego();
                });
                botonIniciar.interactable = true; 
            }

            if (overlayInstructions == null) {
                foreach (Transform t in overlayInicio.GetComponentsInChildren<Transform>(true)) {
                    if (t.name.Contains("Instruction") || t.name.Contains("Intstruction")) {
                        overlayInstructions = t.gameObject;
                        break;
                    }
                }
            }

            if (counterInicio == null) {
                foreach (Transform t in overlayInicio.GetComponentsInChildren<Transform>(true)) {
                    if (t.name == "Counter" || t.name.Contains("Contador")) {
                        counterInicio = t.gameObject;
                        textCountdown = counterInicio.GetComponent<TMP_Text>() ?? counterInicio.GetComponentInChildren<TMP_Text>();
                        break;
                    }
                }
            }
        }

        // --- ESTADO INICIAL DE CALMA ---
        if (overlayInstructions != null) overlayInstructions.SetActive(false);
        if (botonIniciar != null) botonIniciar.gameObject.SetActive(false);
        if (counterInicio != null) counterInicio.SetActive(false);
        _instruccionesMostradas = false;
        _inicioHabilitado = false; // Se activará por la corutina lanzada en Start
        
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
                }
            }

            // --- ASEGURAR ASTRONAUTA DELANTE ---
            if (playerCursor != null) {
                playerCursor.transform.SetAsLastSibling();
                Debug.Log("<color=cyan>LABERINTO: Astronauta forzado al frente.</color>");
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
        if (puntoInicio == null) {
            var go = BuscarObjetoInactivo("StartPoint");
            if (go != null) puntoInicio = go.GetComponent<RectTransform>();
        }
        if (puntoMeta == null) {
            var go = BuscarObjetoInactivo("Goal_Point");
            if (go != null) puntoMeta = go.GetComponent<RectTransform>();
        }
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
                if (botonReload != null) botonReload.SetActive(false); 
                
                // LIMPIEZA PROFUNDA: Apagamos el texto de reintento si está dentro del panel final
                if (overlayRetry != null) overlayRetry.SetActive(false);
                
                // Si la referencia sigue fallando, lo buscamos manualmente para apagarlo
                Transform trRetry = overlayFinal.transform.Find("OverlayRetry");
                if (trRetry != null) trRetry.gameObject.SetActive(false);
            }
        }
        if (overlayRetry != null) overlayRetry.SetActive(false);
        _permitirReintento = false;

        // El botón Volver se pone después del overlay para que SIEMPRE esté encima
        if (botonVolver != null) {
            botonVolver.SetActive(!juego); 
            if (!juego) botonVolver.transform.SetAsLastSibling(); 
        }
    }

    public override void IniciarJuego()
    {
        // 1. Visibilidad: Activamos juego y mantenemos el overlay para el mensaje
        ConfigurarVisibilidad(inicio: true, juego: true, final: false);
        
        // 2. Limpieza: Ocultamos botones e instrucciones, pero dejamos el Counter para el mensaje
        if (overlayInicio != null) {
            foreach (Transform child in overlayInicio.transform) {
                if (child.gameObject != counterInicio) {
                    child.gameObject.SetActive(false);
                }
            }
        }
        if (botonIniciar != null) botonIniciar.gameObject.SetActive(false);

        // 3. Instrucción Manual: Ponemos el mensaje en el objeto del contador
        if (counterInicio != null) {
            counterInicio.SetActive(true);
            if (textCountdown != null) {
                textCountdown.text = "¡Lleva el astronauta al inicio!";
                textCountdown.fontSize = 50; // Ajustamos tamaño para el mensaje largo
            }
        }

        // 4. Activamos el estado de espera
        _esperandoInicioPosicion = true;
        _enCuentaRegresiva = false;
        juegoIniciado = false;
        
        Debug.Log("Waiting for player to reach StartPoint...");
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

        // IMPORTANTE: Volvemos al estado visual de Inicio (pero con el mensaje de arrastrar)
        ConfigurarVisibilidad(inicio: true, juego: true, final: false);

        // Limpieza de UI
        if (overlayInicio != null) {
            foreach (Transform child in overlayInicio.transform) {
                if (child.gameObject != counterInicio) child.gameObject.SetActive(false);
            }
        }

        if (counterInicio != null) {
            counterInicio.SetActive(true);
            if (textCountdown != null) {
                textCountdown.text = "¡Lleva el astronauta al inicio!";
                textCountdown.fontSize = 50;
            }
        }

        _esperandoInicioPosicion = true;
        _enCuentaRegresiva = false;
        juegoIniciado = false;
    }


    protected override void Update()
    {
        base.Update();
        if (_enMeta)
        {
            // Búsqueda de emergencia si la referencia se perdió (por estar anidado)
            if (overlayRetry == null && overlayFinal != null) {
                Transform tr = overlayFinal.transform.Find("OverlayRetry");
                if (tr != null) overlayRetry = tr.gameObject;
            }

            if (!juegoPausado) 
            {
                ManejarReintentoPorParpadeo();
            }
            return;
        }
        if (juegoPausado) return;

        // FASE 0: Antes de siquiera estar en el modo "Llevar al START", buscamos los ojos
        if (!juegoIniciado && !_esperandoInicioPosicion && !_enCuentaRegresiva) {
            ManejarInstruccionesYParpadeo();
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
        
        if (counterInicio != null) counterInicio.SetActive(true);

        if (textCountdown != null) {
            textCountdown.fontSize = 120; // Volvemos al tamaño gigante para los números
            textCountdown.text = "3";
            yield return new WaitForSeconds(1f);
            textCountdown.text = "2";
            yield return new WaitForSeconds(1f);
            textCountdown.text = "1";
            yield return new WaitForSeconds(1f);
            textCountdown.text = "¡Encuentra la salida!";
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
        if (TobiiGazeProvider.Instance == null || !TobiiGazeProvider.Instance.HasGaze) return;

        Vector2 screenPoint = TobiiGazeProvider.Instance.GazePositionScreen;

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

    private System.Collections.IEnumerator RoutineInicioDelayed()
    {
        yield return new WaitForSecondsRealtime(1f);
        _inicioHabilitado = true;
    }

    void ManejarInstruccionesYParpadeo()
    {
        bool eyesDetected = TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.EyeDataValid;
        // Solo consideramos que Tobii falla si el sensor no responde en absoluto
        bool tobiiRealmenteFalla = EyeTracker.Instance == null || EyeTracker.Instance.LatestGazeData == null;

        // 1. Lógica de aparición dinámica (solo si pasó el segundo inicial)
        if (!_instruccionesMostradas && _inicioHabilitado)
        {
            if (overlayInstructions != null)
            {
                // Si Tobii falla, lo dejamos siempre encendido. Si funciona, es dinámico.
                bool mostrarInstrucciones = tobiiRealmenteFalla || eyesDetected;
                
                if (overlayInstructions.activeSelf != mostrarInstrucciones) {
                    overlayInstructions.SetActive(mostrarInstrucciones);
                }
                
                if (mostrarInstrucciones) {
                    if (eyesDetected) {
                        SetOverlayText(overlayInstructions, "<b>¡Ojos detectados!</b>\n\nPestañea o haz clic en el botón inferior para iniciar la aventura.");
                    } else {
                        SetOverlayText(overlayInstructions, "<b>¿Preparado para la misión?</b>\n\nHaz clic en el botón inferior para comenzar.");
                    }
                }
            }

            if (botonIniciar != null && !botonIniciar.gameObject.activeSelf) {
                botonIniciar.gameObject.SetActive(true);
                botonIniciar.interactable = true;
            }
        }

        // 2. Lógica de parpadeo
        if (!eyesDetected)
        {
            if (_eyesWereDetected) _blinkTimer += Time.deltaTime;
        }
        else
        {
            // Si recuperamos los ojos después de un breve lapso (parpadeo)
            if (_eyesWereDetected && _blinkTimer > 0.1f && _blinkTimer < 0.5f)
            {
                _eyesWereDetected = false;
                _blinkTimer = 0;
                Debug.Log("<color=magenta><b>[LABERINTO]</b> Pestañeo detectado. Iniciando...</color>");
                
                if (overlayInstructions != null) overlayInstructions.SetActive(false);
                IniciarJuego();
            }
            else
            {
                _eyesWereDetected = true;
                _blinkTimer = 0;
            }
        }
    }

    void ManejarReintentoPorParpadeo()
    {
        bool eyesDetected = TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.EyeDataValid;

        // Si ya ganamos con 100, no queremos ni ver el overlay de reintento ni procesar parpadeos
        bool tobiiRealmenteFalla = EyeTracker.Instance == null || EyeTracker.Instance.LatestGazeData == null;

        if (overlayRetry != null)
        {
            if (_permitirReintento) {
                // Dinámico: Solo si hay ojos o si Tobii ha muerto
                bool mostrarReintento = tobiiRealmenteFalla || eyesDetected;
                
                if (overlayRetry.activeSelf != mostrarReintento) {
                    overlayRetry.SetActive(mostrarReintento);
                }
                
                if (mostrarReintento) {
                    if (eyesDetected) {
                        SetOverlayText(overlayRetry, "<b>¡Ojos detectados!</b>\n\nPestañea para reintentar la misión.");
                    } else {
                        SetOverlayText(overlayRetry, "Pestañea o haz clic en el botón de abajo para volver a intentarlo");
                    }
                }
            } else {
                overlayRetry.SetActive(false);
            }
        }

        if (!eyesDetected)
        {
            if (_eyesWereDetected) _blinkTimer += Time.deltaTime;
        }
        else
        {
                if (_permitirReintento && _eyesWereDetected && _blinkTimer > 0.1f && _blinkTimer < 0.5f)
            {
                _eyesWereDetected = false;
                _permitirReintento = false; // Bloqueamos para evitar dobles reinicios
                _blinkTimer = 0;
                
                // Mostrar mensaje de confirmación
                if (overlayRetry != null) SetOverlayText(overlayRetry, "<b>¡Pestañeo detectado!</b>\n\nReiniciando misión...");
                
                Invoke("ReiniciarJuego", 0.5f);
            }
            else
            {
                _eyesWereDetected = true;
                _blinkTimer = 0;
            }
        }
    }

    void SetOverlayText(GameObject overlay, string message)
    {
        if (overlay == null) return;
        var tmp = overlay.GetComponentInChildren<TMP_Text>();
        if (tmp != null) {
            tmp.richText = true;
            tmp.text = message;
        }
    }

    void ManejarInstrucciones()
    {
        // Obsolote: reemplazado por ManejarInstruccionesYParpadeo
        ManejarInstruccionesYParpadeo();
    }

    void ManejarMovimientoCursor()
    {
        if (playerCursor == null) return;

        // Usa Tobii si hay datos oculares válidos; si no, fallback al mouse.
        // (usarValidacionOjos controla solo la puerta de inicio de BaseActividad,
        //  no el modo de seguimiento durante el juego.)
        bool tobiiActivo = TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.EyeDataValid;
        if (tobiiActivo) {
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
    // GC-safe cache para el raycast (evita new() en cada frame)
    private PointerEventData _cachedPointerEvent;
    private List<RaycastResult> _cachedRaycastResults = new List<RaycastResult>();

    void ChequearColisiones()
    {
        if (_cachedPointerEvent == null) _cachedPointerEvent = new PointerEventData(EventSystem.current);
        _cachedPointerEvent.position = playerCursor.position;
        _cachedRaycastResults.Clear();
        
        if (EventSystem.current != null) {
            EventSystem.current.RaycastAll(_cachedPointerEvent, _cachedRaycastResults);
            foreach (var r in _cachedRaycastResults) {
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
            GestorPaciente.Instance.GuardarPartida("Laberinto Estelar", puntos, 1, 0, false, tiempoLimite, _conteoErrores);
        }

        ConfigurarVisibilidad(inicio: false, juego: false, final: true);
        // El botón se manejará en la corutina de feedback
        MostrarFeedbackFinal("Has recorrido gran parte del laberinto. \n¡Sigue practicando para ser un maestro!");
    }

    void Ganar()
    {
        if (_enMeta) return;
        _enMeta = true; juegoIniciado = false;
        
        float tiempoUsado = tiempoLimite - _tiempoRestante;
        if (GestorPaciente.Instance != null) {
            GestorPaciente.Instance.GuardarPartida("Laberinto Estelar", 100, 1, 100, true, tiempoUsado, _conteoErrores);
        }

        ConfigurarVisibilidad(inicio: false, juego: false, final: true);
        // El botón se manejará en la corutina de feedback
        
        // LANZAR CONFETI UI (Garantizado)
        IniciarConfetiGarantizado();
        if (overlayRetry != null) overlayRetry.SetActive(false);
        
        // Puntuación REAL (ya no forzamos 100)
        MostrarFeedbackFinal($"Has logrado salir del laberinto en {tiempoUsado:F0} segundos.");
    }
 
    void MostrarFeedbackFinal(string mensaje, bool forzar100 = false)
    {
        float tiempoUsado = tiempoLimite - _tiempoRestante;

        // CÁLCULO DE PUNTUACIÓN (Solo si no forzamos 100)
        float penalizacionTiempo = Mathf.Max(0, tiempoUsado - tiempoMinimoResolucion) * 0.8f;
        float puntaje = 100f - (_conteoErrores * 5f) - penalizacionTiempo;
        puntaje = Mathf.Clamp(puntaje, 0, 100);

        int finalScore = forzar100 ? 100 : Mathf.FloorToInt(puntaje);

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

        // El control de botones se delega a una corutina para el retraso de 3s
        StartCoroutine(RoutineRetrasoBotones(finalScore));
    }

    private System.Collections.IEnumerator RoutineRetrasoBotones(int score)
    {
        // Ocultamos inicialmente para la "calma" post-juego
        _permitirReintento = false;
        if (botonReload != null) botonReload.SetActive(false);
        if (overlayRetry != null) overlayRetry.SetActive(false);

        // Si ha sacado 100, forzamos el apagado y bloqueamos cualquier reintento
        if (score >= 100) {
            _permitirReintento = false;
            if (botonReload != null) botonReload.SetActive(false);
            if (overlayRetry != null) overlayRetry.SetActive(false);
            yield break; 
        }

        // Si ha sacado menos, esperamos 3 segundos de "reflexión"
        yield return new WaitForSeconds(3f);

        _permitirReintento = true;
        if (botonReload != null) botonReload.SetActive(true);
        // Sincronizamos la aparición del mensaje con el botón
        if (overlayRetry != null) {
            overlayRetry.SetActive(true);
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
        if (overlayFinal == null) return;
        GameObject star = new GameObject("StarUI_Confetti"); // Nombre explícito y nunca vacío
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
