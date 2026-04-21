using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Tobii.Research.Unity;

/// <summary>
/// Controlador de la actividad terapéutica de patrón Cuadrado (CometaCuadrado).
/// El objetivo salta o se desliza entre las 4 esquinas de la pantalla.
/// Hereda de BaseActividad para reutilizar el sistema de pausa, guardado y Tobii.
/// </summary>
public class SquareMovementController : BaseActividad
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    /// <summary>Orden de visita de las esquinas.</summary>
    public enum SquareMode { Sequential, Random }

    /// <summary>Tipo de transición entre esquinas.</summary>
    public enum SquareMovementType { SmoothPursuit, Saccadic }

    /// <summary>Nivel de dificultad preconfigurado.</summary>
    public enum DifficultyLevel { Easy, Medium, Hard }

    // =========================================================================
    // INSPECTOR — OBJETOS DE ESCENA
    // =========================================================================

    [Header("Objetos de Escena")]
    [Tooltip("RectTransform del cometa (objetivo) que el paciente debe seguir.")]
    public RectTransform objetivo;

    [Tooltip("Primer segmento del fondo para el scroll infinito.")]
    public RectTransform backgroundScroll;

    [Tooltip("Contenedor del panel de timer.")]
    public GameObject timerContainer;

    [Tooltip("Texto del cronómetro mm:ss.")]
    public TMP_Text textoTimer;

    [Tooltip("Texto de precisión en porcentaje.")]
    public TMP_Text precText;

    [Tooltip("Texto de progreso de nivel.")]
    public TMP_Text avanceText;

    [Tooltip("Imagen de barra de progreso.")]
    public Image barFill;

    [Tooltip("Texto secundario en el overlay de inicio.")]
    public TMP_Text textoSub;

    [Tooltip("Panel con detalles de la actividad.")]
    public GameObject panelDetalle;

    [Header("Overlay Resultados")]
    [Tooltip("Panel que se muestra al finalizar la sesión.")]
    public GameObject overlayResult;
    public TMP_Text titleRes;
    public TMP_Text subRes;
    public TMP_Text percentRes;
    public Button btnAgain;

    // =========================================================================
    // INSPECTOR — CONFIG CUADRADO
    // =========================================================================

    [Header("Square Config")]
    [Tooltip("Nivel de dificultad. Sobreescribe el resto de parámetros al iniciar.")]
    public DifficultyLevel difficulty = DifficultyLevel.Medium;

    [Tooltip("Sequential=esquinas en orden / Random=esquina aleatoria distinta.")]
    public SquareMode squareMode = SquareMode.Sequential;

    [Tooltip("Saccadic=teletransporte instantáneo / SmoothPursuit=deslizamiento continuo.")]
    public SquareMovementType movementType = SquareMovementType.Saccadic;

    [Tooltip("Porcentaje del área de pantalla que ocupa el cuadrado (0.5–0.9).")]
    [Range(0.4f, 0.95f)]
    public float squareSizePercent = 0.70f;

    [Tooltip("Segundos que el objetivo permanece en cada esquina antes de moverse.")]
    public float dwellTime = 1.0f;

    [Tooltip("Velocidad de deslizamiento en unidades UI/s (solo para SmoothPursuit).")]
    public float pursuitSpeed = 200f;

    [Tooltip("Velocidad del scroll de fondo en unidades UI/s.")]
    public float velocidadFondo = 80f;

    [Header("Configuración de Tiempo")]
    public float duracionSesion = 60f;
    private bool _juegoFinalizado = false;

    // =========================================================================
    // ESTADO INTERNO — MOVIMIENTO
    // =========================================================================

    private Vector2[] _esquinas;            // Las 4 esquinas en anchoredPosition
    private int _esquinaActual = 0;
    private int _esquinaAnterior = -1;
    private float _dwellTimer = 0f;
    private bool _enTransicion = false;     // true mientras viaja entre esquinas (SmoothPursuit)

    // =========================================================================
    // ESTADO INTERNO — MÉTRICAS Y GAZE
    // =========================================================================

    private float _tiempoTranscurrido;
    private float _segundosMirando;         // Total de segundos con gaze en target
    private float _dwellOnTarget;           // Segundos de fijación en la esquina actual
    private int _framesTotales;             // Nueva: para precisión total
    private int _framesTargeteados;         // Nueva: para precisión total

    // Votación de precisión por ventana de 1 segundo
    private float _timerUI_Precision;
    private int _votosPositivosPrecision;
    private int _votosTotalesPrecision;

    // Métricas clínicas por transición (para análisis)
    private float _tiempoApareció;          // Momento en que el objetivo llegó a la nueva esquina
    private bool _gazeAterrizó;            // Si ya detectamos el primer gaze en la nueva esquina
    private float _saccadeLatency;          // Latencia desde aparición hasta primer gaze
    private float _landingError;            // Distancia del primer gaze al centro del objetivo

    // =========================================================================
    // ESTADO INTERNO — BIO-TRIGGER
    // =========================================================================

    private float _tiempoOjosCerrados;
    private bool _ojosEstablesParaIniciar;
    private bool _enConteo;

    // =========================================================================
    // FONDO INFINITO
    // =========================================================================

    private List<RectTransform> _bgSegments = new List<RectTransform>();

    // =========================================================================
    // INICIALIZACIÓN
    // =========================================================================

    protected override void Start()
    {
        AplicarPresetDificultad();
        VincularUIAutomaticamente();
        base.Start();

        if (overlayInicio != null) overlayInicio.SetActive(true);
        CalcularEsquinas();
        PreconfigurarPosiciones();
    }

    private void AplicarPresetDificultad()
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                squareMode    = SquareMode.Sequential;
                movementType  = SquareMovementType.SmoothPursuit;
                dwellTime     = 1.5f;
                squareSizePercent = 0.60f;
                break;
            case DifficultyLevel.Hard:
                squareMode    = SquareMode.Random;
                movementType  = SquareMovementType.Saccadic;
                dwellTime     = 0.6f;
                squareSizePercent = 0.80f;
                break;
            default: // Medium
                squareMode    = SquareMode.Sequential;
                movementType  = SquareMovementType.Saccadic;
                dwellTime     = 1.0f;
                squareSizePercent = 0.70f;
                break;
        }
    }

    /// <summary>Calcula las 4 esquinas en coordenadas anchoredPosition del Canvas.</summary>
    private void CalcularEsquinas()
    {
        // Usamos el Canvas padre del objetivo si está disponible,
        // si no, estimamos con Screen resolutions.
        float halfW = Screen.width  * squareSizePercent / 2f;
        float halfH = Screen.height * squareSizePercent / 2f;

        // Si el objetivo tiene un padre Canvas, intentamos escalar correctamente
        if (objetivo != null)
        {
            Canvas canvas = objetivo.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                RectTransform canvasRT = canvas.GetComponent<RectTransform>();
                if (canvasRT != null)
                {
                    halfW = canvasRT.rect.width  * squareSizePercent / 2f;
                    halfH = canvasRT.rect.height * squareSizePercent / 2f;
                }
            }
        }

        // Orden: TopLeft → TopRight → BottomRight → BottomLeft (sentido horario)
        _esquinas = new Vector2[4]
        {
            new Vector2(-halfW,  halfH),   // 0: Arriba-Izquierda
            new Vector2( halfW,  halfH),   // 1: Arriba-Derecha
            new Vector2( halfW, -halfH),   // 2: Abajo-Derecha
            new Vector2(-halfW, -halfH)    // 3: Abajo-Izquierda
        };

        Debug.Log(string.Format(
            "<color=cyan>[CometaCuadrado]</color> Esquinas calculadas: TL={0} TR={1} BR={2} BL={3}",
            _esquinas[0], _esquinas[1], _esquinas[2], _esquinas[3]));
    }

    private void PreconfigurarPosiciones()
    {
        // Colocar el objetivo en la primera esquina
        if (objetivo != null && _esquinas != null && _esquinas.Length > 0)
            objetivo.anchoredPosition = _esquinas[0];

        // Duplicar fondo para scroll infinito
        if (backgroundScroll != null)
        {
            _bgSegments.Clear();
            _bgSegments.Add(backgroundScroll);

            GameObject clon = Instantiate(backgroundScroll.gameObject, backgroundScroll.parent);
            clon.name = "Background_Loop";
            RectTransform rtClon = clon.GetComponent<RectTransform>();
            rtClon.anchoredPosition = new Vector2(backgroundScroll.rect.width, 0f);
            _bgSegments.Add(rtClon);
            rtClon.SetAsFirstSibling();
            backgroundScroll.SetAsFirstSibling();
        }
    }

    // =========================================================================
    // AUTO-VINCULACIÓN DE UI
    // =========================================================================

    private void VincularUIAutomaticamente()
    {
        if (objetivo == null)
            objetivo = BuscarEnEscena("Objetivo")?.GetComponent<RectTransform>()
                    ?? BuscarEnEscena("Cometa")?.GetComponent<RectTransform>()
                    ?? BuscarEnEscena("Star")?.GetComponent<RectTransform>();

        if (backgroundScroll == null)
            backgroundScroll = BuscarEnEscena("Background")?.GetComponent<RectTransform>();

        if (timerContainer == null)
            timerContainer = BuscarEnEscena("Time");

        if (timerContainer != null && textoTimer == null)
            textoTimer = timerContainer.GetComponentInChildren<TMP_Text>(true);

        if (precText == null)   precText   = BuscarEnEscena("PrecText")?.GetComponent<TMP_Text>();
        if (avanceText == null) avanceText = BuscarEnEscena("AvanceText")?.GetComponent<TMP_Text>();
        if (barFill == null)    barFill    = BuscarEnEscena("BarFill")?.GetComponent<Image>();
        if (textoSub == null)   textoSub   = BuscarEnEscena("TextoSub")?.GetComponent<TMP_Text>();
        if (panelDetalle == null) panelDetalle = BuscarEnEscena("detalle");

        if (overlayInicio == null)
            overlayInicio = BuscarEnEscena("OverlayInicio");

        if (overlayResult == null)
            overlayResult = BuscarEnEscena("OverlayResult");

        if (overlayResult != null)
        {
            if (titleRes == null)   titleRes   = overlayResult.transform.Find("TitleRes")?.GetComponent<TMP_Text>();
            if (subRes == null)     subRes     = overlayResult.transform.Find("Subres")?.GetComponent<TMP_Text>();
            if (percentRes == null) percentRes = overlayResult.transform.Find("PercentRes")?.GetComponent<TMP_Text>();
            if (btnAgain == null)   btnAgain   = (overlayResult.transform.Find("BtnAgain") ?? overlayResult.transform.Find("StartButton"))?.GetComponent<Button>();
            overlayResult.SetActive(false);
        }

        if (textoMensajeInicio == null)
        {
            GameObject cont = BuscarEnEscena("Contador");
            if (cont != null)
            {
                textoMensajeInicio = cont.GetComponentInChildren<TMP_Text>(true);
                cont.SetActive(false);
            }
        }
    }

    private GameObject BuscarEnEscena(string nombre)
    {
        GameObject directo = GameObject.Find(nombre);
        if (directo != null) return directo;

        string low = nombre.ToLower();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name.Trim().ToLower() == low && !string.IsNullOrEmpty(t.gameObject.scene.name))
                return t.gameObject;
        }
        return null;
    }

    // =========================================================================
    // INICIO / BIO-TRIGGER
    // =========================================================================

    public override void IniciarJuego()
    {
        if (_enConteo || juegoIniciado) return;
        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        _enConteo = true;

        if (overlayInicio != null) overlayInicio.SetActive(false);
        if (panelDetalle != null)  panelDetalle.SetActive(false);

        if (textoMensajeInicio != null)
        {
            textoMensajeInicio.gameObject.SetActive(true);
            if (textoMensajeInicio.transform.parent != null)
                textoMensajeInicio.transform.parent.gameObject.SetActive(true);
            textoMensajeInicio.color = Color.white;
        }

        for (int i = 3; i > 0; i--)
        {
            if (textoMensajeInicio != null)
                textoMensajeInicio.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (textoMensajeInicio != null)
        {
            textoMensajeInicio.text = "¡YA!";
            yield return new WaitForSecondsRealtime(0.7f);
            textoMensajeInicio.gameObject.SetActive(false);
        }

        // Reiniciar métricas
        _tiempoTranscurrido      = 0f;
        _segundosMirando         = 0f;
        _dwellTimer              = 0f;
        _votosPositivosPrecision = 0;
        _votosTotalesPrecision   = 0;
        _timerUI_Precision       = 0f;
        _esquinaActual           = 0;
        _esquinaAnterior         = -1;
        _enTransicion            = false;
        _framesTotales           = 0;
        _framesTargeteados       = 0;

        // Registrar aparición inicial
        IniciarMetricasEsquina();

        juegoIniciado = true;
        _enConteo = false;
    }

    // =========================================================================
    // UPDATE PRINCIPAL
    // =========================================================================

    protected override void Update()
    {
        base.Update();
        if (_juegoFinalizado) return;

        // Desplazar fondo SIEMPRE para que la escena se sienta viva
        DesplazarFondo();

        if (!juegoIniciado && !juegoPausado && !_enConteo)
        {
            ProcesarBioTrigger();
            return;
        }

        if (juegoIniciado && !juegoPausado)
        {
            _tiempoTranscurrido += Time.deltaTime;
            ActualizarUI();

            if (_tiempoTranscurrido >= duracionSesion)
            {
                FinalizarSesionLocal();
                return;
            }

            GestionarMovimientoCuadrado();
            ProcesarGaze();
            AplicarBrilloObjetivo();
        }
    }

    void FinalizarSesionLocal()
    {
        juegoIniciado = false;
        _juegoFinalizado = true;
        Time.timeScale = 1;

        // --- GUARDADO AUTOMÁTICO ---
        float precisionFinal = (_framesTotales > 0) ? (_framesTargeteados / (float)_framesTotales) * 100f : 0;
        float avanceFinal = (_segundosMirando / _tiempoTranscurrido) * 100f;
        int nivelFinal = Mathf.FloorToInt(_segundosMirando / 10f) + 1;
        this.puntuacion = Mathf.FloorToInt(avanceFinal * 10);

        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.GuardarPartida("Cometa Cuadrado", this.puntuacion, nivelFinal, precisionFinal, true, _tiempoTranscurrido);
        }
        // ---------------------------

        if (overlayResult != null)
        {
            overlayResult.SetActive(true);
            
            if (titleRes != null) titleRes.text = "¡SESIÓN COMPLETADA!";
            if (percentRes != null) percentRes.text = avanceFinal.ToString("F0") + "%";
            
            if (subRes != null) 
            {
                subRes.text = $"<line-height=140%><size=110%>¡Excelente enfoque!</size>\n" +
                              $"Has llegado al <color=#FFD700><b>Nivel {nivelFinal}</b></color>\n" +
                              $"<size=85%>Calidad visual: <color=#00FFFF>{precisionFinal:F0}%</color></size></line-height>";
            }

            if (btnAgain != null)
            {
                btnAgain.onClick.RemoveAllListeners();
                btnAgain.onClick.AddListener(() => {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                });
                
                var txtBtn = btnAgain.GetComponentInChildren<TMP_Text>();
                if (txtBtn != null) txtBtn.text = "¡OTRA VEZ!";
            }
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
        }
    }

    // =========================================================================
    // BIO-TRIGGER (PESTAÑEO)
    // =========================================================================

    private void ProcesarBioTrigger()
    {
        if (EyeTracker.Instance == null) return;

        var gaze = EyeTracker.Instance.LatestGazeData;
        bool detectado = gaze != null && (gaze.Left.GazeOriginValid || gaze.Right.GazeOriginValid);

        if (detectado)
        {
            _tiempoOjosCerrados = 0f;
            float dist = (gaze.Left.GazeOriginInUserCoordinates.z
                        + gaze.Right.GazeOriginInUserCoordinates.z) / 20f;
            _ojosEstablesParaIniciar = (dist >= 40f && dist <= 80f);
        }
        else if (_ojosEstablesParaIniciar)
        {
            _tiempoOjosCerrados += Time.deltaTime;
            if (_tiempoOjosCerrados >= 0.12f && _tiempoOjosCerrados <= 0.65f)
            {
                IniciarJuego();
                _ojosEstablesParaIniciar = false;
            }
        }

        if (textoSub != null)
        {
            textoSub.text  = _ojosEstablesParaIniciar
                ? "¡TE VEO! <b>Pestañea</b> para empezar"
                : "Buscando tus ojos... Mira al sensor";
            textoSub.color = _ojosEstablesParaIniciar ? Color.cyan : Color.white;
        }
    }

    // =========================================================================
    // MOVIMIENTO EN PATRÓN CUADRADO
    // =========================================================================

    /// <summary>
    /// Gestiona el ciclo completo: dwell en esquina → transición → nueva esquina.
    /// </summary>
    private void GestionarMovimientoCuadrado()
    {
        if (objetivo == null || _esquinas == null) return;

        if (_enTransicion)
        {
            // SmoothPursuit: mover hacia destino
            Vector2 destino = _esquinas[_esquinaActual];
            objetivo.anchoredPosition = Vector2.MoveTowards(
                objetivo.anchoredPosition, destino, pursuitSpeed * Time.deltaTime);

            if (Vector2.Distance(objetivo.anchoredPosition, destino) < 1f)
            {
                objetivo.anchoredPosition = destino;
                _enTransicion = false;
                _dwellTimer   = 0f;
                IniciarMetricasEsquina();
            }
        }
        else
        {
            // Acumular tiempo en la esquina y métricas de dwell
            _dwellTimer     += Time.deltaTime;
            _dwellOnTarget  += Time.deltaTime;

            if (_dwellTimer >= dwellTime)
                AvanzarSiguienteEsquina();
        }
    }

    /// <summary>Determina la siguiente esquina y ejecuta la transición correspondiente.</summary>
    private void AvanzarSiguienteEsquina()
    {
        if (_esquinas == null) return;

        RegistrarMetricasTransicion();

        _esquinaAnterior = _esquinaActual;

        if (squareMode == SquareMode.Sequential)
        {
            _esquinaActual = (_esquinaActual + 1) % _esquinas.Length;
        }
        else // Random
        {
            int siguiente = _esquinaActual;
            int intentos  = 0;
            while (siguiente == _esquinaActual && intentos < 10)
            {
                siguiente = Random.Range(0, _esquinas.Length);
                intentos++;
            }
            _esquinaActual = siguiente;
        }

        if (movementType == SquareMovementType.Saccadic)
        {
            // Teletransporte inmediato
            objetivo.anchoredPosition = _esquinas[_esquinaActual];
            _dwellTimer  = 0f;
            _enTransicion = false;
            IniciarMetricasEsquina();
        }
        else // SmoothPursuit
        {
            _enTransicion = true;
        }
    }

    // =========================================================================
    // MÉTRICAS CLÍNICAS
    // =========================================================================

    /// <summary>Registra el momento de llegada a la nueva esquina para medir latencia sacádica.</summary>
    private void IniciarMetricasEsquina()
    {
        _tiempoApareció  = Time.time;
        _gazeAterrizó    = false;
        _saccadeLatency  = 0f;
        _landingError    = 0f;
        _dwellOnTarget   = 0f;
    }

    /// <summary>
    /// Registra las métricas por transición en el log de Unity.
    /// En una integración completa estas se guardarían en GestorPaciente.
    /// </summary>
    private void RegistrarMetricasTransicion()
    {
        string[] nombresEsquinas = { "TopLeft", "TopRight", "BottomRight", "BottomLeft" };
        string desde = (_esquinaAnterior >= 0 && _esquinaAnterior < nombresEsquinas.Length)
            ? nombresEsquinas[_esquinaAnterior] : "Inicio";
        string hasta = (_esquinaActual < nombresEsquinas.Length)
            ? nombresEsquinas[_esquinaActual] : "?";

        Debug.Log(string.Format(
            "<color=cyan>[CometaCuadrado]</color> Transición {0}→{1} | Latencia: {2:F3}s | Error Aterrizaje: {3:F1}px | Dwell: {4:F2}s",
            desde, hasta, _saccadeLatency, _landingError, _dwellOnTarget));
    }

    // =========================================================================
    // FONDO INFINITO
    // =========================================================================

    private void DesplazarFondo()
    {
        foreach (RectTransform seg in _bgSegments)
        {
            if (seg == null) continue;
            seg.anchoredPosition += Vector2.left * velocidadFondo * Time.deltaTime;
            float ancho = seg.rect.width;
            if (seg.anchoredPosition.x < -ancho)
                seg.anchoredPosition += new Vector2(ancho * 2f, 0f);
        }
    }

    // =========================================================================
    // BRILLO ANIMADO
    // =========================================================================

    private void AplicarBrilloObjetivo()
    {
        if (objetivo == null) return;
        float escala = 1f + Mathf.Sin(Time.time * 8.5f) * 0.12f;
        objetivo.localScale = new Vector3(escala, escala, 1f);
    }

    // =========================================================================
    // PROCESAMIENTO DE GAZE
    // =========================================================================

    private void ProcesarGaze()
    {
        _votosTotalesPrecision++;
        _timerUI_Precision += Time.deltaTime;

        Vector2 gazeScreen = Vector2.zero;
        bool tieneGaze = false;

        if (TobiiGazeProvider.Instance != null)
        {
            gazeScreen = TobiiGazeProvider.Instance.GazePositionScreen;
            tieneGaze  = TobiiGazeProvider.Instance.HasGaze;
        }
        else if (EyeTracker.Instance != null)
        {
            var gd = EyeTracker.Instance.LatestGazeData;
            if (gd != null && (gd.Left.GazePointValid || gd.Right.GazePointValid))
            {
                float rx = (gd.Left.GazePointOnDisplayArea.x + gd.Right.GazePointOnDisplayArea.x) / 2f;
                float ry = (gd.Left.GazePointOnDisplayArea.y + gd.Right.GazePointOnDisplayArea.y) / 2f;
                gazeScreen = new Vector2(rx * Screen.width, (1f - ry) * Screen.height);
                tieneGaze  = true;
            }
        }

        if (!tieneGaze || objetivo == null) return;

        _votosPositivosPrecision++;

        // Hit-test sobre el RectTransform del objetivo
        // Conversión exacta a píxeles de pantalla
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, objetivo.position);
        float padding = 80f;
        Vector2 size = Vector2.Scale(objetivo.rect.size, objetivo.lossyScale) + new Vector2(padding, padding);

        Rect hitBox = new Rect(
            screenPoint.x - size.x / 2f,
            screenPoint.y - size.y / 2f,
            size.x, size.y);

        bool mirandoAlObjetivo = hitBox.Contains(gazeScreen);

        _framesTotales++;
        if (mirandoAlObjetivo)
        {
            _segundosMirando += Time.deltaTime;
            _framesTargeteados++;
        }

        // Registrar latencia sacádica (primer gaze tras llegar a esquina)
        if (!_gazeAterrizó && mirandoAlObjetivo)
        {
            _saccadeLatency = Time.time - _tiempoApareció;
            _landingError   = Vector2.Distance(gazeScreen, screenPoint);
            _gazeAterrizó   = true;
        }
    }

    // =========================================================================
    // ACTUALIZACIÓN DE UI
    // =========================================================================

    private void ActualizarUI()
    {
        // Cronómetro
        if (textoTimer != null)
        {
            int min = Mathf.FloorToInt(_tiempoTranscurrido / 60f);
            int seg = Mathf.FloorToInt(_tiempoTranscurrido % 60f);
            textoTimer.text = string.Format("{0:00}:{1:00}", min, seg);
        }

        // Precisión (ventana de 1 segundo)
        if (precText != null)
        {
            float calidad = (_votosTotalesPrecision > 0)
                ? (_votosPositivosPrecision / (float)_votosTotalesPrecision) * 100f
                : 0f;
            precText.text  = Mathf.RoundToInt(calidad).ToString();
            precText.color = calidad > 70f ? Color.green : Color.yellow;

            if (_timerUI_Precision >= 1f)
            {
                _timerUI_Precision       = 0f;
                _votosPositivosPrecision = 0;
                _votosTotalesPrecision   = 0;
            }
        }

        // Progreso de nivel (cada 15s de seguimiento = un ciclo)
        // AVANCE RELATIVO AL NIVEL (Cada 10s sube un nivel)
        float progresoNivel = (_segundosMirando % 10.001f) / 10f * 100f;
        int nivelActual = Mathf.FloorToInt(_segundosMirando / 10f) + 1;

        if (avanceText != null)
        {
            avanceText.text = (nivelActual > 1)
                ? "Lvl " + nivelActual
                : progresoNivel.ToString("F0") + "%";
        }

        if (barFill != null)
            barFill.fillAmount = (_segundosMirando % 10f) / 10f;
    }
}
