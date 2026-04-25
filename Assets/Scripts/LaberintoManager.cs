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
    public TMP_Text textResult; 
    public TMP_Text textMessage;
    public TMP_Text textMetrica; // Nuevo: Para el desglose de tiempo y errores
    public GameObject botonReload; // Referencia para ocultarlo en éxito

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

    protected override void Start()
    {
        // 1. Mapeamos todos los objetos por nombre según la jerarquía
        MapearJerarquia();
        
        usarValidacionOjos = false; 
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
        
        // Overlays (Búsqueda más agresiva por si están desactivados)
        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null) mainCanvas = GameObject.FindFirstObjectByType<Canvas>();

        if (mainCanvas != null) {
            if (overlayInicio == null) {
                Transform t = mainCanvas.transform.Find("OverlayInicio");
                if (t != null) overlayInicio = t.gameObject;
            }
            if (overlayFinal == null) {
                Transform t = mainCanvas.transform.Find("OverlayFinal");
                if (t != null) overlayFinal = t.gameObject;
            }
        }
        
        // Elementos del OverlayFinal (Hijos)
        if (overlayFinal != null) {
            if (textResult == null) textResult = overlayFinal.transform.Find("OverlayResult")?.GetComponent<TMP_Text>();
            if (textMessage == null) textMessage = overlayFinal.transform.Find("OverlayMessage")?.GetComponent<TMP_Text>();
            if (textMetrica == null) textMetrica = overlayFinal.transform.Find("OverlayMetrica")?.GetComponent<TMP_Text>();
            
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
                // Si el juego ha terminado, forzamos que el laberinto se apague
                if (mazeContainer != null) mazeContainer.SetActive(false);
                if (playerCursor != null) playerCursor.gameObject.SetActive(false);
            }
        }
    }

    public override void IniciarJuego()
    {
        base.IniciarJuego(); // Desactiva el overlayInicio base
        ConfigurarVisibilidad(inicio: false, juego: true, final: false);
    }

    public override void ReiniciarJuego()
    {
        // Reseteo rápido sin recargar escena
        _enMeta = false;
        _tiempoRestante = tiempoLimite;
        puntuacion = 0;
        
        if (_generador != null) _generador.Generar();
        
        _indiceValidadoActual = 0;
        _conteoErrores = 0;
        _nodosValidados.Clear();
        _nodosValidados.Add(new Vector2Int(0, 1));
        
        ReiniciarPosicion();
        IniciarJuego();
    }

    protected override void Update()
    {
        base.Update();
        if (_enMeta || !juegoIniciado || juegoPausado) return;

        ProcesarSeguimientoOcular();

        // Cronómetro
        _tiempoRestante -= Time.deltaTime;
        if (timerText != null) 
            timerText.text = Mathf.Max(0, _tiempoRestante).ToString("F0") + "s";

        if (_tiempoRestante <= 0) {
            FinalizarPorTiempo();
            return;
        }

        ManejarMovimientoCursor();
        ChequearColisiones();
    }

    void ManejarMovimientoCursor()
    {
        if (playerCursor == null) return;
        
        RectTransform referenceRT = playerCursor.parent as RectTransform;
        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRT, UnityEngine.Input.mousePosition, null, out localPos)) {
            _posicionActual = Vector2.Lerp(_posicionActual, localPos, Time.deltaTime * velocidadSuavizado);
            playerCursor.anchoredPosition = _posicionActual;
        }
    }

    void ProcesarSeguimientoOcular()
    {
        if (EyeTracker.Instance == null) return;

        var gaze = EyeTracker.Instance.LatestGazeData;
        if (gaze != null && (gaze.Left.GazePointValid || gaze.Right.GazePointValid)) {
            Vector2 viewPos = new Vector2(
                (gaze.Left.GazePointOnDisplayArea.x + gaze.Right.GazePointOnDisplayArea.x) / 2f,
                (gaze.Left.GazePointOnDisplayArea.y + gaze.Right.GazePointOnDisplayArea.y) / 2f
            );
            _gazeDebugPos = new Vector2(viewPos.x * Screen.width, (1f - viewPos.y) * Screen.height);
        }
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
        if (puntoInicio != null && playerCursor != null) {
            // Usamos position (mundo) para evitar líos si tienen padres con distintos pivotes
            playerCursor.position = puntoInicio.position;
            _posicionActual = playerCursor.anchoredPosition;
        }
    }

    void FinalizarPorTiempo()
    {
        if (_enMeta) return;
        _enMeta = true; juegoIniciado = false;
        
        if (GestorPaciente.Instance != null) {
            int puntos = Mathf.FloorToInt((_nodosValidados.Count / (float)(_generador.columnas * _generador.filas)) * 100);
            GestorPaciente.Instance.GuardarPartida("Laberinto", puntos, 1, 0, false, tiempoLimite);
        }

        ConfigurarVisibilidad(inicio: false, juego: false, final: true);
        if (botonReload != null) botonReload.SetActive(true); // Permitir reintento
        MostrarFeedbackFinal("¡CASI LO TIENES!", "Has recorrido gran parte del laberinto.\nSigue practicando para ser un maestro.", new Color(1, 0.5f, 0));
    }

    void Ganar()
    {
        if (_enMeta) return;
        _enMeta = true; juegoIniciado = false;
        
        float tiempoUsado = tiempoLimite - _tiempoRestante;
        if (GestorPaciente.Instance != null) {
            GestorPaciente.Instance.GuardarPartida("Laberinto", 100, 1, 100, true, tiempoUsado);
        }

        ConfigurarVisibilidad(inicio: false, juego: false, final: true);
        if (botonReload != null) botonReload.SetActive(false); // Ocultar en éxito
        MostrarFeedbackFinal("¡ERES UN CRACK!", $"Has completado el laberinto en {tiempoUsado:F0} segundos.\n¡Buen trabajo!", Color.green);
    }

    void MostrarFeedbackFinal(string titulo, string mensaje, Color colorTexto)
    {
        float tiempoUsado = tiempoLimite - _tiempoRestante;
        
        // CÁLCULO DE PUNTUACIÓN MEJORADO
        // No restamos puntos de tiempo si lo hace en menos del tiempo mínimo
        float penalizacionTiempo = Mathf.Max(0, tiempoUsado - tiempoMinimoResolucion) * 0.8f;
        float puntaje = 100f - (_conteoErrores * 5f) - penalizacionTiempo;
        puntaje = Mathf.Clamp(puntaje, 0, 100);
        int finalScore = Mathf.FloorToInt(puntaje);

        // DETERMINAR MENSAJE POR RANGO
        string tituloDinamico = titulo;
        string mensajeDinamico = mensaje;

        if (finalScore >= 91) {
            tituloDinamico = "¡ERES UN CRACK!";
            mensajeDinamico = "¡Puntuación casi perfecta! Eres un auténtico maestro del espacio.";
        } else if (finalScore >= 81) {
            tituloDinamico = "¡INCREÍBLE!";
            mensajeDinamico = "¡Lo has hecho genial! Tienes una vista de lince.";
        } else if (finalScore >= 71) {
            tituloDinamico = "¡GENIAL!";
            mensajeDinamico = "¡Buen trabajo! Vas por muy buen camino, sigue así.";
        } else if (finalScore >= 50) {
            tituloDinamico = "¡BIEN HECHO!";
            mensajeDinamico = "¡Reto superado! Has logrado completar el laberinto con éxito.";
        }

        if (textResult != null) {
            textResult.text = tituloDinamico;
            textResult.color = (finalScore >= 91) ? Color.green : colorTexto;
        }
        if (textMessage != null) {
            textMessage.text = mensajeDinamico;
        }

        // Mostrar Métricas Detalladas (Amigables)
        if (textMetrica != null) {
            int minutos = Mathf.FloorToInt(tiempoUsado / 60);
            int segundos = Mathf.FloorToInt(tiempoUsado % 60);
            
            textMetrica.text = $"<b>TIEMPO USADO:</b> {minutos:00}:{segundos:00}\n" +
                               $"<b>ERRORES:</b> {_conteoErrores}\n" +
                               $"<b>PUNTUACIÓN FINAL:</b> {finalScore}/100";
        }

        // Lógica del Botón Mejorar Puntuación
        if (botonReload != null) {
            if (finalScore >= 100) {
                botonReload.SetActive(false); // Es perfecto, no hace falta repetir
            } else {
                botonReload.SetActive(true);
                Image btnImg = botonReload.GetComponent<Image>();
                if (btnImg != null) btnImg.color = new Color(0, 0.8f, 0); // Verde vibrante
                
                TMP_Text btnTxt = botonReload.GetComponentInChildren<TMP_Text>();
                if (btnTxt != null) btnTxt.text = "MEJORAR PUNTUACIÓN";
            }
        }
    }
}
