using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GlobosManager : BaseActividad
{
    public enum EstadoJuego
    {
        Inicio,
        CuentaRegresiva,
        EnCurso,
        Resumen
    }

    [Header("Ajustes de Juego")]
    public int cantidadGlobos = 5;
    public float tamanoGlobo = 100f;
    public Color colorResaltado = Color.yellow;
    
    [Header("Dificultad")]
    public float tiempoLimiteSegundos = 60f;
    public float penalizacionSegundos = 5f;

    [Header("Referencias UI Especificas")]
    public RectTransform contenedorGlobos;
    public TMP_Text textoInstrucciones;
    public TMP_Text textoTimer;
    public Image flashRojoOverlay;
    public TMP_Text textoContador; // Para el panel de cuenta regresiva

    [Header("Paneles de Estado")]
    public GameObject panelInicio;
    public GameObject panelCuentaRegresiva;
    public GameObject panelJuego;
    public GameObject panelResumen;

    [Header("Tobii / Eye Tracking")]
    public float onsetDelayMs = 175f;
    public float dwellTimeMs = 600f;

    private EstadoJuego _estadoActual = EstadoJuego.Inicio;
    private int _siguienteNumero = 1;
    private List<GloboComponente> _globosActivos = new List<GloboComponente>();
    private bool _juegoTerminado = false;
    private float _tiempoRestante;
    private int _erroresCometidos = 0;
    private float _tiempoInicio;
    private bool _iniciando = false;

    protected override void Start()
    {
        // Detectar automáticamente si el SDK de Tobii está disponible
        if (usarValidacionOjos)
        {
            usarValidacionOjos = (GameObject.FindObjectOfType<TobiiGazeProvider>() != null);
        }
        
        base.Start();
        
        CambiarEstado(EstadoJuego.Inicio);
        
        if (flashRojoOverlay != null) flashRojoOverlay.gameObject.SetActive(false);
        
        _tiempoRestante = tiempoLimiteSegundos;
        ActualizarTimerUI();

        if (botonIniciar != null)
        {
            botonIniciar.onClick.RemoveAllListeners();
            botonIniciar.onClick.AddListener(IniciarJuego);
        }
    }

    public void CambiarEstado(EstadoJuego nuevoEstado)
    {
        _estadoActual = nuevoEstado;
        
        if (panelInicio != null) panelInicio.SetActive(nuevoEstado == EstadoJuego.Inicio);
        if (panelCuentaRegresiva != null) panelCuentaRegresiva.SetActive(nuevoEstado == EstadoJuego.CuentaRegresiva);
        if (panelJuego != null) panelJuego.SetActive(nuevoEstado == EstadoJuego.EnCurso);
        if (panelResumen != null) panelResumen.SetActive(nuevoEstado == EstadoJuego.Resumen);

        Debug.Log($"GlobosManager: Cambiado a estado {nuevoEstado}");
    }

    public override void IniciarJuego()
    {
        if (_estadoActual != EstadoJuego.Inicio || _iniciando) return;
        
        _iniciando = true;
        Time.timeScale = 1;
        LimpiarGlobos(); 
        CambiarEstado(EstadoJuego.CuentaRegresiva);
        StartCoroutine(RutinaInicioConCountdown());
    }

    private IEnumerator RutinaInicioConCountdown()
    {
        if (panelCuentaRegresiva != null) panelCuentaRegresiva.SetActive(true);
        if (panelInicio != null) panelInicio.SetActive(false);

        if (textoContador == null)
        {
            ComenzarJuegoDirecto();
            yield break;
        }

        string[] pasos = { "3", "2", "1", "¡YA!" };
        Color[] colores = { Color.white, Color.white, Color.white, Color.green };

        for (int i = 0; i < pasos.Length; i++)
        {
            textoContador.text = pasos[i];
            textoContador.color = colores[i];
            
            // Efecto POP
            textoContador.transform.localScale = Vector3.one * 1.5f;
            float duracion = pasos[i] == "¡YA!" ? 0.5f : 1.0f;
            float elapsed = 0;
            while (elapsed < duracion)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duracion;
                textoContador.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1.0f, t);
                yield return null;
            }
        }

        if (panelCuentaRegresiva != null) panelCuentaRegresiva.SetActive(false);
        ComenzarJuegoDirecto();
    }

    private void ComenzarJuegoDirecto()
    {
        _iniciando = false;
        base.IniciarJuego(); 
        CambiarEstado(EstadoJuego.EnCurso);
        
        _tiempoInicio = Time.time;
        _tiempoRestante = tiempoLimiteSegundos;
        _juegoTerminado = false;
        
        PrepararNivel();
        StartCoroutine(RutinaTimer());
    }

    private IEnumerator RutinaTimer()
    {
        while (juegoIniciado && !_juegoTerminado && _tiempoRestante > 0)
        {
            if (!juegoPausado)
            {
                _tiempoRestante -= Time.deltaTime;
                ActualizarTimerUI();

                if (_tiempoRestante <= 0)
                {
                    _tiempoRestante = 0;
                    TerminarPartida(false);
                }
            }
            yield return null;
        }
    }

    void ActualizarTimerUI()
    {
        if (textoTimer == null) return;

        int minutos = Mathf.FloorToInt(_tiempoRestante / 60);
        int segundos = Mathf.FloorToInt(_tiempoRestante % 60);
        textoTimer.text = string.Format("{0:00}:{1:00}", minutos, segundos);

        if (_tiempoRestante > 30) textoTimer.color = Color.green;
        else if (_tiempoRestante > 15) textoTimer.color = Color.yellow;
        else textoTimer.color = Color.red;
    }

    void PrepararNivel()
    {
        _siguienteNumero = 1;
        _erroresCometidos = 0;
        LimpiarGlobos();
        
        if (cantidadGlobos <= 5) tamanoGlobo = 120f;
        else if (cantidadGlobos <= 8) tamanoGlobo = 100f;
        else tamanoGlobo = 85f;

        for (int i = 1; i <= cantidadGlobos; i++)
        {
            CrearGlobo(i);
        }
        
        Canvas.ForceUpdateCanvases();
        PosicionarGlobosEnGrid();
        ActualizarInstrucciones();
    }

    private void PosicionarGlobosEnGrid()
    {
        if (contenedorGlobos == null || _globosActivos.Count == 0) return;

        Rect area = contenedorGlobos.rect;
        if (area.width < 100) area = new Rect(-960, -540, 1920, 1080);

        float topMargin = 100f;
        float bottomMargin = 100f;
        float sideMargin = 100f;
        
        float availableWidth = area.width - (sideMargin * 2);
        float availableHeight = area.height - topMargin - bottomMargin;
        
        float aspect = availableWidth / availableHeight;
        int cols = Mathf.CeilToInt(Mathf.Sqrt(_globosActivos.Count * aspect));
        if (cols < 1) cols = 1;
        int rows = Mathf.CeilToInt((float)_globosActivos.Count / cols);
        if (rows < 1) rows = 1;
        
        float cellWidth = availableWidth / cols;
        float cellHeight = availableHeight / rows;
        
        List<int> indices = Enumerable.Range(0, cols * rows).ToList();
        
        // Shuffle
        for (int i = 0; i < indices.Count; i++) {
            int randomIndex = Random.Range(i, indices.Count);
            int temp = indices[i];
            indices[i] = indices[randomIndex];
            indices[randomIndex] = temp;
        }
        
        for (int i = 0; i < _globosActivos.Count; i++)
        {
            if (i >= indices.Count) break;

            int cellIndex = indices[i];
            int r = cellIndex / cols;
            int c = cellIndex % cols;
            
            float startX = area.xMin + sideMargin;
            float startY = area.yMin + bottomMargin;
            
            float centerX = startX + (c + 0.5f) * cellWidth;
            float centerY = startY + (r + 0.5f) * cellHeight;
            
            float jitterX = Random.Range(-cellWidth * 0.15f, cellWidth * 0.15f);
            float jitterY = Random.Range(-cellHeight * 0.15f, cellHeight * 0.15f);
            
            RectTransform rt = _globosActivos[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(centerX + jitterX, centerY + jitterY);
        }
    }

    void CrearGlobo(int numero)
    {
        GameObject go = new GameObject("Globo_" + numero, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(contenedorGlobos, false);
        
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(tamanoGlobo, tamanoGlobo);

        GloboComponente globo = go.AddComponent<GloboComponente>();
        globo.Configurar(numero, tamanoGlobo, this);
        _globosActivos.Add(globo);
    }

    public void IntentarExplotar(GloboComponente globo)
    {
        if (_estadoActual != EstadoJuego.EnCurso || _juegoTerminado) return;

        if (globo.Numero == _siguienteNumero)
        {
            _siguienteNumero++;
            _globosActivos.Remove(globo);
            globo.Explotar();
            
            if (_globosActivos.Count == 0) TerminarPartida(true);
            else ActualizarInstrucciones();
        }
        else
        {
            _erroresCometidos++;
            _tiempoRestante -= penalizacionSegundos;
            StartCoroutine(EfectoFlashRojo());
            CrearTextoFlotante(globo.transform.position, "-" + penalizacionSegundos + "s");
        }
    }

    private IEnumerator EfectoFlashRojo()
    {
        if (flashRojoOverlay != null)
        {
            flashRojoOverlay.gameObject.SetActive(true);
            flashRojoOverlay.color = new Color(1, 0, 0, 0.4f);
            yield return new WaitForSeconds(0.2f);
            flashRojoOverlay.gameObject.SetActive(false);
        }
    }

    private void CrearTextoFlotante(Vector3 posicion, string texto)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.SetParent(contenedorGlobos.transform.parent, false);
        go.transform.position = posicion;
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = texto; txt.color = Color.red; txt.fontSize = 40;
        txt.alignment = TextAlignmentOptions.Center;
        StartCoroutine(AnimacionTextoFlotante(go));
    }

    private IEnumerator AnimacionTextoFlotante(GameObject go)
    {
        Vector3 startPos = go.transform.position;
        TMP_Text txt = go.GetComponent<TMP_Text>();
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            go.transform.position = startPos + Vector3.up * t * 100f;
            txt.color = new Color(1, 0, 0, 1f - t);
            yield return null;
        }
        Destroy(go);
    }

    void ActualizarInstrucciones()
    {
        if (textoInstrucciones != null)
            textoInstrucciones.text = "Busca y explota el número: " + _siguienteNumero;
    }

    private float CalcularMultiplicadorTiempo(float tiempoUsado, float tiempoLimite)
    {
        float porcentajeTiempo = tiempoUsado / tiempoLimite;

        if (porcentajeTiempo <= 0.33f)
            return 1.0f;   // Zona Rápida
        else if (porcentajeTiempo <= 0.66f)
            return 0.65f;  // Zona Media
        else
            return 0.35f;  // Zona Lenta
    }

    private void TerminarPartida(bool completado)
    {
        if (_juegoTerminado) return;
        _juegoTerminado = true;
        juegoIniciado = false;
        
        float tiempoUsado = Time.time - _tiempoInicio;
        int globosExplotados = cantidadGlobos - _globosActivos.Count;

        // 1. Precisión (40%)
        int intentosTotales = globosExplotados + _erroresCometidos;
        float precision = (intentosTotales > 0) ? (globosExplotados / (float)intentosTotales) * 100f : 0f;
        precision = Mathf.Clamp(precision, 0f, 100f);

        // 2. Velocidad (40%)
        float multiplicador = CalcularMultiplicadorTiempo(tiempoUsado, tiempoLimiteSegundos);
        float velocidad;
        string zonaVel = "Lenta";
        if (tiempoUsado <= tiempoLimiteSegundos * 0.33f) zonaVel = "Rápida";
        else if (tiempoUsado <= tiempoLimiteSegundos * 0.66f) zonaVel = "Media";

        if (completado)
        {
            velocidad = 100f * multiplicador; 
        }
        else
        {
            float proporcion = globosExplotados / (float)cantidadGlobos;
            velocidad = proporcion * 50f * multiplicador;
        }
        velocidad = Mathf.Clamp(velocidad, 0f, 100f);

        // 3. Consistencia (20%)
        float consistencia = 100f - (_erroresCometidos * 15f);
        consistencia = Mathf.Clamp(consistencia, 0f, 100f);

        // Puntuación Final
        float puntuacionFinal = (precision * 0.40f) + (velocidad * 0.40f) + (consistencia * 0.20f);
        puntuacionFinal = Mathf.Round(puntuacionFinal * 10f) / 10f;
        puntuacionFinal = Mathf.Clamp(puntuacionFinal, 0f, 100f);

        SesionExplosionGlobos sesion = new SesionExplosionGlobos(
            cantidadGlobos, globosExplotados, _erroresCometidos, 
            tiempoUsado, tiempoLimiteSegundos, completado, 
            zonaVel, precision, velocidad, consistencia, puntuacionFinal
        );

        if (GestorPaciente.Instance != null)
            GestorPaciente.Instance.GuardarSesionExplosionGlobos(sesion);

        CambiarEstado(EstadoJuego.Resumen);
        StartCoroutine(MostrarResultadosAnimados(sesion));
    }

    public override void ReiniciarJuego()
    {
        _juegoTerminado = false;
        _iniciando = false;
        juegoIniciado = false;
        _erroresCometidos = 0;
        _siguienteNumero = 1;
        _tiempoRestante = tiempoLimiteSegundos;
        
        LimpiarGlobos();
        
        if (panelResumen != null) panelResumen.SetActive(false);
        CambiarEstado(EstadoJuego.Inicio);
    }

    private IEnumerator MostrarResultadosAnimados(SesionExplosionGlobos sesion)
    {
        // Limpieza previa
        foreach (Transform child in panelResumen.transform) Destroy(child.gameObject);

        // 1. Overlay de fondo (Negro semitransparente)
        GameObject bg = new GameObject("OverlayBG", typeof(RectTransform));
        bg.transform.SetParent(panelResumen.transform, false);
        Image bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0, 0, 0, 0.85f);
        RectTransform rtBg = bg.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one; rtBg.sizeDelta = Vector2.zero;

        // 2. Contenedor Central
        GameObject containerGo = new GameObject("ContenedorCentral", typeof(RectTransform));
        containerGo.transform.SetParent(bg.transform, false);
        RectTransform rtCont = containerGo.GetComponent<RectTransform>();
        rtCont.sizeDelta = new Vector2(680, 0); // Ancho fijo, altura controlada por Layout
        rtCont.anchorMin = new Vector2(0.5f, 0.5f); rtCont.anchorMax = new Vector2(0.5f, 0.5f); rtCont.pivot = new Vector2(0.5f, 0.5f);

        // Layout Vertical para el contenedor
        VerticalLayoutGroup mainLayout = containerGo.AddComponent<VerticalLayoutGroup>();
        mainLayout.padding = new RectOffset(40, 40, 40, 40);
        mainLayout.spacing = 20;
        mainLayout.childAlignment = TextAnchor.UpperCenter;
        mainLayout.childControlHeight = true; mainLayout.childForceExpandHeight = false;

        ContentSizeFitter csf = containerGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Fondo del contenedor (oscuro redondeado)
        GameObject innerBg = new GameObject("InnerBG", typeof(RectTransform));
        innerBg.transform.SetParent(containerGo.transform, false);
        innerBg.transform.SetAsFirstSibling();
        Image innerImg = innerBg.AddComponent<Image>(); innerImg.color = new Color(0.12f, 0.12f, 0.15f, 1f);
        RectTransform rtInner = innerBg.GetComponent<RectTransform>();
        rtInner.anchorMin = Vector2.zero; rtInner.anchorMax = Vector2.one; rtInner.sizeDelta = Vector2.zero;

        // --- CONTENIDO ---
        var (rango, colorRango, motivacion, estrellas) = ObtenerInfoRango(sesion.puntuacionFinal);

        // Título
        TMP_Text titulo = CrearTextoUI(containerGo, "Titulo", motivacion, 32, colorRango, Vector2.zero);
        titulo.fontStyle = FontStyles.Bold;
        titulo.gameObject.AddComponent<CanvasGroup>().alpha = 0;

        // Fila Métricas
        GameObject filaMetricas = new GameObject("FilaMetricas", typeof(RectTransform));
        filaMetricas.transform.SetParent(containerGo.transform, false);
        HorizontalLayoutGroup hMetricas = filaMetricas.AddComponent<HorizontalLayoutGroup>();
        hMetricas.childAlignment = TextAnchor.MiddleCenter; hMetricas.spacing = 15; hMetricas.childForceExpandWidth = false;
        
        CrearTextoUI(filaMetricas, "TxtGlobos", $"Globos: {sesion.globosExplotados}/{sesion.globosTotales}", 18, Color.white, Vector2.zero);
        CrearTextoUI(filaMetricas, "Sep", " • ", 18, Color.gray, Vector2.zero);
        CrearTextoUI(filaMetricas, "TxtTiempo", $"Tiempo: {sesion.tiempoUsado:F1}s ({sesion.zonaVelocidad})", 18, Color.white, Vector2.zero);
        CrearTextoUI(filaMetricas, "Sep", " • ", 18, Color.gray, Vector2.zero);
        CrearTextoUI(filaMetricas, "TxtErrores", $"Errores: {sesion.errores}", 18, Color.white, Vector2.zero);
        filaMetricas.AddComponent<CanvasGroup>().alpha = 0;

        CrearSeparador(containerGo);

        // Barras
        Color colPrec = Color.green; ColorUtility.TryParseHtmlString("#00E676", out colPrec);
        Color colVel = Color.cyan; ColorUtility.TryParseHtmlString("#29B6F6", out colVel);
        Color colCons = Color.yellow; ColorUtility.TryParseHtmlString("#FFD740", out colCons);

        var bPrec = CrearFilaBarra(containerGo, "Precisión", colPrec);
        var bVel = CrearFilaBarra(containerGo, "Velocidad", colVel);
        var bCons = CrearFilaBarra(containerGo, "Consistencia", colCons);

        CrearSeparador(containerGo);

        // Puntuación
        GameObject filaScore = new GameObject("FilaScore", typeof(RectTransform));
        filaScore.transform.SetParent(containerGo.transform, false);
        HorizontalLayoutGroup hScore = filaScore.AddComponent<HorizontalLayoutGroup>();
        hScore.childAlignment = TextAnchor.MiddleCenter; hScore.spacing = 15; hScore.childForceExpandWidth = false;

        CrearTextoUI(filaScore, "Estrellas", estrellas, 32, Color.white, Vector2.zero);
        TMP_Text scoreTxt = CrearTextoUI(filaScore, "Score", "0,0", 48, colorRango, Vector2.zero);
        scoreTxt.fontStyle = FontStyles.Bold;
        CrearTextoUI(filaScore, "Rango", "/ 100", 24, Color.gray, Vector2.zero);

        CrearSeparador(containerGo, "TU EVOLUCIÓN");

        // Histórico
        GameObject seccionHist = new GameObject("SeccionHistorico", typeof(RectTransform));
        seccionHist.transform.SetParent(containerGo.transform, false);
        var rtHist = seccionHist.GetComponent<RectTransform>(); rtHist.sizeDelta = new Vector2(600, 220);
        var cgHist = seccionHist.AddComponent<CanvasGroup>(); cgHist.alpha = 0;

        // Botones
        GameObject filaBotones = new GameObject("FilaBotones", typeof(RectTransform));
        filaBotones.transform.SetParent(containerGo.transform, false);
        HorizontalLayoutGroup hBotones = filaBotones.AddComponent<HorizontalLayoutGroup>();
        hBotones.spacing = 30; hBotones.childAlignment = TextAnchor.MiddleCenter; hBotones.childForceExpandWidth = false;

        Color colRetry; ColorUtility.TryParseHtmlString("#1565C0", out colRetry);
        Color colMenu; ColorUtility.TryParseHtmlString("#37474F", out colMenu);

        CrearBotonUI(filaBotones, "Reintentar", "REINTENTAR", colRetry, () => ReiniciarJuego());
        CrearBotonUI(filaBotones, "Menu", "MENÚ", colMenu, () => SalirAlMenu());

        // --- ANIMACIONES ---
        StartCoroutine(AnimarFade(titulo.GetComponent<CanvasGroup>(), 1f, 0.4f));
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(AnimarFade(filaMetricas.GetComponent<CanvasGroup>(), 1f, 0.4f));
        yield return new WaitForSeconds(0.3f);

        StartCoroutine(AnimarBarra(bPrec.fill, bPrec.valTxt, sesion.componentePrecision, 0.6f));
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(AnimarBarra(bVel.fill, bVel.valTxt, sesion.componenteVelocidad, 0.6f));
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(AnimarBarra(bCons.fill, bCons.valTxt, sesion.componenteConsistencia, 0.6f));
        yield return new WaitForSeconds(0.4f);

        float sElapsed = 0f;
        while (sElapsed < 0.6f) {
            sElapsed += Time.deltaTime;
            scoreTxt.text = (Mathf.Lerp(0, sesion.puntuacionFinal, sElapsed / 0.6f)).ToString("F1");
            yield return null;
        }
        scoreTxt.text = sesion.puntuacionFinal.ToString("F1");

        yield return new WaitForSeconds(0.3f);
        CargarYMostrarHistorico(seccionHist);
        StartCoroutine(AnimarFade(cgHist, 1f, 0.5f));
    }

    private void CargarYMostrarHistorico(GameObject container)
    {
        List<SesionExplosionGlobos> historial = new List<SesionExplosionGlobos>();
        if (GestorPaciente.Instance != null && GestorPaciente.Instance.pacienteActual != null) {
            historial = GestorPaciente.Instance.pacienteActual.historialExplosionGlobos
                        .OrderByDescending(s => s.fechaHora).ToList();
        }

        if (historial.Count < 1) {
            CrearTextoUI(container, "Msg", "¡Primera sesión! El historial aparecerá aquí.", 18, Color.gray, Vector2.zero);
        } else {
            // Layout horizontal para Gráfica y Stats
            GameObject layoutH = new GameObject("HistLayout", typeof(RectTransform));
            layoutH.transform.SetParent(container.transform, false);
            var rt = layoutH.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(600, 200);
            HorizontalLayoutGroup hLayout = layoutH.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 30; hLayout.childControlWidth = true; hLayout.childForceExpandWidth = false;

            GameObject grafCont = new GameObject("GraficaCont", typeof(RectTransform));
            grafCont.transform.SetParent(layoutH.transform, false);
            grafCont.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 200);
            CrearGrafica(grafCont, historial.Take(8).Reverse().ToList());

            GameObject statsCont = new GameObject("StatsCont", typeof(RectTransform));
            statsCont.transform.SetParent(layoutH.transform, false);
            statsCont.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);
            CrearSeccionStats(statsCont, historial);
        }
    }

    private (Image fill, TMP_Text valTxt) CrearFilaBarra(GameObject parent, string label, Color barColor)
    {
        GameObject fila = new GameObject("Fila_" + label, typeof(RectTransform));
        fila.transform.SetParent(parent.transform, false);
        var rt = fila.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(600, 30);
        HorizontalLayoutGroup hLayout = fila.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 15; hLayout.childControlWidth = true; hLayout.childForceExpandWidth = false;

        TMP_Text lbl = CrearTextoUI(fila, "Label", label, 16, Color.white, Vector2.zero);
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.rectTransform.sizeDelta = new Vector2(130, 30);
        lbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;

        GameObject barBgGo = new GameObject("BarBG", typeof(RectTransform));
        barBgGo.transform.SetParent(fila.transform, false);
        barBgGo.AddComponent<LayoutElement>().flexibleWidth = 1;
        barBgGo.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 1f); // #2A2A2A

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(barBgGo.transform, false);
        var rtF = fillGo.GetComponent<RectTransform>();
        rtF.anchorMin = new Vector2(0, 0); rtF.anchorMax = new Vector2(0, 1); rtF.sizeDelta = Vector2.zero;
        Image img = fillGo.AddComponent<Image>(); img.color = barColor;

        TMP_Text val = CrearTextoUI(fila, "Value", "0", 16, Color.white, Vector2.zero);
        val.alignment = TextAlignmentOptions.Right;
        val.rectTransform.sizeDelta = new Vector2(50, 30);
        val.gameObject.AddComponent<LayoutElement>().preferredWidth = 50;

        return (img, val);
    }

    private IEnumerator AnimarBarra(Image fill, TMP_Text text, float target, float duration)
    {
        float elapsed = 0f;
        RectTransform rt = fill.GetComponent<RectTransform>();
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float current = Mathf.Lerp(0, target, t);
            rt.anchorMax = new Vector2(current / 100f, 1f);
            text.text = Mathf.RoundToInt(current).ToString();
            yield return null;
        }
        rt.anchorMax = new Vector2(target / 100f, 1f);
        text.text = Mathf.RoundToInt(target).ToString();
    }

    private IEnumerator AnimarFade(CanvasGroup cg, float target, float duration)
    {
        float start = cg.alpha;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        cg.alpha = target;
    }

    private void CrearSeparador(GameObject parent, string label = "")
    {
        GameObject sepGo = new GameObject("Separador", typeof(RectTransform));
        sepGo.transform.SetParent(parent.transform, false);
        var rt = sepGo.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(600, 30);

        if (!string.IsNullOrEmpty(label)) {
            TMP_Text t = CrearTextoUI(sepGo, "Text", label, 14, new Color(0.6f, 0.6f, 0.7f), Vector2.zero);
            t.fontStyle = FontStyles.Bold;
        } else {
            GameObject line = new GameObject("Line", typeof(RectTransform));
            line.transform.SetParent(sepGo.transform, false);
            line.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            line.GetComponent<RectTransform>().sizeDelta = new Vector2(580, 1);
        }
    }

    private (string rango, Color color, string motivacion, string estrellas) ObtenerInfoRango(float score)
    {
        if (score >= 90) return ("Excelente", Color.green, "¡Rendimiento excepcional!", "⭐⭐⭐");
        if (score >= 70) return ("Bien", new Color(0.4f, 0.6f, 1f), "¡Buen trabajo!", "⭐⭐");
        if (score >= 50) return ("Regular", Color.yellow, "¡Sigue practicando!", "⭐");
        return ("Iniciando", new Color(1f, 0.6f, 0.2f), "¡Cada sesión cuenta!", "●");
    }

    private void CrearGrafica(GameObject parent, List<SesionExplosionGlobos> data)
    {
        float width = 500f; float height = 200f;
        float spacing = width / (data.Count + 1);

        for (int i = 0; i < data.Count; i++) {
            float h = (data[i].puntuacionFinal / 100f) * height;
            GameObject bar = new GameObject("Bar_" + i, typeof(RectTransform));
            bar.transform.SetParent(parent.transform, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(25, h);
            rt.anchoredPosition = new Vector2((i + 1) * spacing, 40);

            Image img = bar.AddComponent<Image>();
            img.color = (i == data.Count - 1) ? new Color(0.96f, 0.77f, 0.09f) : new Color(0.3f, 0.45f, 0.65f);

            CrearTextoUI(bar, "Val", data[i].puntuacionFinal.ToString("F0"), 12, Color.white, new Vector2(0, h + 15));
            string fecha = data[i].fechaHora.Length >= 10 ? data[i].fechaHora.Substring(8, 2) + "/" + data[i].fechaHora.Substring(5, 2) : "??";
            CrearTextoUI(bar, "Date", fecha, 11, Color.gray, new Vector2(0, -20));
        }
    }

    private void CrearSeccionStats(GameObject parent, List<SesionExplosionGlobos> historial)
    {
        VerticalLayoutGroup vLayout = parent.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.MiddleLeft; vLayout.spacing = 15; vLayout.childForceExpandHeight = false;

        float mejor = historial.Max(s => s.puntuacionFinal);
        float promedio = historial.Average(s => s.puntuacionFinal);
        
        float rec = historial.Take(3).Average(s => s.puntuacionFinal);
        float ant = historial.Skip(3).Any() ? historial.Skip(3).Average(s => s.puntuacionFinal) : rec;
        string tendencia = "→ Estable"; Color colT = Color.white;
        if (rec - ant > 5f) { tendencia = "↑ Mejorando"; colT = Color.green; }
        else if (rec - ant < -5f) { tendencia = "↓ Revisar"; colT = Color.red; }

        CrearTextoUI(parent, "Mejor", $"Mejor: {mejor:F1}", 22, Color.white, Vector2.zero).alignment = TextAlignmentOptions.Left;
        CrearTextoUI(parent, "Prom", $"Promedio: {promedio:F1}", 22, Color.white, Vector2.zero).alignment = TextAlignmentOptions.Left;
        CrearTextoUI(parent, "Trend", tendencia, 26, colT, Vector2.zero).alignment = TextAlignmentOptions.Left;
    }

    private TMP_Text CrearTextoUI(GameObject parent, string name, string content, int size, Color color, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        TMP_Text txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = content; txt.fontSize = size; txt.color = color;
        txt.alignment = TextAlignmentOptions.Center;
        txt.rectTransform.anchoredPosition = pos;
        return txt;
    }

    private void CrearBotonUI(GameObject parent, string name, string label, Color bgColor, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(180, 50);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = go.AddComponent<Image>(); img.color = bgColor;
        Button btn = go.AddComponent<Button>(); btn.onClick.AddListener(action);

        GameObject txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        TMP_Text txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = 16; txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Overflow;
        
        txt.rectTransform.anchorMin = Vector2.zero; txt.rectTransform.anchorMax = Vector2.one; txt.rectTransform.sizeDelta = Vector2.zero;
    }

    void LimpiarGlobos() {
        _globosActivos.Clear();
        if (contenedorGlobos != null) {
            for (int i = contenedorGlobos.childCount - 1; i >= 0; i--)
                DestroyImmediate(contenedorGlobos.GetChild(i).gameObject);
        }
    }
}

public class GloboComponente : MonoBehaviour
{
    public int Numero { get; private set; }
    private GlobosManager _manager;
    private Image _img;

    public void Configurar(int num, float size, GlobosManager manager) {
        Numero = num; _manager = manager;
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        
        _img = gameObject.AddComponent<Image>();
        try {
            _img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            if (_img.sprite == null) {
                #if UNITY_EDITOR
                _img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/CircleCursor.png");
                #endif
            }
        } catch { _img.sprite = null; }
        _img.color = new Color(Random.value, Random.value, Random.value, 0.9f);
        
        GameObject txtGo = new GameObject("Num");
        txtGo.transform.SetParent(transform, false);
        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = num.ToString(); txt.fontSize = size * 0.4f;
        txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white;
        txt.rectTransform.sizeDelta = rt.sizeDelta;

        GameObject ringGo = new GameObject("ProgressRing");
        ringGo.transform.SetParent(transform, false);
        Image ringImg = ringGo.AddComponent<Image>();
        ringImg.type = Image.Type.Filled; ringImg.fillMethod = Image.FillMethod.Radial360;
        ringImg.fillAmount = 0; ringImg.color = new Color(1f, 1f, 1f, 0.6f);
        RectTransform ringRt = ringGo.GetComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero; ringRt.anchorMax = Vector2.one; ringRt.sizeDelta = Vector2.zero;
        ringGo.SetActive(false);

        if (_manager.usarValidacionOjos) {
            GazeDwellHandler dwell = gameObject.AddComponent<GazeDwellHandler>();
            dwell.Configurar(_manager.onsetDelayMs, _manager.dwellTimeMs, ringImg, () => _manager.IntentarExplotar(this));
        }

        Button btn = gameObject.AddComponent<Button>();
        btn.onClick.AddListener(() => _manager.IntentarExplotar(this));
        Navigation nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
    }

    public void Explotar() { StartCoroutine(AnimExplotar()); }

    private IEnumerator AnimExplotar() {
        Vector3 startScale = transform.localScale;
        for (float t = 0; t < 1f; t += Time.deltaTime * 5f) {
            transform.localScale = startScale * (1f + t);
            if(_img != null) _img.color = new Color(_img.color.r, _img.color.g, _img.color.b, 1f - t);
            yield return null;
        }
        Destroy(gameObject);
    }
}
