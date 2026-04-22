using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Tobii.Research.Unity;

/// <summary>
/// Nueva actividad de Carrera Ocular.
/// El jugador se mueve arriba/abajo según la zona de la pantalla que mire el paciente.
/// Debe esquivar obstáculos que vienen desde la derecha.
/// </summary>
public class CarreraOcularManager : BaseActividad
{
    [Header("Objetos de Juego")]
    public RectTransform jugador;
    public RectTransform contenedorObstaculos;
    public RectTransform backgroundScroll;
    
    [Header("UI y Feedback")]
    public TMP_Text textoTimer;
    public TMP_Text textoVidas;
    public TMP_Text textoNivel;
    public GameObject overlayResult;
    
    [Header("Ajustes de Movimiento")]
    public float velocidadJugador = 10f;
    public float velocidadObstaculos = 400f;
    public float velocidadFondo = 200f;
    public float alturaArriba = 250f;
    public float alturaAbajo = -250f;
    
    [Header("Ajustes de Spawn")]
    public GameObject prefabObstaculo;
    public float tiempoEntreObstaculos = 2f;
    
    [Header("Configuración")]
    public float duracionSesion = 60f;
    public int vidasMaximas = 3;

    // Estado interno
    private float _tiempoTranscurrido = 0f;
    private int _vidasActuales;
    private float _targetY;
    private List<RectTransform> _obstaculosActivos = new List<RectTransform>();
    private List<RectTransform> _bgSegments = new List<RectTransform>();
    private float _spawnTimer = 0f;
    private bool _juegoFinalizado = false;

    protected override void Start()
    {
        base.Start();
        VincularUI();
        _vidasActuales = vidasMaximas;
        _targetY = 0;
        PreconfigurarEscena();
    }

    void VincularUI()
    {
        // Si no están asignados, los buscamos por nombre
        if (jugador == null) jugador = GameObject.Find("Player")?.GetComponent<RectTransform>();
        if (backgroundScroll == null) backgroundScroll = GameObject.Find("BG")?.GetComponent<RectTransform>();
        if (contenedorObstaculos == null) contenedorObstaculos = GameObject.Find("Obstacles")?.GetComponent<RectTransform>();
    }

    void PreconfigurarEscena()
    {
        // Configurar fondo infinito
        if (backgroundScroll != null)
        {
            _bgSegments.Clear();
            _bgSegments.Add(backgroundScroll);
            float ancho = backgroundScroll.rect.width > 0 ? backgroundScroll.rect.width : Screen.width;
            
            GameObject clon = Instantiate(backgroundScroll.gameObject, backgroundScroll.parent);
            clon.name = "BG_Loop";
            RectTransform rtClon = clon.GetComponent<RectTransform>();
            rtClon.anchoredPosition = new Vector2(ancho, 0);
            _bgSegments.Add(rtClon);
            
            rtClon.SetAsFirstSibling();
            backgroundScroll.SetAsFirstSibling();
        }

        if (jugador != null) jugador.anchoredPosition = new Vector2(-Screen.width * 0.35f, 0);
    }

    public override void IniciarJuego()
    {
        if (juegoIniciado) return;
        juegoIniciado = true;
        _tiempoTranscurrido = 0f;
    }

    protected override void Update()
    {
        base.Update();
        if (_juegoFinalizado) return;

        MoverFondo();

        if (!juegoIniciado || juegoPausado) return;

        _tiempoTranscurrido += Time.deltaTime;
        ActualizarReloj();

        if (_tiempoTranscurrido >= duracionSesion)
        {
            FinalizarActividadLocal();
            return;
        }

        ProcesarEntradaTobii();
        MoverJugador();
        GestionarObstaculos();
    }

    void MoverFondo()
    {
        foreach (var seg in _bgSegments)
        {
            seg.anchoredPosition += Vector2.left * velocidadFondo * Time.deltaTime;
            if (seg.anchoredPosition.x < -seg.rect.width)
                seg.anchoredPosition += new Vector2(seg.rect.width * 2f, 0);
        }
    }

    void ProcesarEntradaTobii()
    {
        var gaze = EyeTracker.Instance.LatestGazeData;
        if (gaze == null || (!gaze.Left.GazePointValid && !gaze.Right.GazePointValid)) return;

        // Promedio de mirada en coordenadas 0..1 (Y: 0 abajo, 1 arriba)
        float avgY = (gaze.Left.GazePointOnDisplayArea.y + gaze.Right.GazePointOnDisplayArea.y) / 2f;

        // Dividimos la pantalla en dos zonas: Arriba de 0.6 y Abajo de 0.4
        if (avgY > 0.6f) _targetY = alturaArriba;
        else if (avgY < 0.4f) _targetY = alturaAbajo;
    }

    void MoverJugador()
    {
        if (jugador == null) return;
        
        Vector2 pos = jugador.anchoredPosition;
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * velocidadJugador);
        jugador.anchoredPosition = pos;
    }

    void GestionarObstaculos()
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= tiempoEntreObstaculos)
        {
            _spawnTimer = 0;
            CrearObstaculo();
        }

        for (int i = _obstaculosActivos.Count - 1; i >= 0; i--)
        {
            RectTransform obs = _obstaculosActivos[i];
            obs.anchoredPosition += Vector2.left * velocidadObstaculos * Time.deltaTime;

            // Detección de colisión simple por cercanía
            if (Vector2.Distance(obs.anchoredPosition, jugador.anchoredPosition) < 80f)
            {
                RecibirDano();
                Destroy(obs.gameObject);
                _obstaculosActivos.RemoveAt(i);
                continue;
            }

            // Destruir si sale de pantalla
            if (obs.anchoredPosition.x < -Screen.width)
            {
                Destroy(obs.gameObject);
                _obstaculosActivos.RemoveAt(i);
            }
        }
    }

    void CrearObstaculo()
    {
        if (prefabObstaculo == null || contenedorObstaculos == null) return;

        GameObject nuevo = Instantiate(prefabObstaculo, contenedorObstaculos);
        RectTransform rt = nuevo.GetComponent<RectTransform>();
        
        // Aleatorio entre arriba o abajo
        float y = (Random.value > 0.5f) ? alturaArriba : alturaAbajo;
        rt.anchoredPosition = new Vector2(Screen.width, y);
        _obstaculosActivos.Add(rt);
    }

    void RecibirDano()
    {
        _vidasActuales--;
        if (textoVidas != null) textoVidas.text = "Vidas: " + _vidasActuales;
        
        if (_vidasActuales <= 0) FinalizarActividadLocal();
    }

    void ActualizarReloj()
    {
        if (textoTimer == null) return;
        int min = Mathf.FloorToInt(_tiempoTranscurrido / 60);
        int seg = Mathf.FloorToInt(_tiempoTranscurrido % 60);
        textoTimer.text = string.Format("{0:00}:{1:00}", min, seg);
    }

    void FinalizarActividadLocal()
    {
        _juegoFinalizado = true;
        juegoIniciado = false;
        if (overlayResult != null) overlayResult.SetActive(true);
        
        // Guardar métricas (ejemplo)
        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.GuardarPartida("Carrera Ocular", (_vidasActuales * 100), 1, 100, true, _tiempoTranscurrido);
        }
    }
}
