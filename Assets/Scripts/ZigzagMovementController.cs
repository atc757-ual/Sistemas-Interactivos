using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Tobii.Research.Unity;

/// <summary>
/// Controlador de la actividad terapéutica de patrón Zigzag (MeteoroZigzag).
/// Hereda de BaseActividad para reutilizar el sistema de pausa, guardado y Tobii.
/// </summary>
public class ZigzagMovementController : BaseActividad
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    /// <summary>Tipo de movimiento zigzag.</summary>
    public enum ZigzagType { Smooth, Angular }

    /// <summary>Nivel de dificultad preconfigurado.</summary>
    public enum DifficultyLevel { Easy, Medium, Hard }

    // =========================================================================
    // INSPECTOR — OBJETOS DE ESCENA
    // =========================================================================

    [Header("Objetos de Escena")]
    [Tooltip("RectTransform del meteoro que el paciente debe seguir.")]
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
    // INSPECTOR — CONFIG ZIGZAG
    // =========================================================================

    [Header("Zigzag Config")]
    [Tooltip("Smooth=senoidal continuo / Angular=waypoints diagonales discretos.")]
    public ZigzagType zigzagType = ZigzagType.Smooth;

    [Tooltip("Nivel de dificultad. Sobreescribe frequency, amplitude y horizontalSpeed.")]
    public DifficultyLevel difficulty = DifficultyLevel.Medium;

    [Tooltip("Frecuencia de oscilación en Hz.")]
    public float frequency = 0.5f;

    [Tooltip("Amplitud del zigzag en unidades UI (píxeles).")]
    public float amplitude = 220f;

    [Tooltip("Velocidad horizontal del objetivo en unidades UI/s.")]
    public float horizontalSpeed = 150f;

    [Tooltip("Velocidad del scroll de fondo en unidades UI/s.")]
    public float velocidadFondo = 80f;

    [Tooltip("Si true, el objetivo regresa al borde izquierdo al salir por la derecha.")]
    public bool loopHorizontal = true;

    [Header("Configuración de Tiempo")]
    public float duracionSesion = 60f;
    private bool _juegoFinalizado = false;

    // =========================================================================
    // ESTADO INTERNO
    // =========================================================================

    private float _tiempoTranscurrido;
    private float _segundosMirando;
    private int _framesTotales;
    private int _framesTargeteados;

    private float _timerUI_Precision;
    private int _votosPositivosPrecision;
    private int _votosTotalesPrecision;

    private float _tiempoOjosCerrados;
    private bool _ojosEstablesParaIniciar;
    private bool _enConteo;

    private List<RectTransform> _bgSegments = new List<RectTransform>();

    // Waypoints para modo Angular
    private Vector2[] _waypoints;
    private int _waypointActual;

    // Semiancho/semialto del canvas (valores por defecto para 1920x1080)
    private float _canvasHalfW = 960f;

    // =========================================================================
    // INICIALIZACIÓN
    // =========================================================================

    protected override void Start()
    {
        AplicarPresetDificultad();
        VincularUIAutomaticamente();
        base.Start();

        if (overlayInicio != null) overlayInicio.SetActive(true);
        PreconfigurarObjetivo();
        PrepararWaypointsAngular();
    }

    private void AplicarPresetDificultad()
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                frequency = 0.3f; amplitude = 150f; horizontalSpeed = 100f;
                break;
            case DifficultyLevel.Hard:
                frequency = 0.8f; amplitude = 300f; horizontalSpeed = 200f;
                break;
            default: // Medium
                frequency = 0.5f; amplitude = 220f; horizontalSpeed = 150f;
                break;
        }
    }

    private void VincularUIAutomaticamente()
    {
        if (objetivo == null)
            objetivo = BuscarEnEscena("Objetivo")?.GetComponent<RectTransform>()
                    ?? BuscarEnEscena("Meteoro")?.GetComponent<RectTransform>()
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

    private void PreconfigurarObjetivo()
    {
        if (objetivo != null)
            objetivo.anchoredPosition = new Vector2(-_canvasHalfW - 100f, 0f);

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

    private void PrepararWaypointsAngular()
    {
        int totalPuntos = 14;
        _waypoints = new Vector2[totalPuntos];
        float stepX = (_canvasHalfW * 2f) / (totalPuntos - 1);

        for (int i = 0; i < totalPuntos; i++)
        {
            float x = -_canvasHalfW + i * stepX;
            float y = (i % 2 == 0) ? amplitude : -amplitude;
            _waypoints[i] = new Vector2(x, y);
        }

        _waypointActual = 0;
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

        _tiempoTranscurrido      = 0f;
        _segundosMirando         = 0f;
        _votosPositivosPrecision = 0;
        _votosTotalesPrecision   = 0;
        _framesTotales           = 0;
        _framesTargeteados       = 0;

        juegoIniciado = true;
        _enConteo = false;
    }

    // =========================================================================
    // UPDATE
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

            MoverObjetivo();
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
            GestorPaciente.Instance.GuardarPartida("Meteoro Zigzag", this.puntuacion, nivelFinal, precisionFinal, true, _tiempoTranscurrido);
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
            UnityEngine.SceneManagement.SceneManager.LoadScene("Activities");
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
    // MOVIMIENTO DEL OBJETIVO
    // =========================================================================

    private void MoverObjetivo()
    {
        if (objetivo == null) return;

        if (zigzagType == ZigzagType.Smooth)
            MoverSmooth();
        else
            MoverAngular();
    }

    /// <summary>Zigzag senoidal: Y oscila, X avanza linealmente con wrap.</summary>
    private void MoverSmooth()
    {
        float y = Mathf.Sin(Time.time * frequency * 2f * Mathf.PI) * amplitude;
        float x = objetivo.anchoredPosition.x + horizontalSpeed * Time.deltaTime;

        if (loopHorizontal && x > _canvasHalfW + 100f)
            x = -_canvasHalfW - 100f;

        objetivo.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>Zigzag angular: se desplaza por waypoints diagonales con MoveTowards.</summary>
    private void MoverAngular()
    {
        if (_waypoints == null || _waypoints.Length == 0) return;

        Vector2 destino = _waypoints[_waypointActual];
        objetivo.anchoredPosition = Vector2.MoveTowards(
            objetivo.anchoredPosition, destino, horizontalSpeed * Time.deltaTime);

        if (Vector2.Distance(objetivo.anchoredPosition, destino) < 1.5f)
        {
            _waypointActual = (_waypointActual + 1) % _waypoints.Length;
            if (_waypointActual == 0)
                objetivo.anchoredPosition = new Vector2(-_canvasHalfW - 100f, amplitude);
        }
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
        Vector2 tamano = Vector2.Scale(objetivo.rect.size, objetivo.lossyScale) + new Vector2(padding, padding);

        Rect hitBox = new Rect(
            screenPoint.x - tamano.x / 2f,
            screenPoint.y - tamano.y / 2f,
            tamano.x, tamano.y);

        _framesTotales++;
        if (hitBox.Contains(gazeScreen))
        {
            _segundosMirando += Time.deltaTime;
            _framesTargeteados++;
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
