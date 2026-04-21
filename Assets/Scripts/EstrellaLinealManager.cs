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

    [Header("Overlay Resultados")]
    public GameObject overlayResult;
    public TMP_Text titleRes;
    public TMP_Text subRes;
    public TMP_Text percentRes;
    public Button btnAgain;

    [Header("Ajustes de Movimiento")]
    public float velocidadStar = 300f;
    public float velocidadDistractor = 400f;
    public float velocidadFondo = 80f; // <--- Sincronizado con SquareMovement
    public float zigzagAmplitud = 150f;
    public float zigzagFrecuencia = 3f;

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
        if (backgroundScroll == null) backgroundScroll = BuscarObjetoPotente("Background")?.GetComponent<RectTransform>();
        if (timerContainer == null) timerContainer = BuscarObjetoPotente("Time"); 
        
        if (timerContainer != null) textoTimer = textoTimer ?? timerContainer.GetComponentInChildren<TMP_Text>(true);

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
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>()) {
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
        juegoIniciado = true;
        _enConteo = false;
    }

    protected override void Update()
    {
        base.Update();
        if (_juegoFinalizado) return;

        // Scroll de fondo SIEMPRE para que la escena se sienta viva
        if (_bgSegments.Count > 0)
        {
            foreach (var bg in _bgSegments) MoverFondo(bg);
        }
        else if (backgroundScroll != null)
        {
            MoverFondo(backgroundScroll);
        }

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

        if (overlayResult != null)
        {
            overlayResult.SetActive(true);
            
            float precisionFinal = (_votosTotalesPrecision > 0) ? (_votosPositivosPrecision / (float)_votosTotalesPrecision) * 100f : 0;
            float avanceTotal = (_segundosMirando / 15f) * 100f; 
            
            if (titleRes != null) titleRes.text = "¡SESIÓN COMPLETADA!";
            if (percentRes != null) percentRes.text = precisionFinal.ToString("F0") + "%";
            if (subRes != null) subRes.text = $"Nivel Alcanzado: {Mathf.FloorToInt(avanceTotal/100)+1}\nTiempo: {duracionSesion}s";
            
            if (btnAgain != null)
            {
                btnAgain.onClick.RemoveAllListeners();
                btnAgain.onClick.AddListener(() => {
                    FinalizarActividad("Estrella Lineal", precisionFinal, true, _tiempoTranscurrido);
                });
                
                var txtBtn = btnAgain.GetComponentInChildren<TMP_Text>();
                if (txtBtn != null) txtBtn.text = "GUARDAR Y SALIR";
            }
        }
        else
        {
            float prec = (_votosTotalesPrecision > 0) ? (_votosPositivosPrecision / (float)_votosTotalesPrecision) * 100f : 0;
            FinalizarActividad("Estrella Lineal", prec, true, _tiempoTranscurrido);
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
            _ojosEstablesParaIniciar = (dist >= 40 && dist <= 80);
        }
        else if (_ojosEstablesParaIniciar)
        {
            _tiempoOjosCerrados += Time.deltaTime;
            if (_tiempoOjosCerrados >= 0.12f && _tiempoOjosCerrados <= 0.65f) { IniciarJuego(); _ojosEstablesParaIniciar = false; }
        }

        if (textoSub != null)
        {
            textoSub.text = _ojosEstablesParaIniciar ? "¡TE VEO! <b>Pestañea</b> para empezar" : "Buscando tus ojos... Mira al sensor";
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

        // AVANCE INFINITO (Cada 15s de acierto = 100%)
        float avanceTotal = (_segundosMirando / 15f) * 100f;
        float avanceCiclo = avanceTotal % 100.001f;
        if (avanceText != null) avanceText.text = (avanceTotal >= 100) ? "Lvl " + (Mathf.FloorToInt(avanceTotal/100)+1) : avanceCiclo.ToString("F0") + "%";
        if (barFill != null) barFill.fillAmount = (avanceTotal % 100f) / 100f;
    }

    void MoverObjetosAdicionales()
    {
        // 2. MOVIMIENTO DE ÚNICA ESTRELLA
        if (_convoyEstrellas != null)
        {
            foreach (var s in _convoyEstrellas)
            {
                s.anchoredPosition += Vector2.right * velocidadStar * Time.deltaTime;
                // Al salir por la derecha (>1200), vuelve a la izquierda (-1200)
                if (s.anchoredPosition.x > 1200) s.anchoredPosition = new Vector2(-1200, s.anchoredPosition.y);
            }
        }

        // 3. DISTRACTOR (Zigzag + Scroll de mundo)
        if (distractor != null)
        {
            float sinY = Mathf.Sin(Time.time * zigzagFrecuencia) * zigzagAmplitud;
            distractor.anchoredPosition += Vector2.left * (velocidadDistractor + velocidadFondo) * Time.deltaTime;
            distractor.anchoredPosition = new Vector2(distractor.anchoredPosition.x, sinY);
            if (distractor.anchoredPosition.x < -1200) distractor.anchoredPosition = new Vector2(1200, 0);
        }
    }

    void MoverFondo(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchoredPosition += Vector2.left * velocidadFondo * Time.deltaTime;
        float width = rt.rect.width;
        if (rt.anchoredPosition.x < -width) rt.anchoredPosition += new Vector2(width * 2, 0);
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
        if (gaze != null && (gaze.Left.GazePointValid || gaze.Right.GazePointValid))
        {
            Vector2 viewPos = new Vector2(
                (gaze.Left.GazePointOnDisplayArea.x + gaze.Right.GazePointOnDisplayArea.x) / 2f,
                (gaze.Left.GazePointOnDisplayArea.y + gaze.Right.GazePointOnDisplayArea.y) / 2f
            );
            Vector2 screenPos = new Vector2(viewPos.x * Screen.width, (1f - viewPos.y) * Screen.height);

            if (_convoyEstrellas != null)
            {
                foreach (var s in _convoyEstrellas)
                {
                    float padding = 60f;
                    Vector3 worldPos = s.position;
                    Vector2 size = Vector2.Scale(s.rect.size, s.lossyScale) + new Vector2(padding, padding);
                    Rect easyRect = new Rect(worldPos.x - size.x / 2, worldPos.y - size.y / 2, size.x, size.y);
                    if (easyRect.Contains(screenPos)) { _framesTargeteados++; _segundosMirando += Time.deltaTime; break; }
                }
            }
        }
    }
}
