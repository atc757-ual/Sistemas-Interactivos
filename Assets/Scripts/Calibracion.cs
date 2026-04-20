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
    
    [Header("UI Controls")]
    public Image puntoCalibracion;
    public TMP_Text textoInstrucciones;
    public TMP_Text textoProgreso;
    public Button botonIniciarManual;
    public Button botonVolver;

    [Header("Result UI")]
    public TMP_Text textoResultadoTitulo;
    public TMP_Text textoResultadoDesc;
    public TMP_Text textoCountdown; // Se vincula a "CountdownText"
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
        textoProgreso = textoProgreso ?? BuscarObj("ProgresoText")?.GetComponent<TMP_Text>();
        botonIniciarManual = botonIniciarManual ?? BuscarObj("IniciarManual")?.GetComponent<Button>();
        botonVolver = botonVolver ?? BuscarObj("VolverBtn")?.GetComponent<Button>();
        
        textoResultadoTitulo = textoResultadoTitulo ?? BuscarObj("ResultTitle")?.GetComponent<TMP_Text>();
        textoResultadoDesc = textoResultadoDesc ?? BuscarObj("ResultDesc")?.GetComponent<TMP_Text>();
        textoCountdown = textoCountdown ?? BuscarObj("CountdownText")?.GetComponent<TMP_Text>();
        botonReintentar = botonReintentar ?? BuscarObj("RetryBtn")?.GetComponent<Button>();
        botonContinuar = botonContinuar ?? BuscarObj("ContinueBtn")?.GetComponent<Button>();

        if (botonIniciarManual != null) {
            botonIniciarManual.onClick.RemoveAllListeners();
            botonIniciarManual.onClick.AddListener(() => inicioManualPulsado = true);
            botonIniciarManual.gameObject.SetActive(false);
        }
    }

    GameObject BuscarObj(string n) {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>()) 
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
        // Márgenes: 5% a los lados/abajo, pero 15% arriba para evitar el Header
        float minX = 0.05f, maxX = 0.95f, minY = 0.05f, maxY = 0.85f;

        float midX = 0.5f;
        float midY = (minY + maxY) / 2f;
        float innerX1 = Mathf.Lerp(minX, maxX, 0.25f);
        float innerX2 = Mathf.Lerp(minX, maxX, 0.75f);
        float innerY_TopSafe = Mathf.Lerp(minY, maxY, 0.75f);

        // Posiciones UNITY (Abajo=0)
        Vector2[] posUnity = {
            new Vector2(minX, maxY), new Vector2(maxX, maxY), // "Arriba" segura
            new Vector2(minX, minY), new Vector2(maxX, minY), // Abajo
            new Vector2(midX, midY),                         // Centro
            new Vector2(innerX1, midY), new Vector2(innerX2, midY), 
            new Vector2(midX, innerY_TopSafe)
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
                            if (tiempoOjosCerrados >= 0.12f && tiempoOjosCerrados <= 0.65f) // Rango más amplio
                            {
                                Debug.Log("<color=cyan><b>Pestañeo Detectado!</b></color>");
                                listo = true; 
                                break; 
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
            
            // FEEDBACK TRAS IDENTIFICAR EL PARPADEO
            if (botonIniciarManual != null) botonIniciarManual.gameObject.SetActive(false);
            if (textoInstrucciones != null) 
                textoInstrucciones.text = "¡Listo! Pestañeo identificado.\nPreparando calibración...";
            
            yield return new WaitForSeconds(2.0f); // Pausa breve de confirmación

            saltarFaseDeteccion = false; // Consumimos el salto
            BarajarPuntos(); // Puntos en orden nuevo cada vez

            // CALIBRACIÓN LIMPIA
            ConfigurarInterfazCalibracion(activa: true, soloModal: false);

            var thread = new CalibrationThread(EyeTracker.Instance.EyeTrackerInterface, true);
            while (!thread.Running) yield return null;
            thread.EnterCalibrationMode();
            
            if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(true);
            for (int i = 0; i < mapaPuntos.Length; i++)
            {
                var p = mapaPuntos[i];
                if (textoProgreso != null) textoProgreso.text = "Punto " + (i + 1) + " / " + mapaPuntos.Length;
                
                puntoCalibracion.rectTransform.anchorMin = p.posUnity;
                puntoCalibracion.rectTransform.anchorMax = p.posUnity;
                puntoCalibracion.rectTransform.anchoredPosition = Vector2.zero;
                puntoCalibracion.gameObject.SetActive(true);

                yield return new WaitForSeconds(tiempoMovimiento);
                
                thread.CollectData(new CalibrationThread.Point(p.posTobii.x, p.posTobii.y));
                yield return new WaitForSeconds(tiempoRecoleccion);
            }

            if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(false);
            if (textoProgreso != null) textoProgreso.gameObject.SetActive(false);

            Debug.Log("<color=yellow>Calibración: Calculando resultados...</color>");
            var result = thread.ComputeAndApply();
            
            // Timeout de 3 segundos para no quedarse colgado
            float timeout = 0f;
            while (!result.Ready && timeout < 3f) {
                timeout += Time.deltaTime;
                yield return null;
            }

            bool exito = result.Ready && result.Status == CalibrationStatus.Success;
            Debug.Log("<color=white>Calibración finalizada. Éxito: " + exito + "</color>");

            // LOG DE PRECISIÓN DETALLADA Y CÁLCULO DE %
            float puntosOk = 0;
            if (result.Ready && result.LastResult != null)
            {
                Debug.Log("<color=orange>------ REPORTE DE PRECISIÓN (8 PUNTOS) ------</color>");
                int i = 1;
                foreach (var point in result.LastResult.CalibrationPoints)
                {
                    int samplesCount = point.CalibrationSamples.Count;
                    if (samplesCount > 0) puntosOk++;

                    string status = samplesCount > 0 ? "<color=green>OK</color>" : "<color=red>SIN DATOS</color>";
                    Debug.Log($"Punto {i} en ({point.PositionOnDisplayArea.X:F2}, {point.PositionOnDisplayArea.Y:F2}): {status} ({samplesCount} muestras)");
                    i++;
                }
                Debug.Log("<color=orange>-------------------------------------------</color>");
            }

            precisionActual = (puntosOk / 8.0f) * 100.0f;

            thread.LeaveCalibrationMode();
            thread.StopThread();

            // GUARDAR ESTADÍSTICAS EN EL HISTORIAL
            if (GestorPaciente.Instance != null)
            {
                float tiempoTotal = Time.time - tiempoInicioActividad;
                GestorPaciente.Instance.GuardarPartida("Calibración", 100, precisionActual, exito, tiempoTotal);
            }

            yield return MostrarResultados(exito);
        }
    }

    void ConfigurarInterfazCalibracion(bool activa, bool soloModal = false)
    {
        if (_canvasPrincipal == null) return;
        
        HashSet<string> permitidos = new HashSet<string> { "VolverBtn", "ProgresoText", "PuntoCalibracion", "Background" };
        HashSet<string> listaNegra = new HashSet<string> { "ResultPanel", "PuntoCalibracion", "ProgresoText", "PuntoCalibracion_AutoGenerated" };

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
        // 1. Limpieza absoluta: Solo el modal
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
                textoResultadoDesc.color = exito ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f); // Verde suave / Rojo suave
                string infoPrecision = $"\n\n<size=80%>Precisión de la misión: <b>{precisionActual:F0}%</b></size>";
                textoResultadoDesc.text = exito ? 
                    "Enhorabuena, has conseguido que el visor estelar esté calibrado y listo." + infoPrecision : 
                    "Ha habido problemas al calibrar, tendremos que volver a empezar la misión." + infoPrecision;
            }
            
            if (botonContinuar != null) botonContinuar.gameObject.SetActive(exito);
            if (botonReintentar != null) botonReintentar.gameObject.SetActive(!exito);

            if (exito) {
                if (botonReintentar != null) botonReintentar.gameObject.SetActive(false);
                
                // Pequeña pausa para leer el éxito antes del countdown
                yield return new WaitForSeconds(1.5f);

                // Countdown de 5 segundos
                for (int i = 5; i > 0; i--)
                {
                    if (textoCountdown != null) 
                        textoCountdown.text = "Menú principal en " + i + "...";
                    
                    yield return new WaitForSeconds(1f);
                }
                if (panelResultados != null) panelResultados.SetActive(false);
                SceneManager.LoadScene("Home");
            } else {
                if (botonContinuar != null) botonContinuar.gameObject.SetActive(false);
                if (botonReintentar != null) botonReintentar.gameObject.SetActive(false); 

                // Esperamos los 3 segundos que me pediste antes para que se lea el error
                yield return new WaitForSeconds(3.0f);

                if (textoResultadoDesc != null) 
                    textoResultadoDesc.text = "¡<b>Pestañea</b> o pulsa <b>Reintentar</b> para volver a la misión!";

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
                        if (tCerradosError > 0.15f && tCerradosError < 0.6f) reintentarPorPestañeo = true;
                    } else {
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
