using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Tobii.Research.Unity;

public class EstrellaLinealManager : BaseActividad
{
    [Header("Objetos de Juego")]
    public RectTransform star;
    public RectTransform distractor;
    public RectTransform backgroundScroll; // <--- Nuevo: Para el fondo infinito
    public GameObject timerContainer; 
    public TMP_Text textoTimer; 
    public TMP_Text precText;
    public TMP_Text avanceText;
    public Image barFill; 
    public TMP_Text textoSub; 
    public GameObject panelDetalle; 
    
    [Header("Elementos a Ocultar en Partida")]
    public GameObject extraTitle;
    public GameObject barBG;
    public GameObject precBubble;
    public GameObject avanceBubble;

    [Header("Overlay Resultados")]
    public GameObject overlayResult;
    public TMP_Text titleRes;
    public TMP_Text subRes;
    public TMP_Text percentRes;
    public Button btnAgain;

    [Header("Ajustes de Movimiento")]
    public float velocidadStar = 800f; // Velocidad pro para seguimiento fluido
    public float velocidadDistractor = 600f;
    public float velocidadFondo = 150f; 
    public float zigzagAmplitud = 200f;
    public float zigzagFrecuencia = 4f;

    [Header("Configuración de Tiempo")]
    public float duracionSesion = 60f;
    private bool _juegoFinalizado = false;

    private float _tiempoTranscurrido = 0f;
    private int _framesTargeteados = 0;
    private int _framesTotales = 0;
    private float _segundosMirando = 0f;
    private Vector2 _dirDistractor = Vector2.left;
    
    private RectTransform[] _convoyEstrellas; 
    private List<RectTransform> _bgSegments = new List<RectTransform>(); // <--- Cache de segmentos de fondo

    // Control de parpadeo para iniciar
    private float _tiempoOjosCerrados = 0f;
    private bool _ojosEstablesParaIniciar = false;
    private bool _enConteo = false;
    private Vector2 _gazeDebugPos; 

    // Métricas de tiempo de actualización
    private float _timerUI_Precision = 0f;
    private int _votosPositivosPrecision = 0;
    private int _votosTotalesPrecision = 0;

    protected override void Start()
    {
        AutoVincularSeguimiento(); 
        base.Start();              
        
        if (overlayInicio != null) overlayInicio.SetActive(true);
        PreconfigurarPosiciones();
    }

    void VincularUIAutomaticamente()
    {
        // Punteros directos según la jerarquía del usuario
        if (star == null) star = BuscarObjetoPotente("Star")?.GetComponent<RectTransform>();
        if (distractor == null) distractor = BuscarObjetoPotente("Distractor")?.GetComponent<RectTransform>();
        
        if (timerContainer == null) timerContainer = BuscarObjetoPotente("Time");
        if (textoTimer == null) textoTimer = timerContainer?.GetComponent<TMP_Text>() ?? timerContainer?.GetComponentInChildren<TMP_Text>();

        if (precText == null) precText = BuscarObjetoPotente("PrecText")?.GetComponent<TMP_Text>();
        if (avanceText == null) avanceText = BuscarObjetoPotente("AvanceText")?.GetComponent<TMP_Text>();
        if (barFill == null) barFill = BuscarObjetoPotente("BarFill")?.GetComponent<Image>();
        
        if (textoSub == null) {
            // Buscamos 'Sub' con mayúscula como en la foto
            textoSub = BuscarObjetoPotente("Sub")?.GetComponent<TMP_Text>();
        }
        
        if (textoMensajeInicio == null) {
            GameObject contObj = BuscarObjetoPotente("Contador");
            if (contObj != null) textoMensajeInicio = contObj.GetComponentInChildren<TMP_Text>(true);
        }
        
        if (botonIniciar == null) botonIniciar = BuscarObjetoPotente("StartButton")?.GetComponent<Button>();
        if (botonSalir == null) botonSalir = BuscarObjetoPotente("VolverBtn")?.GetComponent<Button>();
        
        // Bubbles y Barras que deben desaparecer
        if (extraTitle == null) extraTitle = BuscarObjetoPotente("Title");
        if (barBG == null) barBG = BuscarObjetoPotente("BarBG");
        if (precBubble == null) precBubble = BuscarObjetoPotente("PrecBubble");
        if (avanceBubble == null) avanceBubble = BuscarObjetoPotente("AvanceBubble");

        if (overlayInicio == null) overlayInicio = BuscarObjetoPotente("OverlayInicio");
        if (overlayResult == null) overlayResult = BuscarObjetoPotente("OverlayResult");
        if (panelDetalle == null) panelDetalle = BuscarObjetoPotente("detalle");
        
        // Forzar activación del inicio para que el Tobii pueda empezar a buscar ojos
        if (overlayInicio != null) overlayInicio.SetActive(true);

        if (overlayResult != null)
        {
            titleRes = titleRes ?? overlayResult.transform.Find("TitleRes")?.GetComponent<TMP_Text>();
            percentRes = percentRes ?? overlayResult.transform.Find("PercentRes")?.GetComponent<TMP_Text>();
            btnAgain = btnAgain ?? overlayResult.GetComponentInChildren<Button>(true);
            
            if (btnAgain != null && btnAgain.name != "BtnAgain") {
                Button b = overlayResult.transform.Find("BtnAgain")?.GetComponent<Button>();
                if (b != null) btnAgain = b;
            }
            overlayResult.SetActive(false); 
        }
    }

    void AutoVincularSeguimiento()
    {
        VincularUIAutomaticamente();
    }

    GameObject BuscarObjetoPotente(string nombre)
    {
        GameObject obj = GameObject.Find(nombre);
        if (obj != null) return obj;
        string busqueda = nombre.ToLower();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            if (t.name.Trim().ToLower() == busqueda && !string.IsNullOrEmpty(t.gameObject.scene.name)) return t.gameObject;
        }
        return null;
    }

    void PreconfigurarPosiciones()
    {
        if (star != null) 
        {
            // Solo una estrella, comienza desde la izquierda
            star.anchoredPosition = new Vector2(-1100, 0);
            _convoyEstrellas = new RectTransform[] { star };
        }
        if (distractor != null) distractor.anchoredPosition = new Vector2(400, 100);
        
        // Duplicar el fondo si es necesario para el scroll continuo
        if (backgroundScroll != null)
        {
            _bgSegments.Clear();
            _bgSegments.Add(backgroundScroll);

            GameObject bgClon = Instantiate(backgroundScroll.gameObject, backgroundScroll.parent);
            bgClon.name = "Background_Loop";
            RectTransform rtBg = bgClon.GetComponent<RectTransform>();
            rtBg.anchoredPosition = new Vector2(backgroundScroll.rect.width, 0);
            
            _bgSegments.Add(rtBg);

            rtBg.SetAsFirstSibling();
            backgroundScroll.SetAsFirstSibling();
        }
    }

    public override void IniciarJuego()
    {
        if (_enConteo || juegoIniciado) return;
        StartCoroutine(RutinaCountdown());
    }

    IEnumerator RutinaCountdown()
    {
        _enConteo = true;
        if (overlayInicio != null) overlayInicio.SetActive(false);
        if (panelDetalle != null) panelDetalle.SetActive(false);

        if (textoMensajeInicio != null) {
            textoMensajeInicio.gameObject.SetActive(true);
            textoMensajeInicio.transform.parent.gameObject.SetActive(true); 
            textoMensajeInicio.color = Color.white;
        }

        for (int i = 3; i > 0; i--)
        {
            if (textoMensajeInicio != null) textoMensajeInicio.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (textoMensajeInicio != null) { textoMensajeInicio.text = "¡YA!"; yield return new WaitForSecondsRealtime(0.7f); }

        if (textoMensajeInicio != null) textoMensajeInicio.gameObject.SetActive(false); 
        
        _tiempoTranscurrido = 0f;
        _framesTargeteados = 0;
        _framesTotales = 0;
        _segundosMirando = 0f; 
        
        ToggleUI(false); // <--- OCULTAR AL EMPEZAR

        juegoIniciado = true;
        _enConteo = false;
    }

    void ToggleUI(bool visible)
    {
        if (extraTitle != null) extraTitle.SetActive(visible);
        if (botonSalir != null) botonSalir.gameObject.SetActive(visible);
        if (barBG != null) barBG.SetActive(visible);
        if (precBubble != null) precBubble.SetActive(visible);
        if (avanceBubble != null) avanceBubble.SetActive(visible);
        
        // El timer SIEMPRE visible según tu petición
        if (timerContainer != null) timerContainer.SetActive(true);
    }

    protected override void Update()
    {
        base.Update();
        if (_juegoFinalizado) return;

        // Scroll de fondo SIEMPRE para que la escena se sienta viva
        // Desplazar fondo SIEMPRE para que la escena se sienta viva
        DesplazarFondo();

        if (!juegoIniciado && !juegoPausado && !_enConteo) { ManejarPestañeoInicio(); return; }

        if (juegoIniciado && !juegoPausado)
        {
            _tiempoTranscurrido += Time.deltaTime;
            ActualizarUI();

            // Verificación de fin de tiempo
            if (_tiempoTranscurrido >= duracionSesion)
            {
                FinalizarSesionLocal();
                return;
            }

            // MoverMundo(); // Movido parcialmente arriba
            MoverObjetosAdicionales(); // Nuevo método para el resto
            ProcesarSeguimientoOcular();
            AplicarBrilloEstrella();
        }
    }

    void FinalizarSesionLocal()
    {
        juegoIniciado = false;
        _juegoFinalizado = true;
        Time.timeScale = 1; 
        
        // --- GUARDADO AUTOMÁTICO ---
        float precisionFinal = (_framesTotales > 0) ? (_framesTargeteados / (float)_framesTotales) * 100f : 0;
        // El porcentaje de avance ahora es relativo al tiempo total de la sesión (Máximo 100%)
        float avanceFinal = (_segundosMirando / _tiempoTranscurrido) * 100f;
        int nivelAlcanzado = Mathf.FloorToInt(_segundosMirando / 10f) + 1;
        this.puntuacion = Mathf.FloorToInt(avanceFinal * 10);
        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.GuardarPartida("Estrella Lineal", this.puntuacion, nivelAlcanzado, precisionFinal, true, _tiempoTranscurrido);
        }
        // ---------------------------------------------------

        ToggleUI(true); 

        if (overlayResult != null)
        {
            overlayResult.SetActive(true);
            
            if (titleRes != null) titleRes.text = "¡SESIÓN COMPLETADA!";
            if (percentRes != null) percentRes.text = avanceFinal.ToString("F0") + "%";
            
            if (subRes != null) 
            {
                subRes.text = $"<line-height=140%><size=110%>¡Excelente enfoque!</size>\n" +
                              $"Has llegado al <color=#FFD700><b>Nivel {nivelAlcanzado}</b></color>\n" +
                              $"<size=85%>Calidad visual: <color=#00FFFF>{precisionFinal:F0}%</color></size></line-height>";
            }
           
            if (btnAgain != null)
            {
                btnAgain.onClick.RemoveAllListeners();
                btnAgain.onClick.AddListener(() => {
                    Debug.Log("<color=orange><b>[SISTEMA]</b></color> Botón OTRA VEZ pulsado. Reiniciando juego...");
                    Time.timeScale = 1.0f;
                    string nombreEscena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscena);
                });
                
                var txtBtn = btnAgain.GetComponentInChildren<TMP_Text>();
                if (txtBtn != null) txtBtn.text = "¡OTRA VEZ!";
            }
        }
        else
        {
            Debug.LogWarning("<color=red><b>[ERROR]</b></color> No se encontró OverlayResult. Volviendo al Home.");
            Time.timeScale = 1.0f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
        }
    }

    void ManejarPestañeoInicio()
    {
        var gaze = EyeTracker.Instance.LatestGazeData;
        // Usamos GazePointValid que es lo que usa la calibración oficial
        bool detectado = gaze != null && (gaze.Left.GazePointValid || gaze.Right.GazePointValid);
        if (detectado)
        {
            _tiempoOjosCerrados = 0f;
            // Los datos de Tobii Pro vienen en mm. Dividimos por 10 para tener cm.
            float distL = gaze.Left.GazeOriginInUserCoordinates.z / 10f;
            float distR = gaze.Right.GazeOriginInUserCoordinates.z / 10f;
            float distMedia = (distL + distR) / 2f;
            
            // Rango de éxito: entre 40cm y 90cm
            _ojosEstablesParaIniciar = (distMedia >= 35 && distMedia <= 95);
            
            if (_ojosEstablesParaIniciar) {
                // Debug.Log("<color=cyan><b>[TOBII]</b> Ojos detectados a " + distMedia.ToString("F0") + " cm. ¡PUEDES PESTAÑEAR!</color>");
            }
        }
        else if (_ojosEstablesParaIniciar)
        {
            _tiempoOjosCerrados += Time.deltaTime;
            if (_tiempoOjosCerrados >= 0.10f && _tiempoOjosCerrados <= 0.70f) 
            { 
                Debug.Log("<color=green><b>[TOBII]</b> Pestañeo detectado. Iniciando actividad...</color>");
                IniciarJuego(); 
                _ojosEstablesParaIniciar = false; 
            }
        }

        if (textoSub != null)
        {
            textoSub.text = _ojosEstablesParaIniciar ? "¡TE VEO! <b>Pestañea</b> ahora" : "Buscando ojos... (Ponte a ~60cm)";
            textoSub.color = _ojosEstablesParaIniciar ? Color.cyan : Color.white;
        }
    }

    void ActualizarUI()
    {
        if (textoTimer != null) {
            int minutos = Mathf.FloorToInt(_tiempoTranscurrido / 60f);
            int segundos = Mathf.FloorToInt(_tiempoTranscurrido % 60f);
            textoTimer.text = string.Format("{0:00}:{1:00}", minutos, segundos);
        }

        if (precText != null) 
        {
            float calidatFoco = (_votosTotalesPrecision > 0) ? (_votosPositivosPrecision / (float)_votosTotalesPrecision) * 100f : 0;
            precText.text = calidatFoco.ToString("F0");
            precText.color = calidatFoco > 70 ? Color.green : Color.yellow;
            if (_timerUI_Precision >= 1f) { _timerUI_Precision = 0; _votosPositivosPrecision = 0; _votosTotalesPrecision = 0; }
        }

        _votosTotalesPrecision++;
        var gd = EyeTracker.Instance.LatestGazeData;
        if (gd != null && (gd.Left.GazeOriginValid || gd.Right.GazeOriginValid)) _votosPositivosPrecision++;

        // AVANCE RELATIVO AL NIVEL (Cada 10s sube un nivel)
        float progresoNivel = (_segundosMirando % 10.001f) / 10f * 100f;
        int nivelActual = Mathf.FloorToInt(_segundosMirando / 10f) + 1;
        
        if (avanceText != null) 
            avanceText.text = (nivelActual > 1) ? "Lvl " + nivelActual : progresoNivel.ToString("F0") + "%";
            
        if (barFill != null) barFill.fillAmount = (_segundosMirando % 10f) / 10f;
    }

    void MoverObjetosAdicionales()
    {
        // 2. MOVIMIENTO DE ÚNICA ESTRELLA
        if (_convoyEstrellas != null)
        {
            float limiteBorde = (Screen.width / 2f); 
            foreach (var s in _convoyEstrellas)
            {
                s.anchoredPosition += Vector2.right * velocidadStar * Time.deltaTime;
                if (s.anchoredPosition.x > limiteBorde) s.anchoredPosition = new Vector2(-limiteBorde, s.anchoredPosition.y);
            }
        }

        // 3. DISTRACTOR (Zigzag + Scroll de mundo)
        if (distractor != null)
        {
            float limiteBordeDist = (Screen.width / 2f) + 80f;
            float sinY = Mathf.Sin(Time.time * zigzagFrecuencia) * zigzagAmplitud;
            distractor.anchoredPosition += Vector2.left * (velocidadDistractor + velocidadFondo) * Time.deltaTime;
            distractor.anchoredPosition = new Vector2(distractor.anchoredPosition.x, sinY);
            if (distractor.anchoredPosition.x < -limiteBordeDist) distractor.anchoredPosition = new Vector2(limiteBordeDist, 0);
        }
    }

    void DesplazarFondo()
    {
        foreach (RectTransform seg in _bgSegments)
        {
            if (seg == null) continue;
            seg.anchoredPosition += Vector2.left * velocidadFondo * Time.deltaTime;
            float width = seg.rect.width;
            if (seg.anchoredPosition.x < -width)
                seg.anchoredPosition += new Vector2(width * 2f, 0f);
        }
    }

    void OnGUI()
    {
        // Solo dibujar el punto si el juego está en marcha y tenemos datos de gaze
        if (juegoIniciado && _gazeDebugPos != Vector2.zero)
        {
            GUI.color = new Color(1, 0, 0, 0.7f); // Rojo semitransparente
            // Convertir de coordenadas de pantalla Unity (0,0 abajo izq) a OnGUI (0,0 arriba izq)
            Rect gazeRect = new Rect(_gazeDebugPos.x - 10, Screen.height - _gazeDebugPos.y - 10, 20, 20);
            GUI.DrawTexture(gazeRect, Texture2D.whiteTexture);
            
            // Opcional: Dibujar coordenadas para ver si llegan a Screen.width
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 300, 20), $"Gaze Pos: {_gazeDebugPos.x:F0}, {_gazeDebugPos.y:F0} | Screen: {Screen.width}x{Screen.height}");
        }
    }

    void AplicarBrilloEstrella()
    {
        if (_convoyEstrellas == null) return;
        float escala = 1.0f + Mathf.Sin(Time.time * 8.5f) * 0.12f;
        foreach (var s in _convoyEstrellas) s.localScale = new Vector3(escala, escala, 1f);
    }

    void ProcesarSeguimientoOcular()
    {
        _framesTotales++;
        var gaze = EyeTracker.Instance.LatestGazeData;
        if (gaze == null) return;

        if (gaze.Left.GazePointValid || gaze.Right.GazePointValid)
        {
            // Promedio directo del SDK
            Vector2 viewPos = new Vector2(
                (gaze.Left.GazePointOnDisplayArea.x + gaze.Right.GazePointOnDisplayArea.x) / 2f,
                (gaze.Left.GazePointOnDisplayArea.y + gaze.Right.GazePointOnDisplayArea.y) / 2f
            );
            
            // Mapeo crudo a píxeles de ventana
            _gazeDebugPos = new Vector2(viewPos.x * Screen.width, (1f - viewPos.y) * Screen.height);
            
            if (_convoyEstrellas != null)
            {
                foreach (var s in _convoyEstrellas)
                {
                    // Detectamos la cámara del canvas de forma dinámica para el Raycast
                    Canvas rootCanvas = s.GetComponentInParent<Canvas>();
                    Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null;

                    // Esta función oficial de Unity maneja el escalado del Canvas automaticamente
                    if (RectTransformUtility.RectangleContainsScreenPoint(s, _gazeDebugPos, uiCam))
                    {
                        _framesTargeteados++;
                        _segundosMirando += Time.deltaTime;
                        break;
                    }
                }
            }
        }
    }
}
