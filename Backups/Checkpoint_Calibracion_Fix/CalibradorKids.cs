using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Tobii.Research.Unity;
using Tobii.Research;

public class CalibradorKids : MonoBehaviour
{
    [Header("UI Elements (Auto-assigned if null)")]
    public GameObject panelCalibracion;
    public GameObject panelResultados; // NEW
    public Image puntoCalibracion;
    public Text textoInstrucciones;
    public Text textoProgreso; // NEW
    public Button botonVolver;
    [Tooltip("Botón opcional para forzar inicio manual sin pestañear")]
    public Button botonIniciarManual;

    [Header("Result UI")]
    public Text textoResultadoTitulo;
    public Text textoResultadoDesc;
    public Button botonReintentar;
    public Button botonContinuar;
    public Text textoContadorRedireccion;

    [Header("Settings")]
    [Tooltip("El script internamente decidirá (entre 4 y 6 puntos) y los aleatorizará en el Start()")]
    [HideInInspector] public Vector2[] puntos;
    
    [Tooltip("Tiempo en segundos que tarda la estrella en moverse de un punto a otro.")]
    public float tiempoMovimiento = 0.8f; 
    
    [Tooltip("Tiempo mínimo que la estrella estará rotando (pulso) recolectando datos.")]
    public float tiempoRecoleccion = 1.2f;

    private CalibrationThread _calibThread;
    private bool inicioManualPulsado = false;
    private Text textoContadorPosiciones;

    void Awake()
    {
        // Búsqueda exhaustiva de referencias si no están asignadas
        if (panelCalibracion == null) panelCalibracion = FindObjectByName("CalibrationPanel");
        if (panelResultados == null) panelResultados = FindObjectByName("ResultPanel");
        if (puntoCalibracion == null) puntoCalibracion = FindObjectByName("PuntoCalibracion")?.GetComponent<Image>();
        
        if (textoInstrucciones == null) textoInstrucciones = FindObjectByName("SubHeader_Instruccion")?.GetComponent<Text>();
        if (textoProgreso == null) textoProgreso = FindObjectByName("ProgresoText")?.GetComponent<Text>();
        if (botonVolver == null) botonVolver = FindObjectByName("VolverBtn")?.GetComponent<Button>();
        if (botonIniciarManual == null) botonIniciarManual = FindObjectByName("IniciarManual")?.GetComponent<Button>();
        
        // Result Panel Auto-assignment
        if (textoResultadoTitulo == null) textoResultadoTitulo = FindObjectByName("ResultTitle")?.GetComponent<Text>();
        if (textoResultadoDesc == null) textoResultadoDesc = FindObjectByName("ResultDesc")?.GetComponent<Text>();
        if (botonReintentar == null) botonReintentar = FindObjectByName("RetryBtn")?.GetComponent<Button>();
        if (botonContinuar == null) botonContinuar = FindObjectByName("ContinueBtn")?.GetComponent<Button>();
        if (textoContadorRedireccion == null) textoContadorRedireccion = FindObjectByName("CountdownText")?.GetComponent<Text>();

        if (puntoCalibracion != null) puntoCalibracion.gameObject.SetActive(false);
        if (panelResultados != null) panelResultados.SetActive(false);
    }

    void Start()
    {
        // --- INICIO GENERACIÓN ALEATORIA (4 A 6 PUNTOS) ---
        int numPuntos = Random.Range(4, 7);
        List<Vector2> seleccionPuntos = new List<Vector2>();

        // Puntos cardinales ancla
        seleccionPuntos.Add(new Vector2(0.1f, 0.1f)); 
        seleccionPuntos.Add(new Vector2(0.9f, 0.1f)); 
        seleccionPuntos.Add(new Vector2(0.1f, 0.9f)); 
        seleccionPuntos.Add(new Vector2(0.9f, 0.9f)); 

        if (numPuntos >= 5) seleccionPuntos.Add(new Vector2(0.5f, 0.5f));

        if (numPuntos == 6)
        {
            Vector2[] extra = new Vector2[] { new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.9f), new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f) };
            seleccionPuntos.Add(extra[Random.Range(0, extra.Length)]);
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

        // Asegurar que el el panel esté activo
        if (panelCalibracion != null) panelCalibracion.SetActive(true);
        
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
        
        if (textoInstrucciones != null) textoInstrucciones.text = "Buscando sensores Tobii...";

        while (EyeTracker.Instance == null || EyeTracker.Instance.EyeTrackerInterface == null)
        {
            yield return new WaitForSeconds(0.5f);
            if (textoInstrucciones != null) textoInstrucciones.text = "Conectando sensores espaciales...";
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

                        if (textoInstrucciones != null)
                        {
                            if (currentZDist > 0 && currentZDist < 45) 
                                textoInstrucciones.text = "<color=red>¡DEMASIADO CERCA!</color>\nAléjate un poquito.";
                            else if (currentZDist > 75)
                                textoInstrucciones.text = "<color=yellow>¡MÁS CERCA!</color>\nAcércate un poquito.";
                            else
                                textoInstrucciones.text = "¡ESTÁS PERFECTO!\nParpadea ahora para empezar.";
                        }
                    }
                }

                // Lógica de parpadeo: Detectar cuando se pierden los ojos tras estar en la distancia correcta
                if (!vistaDetectada)
                {
                    // Solo contamos el tiempo de ojos cerrados si estábamos en el rango de distancia
                    if (currentZDist >= 45 && currentZDist <= 75 || currentZDist == -1) 
                    {
                        tiempoOjosCerrados += Time.deltaTime;
                        if (tiempoOjosCerrados > 0.2f && tiempoOjosCerrados < 0.8f) 
                        { 
                            Debug.Log("CalibradorKids: Parpadeo validado.");
                            listoParaCalibrar = true; 
                        }
                    }

                    // Delay para el mensaje de "Buscando ojos" para no parpadear el texto durante un parpadeo real
                    if (tiempoOjosCerrados > 0.8f && textoInstrucciones != null)
                    {
                        textoInstrucciones.text = "Buscando tus ojos...\nmira hacia el sensor.";
                    }
                }
                else 
                { 
                    tiempoOjosCerrados = 0; 
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
            
            if (botonIniciarManual != null) botonIniciarManual.gameObject.SetActive(false);
            if (botonVolver != null) botonVolver.gameObject.SetActive(false);
            
            if (puntoCalibracion != null)
            {
                puntoCalibracion.gameObject.SetActive(true);
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
                if (textoProgreso != null) textoProgreso.text = $"{i + 1} / {puntos.Length}";
                if (textoInstrucciones != null) textoInstrucciones.text = "Sigue la estrella mágica...";

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

            _calibThread.LeaveCalibrationMode();
            _calibThread.StopThread();
            _calibThread = null;

            yield return MostrarResultados(computeResult.Status == CalibrationStatus.Success);
            break; 
        }
    }

    IEnumerator MostrarResultados(bool exito)
    {
        if (panelResultados != null) panelResultados.SetActive(true);
        if (panelCalibracion != null) panelCalibracion.SetActive(false);
        
        if (textoResultadoTitulo != null) textoResultadoTitulo.text = exito ? "¡CALIBRACIÓN EXITOSA!" : "CALIBRACIÓN FALLIDA";
        if (textoResultadoDesc != null) textoResultadoDesc.text = exito ? "Los sensores están listos para la misión." : "Hubo un error al detectar tus ojos.";
        
        if (botonReintentar != null) {
            botonReintentar.gameObject.SetActive(!exito);
            botonReintentar.onClick.RemoveAllListeners();
            botonReintentar.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        }
        
        if (botonContinuar != null) {
            botonContinuar.gameObject.SetActive(exito);
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(Volver);
        }

        if (exito)
        {
            float timer = 5f;
            while (timer > 0)
            {
                if (textoContadorRedireccion != null) textoContadorRedireccion.text = $"Redirigiendo en {Mathf.CeilToInt(timer)}s...";
                timer -= Time.deltaTime;
                yield return null;
            }
            Volver();
        }
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
