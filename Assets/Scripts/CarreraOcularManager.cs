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
    public List<Image> iconosVidas;
    public TMP_Text textoVidas; // Soporte para el texto "VIDAS: X"
    public TMP_Text textoNivel;
    public GameObject overlayResult;
    
    [Header("Ajustes de Movimiento")]
    public float velocidadJugador = 10f;
    public float velocidadObstaculos = 400f;
    public float velocidadFondo = 50f;
    public float velocidadAvance = 50f; // Velocidad de avance hacia la derecha
    public float alturaArriba = 250f;
    public float alturaCentro = 0f;
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
    private float _lastClickTime = 0f;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    protected override void Start()
    {
        usarValidacionOjos = false; // Desactivar bloqueo por Tobii para pruebas
        base.Start();
        VincularUI();
        _vidasActuales = vidasMaximas;
        _targetY = 0;
        PreconfigurarEscena();

        // Bypass de validación de ojos para modo ratón
        if (botonIniciar != null) botonIniciar.interactable = true;
    }

    void VincularUI()
    {
        // Si no están asignados, los buscamos por nombre
        if (jugador == null) jugador = GameObject.Find("Player")?.GetComponent<RectTransform>();
        if (backgroundScroll == null) backgroundScroll = GameObject.Find("BG")?.GetComponent<RectTransform>();
        if (contenedorObstaculos == null) contenedorObstaculos = GameObject.Find("Obstacles")?.GetComponent<RectTransform>();
        if (textoVidas == null) textoVidas = GameObject.Find("Lives")?.GetComponent<TMP_Text>();
        
        // El botón de reintentar suele estar oculto al inicio, lo buscamos en el panel de resultados
        if (botonReiniciar == null && overlayResult != null)
        {
            botonReiniciar = overlayResult.GetComponentInChildren<Button>(true);
            if (botonReiniciar != null) botonReiniciar.onClick.AddListener(ReiniciarJuego);
        }
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
        base.IniciarJuego();
        
        // Refuerzo para ocultar UI
        if (overlayInicio != null) overlayInicio.SetActive(false);
        if (botonIniciar != null) botonIniciar.gameObject.SetActive(false);
        
        _tiempoTranscurrido = 0f;
        Debug.Log("Carrera Ocular: Iniciando y ocultando UI");
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

        // ProcesarEntradaTobii(); // Comentado por ahora
        ProcesarEntradaMouse(); // Fallback estilo dinosaurio de Chrome
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

    /*
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
    */

    void ProcesarEntradaMouse()
    {
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            float timeSinceLastClick = Time.time - _lastClickTime;

            if (timeSinceLastClick <= DOUBLE_CLICK_TIME)
            {
                // DOBLE CLIC -> Bajar
                if (_targetY == alturaArriba) _targetY = alturaCentro;
                else if (_targetY == alturaCentro) _targetY = alturaAbajo;
                _lastClickTime = 0; // Reset para evitar triple clic
            }
            else
            {
                // UN CLIC -> Subir (esperar un poco para confirmar que no es doble)
                StartCoroutine(EsperarParaSubir());
                _lastClickTime = Time.time;
            }
        }
    }

    private IEnumerator EsperarParaSubir()
    {
        float start = Time.time;
        yield return new WaitForSeconds(DOUBLE_CLICK_TIME);
        
        // Si el tiempo del último clic no ha cambiado significativamente, es un clic simple
        if (Mathf.Abs(_lastClickTime - start) < 0.01f)
        {
            if (_targetY == alturaAbajo) _targetY = alturaCentro;
            else if (_targetY == alturaCentro) _targetY = alturaArriba;
        }
    }

    void MoverJugador()
    {
        if (jugador == null || !juegoIniciado) return;
        
        Vector2 pos = jugador.anchoredPosition;
        
        // Movimiento Vertical (Carriles)
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * velocidadJugador);
        
        // Avance Horizontal: Desactivado por petición (se queda donde esté)
        // if (pos.x < 0) ...

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

            // Detección de colisión: Radio aumentado a 85 para ser más permisivo
            if (Vector3.Distance(obs.position, jugador.position) < 85f) 
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
        if (contenedorObstaculos == null) return;

        GameObject nuevo = null;
        if (prefabObstaculo != null)
        {
            nuevo = Instantiate(prefabObstaculo, contenedorObstaculos);
        }
        else
        {
            // Intentar buscar un objeto en la jerarquía para usarlo como base
            GameObject template = GameObject.Find("Obstacle");
            if (template == null) template = GameObject.Find("Meteorito");
            
            // Si no lo encuentra por nombre global, buscar el primer hijo del contenedor que no sea un clon temporal
            if (template == null && contenedorObstaculos.childCount > 0)
            {
                foreach (Transform child in contenedorObstaculos)
                {
                    if (!child.name.Contains("Temp"))
                    {
                        template = child.gameObject;
                        break;
                    }
                }
            }

            if (template != null)
            {
                nuevo = Instantiate(template, contenedorObstaculos);
                nuevo.SetActive(true);
                nuevo.name = "Obstaculo_Clon";
            }
            else
            {
                // Fallback: Crear un cuadrado rojo si no hay nada
                nuevo = new GameObject("Obstaculo_Temp");
                nuevo.transform.SetParent(contenedorObstaculos, false);
                nuevo.AddComponent<Image>().color = Color.red;
                nuevo.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
            }
        }

        if (nuevo == null) return;

        RectTransform rt = nuevo.GetComponent<RectTransform>();
        
        // Aleatorio entre arriba, centro o abajo
        float rnd = Random.value;
        float y = alturaCentro;
        if (rnd < 0.33f) y = alturaArriba;
        else if (rnd > 0.66f) y = alturaAbajo;

        rt.anchoredPosition = new Vector2(Screen.width, y);
        _obstaculosActivos.Add(rt);
    }

    void RecibirDano()
    {
        if (_juegoFinalizado) return;
        
        _vidasActuales--;
        if (textoVidas != null) textoVidas.text = "VIDAS: " + Mathf.Max(0, _vidasActuales);
        
        // Efecto visual en el jugador
        StartCoroutine(RutinaEfectoDano());
        
        // Tintineo en la vida perdida
        if (iconosVidas != null && _vidasActuales >= 0 && _vidasActuales < iconosVidas.Count)
        {
            StartCoroutine(RutinaTintineoVida(iconosVidas[_vidasActuales]));
        }
        
        if (_vidasActuales <= 0)
        {
            _juegoFinalizado = true;
            FinalizarActividadLocal();
        }
    }

    private IEnumerator RutinaEfectoDano()
    {
        if (jugador == null) yield break;
        Image img = jugador.GetComponent<Image>();
        Color originalColor = img != null ? img.color : Color.white;
        Vector2 originalPos = jugador.anchoredPosition;

        for (int i = 0; i < 3; i++)
        {
            if (img != null) img.color = Color.red;
            jugador.anchoredPosition = originalPos + Random.insideUnitCircle * 15f;
            yield return new WaitForSeconds(0.05f);
            if (img != null) img.color = originalColor;
            jugador.anchoredPosition = originalPos;
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator RutinaTintineoVida(Image icono)
    {
        if (icono == null) yield break;
        Color col = icono.color;
        
        // Tintineo rápido
        for (int i = 0; i < 5; i++)
        {
            icono.enabled = false;
            yield return new WaitForSeconds(0.1f);
            icono.enabled = true;
            yield return new WaitForSeconds(0.1f);
        }

        // Se apaga/vuelve gris
        icono.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
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

    public override void ReiniciarJuego()
    {
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
