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
    private float _tiempoInstruccionesMostradas = 0f;
    private int _dirEstrella = 1; // 1 = derecha, -1 = izquierda
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
        if (star == null) star = BuscarObjetoInactivo("Star")?.GetComponent<RectTransform>();
        if (distractor == null) distractor = BuscarObjetoInactivo("Distractor")?.GetComponent<RectTransform>();
        if (timerContainer == null) timerContainer = BuscarObjetoInactivo("Time");
        if (textoTimer == null) textoTimer = timerContainer?.GetComponent<TMP_Text>() ?? timerContainer?.GetComponentInChildren<TMP_Text>();
        if (precText == null) precText = BuscarObjetoInactivo("PrecText")?.GetComponent<TMP_Text>();
        if (avanceText == null) avanceText = BuscarObjetoInactivo("AvanceText")?.GetComponent<TMP_Text>();
        if (barFill == null) barFill = BuscarObjetoInactivo("BarFill")?.GetComponent<Image>();
        if (textoSub == null) textoSub = BuscarObjetoInactivo("Sub")?.GetComponent<TMP_Text>();
        if (textoMensajeInicio == null) {
            GameObject contObj = BuscarObjetoInactivo("Contador");
            if (contObj != null) textoMensajeInicio = contObj.GetComponentInChildren<TMP_Text>(true);
        }
        if (botonIniciar == null) botonIniciar = BuscarObjetoInactivo("StartButton")?.GetComponent<Button>();
        if (botonSalir == null) botonSalir = BuscarObjetoInactivo("VolverBtn")?.GetComponent<Button>() ?? BuscarObjetoInactivo("BackBtn")?.GetComponent<Button>();
        if (precBubble == null) precBubble = BuscarObjetoInactivo("PrecBubble");
        if (avanceBubble == null) avanceBubble = BuscarObjetoInactivo("AvanceBubble");
        if (overlayInicio == null) overlayInicio = BuscarObjetoInactivo("OverlayInicio");
        if (overlayResult == null) overlayResult = BuscarObjetoInactivo("OverlayResult");
        if (panelDetalle == null) panelDetalle = BuscarObjetoInactivo("detalle");
        if (backgroundScroll == null) backgroundScroll = BuscarObjetoInactivo("BG")?.GetComponent<RectTransform>() ?? BuscarObjetoInactivo("Background")?.GetComponent<RectTransform>();
        
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
        
        // --- PERSONALIZACIÓN DE USUARIO ---
        // Buscar el objeto "Inst" para reemplazar "Astronauta" por el nombre del usuario
        GameObject instObj = BuscarObjetoInactivo("Inst");
        if (instObj != null) {
            TMP_Text instText = instObj.GetComponent<TMP_Text>();
            if (instText != null && instText.text.Contains("Astronauta")) {
                string nombre = GestorPaciente.Instance != null ? GestorPaciente.Instance.GetNombrePacienteFormateado() : "Astronauta";
                instText.text = instText.text.Replace("Astronauta", nombre);
            }
        }
    }

    void AutoVincularSeguimiento()
    {
        VincularUIAutomaticamente();
    }

    // Nota: El método BuscarObjetoPotente ha sido reemplazado por BuscarObjetoInactivo heredado de BaseActividad

    void PreconfigurarPosiciones()
    {
        // Ocultar el temporizador antes de que empiece el juego
        if (timerContainer != null) timerContainer.SetActive(false);

        if (star != null) 
        {
            // Solo vinculamos la estrella, la posición se cambiará cuando termine el contador
            _convoyEstrellas = new RectTransform[] { star };
        }
        if (distractor != null) {
            distractor.anchoredPosition = new Vector2(400, 100);
            distractor.gameObject.SetActive(false); // Ocultar hasta que termine el contador
        }
        
        // Duplicar el fondo si es necesario para el scroll continuo
        if (backgroundScroll != null)
        {
            _bgSegments.Clear();
            _bgSegments.Add(backgroundScroll);

            // Si el ancho es 0 (a veces pasa en el primer frame), usamos el ancho de pantalla como backup
            float anchoFondo = backgroundScroll.rect.width > 0 ? backgroundScroll.rect.width : Screen.width;

            GameObject bgClon = Instantiate(backgroundScroll.gameObject, backgroundScroll.parent);
            bgClon.name = "Background_Loop";
            RectTransform rtBg = bgClon.GetComponent<RectTransform>();
            rtBg.anchoredPosition = new Vector2(anchoFondo, 0);
            
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
        
        // Mantener OverlayInicio encendido pero APAGAR todos sus hijos excepto el Contador
        if (overlayInicio != null) {
            foreach (Transform child in overlayInicio.transform) {
                // Asumimos que el contador se llama "Contador" o contiene el texto de inicio
                if (textoMensajeInicio != null && (child == textoMensajeInicio.transform || child == textoMensajeInicio.transform.parent)) {
                    child.gameObject.SetActive(true);
                } else {
                    child.gameObject.SetActive(false);
                }
            }
        }
        
        if (panelDetalle != null) panelDetalle.SetActive(false);

        if (textoMensajeInicio != null) {
            textoMensajeInicio.gameObject.SetActive(true);
            textoMensajeInicio.color = Color.white;
        }

        for (int i = 3; i > 0; i--)
        {
            if (textoMensajeInicio != null) textoMensajeInicio.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (textoMensajeInicio != null) { textoMensajeInicio.text = "¡YA!"; yield return new WaitForSecondsRealtime(0.7f); }

        if (textoMensajeInicio != null) textoMensajeInicio.gameObject.SetActive(false); 
        
        // Apagar el overlay completo al empezar a jugar
        if (overlayInicio != null) overlayInicio.SetActive(false);
        
        _tiempoTranscurrido = 0f;
        _framesTargeteados = 0;
        _framesTotales = 0;
        _segundosMirando = 0f; 
        
        ToggleUI(false); // <--- OCULTAR AL EMPEZAR
        
        if (distractor != null) distractor.gameObject.SetActive(true); // Mostrar distractor ahora sí

        // Mover la estrella a la posición inicial (izquierda) JUSTO cuando arranca el juego
        if (star != null) 
        {
            float anchoPadre = 1920f;
            RectTransform padre = star.parent as RectTransform;
            if (padre != null && padre.rect.width > 0) anchoPadre = padre.rect.width;
            
            float limiteIzquierdo = -(anchoPadre / 2f) + 100f;
            star.anchoredPosition = new Vector2(limiteIzquierdo, 0);
        }

        juegoIniciado = true;
        _enConteo = false;
    }

    void ToggleUI(bool visible)
    {
        if (botonSalir != null) botonSalir.gameObject.SetActive(visible);
        if (precBubble != null) precBubble.SetActive(visible);
        if (avanceBubble != null) avanceBubble.SetActive(visible);
        
        // El timer SOLO visible durante el juego (!visible significa que estamos en partida)
        if (timerContainer != null) timerContainer.SetActive(!visible);
    }

    protected override void Update()
    {
        // Forzar interactividad para evitar el parpadeo causado por BaseActividad
        if (!juegoIniciado && botonIniciar != null) botonIniciar.interactable = true;

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
            GestorPaciente.Instance.GuardarPartida("Estrella Lineal", this.puntuacion, nivelAlcanzado, precisionFinal, true, _tiempoTranscurrido, 0);
        }
        // ---------------------------------------------------

        ToggleUI(true); 

        // Forzar los valores finales en las burbujas por si no se actualizaron en el último frame
        if (precText != null) precText.text = precisionFinal.ToString("F0");
        if (avanceText != null) avanceText.text = nivelAlcanzado.ToString();

        if (overlayResult != null)
        {
            overlayResult.SetActive(true);
            
            // Mensajes super user-friendly y motivadores
            if (titleRes != null) {
                titleRes.text = this.puntuacion >= 70 ? "¡Misión Estelar Cumplida!" : "¡Casi lo logras!";
            }
            if (percentRes != null) percentRes.text = this.puntuacion.ToString();
            
            if (subRes != null) 
            {
                if (this.puntuacion >= 70) {
                    subRes.text = "¡Lograste acompañar a nuestra estrella durante su recorrido!\nEres un astronauta fantástico.";
                } else {
                    subRes.text = "La estrella se alejó un poquito esta vez.\n¡Mantén tus ojos en ella para la próxima misión!";
                }
            }
           
            if (btnAgain != null)
            {
                btnAgain.onClick.RemoveAllListeners();
                btnAgain.onClick.AddListener(() => {
                    Debug.Log("<color=orange><b>[SISTEMA]</b></color> Botón Jugar de nuevo pulsado. Reiniciando juego...");
                    Time.timeScale = 1.0f;
                    string nombreEscena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscena);
                });
                
                var txtBtn = btnAgain.GetComponentInChildren<TMP_Text>();
                if (txtBtn != null) txtBtn.text = "¡Jugar de nuevo!";
            }
        }
        else
        {
            Debug.LogWarning("<color=red><b>[ERROR]</b></color> No se encontró OverlayResult. Volviendo al Home.");
            Time.timeScale = 1.0f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Activities");
        }
    }

    void ManejarPestañeoInicio()
    {
        // 1. FALLBACK: Asegurar que el botón SIEMPRE se pueda pulsar (ratón)
        if (botonIniciar != null) botonIniciar.interactable = true;

        // 2. DELAY LECTURA: Dejar el texto original de la misión unos segundos
        _tiempoInstruccionesMostradas += Time.deltaTime;
        if (_tiempoInstruccionesMostradas < 5.0f) {
            // Mantener el botón oculto mientras se leen las instrucciones
            if (botonIniciar != null && botonIniciar.gameObject.activeSelf) botonIniciar.gameObject.SetActive(false);
            return; // Esperar a que lean la instrucción antes de buscar ojos
        }

        // Mostrar el botón en el momento en que empezamos a buscar los ojos
        if (botonIniciar != null && !botonIniciar.gameObject.activeSelf) botonIniciar.gameObject.SetActive(true);

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
            float tiempoRestante = Mathf.Max(0, duracionSesion - _tiempoTranscurrido);
            textoTimer.text = tiempoRestante.ToString("F0") + "s";
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
            avanceText.text = (nivelActual > 1) ? nivelActual.ToString() : progresoNivel.ToString("F0");
            
        if (barFill != null) barFill.fillAmount = (_segundosMirando % 10f) / 10f;
    }

    void MoverObjetosAdicionales()
    {
        // 2. MOVIMIENTO DE ÚNICA ESTRELLA (Rebote Izquierda-Derecha)
        if (_convoyEstrellas != null)
        {
            float anchoPadre = 1920f;
            if (_convoyEstrellas.Length > 0 && _convoyEstrellas[0] != null) {
                RectTransform padre = _convoyEstrellas[0].parent as RectTransform;
                if (padre != null && padre.rect.width > 0) anchoPadre = padre.rect.width;
            }

            float limiteBorde = (anchoPadre / 2f) - 100f; // Margen de 100px para que no desaparezca

            foreach (var s in _convoyEstrellas)
            {
                s.anchoredPosition += Vector2.right * (velocidadStar * _dirEstrella * Time.deltaTime);
                
                if (s.anchoredPosition.x > limiteBorde) {
                    s.anchoredPosition = new Vector2(limiteBorde, s.anchoredPosition.y);
                    _dirEstrella = -1; // Rebotar hacia la izquierda
                }
                else if (s.anchoredPosition.x < -limiteBorde) {
                    s.anchoredPosition = new Vector2(-limiteBorde, s.anchoredPosition.y);
                    _dirEstrella = 1; // Rebotar hacia la derecha
                }
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
        var gaze = EyeTracker.Instance != null ? EyeTracker.Instance.LatestGazeData : null;

        if (gaze != null && (gaze.Left.GazePointValid || gaze.Right.GazePointValid))
        {
            // Promedio directo del SDK
            Vector2 viewPos = new Vector2(
                (gaze.Left.GazePointOnDisplayArea.x + gaze.Right.GazePointOnDisplayArea.x) / 2f,
                (gaze.Left.GazePointOnDisplayArea.y + gaze.Right.GazePointOnDisplayArea.y) / 2f
            );
            
            // Mapeo crudo a píxeles de ventana
            _gazeDebugPos = new Vector2(viewPos.x * Screen.width, (1f - viewPos.y) * Screen.height);
        }
        else
        {
            // Fallback al ratón si Tobii no está conectado o no detecta ojos
            _gazeDebugPos = Input.mousePosition;
        }
            
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
