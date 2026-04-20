using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using Tobii.Research.Unity;

public class EstrellaLinealManager : BaseActividad
{
    [Header("Objetos de Juego")]
    public RectTransform star;
    public RectTransform distractor;
    public GameObject timerContainer; 
    public TMP_Text textoTimer; 
    public TMP_Text precText;
    public TMP_Text avanceText;
    public Image barFill; 
    public TMP_Text textoSub; 
    public GameObject panelDetalle; 

    [Header("Overlay Resultados")]
    public GameObject overlayResult;
    public TMP_Text titleRes;
    public TMP_Text subRes;
    public TMP_Text percentRes;
    public Button btnAgain;

    [Header("Ajustes de Movimiento")]
    public float velocidadStar = 300f;
    public float velocidadDistractor = 400f;
    public float zigzagAmplitud = 150f;
    public float zigzagFrecuencia = 3f;

    private float _tiempoRestante = 30f;
    private int _framesTargeteados = 0;
    private int _framesTotales = 0;
    private Vector2 _dirDistractor = Vector2.left;
    
    private RectTransform[] _convoyEstrellas; // Para el movimiento infinito

    // Control de parpadeo para iniciar
    private float _tiempoOjosCerrados = 0f;
    private bool _ojosEstablesParaIniciar = false;
    private bool _enConteo = false;

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

    void AutoVincularSeguimiento()
    {
        if (star == null) star = BuscarObjetoPotente("Star")?.GetComponent<RectTransform>();
        if (distractor == null) distractor = BuscarObjetoPotente("Distractor")?.GetComponent<RectTransform>();
        if (timerContainer == null) timerContainer = BuscarObjetoPotente("Time"); 
        
        if (timerContainer != null) {
            textoTimer = textoTimer ?? timerContainer.GetComponentInChildren<TMP_Text>(true);
        }

        if (precText == null) precText = BuscarObjetoPotente("PrecText")?.GetComponent<TMP_Text>();
        if (avanceText == null) avanceText = BuscarObjetoPotente("AvanceText")?.GetComponent<TMP_Text>();
        if (barFill == null) barFill = BuscarObjetoPotente("BarFill")?.GetComponent<Image>();

        if (textoMensajeInicio == null) {
            GameObject contObj = BuscarObjetoPotente("Contador");
            if (contObj != null) {
                textoMensajeInicio = contObj.GetComponentInChildren<TMP_Text>(true);
                contObj.SetActive(false); 
            }
        }
        
        if (textoSub == null) {
            GameObject subObj = BuscarObjetoPotente("Sub");
            if (subObj != null) textoSub = subObj.GetComponentInChildren<TMP_Text>(true);
        }

        if (overlayInicio == null) overlayInicio = BuscarObjetoPotente("OverlayInicio");
        if (overlayResult == null) overlayResult = BuscarObjetoPotente("OverlayResult");
        if (panelDetalle == null) panelDetalle = BuscarObjetoPotente("detalle");
        
        if (overlayResult != null)
        {
            titleRes = titleRes ?? overlayResult.transform.Find("TitleRes")?.GetComponent<TMP_Text>();
            subRes = subRes ?? overlayResult.transform.Find("Subres")?.GetComponent<TMP_Text>();
            percentRes = percentRes ?? overlayResult.transform.Find("PercentRes")?.GetComponent<TMP_Text>();
            btnAgain = btnAgain ?? overlayResult.transform.Find("StartButton")?.GetComponent<Button>();
            
            overlayResult.SetActive(false); 
        }
    }

    GameObject BuscarObjetoPotente(string nombre)
    {
        GameObject obj = GameObject.Find(nombre);
        if (obj != null) return obj;

        string busqueda = nombre.ToLower();
        Transform[] todos = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in todos) {
            if (t.name.ToLower() == busqueda && !string.IsNullOrEmpty(t.gameObject.scene.name)) return t.gameObject;
        }
        return null;
    }

    void PreconfigurarPosiciones()
    {
        if (star != null) 
        {
            star.anchoredPosition = new Vector2(-400, 0);
            
            // CREAR EL CONVOY (Duplicamos la estrella para que siempre haya una entrando)
            GameObject estrella2 = Instantiate(star.gameObject, star.parent);
            estrella2.name = "Star_2";
            RectTransform rt2 = estrella2.GetComponent<RectTransform>();
            rt2.anchoredPosition = new Vector2(-1400, 0); // La ponemos lejos a la izquierda
            
            _convoyEstrellas = new RectTransform[] { star, rt2 };
        }
        
        if (distractor != null) distractor.anchoredPosition = new Vector2(400, 100);
    }

    public override void IniciarJuego()
    {
        if (_enConteo || juegoIniciado) return;
        StartCoroutine(RutinaCountdown());
    }

    IEnumerator RutinaCountdown()
    {
        _enConteo = true;

        // --- LIMPIEZA TOTAL ANTES DEL CONTEO ---
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

        if (textoMensajeInicio != null) textoMensajeInicio.text = "¡YA!";
        yield return new WaitForSecondsRealtime(0.7f);

        // --- CIERRE SINCRONIZADO DE TODO EL ENTORNO DE INICIO ---
        if (overlayInicio != null) overlayInicio.SetActive(false);
        if (panelDetalle != null) panelDetalle.SetActive(false);
        if (textoMensajeInicio != null) textoMensajeInicio.gameObject.SetActive(false); 
        
        if (textoSub != null) {
            textoSub.text = "";
            textoSub.gameObject.SetActive(false);
        }
        
        if (star != null) star.gameObject.SetActive(true);
        if (distractor != null) distractor.gameObject.SetActive(true);
        
        if (timerContainer != null) 
        {
            timerContainer.SetActive(true);
            var t = timerContainer.GetComponentInChildren<TMP_Text>(true);
            if (t != null) {
                t.gameObject.SetActive(true);
                t.text = "00:30"; 
            }
        }

        _tiempoRestante = 30f;
        _framesTargeteados = 0;
        _framesTotales = 0;
        juegoIniciado = true;
        _enConteo = false;

        // Inicialización de textos para que no estén vacíos
        if (avanceText != null) avanceText.text = "0%";
        if (precText != null) precText.text = "0";
    }

    protected override void Update()
    {
        base.Update();
        if (!juegoIniciado && !juegoPausado && !_enConteo) { ManejarPestañeoInicio(); return; }

        if (juegoIniciado && !juegoPausado)
        {
            ManejarTiempo();
            MoverObjetos();
            ProcesarSeguimientoOcular();
        }
    }

    void ManejarPestañeoInicio()
    {
        var gaze = EyeTracker.Instance.LatestGazeData;
        bool detectado = gaze != null && (gaze.Left.GazeOriginValid || gaze.Right.GazeOriginValid);

        if (detectado)
        {
            _tiempoOjosCerrados = 0f;
            float dist = (gaze.Left.GazeOriginInUserCoordinates.z + gaze.Right.GazeOriginInUserCoordinates.z) / 20f;
            if (dist >= 40 && dist <= 80) _ojosEstablesParaIniciar = true;
            else _ojosEstablesParaIniciar = false;
        }
        else
        {
            if (_ojosEstablesParaIniciar)
            {
                _tiempoOjosCerrados += Time.deltaTime;
                if (_tiempoOjosCerrados >= 0.12f && _tiempoOjosCerrados <= 0.65f)
                {
                    IniciarJuego(); 
                    _ojosEstablesParaIniciar = false;
                }
            }
        }

        if (textoSub != null)
        {
            if (_ojosEstablesParaIniciar) {
                textoSub.text = "¡He detectado tus ojos! <b>Pestañea</b> para empezar";
                textoSub.color = Color.cyan;
            } else {
                textoSub.text = "Buscando tus ojos... Mira al sensor";
                textoSub.color = Color.white;
            }
        }
    }

    void ManejarTiempo()
    {
        _tiempoRestante -= Time.deltaTime;
        if (textoTimer != null && _tiempoRestante > 0) 
        {
            int segundos = Mathf.CeilToInt(_tiempoRestante);
            textoTimer.text = string.Format("00:{0:00}", segundos);
        }

        if (_tiempoRestante <= 0 && juegoIniciado) FinalizarJuego();
    }

    void FinalizarJuego()
    {
        juegoIniciado = false;
        float precisionFinal = (_framesTotales > 0) ? (_framesTargeteados / (float)_framesTotales) * 100f : 0;
        bool exito = precisionFinal >= 70f;

        if (overlayResult != null)
        {
            overlayResult.SetActive(true);
            if (titleRes != null) {
                titleRes.text = exito ? "¡MISIÓN CUMPLIDA!" : "¡MISIÓN FALLIDA!";
                titleRes.color = exito ? Color.cyan : Color.yellow;
            }
            if (percentRes != null) {
                percentRes.text = precisionFinal.ToString("F0") + "%";
                percentRes.color = exito ? Color.green : Color.red;
            }
            if (subRes != null) {
                subRes.text = $"Precisión: {precisionFinal:F0}%\nObjetivo: 70%\nSesión: 30 seg.";
            }
            if (btnAgain != null) {
                btnAgain.gameObject.SetActive(!exito); 
                btnAgain.onClick.RemoveAllListeners();
                btnAgain.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
            }
        }

        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.GuardarPartida("Estrella Lineal", Mathf.RoundToInt(precisionFinal), precisionFinal, true, 30f);
        }
    }

    void MoverObjetos()
    {
        // 1. CONVOY DE ESTRELLAS: Movimiento Lineal INFINITO Real
        if (_convoyEstrellas != null)
        {
            foreach (var s in _convoyEstrellas)
            {
                s.anchoredPosition += Vector2.right * velocidadStar * Time.deltaTime;
                
                // Si la estrella sale por la derecha (+1000), se va a la cola por la izquierda (-1000)
                if (s.anchoredPosition.x > 1000) {
                    s.anchoredPosition = new Vector2(-1000, s.anchoredPosition.y);
                }
            }
        }

        if (distractor != null)
        {
            float sinY = Mathf.Sin(Time.time * zigzagFrecuencia) * zigzagAmplitud;
            distractor.anchoredPosition += _dirDistractor * velocidadDistractor * Time.deltaTime;
            distractor.anchoredPosition = new Vector2(distractor.anchoredPosition.x, sinY);
            if (Mathf.Abs(distractor.anchoredPosition.x) > 800) _dirDistractor *= -1;
        }
    }

    void ProcesarSeguimientoOcular()
    {
        _framesTotales++;
        var gaze = EyeTracker.Instance.LatestGazeData;
        if (gaze != null && (gaze.Left.GazePointValid || gaze.Right.GazePointValid))
        {
            Vector2 viewPos = new Vector2(
                (gaze.Left.GazePointOnDisplayArea.x + gaze.Right.GazePointOnDisplayArea.x) / 2f,
                (gaze.Left.GazePointOnDisplayArea.y + gaze.Right.GazePointOnDisplayArea.y) / 2f
            );
            Vector2 screenPos = new Vector2(viewPos.x * Screen.width, (1f - viewPos.y) * Screen.height);

            // ¿Está el punto dentro de alguna de las estrellas del convoy?
            if (_convoyEstrellas != null)
            {
                foreach (var s in _convoyEstrellas)
                {
                    float padding = 50f;
                    Vector3 worldPos = s.position;
                    Vector2 size = Vector2.Scale(s.rect.size, s.lossyScale) + new Vector2(padding, padding);
                    Rect easyRect = new Rect(worldPos.x - size.x / 2, worldPos.y - size.y / 2, size.x, size.y);

                    if (easyRect.Contains(screenPos))
                    {
                        _framesTargeteados++;
                        break; // Solo sumamos una vez aunque 'toque' las dos
                    }
                }
            }
        }

        _votosTotalesPrecision++;
        var gazeData = EyeTracker.Instance.LatestGazeData;
        if (gazeData != null && (gazeData.Left.GazeOriginValid || gazeData.Right.GazeOriginValid))
            _votosPositivosPrecision++;

        // 2. ACTUALIZACIÓN DE PRECISIÓN (Cada frame para que no desaparezca)
        if (precText != null) 
        {
            float calidatFoco = (_votosTotalesPrecision > 0) ? (_votosPositivosPrecision / (float)_votosTotalesPrecision) * 100f : 0;
            precText.text = calidatFoco.ToString("F0");
            precText.color = calidatFoco > 70 ? Color.green : Color.yellow;

            // Reseteamos el contador de promedio cada segundo para suavizar, pero sin borrar el texto
            if (_timerUI_Precision >= 1f) {
                _timerUI_Precision = 0;
                _votosPositivosPrecision = 0;
                _votosTotalesPrecision = 0;
            }
        }

        // 3. ACTUALIZACIÓN DE AVANCE (Progresivo por TIEMPO)
        float avance = Mathf.Clamp01((30f - _tiempoRestante) / 30f) * 100f;
        if (avanceText != null) avanceText.text = avance.ToString("F0") + "%";
        if (barFill != null) barFill.fillAmount = avance / 100f;
    }
}
