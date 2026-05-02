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
    
    [Header("Elementos a Ocultar en Partida")]
    public GameObject precBubble;
    public GameObject avanceBubble;

    [Header("Overlay Resultados")]
    public GameObject overlayResult;
    public TMP_Text titleRes;
    public TMP_Text subRes;
    public TMP_Text percentRes;
    public TMP_Text subDetail; // <--- Nuevo: Para manejar el detalle inferior por separado
    public Button btnAgain;

    [Header("Ajustes de Movimiento")]
    public float velocidadStar = 800f; // Velocidad pro para seguimiento fluido
    public float velocidadDistractor = 600f;
    public float velocidadFondo = 150f; 
    public float zigzagAmplitud = 200f;
    public float zigzagFrecuencia = 4f;

    [Header("Configuración de Tiempo")]
    public float duracionSesion = 30f; // Se ajusto a 30 
    private bool _juegoFinalizado = false;
    private float _resultsDelayTimer = 0f;

    private float _tiempoTranscurrido = 0f;
    private int _framesTargeteados = 0;
    private float _segundosMirando = 0f;
    private Vector2 _dirDistractor = Vector2.left;
    
    private RectTransform[] _convoyEstrellas; 
    private List<RectTransform> _bgSegments = new List<RectTransform>(); // <--- Cache de segmentos de fondo

    // Control de parpadeo para iniciar
    // Control de parpadeo (estandarizado)
    private float _blinkTimer = 0f;
    private bool _eyesWereDetected = false;
    private bool _enConteo = false;
    private float _tiempoInstruccionesMostradas = 0f;
    private int _dirEstrella = 1; // 1 = derecha, -1 = izquierda
    private Vector2 _gazeDebugPos; 
    private bool _permitirReintento = false;

    // Métricas de tiempo de actualización
    private int _votosPositivosPrecision = 0;
    private int _votosTotalesPrecision = 0;
    private int _ultimoFrameProcesado = -1;

    [Header("Debug")]
    public GameObject gazeDebug;

    protected override void Start()
    {
        if (GestorPaciente.Instance == null || !GestorPaciente.Instance.EsSesionValida()) return;

        // Forzamos la duración a 30s para ignorar el valor antiguo del Inspector
        duracionSesion = 30f; 

        AutoVincularSeguimiento(); 
        base.Start();              
        
        if (overlayInicio != null) overlayInicio.SetActive(true);
        PreconfigurarPosiciones();
        ConfigurarEstela(); // <--- Nueva estela visual
    }

    void VincularUIAutomaticamente()
    {
        // Punteros directos según la jerarquía del usuario
        if (star == null) star = BuscarObjetoInactivo("Star")?.GetComponent<RectTransform>();
        if (distractor == null) distractor = BuscarObjetoInactivo("Distractor")?.GetComponent<RectTransform>();
        if (timerContainer == null) timerContainer = BuscarObjetoInactivo("Time");
        if (textoTimer == null) textoTimer = timerContainer?.GetComponent<TMP_Text>() ?? timerContainer?.GetComponentInChildren<TMP_Text>();
        // Se buscarán dentro de las burbujas más adelante
        if (barFill == null) barFill = BuscarObjetoInactivo("BarFill")?.GetComponent<Image>();
        if (textoSub == null) textoSub = BuscarObjetoInactivo("Sub")?.GetComponent<TMP_Text>();
        if (textoMensajeInicio == null) {
            GameObject contObj = BuscarObjetoInactivo("Contador");
            if (contObj != null) textoMensajeInicio = contObj.GetComponentInChildren<TMP_Text>(true);
        }
        if (botonIniciar == null) botonIniciar = BuscarObjetoInactivo("StartButton")?.GetComponent<Button>();
        if (botonSalir == null) botonSalir = BuscarObjetoInactivo("VolverBtn")?.GetComponent<Button>() ?? BuscarObjetoInactivo("BackBtn")?.GetComponent<Button>();
        if (precBubble == null) precBubble = BuscarObjetoInactivo("PrecBubble");
        if (avanceBubble == null) avanceBubble = BuscarObjetoInactivo("AvanceBubble");

        // Vincular los textos directamente desde los objetos encontrados
        if (precBubble != null) precText = precBubble.GetComponent<TMP_Text>();
        if (avanceBubble != null) avanceText = avanceBubble.GetComponent<TMP_Text>();
        if (overlayInicio == null) overlayInicio = BuscarObjetoInactivo("OverlayInicio");
        if (overlayResult == null) overlayResult = BuscarObjetoInactivo("OverlayResult");
        if (panelDetalle == null) panelDetalle = BuscarObjetoInactivo("detalle");
        if (backgroundScroll == null) backgroundScroll = BuscarObjetoInactivo("BG")?.GetComponent<RectTransform>() ?? BuscarObjetoInactivo("Background")?.GetComponent<RectTransform>();
        
        // Puntero de depuración (Punto rojo)
        if (gazeDebug == null) gazeDebug = BuscarObjetoInactivo("gazeDebug");
        
        // Forzar activación del inicio para que el Tobii pueda empezar a buscar ojos
        if (overlayInicio != null) overlayInicio.SetActive(true);

        if (overlayResult != null)
        {
            titleRes = titleRes ?? overlayResult.transform.Find("TitleRes")?.GetComponent<TMP_Text>();
            
            // Búsqueda más robusta de los textos de resultados
            TMP_Text[] todosLosTextos = overlayResult.GetComponentsInChildren<TMP_Text>(true);
            foreach(var t in todosLosTextos) {
                string n = t.name.ToLower();
                string p = t.transform.parent.name.ToLower();
                
                // Evitamos vincular el Timer (que suele tener ":" o "s") a las burbujas de métricas
                if (n.Contains("timer") || n.Contains("clock") || t.text.Contains(":")) continue;

                // Búsqueda jerárquica: Priorizar objetos dentro de las burbujas que parezcan valores
                if (p.Contains("precbubble") && (n.Contains("text") || n.Contains("val") || n.Contains("num") || t.text.Contains("%"))) 
                    precText = t;
                else if (p.Contains("avancebubble") && (n.Contains("text") || n.Contains("val") || n.Contains("num"))) 
                    avanceText = t;
                else if (n.Contains("percent") || n.Contains("puntos") || n.Contains("score")) 
                    percentRes = t;
                
                if (n.Contains("subres")) subRes = t;
                if (n.Contains("subdetail")) subDetail = t;
            }

            btnAgain = btnAgain ?? overlayResult.GetComponentInChildren<Button>(true);
            
            if (btnAgain != null && btnAgain.name != "BtnAgain") {
                Button b = overlayResult.transform.Find("BtnAgain")?.GetComponent<Button>();
                if (b != null) btnAgain = b;
            }
            overlayResult.SetActive(false); 
        }
        
        // --- PERSONALIZACIÓN DE USUARIO ---
        // Buscar el objeto "Inst" para reemplazar "Astronauta" por el nombre del usuario
        GameObject instObj = BuscarObjetoInactivo("Inst");
        if (instObj != null) {
            TMP_Text instText = instObj.GetComponent<TMP_Text>();
            if (instText != null && instText.text.Contains("Astronauta")) {
                string nombre = GestorPaciente.Instance != null ? GestorPaciente.Instance.GetNombrePacienteFormateado() : "Astronauta";
                instText.text = instText.text.Replace("Astronauta", nombre);
            }
        }
    }

    void AutoVincularSeguimiento()
    {
        VincularUIAutomaticamente();
    }

    // Nota: El método BuscarObjetoPotente ha sido reemplazado por BuscarObjetoInactivo heredado de BaseActividad

    void PreconfigurarPosiciones()
    {
        // Ocultar el temporizador antes de que empiece el juego
        if (timerContainer != null) timerContainer.SetActive(false);

        if (star != null) 
        {
            _convoyEstrellas = new RectTransform[] { star };
            
            // Posición inicial: Extremo izquierdo
            float startX = -Screen.width / 2f + 100f;
            // Altura aleatoria (Rango seguro para evitar bordes)
            float randomY = Random.Range(-250f, 250f);
            
            star.anchoredPosition = new Vector2(startX, randomY);
            star.gameObject.SetActive(true);
        }
        
        if (distractor != null) {
            distractor.anchoredPosition = new Vector2(400, 100);
            distractor.gameObject.SetActive(false); 
        }
        
        // --- MANEJO DEL FONDO (Loop Infinito) ---
        if (backgroundScroll != null)
        {
            float anchoFondo = backgroundScroll.rect.width > 0 ? backgroundScroll.rect.width : Screen.width;

            // Si es la primera vez (solo está el original), creamos el clon
            if (_bgSegments.Count < 2)
            {
                _bgSegments.Clear();
                _bgSegments.Add(backgroundScroll);

                GameObject bgClon = Instantiate(backgroundScroll.gameObject, backgroundScroll.parent);
                bgClon.name = "Background_Loop";
                RectTransform rtBg = bgClon.GetComponent<RectTransform>();
                
                _bgSegments.Add(rtBg);
                rtBg.SetAsFirstSibling();
                backgroundScroll.SetAsFirstSibling();
            }

            // SIEMPRE resetear las posiciones para que no queden huecos al reintentar
            if (_bgSegments.Count >= 2)
            {
                _bgSegments[0].anchoredPosition = Vector2.zero;
                _bgSegments[1].anchoredPosition = new Vector2(anchoFondo, 0);
            }
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
        
        // Si el OverlayResult ya está activo (por un reintento), no mostramos el OverlayInicio para no solapar fondos
        bool desdeResultados = (overlayResult != null && overlayResult.activeSelf);
        
        if (overlayInicio != null && !desdeResultados) {
            overlayInicio.SetActive(true);
            foreach (Transform child in overlayInicio.transform) {
                if (textoMensajeInicio != null && (child == textoMensajeInicio.transform || child == textoMensajeInicio.transform.parent)) {
                    child.gameObject.SetActive(true);
                } else {
                    child.gameObject.SetActive(false);
                }
            }
        }

        // Asegurarnos de que la estrella esté visible para el inicio
        if (star != null) star.gameObject.SetActive(true);
        PreconfigurarPosiciones(); // Resetear a la posición inicial
        
        if (panelDetalle != null) panelDetalle.SetActive(false);

        if (textoMensajeInicio != null) {
            textoMensajeInicio.gameObject.SetActive(true);
            textoMensajeInicio.color = Color.white;
        }

        _votosPositivosPrecision = 0;
        _votosTotalesPrecision = 0;
        _segundosMirando = 0;
        _tiempoTranscurrido = 0;

        PreconfigurarPosiciones(); // <--- Llamada CRÍTICA aquí para aleatorizar cada vez

        for (int i = 3; i > 0; i--)
        {
            if (textoMensajeInicio != null) textoMensajeInicio.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (textoMensajeInicio != null) { textoMensajeInicio.text = "¡No la pierdas de vista!"; yield return new WaitForSecondsRealtime(0.7f); }

        // Apagar solo los objetos de mensaje/overlay, NO el padre (podría ser el Canvas)
        if (textoMensajeInicio != null) textoMensajeInicio.gameObject.SetActive(false);
        if (textoMensajeInicio != null && textoMensajeInicio.transform.parent != null && textoMensajeInicio.transform.parent.name.Contains("Contador"))
            textoMensajeInicio.transform.parent.gameObject.SetActive(false);

        if (overlayInicio != null) overlayInicio.SetActive(false);
        if (overlayResult != null) overlayResult.SetActive(false);
        
        if (star != null) star.gameObject.SetActive(true); // Asegurar que la estrella sea visible
        
        _tiempoTranscurrido = 0f;
        _framesTargeteados = 0;
        _segundosMirando = 0f; 
        _votosPositivosPrecision = 0;
        _votosTotalesPrecision = 0;
        
        ActualizarUI(); // <--- Forzar actualización inmediata para evitar ver el 60s del prefab
        ToggleUI(false); // <--- OCULTAR AL EMPEZAR
        
        if (distractor != null) distractor.gameObject.SetActive(true); // Mostrar distractor ahora sí

        // Mover la estrella a la posición inicial (izquierda) respetando la altura aleatoria ya calculada
        if (star != null) 
        {
            float anchoPadre = 1920f;
            RectTransform padre = star.parent as RectTransform;
            if (padre != null && padre.rect.width > 0) anchoPadre = padre.rect.width;
            
            float limiteIzquierdo = -(anchoPadre / 2f) + 100f;
            // MANTENER la Y que puso PreconfigurarPosiciones
            star.anchoredPosition = new Vector2(limiteIzquierdo, star.anchoredPosition.y);
        }

        juegoIniciado = true;
        _enConteo = false;
    }

    void ToggleUI(bool visible)
    {
        if (botonSalir != null) botonSalir.gameObject.SetActive(visible);
        if (precBubble != null) precBubble.SetActive(visible);
        if (avanceBubble != null) avanceBubble.SetActive(visible);
        
        // El timer SOLO visible durante el juego (!visible significa que estamos en partida)
        if (timerContainer != null) timerContainer.SetActive(!visible);
    }

    protected override void Update()
    {
        // Forzar interactividad para evitar el parpadeo causado por BaseActividad
        if (!juegoIniciado && botonIniciar != null) botonIniciar.interactable = true;

        base.Update();
        if (_juegoFinalizado)
        {
            if (_resultsDelayTimer > 0)
            {
                _resultsDelayTimer -= Time.deltaTime;
                if (btnAgain != null) btnAgain.gameObject.SetActive(false);
                if (subDetail    != null) subDetail.gameObject.SetActive(false);
            }
            else
            {
                if (!juegoPausado) 
                {
                    if (btnAgain != null && !btnAgain.gameObject.activeSelf) btnAgain.gameObject.SetActive(true);
                    if (subDetail != null && !subDetail.gameObject.activeSelf) subDetail.gameObject.SetActive(true);
                    ManejarReintentoPorParpadeo();
                }
            }
            return;
        }

        // Desplazar fondo SIEMPRE para que la escena se sienta viva
        DesplazarFondo();

        if (!juegoIniciado && !juegoPausado && !_enConteo) { ManejarInstruccionesYParpadeo(); return; }

        if (juegoIniciado && !juegoPausado)
        {
            _tiempoTranscurrido += Time.deltaTime;
            ActualizarUI();
            ManejarEstelaFantasmas(); // <--- Activar estela visual

            // Verificación de fin de tiempo
            if (_tiempoTranscurrido >= duracionSesion)
            {
                FinalizarSesionLocal();
                return;
            }

            // MoverMundo(); // Movido parcialmente arriba
            MoverObjetosAdicionales(); // Nuevo método para el resto
            ProcesarSeguimientoOcular(); // Restaurar para que el puntero rojo se mueva
            AplicarBrilloEstrella();
        }
    }

    void FinalizarSesionLocal()
    {
        juegoIniciado = false;
        _juegoFinalizado = true;
        _resultsDelayTimer = 10f; // 10 segundos de calma para leer resultados
        Time.timeScale = 1; 

        // Ocultar elementos de juego para que no estorben en los resultados
        if (star != null) star.gameObject.SetActive(false);
        if (distractor != null) distractor.gameObject.SetActive(false);
        
        // --- GUARDADO AUTOMÁTICO ---
        // Cálculo de precisión basado en votos (Tobii o Mouse)
        float precisionFinal = (_votosTotalesPrecision > 0) ? (_votosPositivosPrecision / (float)_votosTotalesPrecision) * 100f : 0;
        int nivelAlcanzado = 1;
        
        // Si no hubo actividad real (votos muy bajos), penalizar a 0
        if (_votosPositivosPrecision <= 5) 
        {
            precisionFinal = 0;
            _segundosMirando = 0;
            this.puntuacion = 0;
            nivelAlcanzado = 0;
        }
        else
        {
            // 1. Acompañamiento Base (0-100 para el cálculo interno)
            float pctSeguimiento = (duracionSesion > 0) ? (_segundosMirando / duracionSesion) * 100f : 0;
            
            // 2. Puntaje Final Ponderado (Para que no sea simplemente Acompañamiento * 10)
            // Fórmula: 70% Tiempo + 30% Puntería
            float scorePonderado = (pctSeguimiento * 0.7f) + (precisionFinal * 0.3f);
            this.puntuacion = Mathf.RoundToInt(scorePonderado);
            
            // 3. Nivel (Acompañamiento 0-10 clínico)
            nivelAlcanzado = Mathf.RoundToInt(pctSeguimiento / 10f); 
            if (nivelAlcanzado < 1 && pctSeguimiento > 0) nivelAlcanzado = 1;
        }

        Debug.Log($"[ESTRELLA] Finalizando: SegundosMirando={_segundosMirando}, Score={this.puntuacion}, Precision={precisionFinal}%");
        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.GuardarPartida("Estrella Lineal", this.puntuacion, nivelAlcanzado, precisionFinal, true, _tiempoTranscurrido, 0);
        }
        // ---------------------------------------------------

        ToggleUI(true); 

        // Forzar los valores finales en las burbujas
        if (precText != null) precText.text = precisionFinal.ToString("F0") + "%";
        
        // Acompañamiento 0-10 (basado en el seguimiento real)
        float pctSeguimientoFinal = (duracionSesion > 0) ? (_segundosMirando / duracionSesion) * 100f : 0;
        float acompañamientoFinal0a10 = pctSeguimientoFinal / 10f; 
        if (avanceText != null) avanceText.text = acompañamientoFinal0a10.ToString("F1");
        
        Debug.Log($"[UI] Actualizando: Prec={precisionFinal}%, Avance={acompañamientoFinal0a10}");

        if (overlayResult != null)
        {
            overlayResult.SetActive(true);
            
            // Iniciamos la corutina para el retraso de los botones de reintento
            StartCoroutine(RoutineRetrasoBotonesEstrella(this.puntuacion));
            
            // Asegurarnos de encender todo lo que ocultamos para el reintento
            foreach (Transform child in overlayResult.transform)
            {
                // No activamos el botón de nuevo (btnAgain) aquí, lo hará la corutina tras el delay
                bool esBotonReintento = (btnAgain != null && child.gameObject == btnAgain.gameObject);
                if (child.name != "Contador" && !esBotonReintento) child.gameObject.SetActive(true);
            }
            
            // Mensajes super user-friendly y motivadores
            if (titleRes != null) {
                titleRes.text = this.puntuacion >= 70 ? "¡Misión Estelar Cumplida!" : "¡Casi lo logras!";
            }
            if (percentRes != null) percentRes.text = this.puntuacion.ToString();
            
            if (subRes != null) 
            {
                if (this.puntuacion >= 70) {
                    subRes.text = "¡Lograste acompañar a nuestra estrella durante su recorrido!\nEres un astronauta fantástico.";
                } else {
                    subRes.text = "La estrella se alejó un poquito esta vez.\n¡Mantén tus ojos en ella para la próxima misión!";
                }
            }
           
            if (btnAgain != null)
            {
                btnAgain.onClick.RemoveAllListeners();
                btnAgain.onClick.AddListener(() => {
                    Debug.Log("<color=orange><b>[SISTEMA]</b></color> Botón Reintentar pulsado.");
                    ReiniciarJuegoSinCarga();
                });
            }
        }
        else
        {
            Debug.LogWarning("<color=red><b>[ERROR]</b></color> No se encontró OverlayResult. Volviendo al Home.");
            Time.timeScale = 1.0f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Activities");
        }
    }

    void ManejarInstruccionesYParpadeo()
    {
        bool eyesDetected = TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.EyeDataValid;


        // Mostrar botón si detectamos ojos o si ha pasado el tiempo de lectura
        _tiempoInstruccionesMostradas += Time.deltaTime;
        if (botonIniciar != null && !botonIniciar.gameObject.activeSelf) {
            // Una vez que aparece (por ojos o por tiempo), se queda encendido para evitar parpadeos
            bool mostrarBoton = eyesDetected || _tiempoInstruccionesMostradas > 3.0f;
            if (mostrarBoton) botonIniciar.gameObject.SetActive(true);
            botonIniciar.interactable = true;
        }

        if (!eyesDetected)
        {
            if (_eyesWereDetected) _blinkTimer += Time.deltaTime;
        }
        else
        {
            if (_eyesWereDetected && _blinkTimer > 0.1f && _blinkTimer < 0.5f)
            {
                _eyesWereDetected = false;
                _blinkTimer = 0;
                Debug.Log("<color=magenta><b>[ESTRELLA]</b> Pestañeo detectado. Iniciando...</color>");
                IniciarJuego();
            }
            else
            {
                _eyesWereDetected = true;
                _blinkTimer = 0;
            }
        }
    }

    void ManejarReintentoPorParpadeo()
    {
        bool eyesDetected = TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.EyeDataValid;

        if (overlayResult != null && subDetail != null)
        {
            if (_permitirReintento) {
                subDetail.gameObject.SetActive(true);
                if (eyesDetected) {
                    SetOverlayText(subDetail.gameObject, "<b>¡Hemos detectado tus ojos!</b>\n\nPestañea para reintentar la misión.");
                } else {
                    SetOverlayText(subDetail.gameObject, "Pestañea o haz clic en el botón de abajo para volver a intentarlo");
                }
            } else {
                subDetail.gameObject.SetActive(false);
            }
        }

        if (!eyesDetected)
        {
            if (_eyesWereDetected) _blinkTimer += Time.deltaTime;
        }
        else
        {
            if (_permitirReintento && _eyesWereDetected && _blinkTimer > 0.1f && _blinkTimer < 0.5f)
            {
                _eyesWereDetected = false;
                _permitirReintento = false;
                _blinkTimer = 0;
                
                SetOverlayText(subRes.gameObject, "<b>¡Pestañeo detectado!</b>\n\nReiniciando misión...");
                if (btnAgain != null) btnAgain.gameObject.SetActive(false);
                
                // Reiniciar in-situ para cumplir con el flujo de UI solicitado
                Invoke("ReiniciarJuegoSinCarga", 0.5f);
            }
            else
            {
                _eyesWereDetected = true;
                _blinkTimer = 0;
            }
        }
    }

    void ReiniciarJuegoSinCarga()
    {
        Debug.Log("<color=cyan><b>[SISTEMA]</b></color> Reiniciando juego in-situ...");
        _juegoFinalizado = false;
        juegoIniciado = false;
        _enConteo = false;
        _tiempoTranscurrido = 0f;
        _segundosMirando = 0f;
        _framesTargeteados = 0;
        _votosPositivosPrecision = 0;
        _votosTotalesPrecision = 0;

        // Ocultar componentes de resultados en OverlayResult
        if (overlayResult != null)
        {
            foreach (Transform child in overlayResult.transform)
            {
                if (textoMensajeInicio != null && (child == textoMensajeInicio.transform || child == textoMensajeInicio.transform.parent))
                    continue;
                
                child.gameObject.SetActive(false);
            }
            overlayResult.SetActive(true); 
        }

        if (overlayInicio != null) overlayInicio.SetActive(false);

        // Aseguramos que el contador sea visible
        if (textoMensajeInicio != null) 
        {
            textoMensajeInicio.gameObject.SetActive(true);
            if (textoMensajeInicio.transform.parent != null)
                textoMensajeInicio.transform.parent.gameObject.SetActive(true);
        }

        // Resetear posiciones de objetos
        PreconfigurarPosiciones();

        // Lanzar la cuenta regresiva
        IniciarJuego();
    }

    void ReiniciarEscena()
    {
        // Mantenemos este método por compatibilidad si se llama desde otro lado, 
        // pero preferimos ReiniciarJuegoSinCarga
        Time.timeScale = 1.0f;
        string nombreEscena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscena);
    }

    private float _trailTimer = 0f;
    void ManejarEstelaFantasmas()
    {
        if (star == null) return;
        
        _trailTimer += Time.deltaTime;
        if (_trailTimer > 0.05f) // Crear un fantasma cada 0.05 segundos
        {
            _trailTimer = 0f;
            CrearFantasma();
        }
    }

    void CrearFantasma()
    {
        // Crear un nuevo objeto para el fantasma
        GameObject ghost = new GameObject("StarGhost");
        ghost.transform.SetParent(star.parent);
        ghost.transform.SetSiblingIndex(star.GetSiblingIndex()); // Ponerlo justo detrás o delante
        
        RectTransform rt = ghost.AddComponent<RectTransform>();
        rt.anchoredPosition = star.anchoredPosition;
        rt.sizeDelta = star.sizeDelta;
        rt.localScale = star.localScale;
        rt.rotation = star.rotation;

        // Copiar la imagen
        Image starImg = star.GetComponent<Image>();
        if (starImg != null)
        {
            Image ghostImg = ghost.AddComponent<Image>();
            ghostImg.sprite = starImg.sprite;
            ghostImg.color = new Color(1, 0.9f, 0.2f, 0.5f); // Dorado semitransparente
            
            // Iniciar desvanecimiento
            StartCoroutine(RoutineFadeGhost(ghostImg));
        }
        else {
            Destroy(ghost);
        }
    }

    System.Collections.IEnumerator RoutineFadeGhost(Image img)
    {
        float duracion = 0.4f;
        float elapsed = 0f;
        Vector3 initialScale = img.rectTransform.localScale;
        Color color = img.color;

        while (elapsed < duracion)
        {
            if (img == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duracion;

            // Desvanecer alfa y encoger
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.5f, 0f, t));
            img.rectTransform.localScale = Vector3.Lerp(initialScale, initialScale * 0.2f, t);
            
            yield return null;
        }

        if (img != null) Destroy(img.gameObject);
    }

    // Eliminamos el método antiguo que no funcionaba en UI Overlay
    void ConfigurarEstela() { }

    void SetOverlayText(GameObject obj, string message)
    {
        if (obj == null) return;
        var tmp = obj.GetComponent<TMP_Text>();
        if (tmp == null) tmp = obj.GetComponentInChildren<TMP_Text>();
        
        if (tmp != null) {
            tmp.richText = true;
            tmp.text = message;
        }
    }

    void ManejarPestañeoInicio()
    {
        ManejarInstruccionesYParpadeo();
    }

    void ActualizarUI()
    {
        if (textoTimer != null) {
            float tiempoRestante = Mathf.Max(0, duracionSesion - _tiempoTranscurrido);
            textoTimer.text = tiempoRestante.ToString("F0") + "s";
        }

        // 1. Precisión Clínica (%): De todo el tiempo que el sensor detectó ojos, 
        // ¿cuánta parte de ese tiempo estuviste sobre el objetivo?
        // Esto mide la PUNTERÍA real del paciente.
        float precisionClinica = (_votosTotalesPrecision > 0) ? (_votosPositivosPrecision / (float)_votosTotalesPrecision) * 100f : 0;
        
        if (precText != null) 
        {
            precText.text = precisionClinica.ToString("F0") + "%";
            precText.color = precisionClinica > 70 ? Color.green : Color.yellow;
        }

        // 2. Acompañamiento (0 a 10): Eficacia respecto al tiempo TOTAL de la sesión.
        float acompañamientoPct = (duracionSesion > 0) ? (_segundosMirando / duracionSesion) * 100f : 0;
        float acompañamiento0a10 = (acompañamientoPct / 10f);
        if (avanceText != null) avanceText.text = acompañamiento0a10.ToString("F1");
            
        if (barFill != null) barFill.fillAmount = acompañamientoPct / 100f;

        // 3. Puntos (0 a 100): Puntuación Ponderada (70% Eficacia + 30% Puntería)
        float scorePonderado = (acompañamientoPct * 0.7f) + (precisionClinica * 0.3f);
        this.puntuacion = Mathf.RoundToInt(scorePonderado);
        
        if (percentRes != null) {
            percentRes.text = this.puntuacion.ToString();
        }
    }

    void MoverObjetosAdicionales()
    {
        // 2. MOVIMIENTO DE ÚNICA ESTRELLA (Rebote Izquierda-Derecha)
        if (_convoyEstrellas != null)
        {
            float anchoPadre = 1920f;
            if (_convoyEstrellas.Length > 0 && _convoyEstrellas[0] != null) {
                RectTransform padre = _convoyEstrellas[0].parent as RectTransform;
                if (padre != null && padre.rect.width > 0) anchoPadre = padre.rect.width;
            }

            float limiteBorde = (anchoPadre / 2f) - 100f; // Margen de 100px para que no desaparezca

            foreach (var s in _convoyEstrellas)
            {
                s.anchoredPosition += Vector2.right * (velocidadStar * _dirEstrella * Time.deltaTime);
                
                if (s.anchoredPosition.x > limiteBorde) {
                    s.anchoredPosition = new Vector2(limiteBorde, s.anchoredPosition.y);
                    _dirEstrella = -1; // Rebotar hacia la izquierda
                }
                else if (s.anchoredPosition.x < -limiteBorde) {
                    s.anchoredPosition = new Vector2(-limiteBorde, s.anchoredPosition.y);
                    _dirEstrella = 1; // Rebotar hacia la derecha
                }
            }
        }

        // 3. DISTRACTOR (Zigzag + Scroll de mundo)
        if (distractor != null)
        {
            float limiteBordeDist = (Screen.width / 2f) + 80f;
            float sinY = Mathf.Sin(Time.time * zigzagFrecuencia) * zigzagAmplitud;
            distractor.anchoredPosition += Vector2.left * (velocidadDistractor + velocidadFondo) * Time.deltaTime;
            distractor.anchoredPosition = new Vector2(distractor.anchoredPosition.x, sinY);
            if (distractor.anchoredPosition.x < -limiteBordeDist) distractor.anchoredPosition = new Vector2(limiteBordeDist, 0);
        }
    }

    void DesplazarFondo()
    {
        foreach (RectTransform seg in _bgSegments)
        {
            if (seg == null) continue;
            seg.anchoredPosition += Vector2.left * velocidadFondo * Time.deltaTime;
            float width = seg.rect.width;
            if (seg.anchoredPosition.x < -width)
                seg.anchoredPosition += new Vector2(width * 2f, 0f);
        }
    }

    void OnGUI()
    {
        // Solo dibujar el punto si el juego está en marcha y tenemos datos de gaze
        if (juegoIniciado && _gazeDebugPos != Vector2.zero)
        {
            GUI.color = new Color(1, 0, 0, 0.7f); // Rojo semitransparente
            // Convertir de coordenadas de pantalla Unity (0,0 abajo izq) a OnGUI (0,0 arriba izq)
            Rect gazeRect = new Rect(_gazeDebugPos.x - 10, Screen.height - _gazeDebugPos.y - 10, 20, 20);
            GUI.DrawTexture(gazeRect, Texture2D.whiteTexture);
            
            // Opcional: Dibujar coordenadas para ver si llegan a Screen.width
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 300, 20), $"Gaze Pos: {_gazeDebugPos.x:F0}, {_gazeDebugPos.y:F0} | Screen: {Screen.width}x{Screen.height}");
        }
    }

    void AplicarBrilloEstrella()
    {
        if (_convoyEstrellas == null) return;
        float escala = 1.0f + Mathf.Sin(Time.time * 8.5f) * 0.12f;
        foreach (var s in _convoyEstrellas) s.localScale = new Vector3(escala, escala, 1f);
    }

    void ProcesarSeguimientoOcular()
    {
        if (!juegoIniciado) return;

        // Protección: Solo un procesamiento por frame
        if (Time.frameCount == _ultimoFrameProcesado) return;
        _ultimoFrameProcesado = Time.frameCount;

        // Incrementamos ambos al mismo tiempo. 
        // Solo si algo falla (parpadeo) dejaremos de incrementar el positivo.
        _votosTotalesPrecision++;
        
        // 1. Intentar obtener datos del Tobii Pro SDK
        bool tobiiValido = false;
        if (Tobii.Research.Unity.EyeTracker.Instance != null)
        {
            var data = Tobii.Research.Unity.EyeTracker.Instance.LatestGazeData;
            if (data != null && (data.Left.GazePointValid || data.Right.GazePointValid))
            {
                tobiiValido = true;
                _votosTotalesPrecision++; // <--- Se sumó un frame de señal válida
                
                Vector2 viewPos = new Vector2(
                    (data.Left.GazePointOnDisplayArea.x + data.Right.GazePointOnDisplayArea.x) / 2f,
                    (data.Left.GazePointOnDisplayArea.y + data.Right.GazePointOnDisplayArea.y) / 2f
                );
                _gazeDebugPos = new Vector2(viewPos.x * Screen.width, (1f - viewPos.y) * Screen.height);
            }
        }

        if (!tobiiValido)
        {
            // Fallback al ratón
            _gazeDebugPos = Input.mousePosition;
            _votosTotalesPrecision++; // El mouse siempre cuenta como señal válida
        }

        // 2. Mover el Puntero Rojo
        if (gazeDebug != null)
        {
            gazeDebug.SetActive(true);
            gazeDebug.transform.position = _gazeDebugPos;
        }
            
        if (_convoyEstrellas != null)
        {
            foreach (var s in _convoyEstrellas)
            {
                // Detectamos la cámara del canvas de forma dinámica para el Raycast
                Canvas rootCanvas = s.GetComponentInParent<Canvas>();
                Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null;

                if (RectTransformUtility.RectangleContainsScreenPoint(s, _gazeDebugPos, uiCam))
                {
                    _votosPositivosPrecision++; // <--- ¡Solo si está sobre la estrella!
                    _framesTargeteados++;
                    _segundosMirando += Time.deltaTime;
                    break;
                }
            }
        }
    }
    
    private System.Collections.IEnumerator RoutineRetrasoBotonesEstrella(int score)
    {
        // Ocultamos inicialmente
        _permitirReintento = false;
        if (btnAgain != null) btnAgain.gameObject.SetActive(false);
        if (subRes != null) subRes.gameObject.SetActive(false);

        // Si ha sacado 100, forzamos el apagado y bloqueamos reintento
        if (score >= 100) {
            _permitirReintento = false;
            if (btnAgain != null) btnAgain.gameObject.SetActive(false);
            if (subRes != null) subRes.gameObject.SetActive(false);
            yield break;
        }

        // Esperamos 5 segundos (Sincronizado con Explosión Globos)
        yield return new WaitForSeconds(3f);

        _permitirReintento = true;
        if (btnAgain != null) btnAgain.gameObject.SetActive(true);
        if (subRes != null) subRes.gameObject.SetActive(true);

    }
}
