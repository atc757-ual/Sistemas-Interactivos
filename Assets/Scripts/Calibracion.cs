using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Tobii.Research.Unity;
using Tobii.Research;

public class Calibracion : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject panelCalibracion;
    public GameObject panelResultados; 
    public GameObject headerPanel;
    public GameObject subHeaderSecundario;
    public GameObject statusPanel;
    public GameObject bottomButtonBar;
    public GameObject overlayInicio;
    public GameObject panelDetalle;
    
    [Header("UI Controls")]
    public Image puntoCalibracion;
    public TMP_Text textoInstrucciones;
    public Button botonIniciarManual;
    public Button botonVolver;

    [Header("Result UI")]
    public TMP_Text textoResultadoTitulo;
    public TMP_Text textoResultadoDesc;
    public TMP_Text textoCountdown; // Se vincula a "CountdownText"
    public TMP_Text textoCounter;   // Se vincula a "Counter"
    public Button botonReintentar;
    public Button botonContinuar;

    [Header("Settings")]
    public float tiempoMovimiento = 0.5f; 
    public float tiempoRecoleccion = 2.5f; // Total 3 segundos por punto

    private Canvas _canvasPrincipal;
    private bool inicioManualPulsado = false;
    private float currentZDist = 0f;
    private float tiempoInicioActividad = 0f; // Para medir cuánto dura la sesión
    private float tiempoOjosCerrados = 0f;
    private float tiempoEstableRequerido = 0f;
    private bool ojosDetectadosEstables = false;
    private List<Transform> _canvasChildrenCache = new List<Transform>();
    
    // Almacenamos el par de coordenadas (Visual vs Sensor)
    private struct PuntoMapeado {
        public Vector2 posUnity;
        public Vector2 posTobii;
    }
    private PuntoMapeado[] mapaPuntos;
    private float precisionActual = 0f;

    void Awake()
    {
        Debug.Log("<color=cyan><b>[Calibración]</b> Iniciando...</color>");
        _canvasPrincipal = FindFirstObjectByType<Canvas>();

        foreach (var sdkCal in Object.FindObjectsByType<Tobii.Research.Unity.Calibration>(FindObjectsSortMode.None)) {
            if (sdkCal.gameObject != this.gameObject) sdkCal.enabled = false;
        }
        foreach (var guide in Object.FindObjectsByType<Tobii.Research.Unity.TrackBoxGuide>(FindObjectsSortMode.None)) {
            guide.enabled = false;
        }

        if (EventSystem.current == null) new GameObject("EventSystem_Auto", typeof(EventSystem), typeof(StandaloneInputModule));

        AutoVincular();
    }

    void AutoVincular()
    {
        panelCalibracion = panelCalibracion ?? BuscarObj("CalibrationPanel");
        panelResultados = panelResultados ?? BuscarObj("ResultPanel");
        headerPanel = headerPanel ?? BuscarObj("Header_Calibracion");
        subHeaderSecundario = subHeaderSecundario ?? BuscarObj("SubHeader_Secundario");
        statusPanel = statusPanel ?? BuscarObj("StatusPanel");
        bottomButtonBar = bottomButtonBar ?? BuscarObj("BottomButtonBar");

        puntoCalibracion = puntoCalibracion ?? BuscarObj("PuntoCalibracion")?.GetComponent<Image>();
        textoInstrucciones = textoInstrucciones ?? BuscarObj("SubHeader_Instruccion")?.GetComponent<TMP_Text>();
        botonIniciarManual = botonIniciarManual ?? BuscarObj("IniciarManual")?.GetComponent<Button>();
        botonVolver = botonVolver ?? BuscarObj("VolverBtn")?.GetComponent<Button>();
        
        textoResultadoTitulo = textoResultadoTitulo ?? BuscarObj("ResultTitle")?.GetComponent<TMP_Text>();
        textoResultadoDesc = textoResultadoDesc ?? BuscarObj("ResultDesc")?.GetComponent<TMP_Text>();
        textoCountdown = textoCountdown ?? BuscarObj("CountdownText")?.GetComponent<TMP_Text>();
        textoCounter = textoCounter ?? BuscarObj("Counter")?.GetComponent<TMP_Text>();
        botonReintentar = botonReintentar ?? BuscarObj("RetryBtn")?.GetComponent<Button>();
        botonContinuar = botonContinuar ?? BuscarObj("ContinueBtn")?.GetComponent<Button>();

        overlayInicio = overlayInicio ?? BuscarObj("OverlayInicio");
        panelDetalle = panelDetalle ?? BuscarObj("detalle");

        if (textoCounter != null) textoCounter.gameObject.SetActive(false);

        if (botonIniciarManual != null) {
            botonIniciarManual.onClick.RemoveAllListeners();
            botonIniciarManual.onClick.AddListener(() => inicioManualPulsado = true);
            botonIniciarManual.gameObject.SetActive(false);
        }

        if (botonReintentar != null) {
            botonReintentar.onClick.RemoveAllListeners();
            botonReintentar.onClick.AddListener(() => {
                if (panelResultados != null) panelResultados.SetActive(false);
                saltarFaseDeteccion = true;
            });
        }

        if (botonContinuar != null) {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() => Volver());
        }
    }

    GameObject BuscarObj(string n) {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) 
            if (go.name == n && !string.IsNullOrEmpty(go.gameObject.scene.name)) return go;
        return null;
    }

    private bool saltarFaseDeteccion = false;

    void Start()
    {
        AutoVincular();
        
        if (_canvasPrincipal != null) {
            foreach (Transform t in _canvasPrincipal.transform) _canvasChildrenCache.Add(t);
        }

        CalcularPuntosProgramaticos();
        
        ConfigurarInterfazCalibracion(false);
        if (panelResultados != null) panelResultados.SetActive(false);
        if (panelCalibracion != null) panelCalibracion.SetActive(true);
        if (botonVolver != null) botonVolver.onClick.AddListener(Volver);
        
        StartCoroutine(FlujoPrincipal());
    }

    void CalcularPuntosProgramaticos()
    {
        // Márgenes del 10% en todos los lados como pediste
        float minX = 0.1f, maxX = 0.9f, minY = 0.1f, maxY = 0.9f;

        float midX = 0.5f;
        float midY = 0.5f;
        
        // 9 puntos bien distribuidos (Grid 3x3)
        Vector2[] posUnity = {
            new Vector2(minX, maxY), new Vector2(midX, maxY), new Vector2(maxX, maxY), // Arriba
            new Vector2(minX, midY), new Vector2(midX, midY), new Vector2(maxX, midY), // Medio
            new Vector2(minX, minY), new Vector2(midX, minY), new Vector2(maxX, minY)  // Abajo
        };

        mapaPuntos = new PuntoMapeado[posUnity.Length];
        for (int i = 0; i < posUnity.Length; i++)
        {
            mapaPuntos[i] = new PuntoMapeado {
                posUnity = posUnity[i],
                posTobii = new Vector2(posUnity[i].x, 1.0f - posUnity[i].y) // Inversión para Tobii
            };
        }
    }

    // Eliminado: CrearAreaVisual [ContextMenu]
    
    void BarajarPuntos()
    {
        if (mapaPuntos == null) return;
        for (int i = 0; i < mapaPuntos.Length; i++) {
            var temp = mapaPuntos[i];
            int randomIndex = Random.Range(i, mapaPuntos.Length);
            mapaPuntos[i] = mapaPuntos[randomIndex];
            mapaPuntos[randomIndex] = temp;
        }
    }

    IEnumerator FlujoPrincipal()
    {
        tiempoInicioActividad = Time.time; // Empezamos a contar el tiempo
        yield return new WaitForSeconds(0.5f);
        
        bool yaCalibrado = GestorPaciente.Instance != null && GestorPaciente.Instance.haCalibradoEnEstaSesion;
        if (textoInstrucciones != null) 
            textoInstrucciones.text = yaCalibrado ? "¡Visor estelar listo!\n¿Quieres calibrar de nuevo?" : "¡Hola pequeño astronauta!";
        
        yield return new WaitForSeconds(2.0f);

        while (true)
        {
            // FASE 1: DETECCIÓN Y PARPADEO (Se salta en reintentos)
            if (!saltarFaseDeteccion)
            {
                bool listo = false;
                inicioManualPulsado = false;
                tiempoEstableRequerido = 0f;
                ojosDetectadosEstables = false;
                if (botonIniciarManual != null) botonIniciarManual.gameObject.SetActive(false);

                while (!listo)
                {
                    if (inicioManualPulsado) { listo = true; break; }

                    var gaze = EyeTracker.Instance.LatestGazeData;
                    bool detectado = gaze != null && (gaze.Left.GazeOriginValid || gaze.Right.GazeOriginValid);

                    if (detectado)
                    {
                        tiempoOjosCerrados = 0f;
                        float distL = gaze.Left.GazeOriginInUserCoordinates.z;
                        float distR = gaze.Right.GazeOriginInUserCoordinates.z;
                        currentZDist = (distL + distR) / 20.0f;

                        if (currentZDist >= 40 && currentZDist <= 80) // Rango más generoso 
                        {
                            tiempoEstableRequerido += Time.deltaTime;
                            if (tiempoEstableRequerido > 1.0f) // Más rápido (de 1.5s a 1.0s)
                            {
                                if (!ojosDetectadosEstables) Debug.Log("<color=green>Ojos Estables. Listo para parpadeo.</color>");
                                ojosDetectadosEstables = true;
                                if (textoInstrucciones != null) {
                                    textoInstrucciones.text = "¡TE VEO! <b>Pestañea</b> para empezar.";
                                }
                                if (botonIniciarManual != null) botonIniciarManual.gameObject.SetActive(true);
                            }
                        }
                        else 
                        {
                            tiempoEstableRequerido = 0f; ojosDetectadosEstables = false;
                            if (textoInstrucciones != null) textoInstrucciones.text = (currentZDist < 40) ? "¡Muy cerca!" : "¡Muy lejos!";
                            if (botonIniciarManual != null) botonIniciarManual.gameObject.SetActive(false);
                        }
                    }
                    else // NO DETECTADO (Posible parpadeo)
                    {
                        if (ojosDetectadosEstables) 
                        {
                            tiempoOjosCerrados += Time.deltaTime;
                            if (tiempoOjosCerrados >= 0.12f && tiempoOjosCerrados <= 0.65f) 
                            {
                                // Esperamos a que los vuelva a abrir para confirmar el pestañeo
                                float waitOpen = 0f;
                                bool abrio = false;
                                while(waitOpen < 0.2f) {
                                    var gCheck = EyeTracker.Instance.LatestGazeData;
                                    if(gCheck != null && (gCheck.Left.GazeOriginValid || gCheck.Right.GazeOriginValid)) {
                                        abrio = true; break;
                                    }
                                    waitOpen += Time.deltaTime;
                                    yield return null;
                                }

                                if (abrio || waitOpen >= 0.2f) {
                                    Debug.Log("<color=cyan><b>Pestañeo Confirmado!</b></color>");
                                    listo = true; 
                                    break;
                                }
                            }
                        }
                        else 
                        {
                            if (textoInstrucciones != null) 
                                textoInstrucciones.text = yaCalibrado ? "¡Visor listo!\nPulsa VOLVER para jugar." : "Buscando tus ojos...";
                        }
                    }
                    yield return null;
                }
            }
            
            // --- FASE DE CONTEO ESTELAR (3, 2, 1, YA) ---
            ConfigurarInterfazCalibracion(true); 
            if (statusPanel != null) statusPanel.SetActive(false);
            if (bottomButtonBar != null) bottomButtonBar.SetActive(false);
            if (botonIniciarManual != null) botonIniciarManual.gameObject.SetActive(false);
            if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(false);

            if (textoCounter != null)
            {
                textoCounter.gameObject.SetActive(true);
                for (int i = 3; i > 0; i--)
                {
                    textoCounter.text = i.ToString();
                    yield return new WaitForSeconds(0.7f);
                }
                
                // --- OPTIMIZACIÓN: Iniciamos el hilo MIENTRAS dice YA ---
                BarajarPuntos(); 
                ConfigurarInterfazCalibracion(activa: true);
                
                try { 
                    var safetyThread = new CalibrationThread(EyeTracker.Instance.EyeTrackerInterface, true);
                    safetyThread.LeaveCalibrationMode();
                    safetyThread.StopThread();
                } catch { }

                var calibrationThread = new CalibrationThread(EyeTracker.Instance.EyeTrackerInterface, true);
                
                textoCounter.text = "¡YA!";
                yield return new WaitForSeconds(0.4f); // Delay reducido
                textoCounter.gameObject.SetActive(false);

                while (!calibrationThread.Running) yield return null;
                yield return EjecutarCalibracion(calibrationThread);
            }

            yield return MostrarResultados(precisionActual >= 75f);
        }
    }

    IEnumerator EjecutarCalibracion(CalibrationThread thread)
    {
        try 
        {
            thread.EnterCalibrationMode();
            
            if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(true);
            for (int i = 0; i < mapaPuntos.Length; i++)
            {
                var p = mapaPuntos[i];
                puntoCalibracion.rectTransform.anchorMin = p.posUnity;
                puntoCalibracion.rectTransform.anchorMax = p.posUnity;
                puntoCalibracion.rectTransform.anchoredPosition = Vector2.zero;
                puntoCalibracion.gameObject.SetActive(true);

                yield return new WaitForSeconds(tiempoMovimiento);
                
                thread.CollectData(new CalibrationThread.Point(p.posTobii.x, p.posTobii.y));
                yield return new WaitForSeconds(tiempoRecoleccion);
            }

            if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(false);
            Debug.Log("<color=yellow>Calibración: Calculando resultados...</color>");
            var result = thread.ComputeAndApply();
            
            float timeout = 0f;
            while (!result.Ready && timeout < 3f) {
                timeout += Time.deltaTime;
                yield return null;
            }

            procesarResultadosYGuardar(result);
        }
        finally 
        {
            thread.LeaveCalibrationMode();
            thread.StopThread();
        }
    }

    void procesarResultadosYGuardar(CalibrationThread.MethodResult result) {
        float errorTotalAcumulado = 0;
        int totalMuestrasOjos = 0;
        int puntosConDatos = 0;

        Debug.Log("<color=orange>------ [DIAGNÓSTICO] ANALISIS DE PRECISIÓN ------</color>");

        if (result.Ready && result.LastResult != null)
        {
            int iPunto = 1;
            foreach (var point in result.LastResult.CalibrationPoints)
            {
                Vector2 targetPos = new Vector2(point.PositionOnDisplayArea.X, point.PositionOnDisplayArea.Y);
                float errorPunto = 0;
                int muestrasPunto = 0;

                foreach (var sample in point.CalibrationSamples)
                {
                    // Registramos como válido si Tobii dice que lo usó O si fue detectado pero no usado
                    bool lVal = sample.LeftEye.Validity == CalibrationEyeValidity.ValidAndUsed || sample.LeftEye.Validity == CalibrationEyeValidity.ValidButNotUsed; 
                    bool rVal = sample.RightEye.Validity == CalibrationEyeValidity.ValidAndUsed || sample.RightEye.Validity == CalibrationEyeValidity.ValidButNotUsed;

                    if (lVal) {
                        errorPunto += Vector2.Distance(new Vector2(sample.LeftEye.PositionOnDisplayArea.X, sample.LeftEye.PositionOnDisplayArea.Y), targetPos);
                        muestrasPunto++;
                    }
                    if (rVal) {
                        errorPunto += Vector2.Distance(new Vector2(sample.RightEye.PositionOnDisplayArea.X, sample.RightEye.PositionOnDisplayArea.Y), targetPos);
                        muestrasPunto++;
                    }
                }

                if (muestrasPunto > 0) {
                    float errorMedioPunto = errorPunto / muestrasPunto;
                    errorTotalAcumulado += errorMedioPunto;
                    totalMuestrasOjos++;
                    puntosConDatos++;
                    Debug.Log($"<color=green>Punto {iPunto}:</color> OK ({muestrasPunto} muestras). Error: {errorMedioPunto:F3}");
                } else {
                    errorTotalAcumulado += 0.3f; // Ajustado: Antes 0.5f para ser más permisivo
                    totalMuestrasOjos++;
                    Debug.Log($"<color=red>Punto {iPunto}:</color> SIN DATOS. El sensor no vio ojos aquí.");
                }
                iPunto++;
            }
        }
        else {
            Debug.Log("<color=red>ERROR: Tobii no devolvió resultados de calibración. ¿Está el sensor conectado?</color>");
        }

        Debug.Log("<color=orange>--------------------------------------------------</color>");

        if (puntosConDatos > 0) {
            float errorPromedio = errorTotalAcumulado / totalMuestrasOjos;
            precisionActual = Mathf.Clamp((1.0f - (errorPromedio * 2.0f)) * 100f, 0, 100f); 
        } else {
            precisionActual = 0;
        }

        if (GestorPaciente.Instance != null)
        {
             float tiempoTotal = Time.time - tiempoInicioActividad;
             GestorPaciente.Instance.GuardarPartida("Calibración", 100, 1, precisionActual, precisionActual >= 75, tiempoTotal);
        }
    }

    void ConfigurarInterfazCalibracion(bool activa, bool soloModal = false)
    {
        if (_canvasPrincipal == null) return;
        
        HashSet<string> permitidos = new HashSet<string> { "PuntoCalibracion", "Background" };
        HashSet<string> listaNegra = new HashSet<string> { "ResultPanel", "PuntoCalibracion", "PuntoCalibracion_AutoGenerated", "Counter" };

        foreach (Transform child in _canvasChildrenCache)
        {
            if (child == null) continue;
            if (soloModal) // MODO RESULTADOS 
            {
                if (child.name == "ResultPanel" || child.name == "Background" || child.name == "VolverBtn")
                    child.gameObject.SetActive(true);
                else
                    child.gameObject.SetActive(false);
            }
            else if (activa) // MODO CALIBRACIÓN (OCULTAR EXTRAS)
            {
                if (permitidos.Contains(child.name)) 
                    child.gameObject.SetActive(true);
                else 
                    child.gameObject.SetActive(false);
            }
            else // MODO DETECCIÓN (NORMAL)
            {
                if (listaNegra.Contains(child.name))
                    child.gameObject.SetActive(false);
                else
                    child.gameObject.SetActive(true);
            }
        }
    }

    IEnumerator MostrarResultados(bool exito)
    {
        ConfigurarInterfazCalibracion(false, true); 
        
        if (exito && GestorPaciente.Instance != null) GestorPaciente.Instance.haCalibradoEnEstaSesion = true;
        
        // 2. Localización de emergencia por si se perdió la referencia
        if (panelResultados == null) panelResultados = BuscarObj("ResultPanel");

        if (panelResultados != null)
        {
            panelResultados.SetActive(true);
            panelResultados.transform.SetAsLastSibling(); // Ponerlo al frente de todo
            
            if (panelCalibracion != null) panelCalibracion.SetActive(false);
            
            if (textoResultadoTitulo != null) 
            {
                textoResultadoTitulo.text = exito ? "¡MISIÓN CUMPLIDA!" : "¡MISIÓN FALLIDA!";
                textoResultadoTitulo.color = exito ? Color.white : Color.yellow;
            }
            
            if (textoResultadoDesc != null) 
            {
                string infoPerc = $"\n\n<size=80%>Precisión: <b>{precisionActual:F0}%</b> (Mínimo: 75%)</size>";
                
                if (exito) {
                    textoResultadoDesc.color = new Color(0.4f, 1f, 0.4f);
                    textoResultadoDesc.text = "¡Excelente! El visor está perfectamente calibrado." + infoPerc;
                } else {
                    textoResultadoDesc.color = new Color(1f, 0.4f, 0.4f);
                    textoResultadoDesc.text = "¡Oh no! Necesitamos más precisión. Intenta llegar al 75%." + infoPerc;
                }
            }
            
            if (botonContinuar != null) botonContinuar.gameObject.SetActive(exito);
            if (botonReintentar != null) botonReintentar.gameObject.SetActive(!exito); 

            if (exito) 
            {
                // Pequeña pausa para leer el éxito antes del countdown
                yield return new WaitForSeconds(1.5f);

                // Countdown de 5 segundos para volver automáticamente
                for (int i = 5; i > 0; i--)
                {
                    if (textoCountdown != null) 
                        textoCountdown.text = "Menú principal en " + i + "...";
                    
                    yield return new WaitForSeconds(1f);
                }
                if (panelResultados != null) panelResultados.SetActive(false);
                SceneManager.LoadScene("Home");
            } 
            else 
            {
                if (botonContinuar != null) botonContinuar.gameObject.SetActive(false);
                if (botonReintentar != null) botonReintentar.gameObject.SetActive(false); 

                // Esperamos los 3 segundos que me pediste antes para que se lea el error
                yield return new WaitForSeconds(3.0f);

                if (textoResultadoDesc != null) 
                    textoResultadoDesc.text = "¡Pestañea o pulsa Reintentar para volver a la misión!";

                if (botonReintentar != null) botonReintentar.gameObject.SetActive(true);

                // Lógica de parpadeo para reintentar desde el modal de error
                float tCerradosError = 0f;
                bool reintentarPorPestañeo = false;

                while (panelResultados.activeSelf && !reintentarPorPestañeo)
                {
                    var g = EyeTracker.Instance.LatestGazeData;
                    bool detecta = g != null && (g.Left.GazeOriginValid || g.Right.GazeOriginValid);
                    
                    if (!detecta) {
                        tCerradosError += Time.deltaTime;
                    } else {
                        // Si se abrieron después de un cierre válido (0.12s a 0.65s), reintentar
                        if (tCerradosError >= 0.12f && tCerradosError <= 0.65f) {
                            reintentarPorPestañeo = true;
                        }
                        tCerradosError = 0f;
                    }
                    yield return null;
                }

                if (panelResultados != null) panelResultados.SetActive(false);
                saltarFaseDeteccion = true; // El siguiente ciclo de calibración empezará sin buscar ojos
                if (panelCalibracion != null) panelCalibracion.SetActive(true);
            }
        }
        else {
            if (exito) SceneManager.LoadScene("Home");
            else yield return new WaitForSeconds(2f);
        }
    }

    public void Volver() { SceneManager.LoadScene("Home"); }
}
