using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Requerido para look Premium
using System.Collections;
using System.Collections.Generic;
using Tobii.Research.Unity;
using Tobii.Research;

public class CalibradorKids : MonoBehaviour
{
    [Header("Debug / Diseño")]
    public bool visualizarTodoParaEstilo = false;

    [Header("UI Elements (Auto-assigned if null)")]
    public GameObject panelCalibracion;
    public GameObject panelResultados; 
    public GameObject headerPanel; // NEW: To hide title
    public GameObject statusPanel; // NEW: To hide footer info
    public Image puntoCalibracion;
    public TMP_Text textoInstrucciones;
    public TMP_Text textoProgreso; 
    public GameObject subHeaderSecundario; // NEW: Para asegurar limpieza total
    public Button botonVolver;
    [Tooltip("Botón opcional para forzar inicio manual sin pestañear")]
    public Button botonIniciarManual;

    [Header("Result UI")]
    public TMP_Text textoResultadoTitulo;
    public TMP_Text textoResultadoDesc;
    public Button botonReintentar;
    public Button botonContinuar;
    public TMP_Text textoContadorRedireccion;

    [Header("Settings")]
    [Tooltip("El script internamente decidirá (entre 4 y 6 puntos) y los aleatorizará en el Start()")]
    [HideInInspector] public Vector2[] puntos;
    
    [Tooltip("Tiempo en segundos que tarda la estrella en moverse de un punto a otro.")]
    public float tiempoMovimiento = 0.8f; 
    
    [Tooltip("Tiempo mínimo que la estrella estará rotando (pulso) recolectando datos.")]
    public float tiempoRecoleccion = 1.2f;

    private CalibrationThread _calibThread;
    private bool inicioManualPulsado = false;
    private TMP_Text textoContadorPosiciones;

    void Awake()
    {
        // Búsqueda exhaustiva de referencias si no están asignadas
        if (panelCalibracion == null) panelCalibracion = FindObjectByName("CalibrationPanel");
        if (panelResultados == null) panelResultados = FindObjectByName("ResultPanel");
        if (headerPanel == null) headerPanel = FindObjectByName("Header_Calibracion");
        if (statusPanel == null) statusPanel = FindObjectByName("StatusPanel");

        if (puntoCalibracion == null) puntoCalibracion = FindObjectByName("PuntoCalibracion")?.GetComponent<Image>();
        
        if (textoInstrucciones == null) textoInstrucciones = FindObjectByName("SubHeader_Instruccion")?.GetComponent<TMP_Text>();
        if (subHeaderSecundario == null) subHeaderSecundario = FindObjectByName("SubHeader_Secundario");
        if (textoProgreso == null) textoProgreso = FindObjectByName("ProgresoText")?.GetComponent<TMP_Text>();
        if (botonVolver == null) botonVolver = FindObjectByName("VolverBtn")?.GetComponent<Button>();
        
        // Estilo Senior: Asegurar que el botón volver sea brillante y visible
        if (botonVolver != null && botonVolver.image != null)
        {
            botonVolver.interactable = true;
            botonVolver.image.color = new Color(1f, 0.5f, 0.5f, 1f); // Coral brillante
            botonVolver.image.canvasRenderer.SetAlpha(1f);
        }
        if (botonIniciarManual == null) botonIniciarManual = FindObjectByName("IniciarManual")?.GetComponent<Button>();
        
        // Result Panel Auto-assignment
        if (textoResultadoTitulo == null) textoResultadoTitulo = FindObjectByName("ResultTitle")?.GetComponent<TMP_Text>();
        if (textoResultadoDesc == null) textoResultadoDesc = FindObjectByName("ResultDesc")?.GetComponent<TMP_Text>();
        if (botonReintentar == null) botonReintentar = FindObjectByName("RetryBtn")?.GetComponent<Button>();
        if (botonContinuar == null) botonContinuar = FindObjectByName("ContinueBtn")?.GetComponent<Button>();
        if (textoContadorRedireccion == null) textoContadorRedireccion = FindObjectByName("CountdownText")?.GetComponent<TMP_Text>();

        // Sincronizar forma del botón pero personalizar colores
        if (botonReintentar != null && botonContinuar != null)
        {
            botonReintentar.image.sprite = botonContinuar.image.sprite;
            botonReintentar.image.type = botonContinuar.image.type;
            
            // Colores temáticos distintos
            botonReintentar.image.color = new Color(1f, 0.3f, 0.3f, 1f); // Rojo Coral
            botonContinuar.image.color = new Color(0f, 0.8f, 1f, 1f); // Cyan Neón
            
            // Configurar transiciones
            ColorBlock colorsReintentar = botonReintentar.colors;
            colorsReintentar.normalColor = new Color(1f, 0.3f, 0.3f, 1f);
            colorsReintentar.highlightedColor = new Color(1f, 0.5f, 0.5f, 1f);
            botonReintentar.colors = colorsReintentar;
        }

        EnsureCalibrationPoint();
        
        if (puntoCalibracion != null) {
            puntoCalibracion.gameObject.SetActive(false);
        }
        if (panelResultados != null) panelResultados.SetActive(false);
    }

    private void EnsureCalibrationPoint()
    {
        if (puntoCalibracion == null) puntoCalibracion = FindObjectByName("PuntoCalibracion")?.GetComponent<Image>();

        if (puntoCalibracion == null)
        {
            Debug.LogWarning("CalibradorKids: No se encontró PuntoCalibracion. Creando uno nuevo de emergencia.");
            GameObject newOrb = new GameObject("PuntoCalibracion_AutoGenerated");
            
            // Buscar el canvas principal para emparentar
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) newOrb.transform.SetParent(canvas.transform, false);
            
            puntoCalibracion = newOrb.AddComponent<Image>();
            
            // Estilo Neo-Orb Premium (Cian/Neon)
            puntoCalibracion.color = new Color(0f, 0.8f, 1f, 1f); 
            
            // Asignar sprite circular por defecto si existe, si no, cuadrado blanco (mejor que nada)
            Sprite circle = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            if (circle != null) puntoCalibracion.sprite = circle;

            RectTransform rt = newOrb.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        }

        // Asegurar que sea visible y esté en primer plano
        if (puntoCalibracion != null)
        {
            puntoCalibracion.transform.SetAsLastSibling();
            puntoCalibracion.rectTransform.localScale = Vector3.one;
            
            // Si no tiene el efecto de destello, añadirlo
            if (puntoCalibracion.GetComponent<EfectoDestelloCalibracion>() == null)
            {
                puntoCalibracion.gameObject.AddComponent<EfectoDestelloCalibracion>();
            }
        }
    }

    void Start()
    {
        // --- INICIO GENERACIÓN ALEATORIA (7 A 8 PUNTOS) ---
        int numPuntos = Random.Range(7, 9);
        List<Vector2> seleccionPuntos = new List<Vector2>();

        // Puntos cardinales principales (esquinas)
        seleccionPuntos.Add(new Vector2(0.1f, 0.1f)); 
        seleccionPuntos.Add(new Vector2(0.9f, 0.1f)); 
        seleccionPuntos.Add(new Vector2(0.1f, 0.9f)); 
        seleccionPuntos.Add(new Vector2(0.9f, 0.9f)); 
        
        // Puntos intermedios para mayor precisión
        seleccionPuntos.Add(new Vector2(0.5f, 0.5f)); // Centro
        seleccionPuntos.Add(new Vector2(0.5f, 0.15f)); // Arriba centro
        seleccionPuntos.Add(new Vector2(0.5f, 0.85f)); // Abajo centro

        // Si pedimos 8, añadimos uno lateral aleatorio
        if (numPuntos == 8)
        {
            Vector2[] laterales = new Vector2[] { new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.5f) };
            seleccionPuntos.Add(laterales[Random.Range(0, laterales.Length)]);
        }

        // Shuffle
        for (int i = 0; i < seleccionPuntos.Count; i++)
        {
            int r = Random.Range(i, seleccionPuntos.Count);
            Vector2 temp = seleccionPuntos[i];
            seleccionPuntos[i] = seleccionPuntos[r];
            seleccionPuntos[r] = temp;
        }
        
        puntos = seleccionPuntos.ToArray();

        // Configurar botones
        if (botonVolver != null) {
            botonVolver.onClick.RemoveAllListeners();
            botonVolver.onClick.AddListener(Volver);
        }
        if (botonIniciarManual != null) {
            botonIniciarManual.onClick.RemoveAllListeners();
            botonIniciarManual.onClick.AddListener(InicioManualPresionado);
            botonIniciarManual.gameObject.SetActive(false);
        }

        if (visualizarTodoParaEstilo)
        {
            MostrarTodaLaUI();
            return; // No iniciar lógica de calibración si estamos diseñando
        }

        // Asegurar que el el panel esté activo al inicio
        if (panelCalibracion != null) panelCalibracion.SetActive(true);
        if (headerPanel != null) headerPanel.SetActive(true);
        if (statusPanel != null) statusPanel.SetActive(true);
        
        StartCoroutine(RutinaCalibracionInfantil());
    }

    private void InicioManualPresionado()
    {
        inicioManualPulsado = true;
        Debug.Log("CalibradorKids: Botón INICIAR pulsado.");
        // Pequeño feedback visual
        if (botonIniciarManual != null) botonIniciarManual.transform.localScale = Vector3.one * 0.9f;
    }

    private GameObject FindObjectByName(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go.name == name) return go;
        }
        return null;
    }

    IEnumerator RutinaCalibracionInfantil()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (textoInstrucciones != null) textoInstrucciones.text = "Preparando...";

        while (EyeTracker.Instance == null || EyeTracker.Instance.EyeTrackerInterface == null)
        {
            yield return new WaitForSeconds(0.5f);
            if (textoInstrucciones != null) textoInstrucciones.text = "Preparando sensores espaciales...";
        }

        Debug.Log("CalibradorKids: Sensores encontrados. Iniciando narrativa.");

        // FASE 1: NARRATIVA INICIAL
        if (textoInstrucciones != null) 
        {
            textoInstrucciones.text = "¡Hola pequeño astronauta!";
            yield return new WaitForSeconds(2f);
            textoInstrucciones.text = "Estamos preparando el visor de estrellas...";
            yield return new WaitForSeconds(1.5f);
        }

        while (true)
        {
            if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(false);
            
            bool listoParaCalibrar = false;
            inicioManualPulsado = false;
            float tiempoOjosCerrados = 0f;
            float tiempoSinOjos = 0f; // NEW: Grace period for blinks
            bool ojosDetectadosEstables = false; 

            if (textoInstrucciones != null) 
                textoInstrucciones.text = "Asegúrate de estar a 60cm,\nparpadea o pulsa INICIAR.";
            
            if (botonIniciarManual != null) botonIniciarManual.gameObject.SetActive(true);

            while (!listoParaCalibrar)
            {
                if (inicioManualPulsado) { 
                    Debug.Log("CalibradorKids: Iniciando por botón manual.");
                    listoParaCalibrar = true; 
                    break; 
                }

                var data = EyeTracker.Instance.LatestGazeData;
                bool vistaDetectada = false;
                float currentZDist = -1;

                if (data != null)
                {
                    bool validL = data.Left.GazeOriginValid;
                    bool validR = data.Right.GazeOriginValid;
                    vistaDetectada = validL || validR;

                    if (vistaDetectada)
                    {
                        float distL = data.Left.GazeOriginValid ? data.Left.GazeOriginInUserCoordinates.z : 0;
                        float distR = data.Right.GazeOriginValid ? data.Right.GazeOriginInUserCoordinates.z : 0;
                        currentZDist = (data.Left.GazeOriginValid && data.Right.GazeOriginValid) ? (distL + distR) / 20.0f : (distL + distR) / 10f;

                        if (currentZDist >= 45 && currentZDist <= 75) 
                        {
                            ojosDetectadosEstables = true;
                            tiempoSinOjos = 0f;
                            if (textoInstrucciones != null)
                                textoInstrucciones.text = "¡ESTÁS PERFECTO!\nParpadea ahora para empezar.";
                        }
                    }
                    else {
                        // En lugar de resetear inmediatamente, contamos el tiempo de pérdida
                        tiempoSinOjos += Time.deltaTime;
                        if (tiempoSinOjos > 1f) // 1 segundo de gracia para parpadear
                        {
                            ojosDetectadosEstables = false;
                            if (textoInstrucciones != null) textoInstrucciones.text = "Buscando tus ojos...\nmira hacia el sensor.";
                        }
                    }
                }
                else {
                    tiempoSinOjos += Time.deltaTime;
                    if (tiempoSinOjos > 1f)
                    {
                        ojosDetectadosEstables = false;
                        if (textoInstrucciones != null) textoInstrucciones.text = "Buscando tus ojos...\nmira hacia el sensor.";
                    }
                }

                // Lógica de parpadeo: SOLO si se detectaron ojos estables PREVIAMENTE
                if (!vistaDetectada)
                {
                    if (ojosDetectadosEstables) 
                    {
                        tiempoOjosCerrados += Time.deltaTime;
                        if (tiempoOjosCerrados > 0.08f && tiempoOjosCerrados < 0.8f) 
                        { 
                            Debug.Log("CalibradorKids: Parpadeo validado.");
                            listoParaCalibrar = true; 
                        }
                    }
                }
                else 
                { 
                    tiempoOjosCerrados = 0f; 
                }

                // El botón se mantiene visible como fallback manual
                if (botonIniciarManual != null && !botonIniciarManual.gameObject.activeSelf) 
                    botonIniciarManual.gameObject.SetActive(true);

                yield return null;
            }

            // FASE 3: CALIBRACIÓN - NARRATIVA FINAL
            if (textoInstrucciones != null) 
            {
                textoInstrucciones.text = "¡Eso es! Ahora viene lo más importante...";
                yield return new WaitForSeconds(2f);
                textoInstrucciones.text = "Sigue la estrella con tus ojos\nsin mover la cabecita.";
                yield return new WaitForSeconds(3f);
                textoInstrucciones.text = "¡3... 2... 1... DESPEGUE!";
                yield return new WaitForSeconds(1f);
                textoInstrucciones.text = ""; 
            }
            
            // --- LIMPIEZA ABSOLUTA DE HUD ANTES DE MOSTRAR RESULTADOS ---
            if (headerPanel != null) headerPanel.SetActive(false);
            if (statusPanel != null) statusPanel.SetActive(false);
            if (subHeaderSecundario != null) subHeaderSecundario.SetActive(false);
            if (textoInstrucciones != null) textoInstrucciones.gameObject.SetActive(false);
            if (textoProgreso != null) textoProgreso.gameObject.SetActive(false);
            if (botonIniciarManual != null) botonIniciarManual.gameObject.SetActive(false);
            if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(false);
            
            // El botón Volver se mantiene (requerimiento senior)
            if (botonVolver != null) botonVolver.gameObject.SetActive(true);
            
            if (puntoCalibracion != null)
            {
                EnsureCalibrationPoint(); // Doble verificación para autocuración
                puntoCalibracion.gameObject.SetActive(true);
                puntoCalibracion.transform.SetAsLastSibling(); // Asegurar que está sobre el fondo superior
                Vector2 initialPoint = new Vector2(puntos[0].x, 1f - puntos[0].y);
                puntoCalibracion.rectTransform.anchorMin = initialPoint;
                puntoCalibracion.rectTransform.anchorMax = initialPoint;
                puntoCalibracion.rectTransform.anchoredPosition = Vector2.zero;
            }

            if (_calibThread == null)
            {
                Debug.Log("CalibradorKids: Creando CalibrationThread...");
                _calibThread = new CalibrationThread(EyeTracker.Instance.EyeTrackerInterface, screenBased: true);
            }
            
            float waitThread = 0;
            while (!_calibThread.Running && waitThread < 3f)
            {
                waitThread += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (!_calibThread.Running)
            {
                Debug.LogError("CalibradorKids: Error - El hilo de calibración no inició.");
                if (textoInstrucciones != null) textoInstrucciones.text = "Error de conexión. Reintentando...";
                _calibThread.StopThread();
                _calibThread = null;
                yield return new WaitForSeconds(2f);
                continue;
            }

            Debug.Log("CalibradorKids: Entrando en modo calibración.");
            var enterResult = _calibThread.EnterCalibrationMode();
            while (!enterResult.Ready) yield return null;

            if (enterResult.Status != CalibrationStatus.Success) {
                Debug.LogError("CalibradorKids: Fallo al entrar en modo calibración: " + enterResult.Status);
                _calibThread.StopThread();
                _calibThread = null;
                continue;
            }
            
            RectTransform ptRec = puntoCalibracion != null ? puntoCalibracion.rectTransform : null;
            
            for(int i = 0; i < puntos.Length; i++)
            {
                if (textoProgreso != null) textoProgreso.text = $"Posición {i + 1} / {puntos.Length}";

                Vector2 targetPoint = new Vector2(puntos[i].x, 1f - puntos[i].y);
                
                if (ptRec != null)
                {
                    float moveT = 0;
                    Vector2 startPoint = ptRec.anchorMin;
                    while (moveT < tiempoMovimiento)
                    {
                        moveT += Time.deltaTime;
                        float t = Mathf.Clamp01(moveT / tiempoMovimiento);
                        float progress = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                        
                        Vector2 cur = Vector2.LerpUnclamped(startPoint, targetPoint, progress);
                        ptRec.anchorMin = cur;
                        ptRec.anchorMax = cur;
                        ptRec.anchoredPosition = Vector2.zero;
                        yield return null;
                    }
                }

                var collectResult = _calibThread.CollectData(new CalibrationThread.Point(puntos[i]));
                float collectT = 0;
                while (!collectResult.Ready || collectT < tiempoRecoleccion)
                {
                    collectT += Time.deltaTime;
                    yield return null;
                }
            }

            if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(false);
            if (textoProgreso != null) textoProgreso.text = "Completado";
            if (textoInstrucciones != null) textoInstrucciones.text = "Generando evaluación...";
            yield return new WaitForSeconds(1.5f);

            var computeResult = _calibThread.ComputeAndApply();
            while (!computeResult.Ready) yield return null;

            Debug.Log($"[CALIBRACIÓN] Resultado Final: {computeResult.Status}");

            // --- DEPURACIÓN DETALLADA PARA EL USUARIO ---
            if (computeResult.Status != CalibrationStatus.Success && computeResult.LastResult != null)
            {
                Debug.LogWarning("--- DIAGNÓSTICO DE FALLO ---");
                int puntosOk = 0;
                foreach (var cp in computeResult.LastResult.CalibrationPoints)
                {
                    bool leftOk = false;
                    bool rightOk = false;
                    foreach (var sample in cp.CalibrationSamples)
                    {
                        if (sample.LeftEye.Validity == CalibrationEyeValidity.ValidAndUsed) leftOk = true;
                        if (sample.RightEye.Validity == CalibrationEyeValidity.ValidAndUsed) rightOk = true;
                    }
                    
                    if (leftOk && rightOk) puntosOk++;
                    Debug.Log($"Punto en {cp.PositionOnDisplayArea}: Ojo Izq {(leftOk?"OK":"ERROR")}, Ojo Der {(rightOk?"OK":"ERROR")}");
                }
                Debug.Log($"Total puntos válidos: {puntosOk} de {puntos.Length}");
                if (puntosOk < puntos.Length / 2) Debug.LogError("FALLO CRÍTICO: Menos del 50% de los puntos tienen datos.");
            }

            _calibThread.LeaveCalibrationMode();
            _calibThread.StopThread();
            _calibThread = null;

            yield return MostrarResultados(computeResult.Status == CalibrationStatus.Success);
        }
    }

    IEnumerator MostrarResultados(bool exito)
    {
        if (panelResultados != null) panelResultados.SetActive(true);
        if (panelCalibracion != null) panelCalibracion.SetActive(false);
        if (headerPanel != null) headerPanel.SetActive(false);
        if (statusPanel != null) statusPanel.SetActive(false);
        
        if (textoResultadoTitulo != null) {
            textoResultadoTitulo.text = exito ? "¡CALIBRACIÓN EXITOSA!" : "CALIBRACIÓN FALLIDA";
            textoResultadoTitulo.color = exito ? new Color(0.1f, 1f, 0.9f, 1f) : new Color(1f, 0.42f, 0.42f, 1f); 
            textoResultadoTitulo.gameObject.SetActive(true);
        }
        
        if (textoResultadoDesc != null) {
            textoResultadoDesc.text = exito ? "Los sensores están listos para la misión de hoy." : "No pudimos detectar tus ojos correctamente.\n¿Lo intentamos de nuevo?";
            textoResultadoDesc.gameObject.SetActive(true);
        }
        
        if (botonReintentar != null) {
            botonReintentar.gameObject.SetActive(!exito);
            var colors = botonReintentar.colors;
            colors.normalColor = new Color(1f, 0.5f, 0.5f); // Coral Red
            colors.highlightedColor = new Color(0.9f, 0.4f, 0.4f);
            colors.pressedColor = new Color(0.8f, 0.3f, 0.3f);
            botonReintentar.colors = colors;
            botonReintentar.onClick.RemoveAllListeners();
            botonReintentar.onClick.AddListener(() => {
                panelResultados.SetActive(false);
            });
        }
        
        if (botonContinuar != null) {
            botonContinuar.gameObject.SetActive(exito);
            var colors = botonContinuar.colors;
            colors.normalColor = Color.cyan;
            botonContinuar.colors = colors;
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() => SceneManager.LoadScene("SelectorActividades"));
        }

        if (exito) {
            if (textoContadorRedireccion != null) textoContadorRedireccion.gameObject.SetActive(true);
            float timer = 5f;
            while (timer > 0) {
                if (textoContadorRedireccion != null) textoContadorRedireccion.text = $"Redirigiendo en {Mathf.CeilToInt(timer)}...";
                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }
            SceneManager.LoadScene("SelectorActividades");
        }
        else {
            if (textoContadorRedireccion != null) textoContadorRedireccion.gameObject.SetActive(false);
        }
    }

    [ContextMenu("Debug: Mostrar Toda la UI")]
    public void MostrarTodaLaUI()
    {
        Debug.Log("CalibradorKids: Modo Diseño activado. Mostrando todos los paneles.");
        if (panelCalibracion != null) panelCalibracion.SetActive(true);
        if (panelResultados != null) panelResultados.SetActive(true);
        if (headerPanel != null) headerPanel.SetActive(true);
        if (statusPanel != null) statusPanel.SetActive(true);
        if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(true);
        
        if (textoInstrucciones != null) {
            textoInstrucciones.gameObject.SetActive(true);
            textoInstrucciones.text = "ESTO ES UN TEXTO DE PRUEBA (MODO DISEÑO)";
        }
        
        if (textoResultadoTitulo != null) textoResultadoTitulo.text = "¡TITULO DE RESULTADO!";
        if (textoProgreso != null) textoProgreso.text = "Posición X / Y";
    }

    public void Volver()
    {
        Debug.Log("Volviendo al menú principal...");
        SceneManager.LoadScene("MenuPrincipal");
    }
    
    private void OnDisable()
    {
        if (_calibThread != null)
        {
            _calibThread.StopThread();
            _calibThread = null;
        }
    }
}
