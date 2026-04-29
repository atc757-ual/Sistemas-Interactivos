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
    public GameObject luna;
    
    [Header("UI y Feedback")]
    public TMP_Text textoTimer;
    public List<Image> iconosVidas;
    public TMP_Text textoVidas; // Soporte para el texto "VIDAS: X"
    public TMP_Text textoNivel;
    public TMP_Text textoContador; // Para el 3-2-1 3, 2, 1... ¡YA!
    
    [Header("Resultados UI")]
    public GameObject overlayResult;
    public TMP_Text titleRes;
    public TMP_Text subRes;
    public TMP_Text puntajeText;
    public TMP_Text colisionesText;
    public TMP_Text vidasRestantesText;
    
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
    public float tiempoEntreObstaculos = 0.8f;
    
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
    private bool _enConteo = false;
    private int _colisiones = 0;
    private bool _timerIniciado = false;
    private float _lastClickTime = 0f;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    protected override void Start()
    {
        usarValidacionOjos = false; // Desactivar bloqueo por Tobii para pruebas
        
        // Ejecutamos NUESTRA vinculación robusta PRIMERO
        VincularUI();
        
        // LUEGO llamamos a la base para que asigne los OnClick a los botones ya encontrados
        base.Start();

        // GARANTÍA ABSOLUTA: Forzamos nosotros mismos la conexión del evento de clic
        if (botonIniciar != null) {
            botonIniciar.onClick.RemoveAllListeners();
            botonIniciar.onClick.AddListener(IniciarJuego);
        }
        
        // Garantizar estados iniciales de la UI por si se guardó la escena apagada
        if (overlayInicio != null) overlayInicio.SetActive(true);
        if (overlayResult != null) overlayResult.SetActive(false);
        
        // ¡Ocultar el contador al inicio para que no se vea el "3"!
        if (textoContador != null) textoContador.gameObject.SetActive(false);
        if (textoMensajeInicio != null) textoMensajeInicio.gameObject.SetActive(false);
        if (luna != null) luna.SetActive(false);
        
        _vidasActuales = vidasMaximas;
        _targetY = 0;
        PreconfigurarEscena();

        // Bypass de validación de ojos para modo ratón
        if (botonIniciar != null) {
            botonIniciar.interactable = true;
        }
    }

    void VincularUI()
    {
        // --- AUTO VINCULAR ELEMENTOS BASE ---
        if (overlayInicio == null) overlayInicio = BuscarObjetoInactivo("OverlayInicio");
        
        // ¡SOBRESCRIBIR botón iniciar por si en el Inspector está puesto uno viejo invisible!
        var btnStart = BuscarObjetoInactivo("StartButton");
        if (btnStart != null) botonIniciar = btnStart.GetComponent<Button>();
        
        if (botonSalir == null) {
            var btnBack = BuscarObjetoInactivo("BackBtn");
            if (btnBack != null) botonSalir = btnBack.GetComponent<Button>();
        }

        // Si no están asignados, los buscamos por nombre
        if (jugador == null) jugador = GameObject.Find("Player")?.GetComponent<RectTransform>();
        if (backgroundScroll == null) backgroundScroll = GameObject.Find("BG")?.GetComponent<RectTransform>();
        if (contenedorObstaculos == null) contenedorObstaculos = GameObject.Find("Obstacles")?.GetComponent<RectTransform>();
        if (textoVidas == null) textoVidas = GameObject.Find("Lives")?.GetComponent<TMP_Text>();
        if (textoTimer == null) {
            var t = BuscarObjetoInactivo("Timer");
            if (t != null) textoTimer = t.GetComponent<TMP_Text>();
        }
        
        // --- AUTO VINCULAR RESULTADOS (INCLUSO INACTIVOS) ---
        if (overlayResult == null) overlayResult = BuscarObjetoInactivo("OverlayResultado");
        
        if (overlayResult != null)
        {
            if (titleRes == null) {
                var t = BuscarObjetoInactivo("ResultText");
                if (t != null) titleRes = t.GetComponent<TMP_Text>();
            }
            if (subRes == null) {
                var t = BuscarObjetoInactivo("SubRes");
                if (t != null) subRes = t.GetComponent<TMP_Text>();
            }
            if (colisionesText == null) {
                var t = BuscarObjetoInactivo("Colisiones");
                if (t != null) colisionesText = t.GetComponentInChildren<TMP_Text>();
            }
            if (vidasRestantesText == null) {
                var t = BuscarObjetoInactivo("VidasRestantes");
                if (t != null) vidasRestantesText = t.GetComponentInChildren<TMP_Text>();
            }
            if (puntajeText == null) {
                var t = BuscarObjetoInactivo("Puntaje");
                if (t != null) puntajeText = t.GetComponentInChildren<TMP_Text>();
            }

            // El botón de reintentar suele estar oculto al inicio
            if (botonReiniciar == null)
            {
                var btnRetry = BuscarObjetoInactivo("RetryButton");
                if (btnRetry != null) botonReiniciar = btnRetry.GetComponent<Button>();
            }
        }

        if (textoContador == null) {
            var contObj = BuscarObjetoInactivo("Counter");
            if (contObj == null) contObj = BuscarObjetoInactivo("CounterResult");
            if (contObj != null) textoContador = contObj.GetComponent<TMP_Text>();
        }

        if (textoMensajeInicio == null) {
            var mObj = BuscarObjetoInactivo("StartMessage");
            if (mObj != null) textoMensajeInicio = mObj.GetComponent<TMP_Text>();
        }

        if (luna == null) {
            luna = BuscarObjetoInactivo("Luna");
            if (luna == null) luna = BuscarObjetoInactivo("Moon");
            if (luna == null) luna = BuscarObjetoInactivo("MediaLuna");
        }
    }

    GameObject BuscarObjetoInactivo(string nombre)
    {
        string busqueda = nombre.ToLower();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            if (t.name.Trim().ToLower() == busqueda && !string.IsNullOrEmpty(t.gameObject.scene.name)) return t.gameObject;
        }
        return null;
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
        if (_enConteo || juegoIniciado) return;
        
        // Ocultar botón volver durante la partida
        if (botonSalir != null) botonSalir.gameObject.SetActive(false);
        
        // Mostrar la luna solo al empezar
        if (luna != null) luna.SetActive(true);
        
        StartCoroutine(RutinaCountdown());
    }

    IEnumerator RutinaCountdown()
    {
        _enConteo = true;
        
        // Mantener OverlayInicio encendido pero APAGAR todos sus hijos excepto el Contador
        if (overlayInicio != null) {
            foreach (Transform child in overlayInicio.transform) {
                if (textoContador != null && child == textoContador.transform) {
                    // Si el contador es hijo directo del Overlay
                    child.gameObject.SetActive(true);
                } else if (textoContador != null && child == textoContador.transform.parent) {
                    // Si el contador está DENTRO del Botón (StartButton)
                    child.gameObject.SetActive(true); // Mantenemos el botón encendido para que el contador exista
                    
                    // Pero apagamos el fondo del botón
                    var img = child.GetComponent<Image>();
                    if (img != null) img.enabled = false;
                    
                    // Y apagamos el icono y el texto "Comenzar", dejando SOLO el contador
                    foreach (Transform subChild in child) {
                        if (subChild != textoContador.transform) {
                            subChild.gameObject.SetActive(false);
                        }
                    }
                } else {
                    // Cualquier otro elemento (OverlayTitle, Description, etc) se apaga completamente
                    child.gameObject.SetActive(false);
                }
            }
        }

        if (textoContador != null) {
            textoContador.gameObject.SetActive(true);
            textoContador.color = Color.white;
        }

        for (int i = 3; i > 0; i--)
        {
            if (textoContador != null) textoContador.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (textoContador != null) { textoContador.text = "¡Empezar!"; yield return new WaitForSecondsRealtime(0.7f); }
        if (textoContador != null) textoContador.gameObject.SetActive(false); 
        
        if (overlayInicio != null) overlayInicio.SetActive(false);

        _tiempoTranscurrido = 0f;
        _colisiones = 0;
        
        base.IniciarJuego();
        _enConteo = false;
        _timerIniciado = false; // El tiempo real solo empezará con el primer obstáculo
    }

    protected override void Update()
    {
        // Forzar interactividad del botón y Ocultar estrictamente el contador hasta que empiece el conteo
        if (!juegoIniciado)
        {
            if (botonIniciar != null) botonIniciar.interactable = true;
            
            if (!_enConteo && textoContador != null && textoContador.gameObject.activeSelf) 
            {
                textoContador.gameObject.SetActive(false);
            }

            // --- LÓGICA DE DETECCIÓN DE OJOS ---
            if (!juegoIniciado && !_enConteo && textoMensajeInicio != null)
            {
                // Si Tobii está conectado y detecta ojos, mostramos el mensaje. Si no, oculto.
                var gaze = (EyeTracker.Instance != null) ? EyeTracker.Instance.LatestGazeData : null;
                bool detectado = (gaze != null && (gaze.Left.GazePointValid || gaze.Right.GazePointValid));
                textoMensajeInicio.gameObject.SetActive(detectado);
            }

            // FALLBACK A PRUEBA DE BALAS: Si el botón UI está bloqueado por algún panel invisible,
            // cualquier clic de ratón en la pantalla forzará el inicio de la partida.
            if (!_enConteo && UnityEngine.Input.GetMouseButtonDown(0))
            {
                IniciarJuego();
            }
        }

        base.Update();
        if (_juegoFinalizado) return;

        MoverFondo();

        if (!juegoIniciado || juegoPausado || _enConteo) return;

        // Actualizar reloj siempre para que se vea el "60s" inicial
        ActualizarReloj();

        if (_timerIniciado)
        {
            _tiempoTranscurrido += Time.deltaTime;

            if (_tiempoTranscurrido >= duracionSesion)
            {
                FinalizarActividadLocal();
                return;
            }
        }

        ProcesarEntradaTobii();
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

    void ProcesarEntradaTobii()
    {
        if (TobiiGazeProvider.Instance == null) return;
        
        if (!TobiiGazeProvider.Instance.HasGaze) return;

        // GazePositionScreen está en píxeles (Y=0 abajo, Y=Height arriba)
        float yNorm = TobiiGazeProvider.Instance.GazePositionScreen.y / Screen.height;

        // Si mira al 40% superior, sube. Al 40% inferior, baja.
        if (yNorm > 0.6f) _targetY = alturaArriba;
        else if (yNorm < 0.4f) _targetY = alturaAbajo;
        else _targetY = alturaCentro;
    }

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
        if (jugador == null) return;
        
        // --- Movimiento Vertical (Controlado por Gaze o Mouse) ---
        float currentY = jugador.anchoredPosition.y;
        float newY = Mathf.Lerp(currentY, _targetY, Time.deltaTime * velocidadJugador);
        
        // --- Movimiento Horizontal (Oscilación MUY sutil) ---
        // Ahora solo oscila un poquito (5% del ancho) desde su posición base (-35%) hacia adelante
        float xBase = -Screen.width * 0.35f;
        float xAvance = -Screen.width * 0.30f; 
        
        // Oscilación más lenta y cortita
        float oscilacion = (Mathf.Sin(Time.time * 0.6f) + 1f) / 2f; 
        float newX = Mathf.Lerp(xBase, xAvance, oscilacion);

        jugador.anchoredPosition = new Vector2(newX, newY);
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
        _timerIniciado = true; // Empezamos a contar tiempo en el primer spawn

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
        
        // --- MAYOR DIVERSIFICACIÓN (Dispersión) ---
        // En lugar de 3 carriles fijos, usamos todo el rango con un poco de margen extra
        float yAleatorio = Random.Range(alturaAbajo - 60f, alturaArriba + 60f);
        
        // Añadimos variación en X para que no aparezcan todos en una línea recta perfecta
        float xJitter = Random.Range(0f, 200f);
        
        // Variación de tamaño aleatoria para que unos meteoritos sean más grandes que otros (0.8x a 1.4x)
        float escalaAleatoria = Random.Range(0.8f, 1.4f);
        rt.localScale = Vector3.one * escalaAleatoria;

        rt.anchoredPosition = new Vector2(Screen.width + xJitter, yAleatorio);
        _obstaculosActivos.Add(rt);
    }

    void RecibirDano()
    {
        if (_juegoFinalizado) return;
        
        _vidasActuales--;
        _colisiones++;
        
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
        float tiempoRestante = Mathf.Max(0, duracionSesion - _tiempoTranscurrido);
        textoTimer.text = tiempoRestante.ToString("F0") + "s";
    }

    void FinalizarActividadLocal()
    {
        _juegoFinalizado = true;
        juegoIniciado = false;
        
        // Mostrar de nuevo el botón Volver
        if (botonSalir != null) botonSalir.gameObject.SetActive(true);
        
        // --- CALCULAR PUNTUACIÓN (Versión más generosa) ---
        // Tiempo suma puntos (hasta 100), colisiones restan, si mueres se resta penalización
        float puntosTiempo = Mathf.Clamp01(_tiempoTranscurrido / duracionSesion) * 100f;
        
        // Penalizaciones más suaves: 8 por colisión y 15 por derrota total
        int penalizacion = (_colisiones * 8) + (_vidasActuales <= 0 ? 15 : 0);
        int finalScore = Mathf.Clamp(Mathf.FloorToInt(puntosTiempo) - penalizacion, 0, 100);

        // Si sobrevivió bastante tiempo (más de 15s), garantizamos al menos 1 punto para que no sea 0
        if (finalScore == 0 && _tiempoTranscurrido > 15f) finalScore = 5;

        // Actualizar UI de Resultados
        if (colisionesText != null) colisionesText.text = _colisiones.ToString();
        if (vidasRestantesText != null) vidasRestantesText.text = Mathf.Max(0, _vidasActuales).ToString();
        if (puntajeText != null) puntajeText.text = finalScore.ToString();

        if (titleRes != null) {
            titleRes.text = finalScore >= 50 ? "¡Carrera Completada!" : "¡Casi lo logras!";
        }
        
        if (subRes != null) {
            if (finalScore >= 50) {
                subRes.text = "¡Esquivaste los obstáculos como un profesional!\n¡Eres un gran piloto estelar!";
            } else {
                subRes.text = "Los obstáculos te han golpeado varias veces.\n¡Concéntrate para la próxima misión!";
            }
        }

        if (overlayResult != null) overlayResult.SetActive(true);
        
        // Ocultar la luna al terminar
        if (luna != null) luna.SetActive(false);
        
        // Guardar métricas
        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.GuardarPartida("Carrera Ocular", finalScore, 1, 100, true, _tiempoTranscurrido, _colisiones);
        }
    }

    public override void ReiniciarJuego()
    {
        // Reset de estado interno
        _juegoFinalizado = false;
        juegoIniciado = false;
        _enConteo = false;
        _timerIniciado = false;
        _tiempoTranscurrido = 0f;
        _colisiones = 0;
        _vidasActuales = vidasMaximas;
        _spawnTimer = 0f;

        // Limpiar obstáculos existentes
        foreach (var obs in _obstaculosActivos) if (obs != null) Destroy(obs.gameObject);
        _obstaculosActivos.Clear();

        // UI Reset
        if (overlayResult != null) overlayResult.SetActive(false);
        if (textoVidas != null) textoVidas.text = "VIDAS: " + _vidasActuales;
        ActualizarReloj();

        // Lanzar el inicio con su contador
        IniciarJuego();
    }
}
