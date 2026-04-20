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
    public GameObject timerContainer; // El objeto "time" que mencionas
    public TMP_Text textoTimer; 
    public TMP_Text precText;
    public TMP_Text avanceText;
    public Image barFill; // Nuevo: La barra de progreso azul/verde
    public TMP_Text textoSub; // Nuevo: Para los mensajes de "Buscando ojos..."

    [Header("Ajustes de Movimiento")]
    public float velocidadStar = 300f;
    public float velocidadDistractor = 400f;
    public float zigzagAmplitud = 150f;
    public float zigzagFrecuencia = 3f;

    private float _tiempoRestante = 30f;
    private int _framesTargeteados = 0;
    private int _framesTotales = 0;
    private Vector2 _dirStar = Vector2.right;
    private Vector2 _dirDistractor = Vector2.left;

    // Control de parpadeo para iniciar
    private float _tiempoOjosCerrados = 0f;
    private bool _ojosEstablesParaIniciar = false;
    private bool _enConteo = false;

    // Métricas de tiempo de actualización
    private float _timerUI_Avance = 0f;
    private float _timerUI_Precision = 0f;
    private int _votosPositivosPrecision = 0;
    private int _votosTotalesPrecision = 0;

    protected override void Start()
    {
        AutoVincularSeguimiento(); // 1. Buscamos los objetos primero
        base.Start();              // 2. Ejecutamos la base (encenderá el overlay)
        
        // 3. Encendido forzado de seguridad
        if (overlayInicio != null) overlayInicio.SetActive(true);

        PreconfigurarPosiciones();
    }

    void AutoVincularSeguimiento()
    {
        // Buscamos con un método más potente e insensible a mayúsculas
        if (star == null) star = BuscarObjetoPotente("Star")?.GetComponent<RectTransform>();
        if (distractor == null) distractor = BuscarObjetoPotente("Distractor")?.GetComponent<RectTransform>();
        if (timerContainer == null) timerContainer = BuscarObjetoPotente("Time"); // Plan B
        
        if (timerContainer != null) {
            textoTimer = textoTimer ?? timerContainer.GetComponentInChildren<TMP_Text>(true);
        }

        if (precText == null) precText = BuscarObjetoPotente("PrecText")?.GetComponent<TMP_Text>();
        if (avanceText == null) avanceText = BuscarObjetoPotente("AvanceText")?.GetComponent<TMP_Text>();
        if (barFill == null) barFill = BuscarObjetoPotente("BarFill")?.GetComponent<Image>();

        // 1. El contador va al objeto 'Contador'
        if (textoMensajeInicio == null) {
            GameObject contObj = BuscarObjetoPotente("Contador");
            if (contObj != null) {
                textoMensajeInicio = contObj.GetComponentInChildren<TMP_Text>(true);
                contObj.SetActive(false); // Mantenerlo oculto hasta el conteo
            }
        }
        
        // 2. Las instrucciones van al objeto 'Sub'
        if (textoSub == null) {
            GameObject subObj = BuscarObjetoPotente("Sub");
            if (subObj != null) textoSub = subObj.GetComponentInChildren<TMP_Text>(true);
        }

        if (overlayInicio == null) overlayInicio = BuscarObjetoPotente("OverlayInicio");
    }

    GameObject BuscarObjetoPotente(string nombre)
    {
        // 1. Intentamos búsqueda normal
        GameObject obj = GameObject.Find(nombre);
        if (obj != null) return obj;

        // 2. Si no, recorremos TODO el escenario (incluyendo desactivados) de forma insensible
        string busqueda = nombre.ToLower();
        Transform[] todos = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in todos) {
            if (t.name.ToLower() == busqueda) return t.gameObject;
        }
        return null;
    }

    void PreconfigurarPosiciones()
    {
        if (star != null) star.anchoredPosition = new Vector2(-400, 0);
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
        
        // Asegurar visibilidad del contador
        if (textoMensajeInicio != null) {
            textoMensajeInicio.gameObject.SetActive(true);
            textoMensajeInicio.transform.parent.gameObject.SetActive(true); // Activar carpeta padre si existe
            textoMensajeInicio.color = Color.white;
        }

        if (overlayInicio != null) {
            // Buscamos y apagamos TODO lo que no sea el contador para dejar espacio limpio
            Transform instT = overlayInicio.transform.Find("Inst");
            Transform btnT = overlayInicio.transform.Find("StartButton");
            Transform subT = overlayInicio.transform.Find("Sub"); // Añadido: Apagar el Sub también

            if (instT != null) instT.gameObject.SetActive(false);
            if (btnT != null) btnT.gameObject.SetActive(false);
            if (subT != null) subT.gameObject.SetActive(false);
        }

        for (int i = 3; i > 0; i--)
        {
            if (textoMensajeInicio != null) 
            {
                textoMensajeInicio.text = i.ToString();
            }
            yield return new WaitForSecondsRealtime(1f);
        }

        if (textoMensajeInicio != null) 
            textoMensajeInicio.text = "¡A VOLAR!";
        
        yield return new WaitForSecondsRealtime(0.7f);

        // AHORA bajamos el telón y mostramos el tiempo y el juego
        if (overlayInicio != null) overlayInicio.SetActive(false);
        if (textoMensajeInicio != null) textoMensajeInicio.gameObject.SetActive(false); 
        
        // Limpiar y apagar instrucciones
        if (textoSub != null) {
            textoSub.text = "";
            textoSub.gameObject.SetActive(false);
        }
        
        // ACTIVACIÓN FORZADA DE OBJETOS
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
    }

    protected override void Update()
    {
        base.Update();
        
        // FASE DE INICIO: Parpadeo para arrancar (SOLO si no estamos ya contando)
        if (!juegoIniciado && !juegoPausado && !_enConteo)
        {
            ManejarPestañeoInicio();
            return; 
        }

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
                    Debug.Log("<color=cyan>Pestañeo de inicio detectado!</color>");
                    IniciarJuego(); // Lanza el countdown
                    _ojosEstablesParaIniciar = false;
                }
            }
        }

        // Actualizar mensaje de BaseActividad a través del objeto 'Sub'
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

        if (_tiempoRestante <= 0)
        {
            juegoIniciado = false;
            
            if (textoTimer != null) textoTimer.text = "Completado";

            float precisionFinal = (_framesTotales > 0) ? (_framesTargeteados / (float)_framesTotales) * 100f : 0;
            
            // Guardamos pero NO redirigimos
            if (GestorPaciente.Instance != null)
            {
                GestorPaciente.Instance.GuardarPartida("Estrella Lineal", Mathf.RoundToInt(precisionFinal), precisionFinal, true, 30f);
            }
        }
    }

    void MoverObjetos()
    {
        // 1. STAR: Movimiento Lineal con rebote
        if (star != null)
        {
            star.anchoredPosition += _dirStar * velocidadStar * Time.deltaTime;
            if (Mathf.Abs(star.anchoredPosition.x) > 800) _dirStar *= -1;
        }

        // 2. DISTRACTOR: Zigzag opuesto
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
            // Convertimos la mirada de Tobii (Normalizada 0-1) a Pantalla
            Vector2 viewPos = new Vector2(
                (gaze.Left.GazePointOnDisplayArea.x + gaze.Right.GazePointOnDisplayArea.x) / 2f,
                (gaze.Left.GazePointOnDisplayArea.y + gaze.Right.GazePointOnDisplayArea.y) / 2f
            );

            Vector2 screenPos = new Vector2(viewPos.x * Screen.width, (1f - viewPos.y) * Screen.height);

            // ¿Está el punto dentro del Rect de la estrella?
            if (star != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

                if (RectTransformUtility.RectangleContainsScreenPoint(star, screenPos, cam))
                {
                    _framesTargeteados++;
                }
            }
        }

        // --- UI REFINADA CON TIEMPOS DE ACTUALIZACIÓN ---
        _timerUI_Avance += Time.deltaTime;
        _timerUI_Precision += Time.deltaTime;

        // 1. PRECISIÓN (Cada 1 segundo): Calidad del foco
        if (_timerUI_Precision >= 1f)
        {
            if (precText != null) 
            {
                float calidatFoco = (_votosTotalesPrecision > 0) ? (_votosPositivosPrecision / (float)_votosTotalesPrecision) * 100f : 0;
                precText.text = calidatFoco.ToString("F0");
                precText.color = calidatFoco > 70 ? Color.green : Color.yellow;
            }
            _timerUI_Precision = 0;
            _votosPositivosPrecision = 0;
            _votosTotalesPrecision = 0;
        }
        else 
        {
            // Acumulamos votos durante el segundo actual
            _votosTotalesPrecision++;
            var gazeData = EyeTracker.Instance.LatestGazeData;
            if (gazeData != null && (gazeData.Left.GazeOriginValid || gazeData.Right.GazeOriginValid))
                _votosPositivosPrecision++;
        }

        // 2. AVANCE (Cada 2 segundos): Progreso acumulado
        if (_timerUI_Avance >= 2f)
        {
            if (avanceText != null && _framesTotales > 0) 
            {
                float avance = (_framesTargeteados / (float)_framesTotales) * 100f;
                avanceText.text = avance.ToString("F0") + "%";
                if (barFill != null) barFill.fillAmount = avance / 100f;
            }
            _timerUI_Avance = 0;
        }
    }

    // Nueva versión de finalizar que aprovecha los datos de GestorPaciente
    void FinalizarActividadConDatos(string nombre, int puntos, float precision, bool exito, float tiempo)
    {
        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.GuardarPartida(nombre, puntos, precision, exito, tiempo);
        }
        SceneManager.LoadScene("History");
    }
}
