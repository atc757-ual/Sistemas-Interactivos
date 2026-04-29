using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class ExplosionGlobosManager : MonoBehaviour
{
    public enum GameState { Inicio, Playing, Results }

    [Header("Configuración")]
    [SerializeField] int   cantidadItems      = 7;
    [SerializeField] float tiempoSesion       = 60f;
    [SerializeField] int   maxVidas           = 3;
    [SerializeField] bool  useEyeTracking     = true;

    [Header("UI Hierarchy")]
    [SerializeField] GameObject overlayInicio;
    [SerializeField] GameObject overlayFinal;
    [SerializeField] GameObject overlayInstructions;
    [SerializeField] GameObject overlayRetry;
    [SerializeField] GameObject playerCursor;
    [SerializeField] GameObject itemTemplate; 
    [SerializeField] RectTransform container;
    
    [Header("Componentes UI")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] TMP_Text fallosText;
    [SerializeField] TMP_Text textoContador;    // Para OverlayInicio
    [SerializeField] TMP_Text counterRetry;     // Para OverlayFinal
    [SerializeField] TMP_Text finalScoreText;    // OverlayMetricaScore
    [SerializeField] TMP_Text finalErrorsText;   // OverlayMetricaErrores
    [SerializeField] TMP_Text finalTimeText;     // OverlayMetricaTiempo
    [SerializeField] TMP_Text finalResultText;   // OverlayResult
    [SerializeField] TMP_Text finalMessageText;  // OverlayMessage
    [SerializeField] Button volverBtn;
    [SerializeField] Button startBtn;
    [SerializeField] Button retryBtn; 
    [SerializeField] Image flashDano;
    [SerializeField] RectTransform heartsContainer;
    private List<Image> _lifeIcons = new();

    private GameState _state;
    private List<BalloonController> _activeItems = new();
    private int   _itemsPopped, _vidasRestantes, _errors;
    private float _startTime, _timeRemaining;
    private bool  _timerRunning;
    private float _finalScore;

    // Blink detection
    private float _blinkTimer = 0f;
    private bool  _eyesWereDetected = false;

    void Start()
    {
        // GUARDIA DE SESIÓN: si no hay login válido, volver al Login (igual que BaseActividad)
        if (GestorPaciente.Instance == null || !GestorPaciente.Instance.EsSesionValida())
        {
            Debug.LogWarning("[ExplosionGlobos] Sin sesión válida. Redirigiendo a Login.");
            SceneManager.LoadScene("Login");
            return;
        }

        AutoBind();
        if (startBtn) startBtn.onClick.AddListener(EmpezarConteo);
        if (retryBtn) retryBtn.onClick.AddListener(ReiniciarJuego);
        if (volverBtn) volverBtn.onClick.AddListener(() => SceneManager.LoadScene("Activities"));
        
        SetState(GameState.Inicio);
    }

    void ConfigurarCursorLaser(GameObject cursor)
    {
        if (cursor == null) return;
        
        var cursorImg = cursor.GetComponent<Image>();
        if (cursorImg != null)
        {
            cursorImg.sprite = null; // Quitar el meteorito
            cursorImg.color = Color.red;
            
            // Hacerlo un punto pequeño
            var rt = cursor.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10, 10);
        }

        // Añadir un brillo suave
        GameObject glow = new GameObject("LaserGlow");
        glow.transform.SetParent(cursor.transform, false);
        var glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(1, 0, 0, 0.4f);
        var glowRt = glow.GetComponent<RectTransform>();
        glowRt.sizeDelta = new Vector2(20, 20);

        StartCoroutine(RutinaPulsoLaser(cursor.transform, glowRt));
    }

    IEnumerator RutinaPulsoLaser(Transform dot, RectTransform glow)
    {
        while (true)
        {
            float scale = 1f + Mathf.PingPong(Time.time * 2f, 0.2f);
            if (dot) dot.localScale = new Vector3(scale, scale, 1f);
            if (glow) glow.localScale = new Vector3(scale * 1.15f, scale * 1.15f, 1f);
            yield return null;
        }
    }

    void AutoBind()
    {
        var canvas = GameObject.Find("Canvasa") ?? GameObject.Find("Canvas");
        if (canvas != null)
        {
            if (overlayInicio == null) overlayInicio = canvas.transform.Find("OverlayInicio")?.gameObject;
            if (overlayInstructions == null) overlayInstructions = canvas.transform.Find("OverlayInstructions")?.gameObject;
            if (overlayRetry == null) overlayRetry = canvas.transform.Find("OverlayRetry")?.gameObject;
            // Acepta tanto "OverlayFinal" como "OverlayResult" como nombre del overlay de resultados
            if (overlayFinal == null) overlayFinal = canvas.transform.Find("OverlayFinal")?.gameObject
                                                  ?? canvas.transform.Find("OverlayResult")?.gameObject;
            if (playerCursor == null) playerCursor = canvas.transform.Find("PlayerCursor")?.gameObject;
            
            // Creamos un clon oculto para usar como plantilla ANTES de tocar el cursor
            if (itemTemplate == null && playerCursor != null) {
                itemTemplate = Instantiate(playerCursor, container);
                itemTemplate.name = "ItemTemplate_Hidden";
                itemTemplate.SetActive(false);

                // Convertir el PlayerCursor en un punto láser rojo
                ConfigurarCursorLaser(playerCursor);
            }

            if (container == null) container = canvas.GetComponent<RectTransform>();
            if (timerText == null) timerText = canvas.transform.Find("TimerText")?.GetComponent<TMP_Text>();
            if (fallosText == null) fallosText = canvas.transform.Find("fallos")?.GetComponent<TMP_Text>();
            if (flashDano == null) flashDano = canvas.transform.Find("FlashDano")?.GetComponent<Image>();

            // VolverBtn: buscar primero en raíz del Canvas, luego dentro del OverlayFinal
            if (volverBtn == null) {
                volverBtn = canvas.transform.Find("VolverBtn")?.GetComponent<Button>()
                         ?? canvas.transform.Find("BackBtn")?.GetComponent<Button>()
                         ?? canvas.transform.Find("BotonVolver")?.GetComponent<Button>();
            }
            // Si no estaba en la raíz, buscar dentro del overlay de resultados
            if (volverBtn == null && overlayFinal != null) {
                volverBtn = FindInChildRecursive(overlayFinal.transform, "VolverBtn")?.GetComponent<Button>()
                         ?? FindInChildRecursive(overlayFinal.transform, "BackBtn")?.GetComponent<Button>()
                         ?? FindInChildRecursive(overlayFinal.transform, "BotonVolver")?.GetComponent<Button>();
            }

            if (heartsContainer == null) {
                var hc = canvas.transform.Find("HeartsContainer");
                if (hc != null) heartsContainer = hc.GetComponent<RectTransform>();
            }

            // Asignar listener al VolverBtn (sin importar dónde se encontró)
            if (volverBtn != null) {
                volverBtn.onClick.RemoveAllListeners();
                volverBtn.onClick.AddListener(() => SceneManager.LoadScene("Activities"));
            }
            
            if (textoContador == null && overlayInicio != null) 
                textoContador = FindInChildRecursive(overlayInicio.transform, "TextoContador")?.GetComponent<TMP_Text>();
            
            if (retryBtn == null && overlayFinal != null)
                retryBtn = FindInChildRecursive(overlayFinal.transform, "BotonReload")?.GetComponent<Button>();

            if (counterRetry == null && overlayFinal != null)
                counterRetry = FindInChildRecursive(overlayFinal.transform, "CounterRetry")?.GetComponent<TMP_Text>();

            // Binding de resultados específicos
            if (overlayFinal != null)
            {
                if (finalScoreText == null) finalScoreText = FindInChildRecursive(overlayFinal.transform, "OverlayMetricaScore")?.GetComponent<TMP_Text>();
                if (finalErrorsText == null) finalErrorsText = FindInChildRecursive(overlayFinal.transform, "OverlayMetricaErrores")?.GetComponent<TMP_Text>();
                if (finalTimeText == null) finalTimeText = FindInChildRecursive(overlayFinal.transform, "OverlayMetricaTiempo")?.GetComponent<TMP_Text>();
                if (finalResultText == null) finalResultText = FindInChildRecursive(overlayFinal.transform, "OverlayResult")?.GetComponent<TMP_Text>();
                if (finalMessageText == null) finalMessageText = FindInChildRecursive(overlayFinal.transform, "OverlayMessage")?.GetComponent<TMP_Text>();
            }
        }

        if (startBtn == null && overlayInicio != null) 
            startBtn = overlayInicio.GetComponentInChildren<Button>(true);

        // Forzamos 20 elementos si el usuario así lo quiere
        if (cantidadItems < 20) cantidadItems = 20;

        VincularCorazones();
    }


    void VincularCorazones()
    {
        if (heartsContainer == null) return;
        _lifeIcons.Clear();
        foreach (Transform child in heartsContainer)
        {
            Image img = child.GetComponent<Image>();
            if (img != null) _lifeIcons.Add(img);
        }
    }

    Transform FindInChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent) {
            if (child.name == name) return child;
            var res = FindInChildRecursive(child, name);
            if (res != null) return res;
        }
        return null;
    }

    void EmpezarConteo()
    {
        if (_state == GameState.Playing || _isStarting) return;
        _isStarting = true;
        if (overlayInstructions != null) overlayInstructions.SetActive(false);
        StartCoroutine(RutinaCountdown(overlayInicio, textoContador));
    }

    public void ReiniciarJuego()
    {
        ClearItems();
        if (overlayRetry != null) overlayRetry.SetActive(false);
        StartCoroutine(RutinaCountdown(overlayFinal, counterRetry));
    }

    IEnumerator RutinaCountdown(GameObject overlay, TMP_Text counter)
    {
        if (overlay == null) { IniciarJuego(); yield break; }

        if (counter != null) {
            if (counter.transform.parent != overlay.transform) {
                counter.transform.SetParent(overlay.transform, true);
            }
        }

        // Apagar todos los hijos excepto el contador
        foreach (Transform child in overlay.transform) {
            if (counter != null && child == counter.transform) child.gameObject.SetActive(true);
            else child.gameObject.SetActive(false);
        }

        if (counter) {
            counter.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--) {
                counter.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }
            counter.text = "¡Explota!";
            yield return new WaitForSecondsRealtime(0.5f);
            counter.gameObject.SetActive(false);
        }

        IniciarJuego();
    }

    void ManejarInstruccionesYParpadeo()
    {
        bool eyesDetected = TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.EyeDataValid;

        // Mostrar instrucciones solo si se detectan ojos
        if (overlayInstructions != null)
        {
            if (overlayInstructions.activeSelf != eyesDetected) {
                overlayInstructions.SetActive(eyesDetected);
                if (eyesDetected) SetOverlayText(overlayInstructions, "<b>¡Hemos detectado tus ojos!</b>\n\nPestañea o haz clic en el botón inferior para iniciar la aventura.");
            }
        }

        if (!eyesDetected)
        {
            if (_eyesWereDetected) _blinkTimer += Time.deltaTime;
        }
        else
        {
            // Si recuperamos los ojos después de un breve lapso (parpadeo)
            if (_eyesWereDetected && _blinkTimer > 0.1f && _blinkTimer < 0.5f)
            {
                _eyesWereDetected = false; // Reset para evitar disparos múltiples
                _blinkTimer = 0;
                
                // Mostrar mensaje de confirmación antes de iniciar
                SetOverlayText(overlayInstructions, "<b>¡Pestañeo detectado!</b>\n\nIniciando aventura...");
                Debug.Log("<color=magenta><b>[GAME]</b> Pestañeo detectado. Iniciando...</color>");
                
                Invoke("EmpezarConteo", 0.5f); // Breve delay para leer el mensaje
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

        if (overlayRetry != null)
        {
            if (overlayRetry.activeSelf != eyesDetected) {
                overlayRetry.SetActive(eyesDetected);
                if (eyesDetected) SetOverlayText(overlayRetry, "<b>¡Hemos detectado tus ojos!</b>\n\nPestañea para reintentar la misión.");
            }
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
                
                // Mostrar mensaje de confirmación
                SetOverlayText(overlayRetry, "<b>¡Pestañeo detectado!</b>\n\nReiniciando misión...");
                
                Invoke("ReiniciarJuego", 0.5f); // Breve delay para leer el mensaje
            }
            else
            {
                _eyesWereDetected = true;
                _blinkTimer = 0;
            }
        }
    }

    void SetOverlayText(GameObject overlay, string message)
    {
        if (overlay == null) return;
        var tmp = overlay.GetComponentInChildren<TMP_Text>();
        if (tmp != null) {
            tmp.richText = true; // Forzar habilitación de tags
            tmp.text = message;
        }
    }

    void Update()

    {
        if (_state == GameState.Inicio)
        {
            if (!_isStarting) ManejarInstruccionesYParpadeo();
            return;
        }

        if (_state == GameState.Results)
        {
            if (_finalScore < 100f) ManejarReintentoPorParpadeo();
            return;
        }

        if (_state != GameState.Playing) return;

        if (_timerRunning)
        {
            _timeRemaining -= Time.deltaTime;
            if (timerText) timerText.text = Mathf.CeilToInt(_timeRemaining) + "s";
            if (_timeRemaining <= 0) EndGame(false);
        }

        if (useEyeTracking && TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
        {
            UpdateCursorPosition(TobiiGazeProvider.Instance.GazePositionScreen);
        }
    }

    void UpdateCursorPosition(Vector2 screenPos)
    {
        if (playerCursor == null) return;
        RectTransform rt = playerCursor.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt.parent as RectTransform, screenPos, null, out Vector2 local);
        rt.anchoredPosition = local;
    }

    private bool _isStarting = false;

    public void IniciarJuego()
    {
        cantidadItems = 7;
        _isStarting = false;

        if (itemTemplate != null) itemTemplate.SetActive(false); 

        _itemsPopped = 0;
        _errors = 0;
        _vidasRestantes = maxVidas;
        _timeRemaining = tiempoSesion;
        _startTime = Time.time;
        
        ClearItems();
        _activeItems = CreateItems(cantidadItems);
        
        UpdateFallosUI();
        SetState(GameState.Playing);
        
        // Reactivamos el cursor para que siga la mirada, PERO el template sigue oculto
        if (playerCursor != null) playerCursor.SetActive(true);

        _timerRunning = true;

        if (heartsContainer) heartsContainer.gameObject.SetActive(true);
        foreach (var icon in _lifeIcons) {
            if (icon) icon.color = Color.white;
            if (icon) icon.gameObject.SetActive(true);
        }
    }

    void SetState(GameState state)
    {
        _state = state;
        bool isPlaying = state == GameState.Playing;

        if (overlayInicio) 
        {
            overlayInicio.SetActive(state == GameState.Inicio);
            if (state == GameState.Inicio)
            {
                foreach (Transform child in overlayInicio.transform) child.gameObject.SetActive(true);
                if (textoContador) textoContador.gameObject.SetActive(false);
            }
        }
        if (overlayFinal) 
        {
            overlayFinal.SetActive(state == GameState.Results);
            if (state == GameState.Results)
            {
                foreach (Transform child in overlayFinal.transform) child.gameObject.SetActive(true);
                if (counterRetry) counterRetry.gameObject.SetActive(false);
            }
        }
        if (timerText) timerText.gameObject.SetActive(isPlaying);
        if (fallosText) fallosText.gameObject.SetActive(isPlaying);
        if (heartsContainer) heartsContainer.gameObject.SetActive(isPlaying);
        if (playerCursor) playerCursor.SetActive(isPlaying);
        if (volverBtn) volverBtn.gameObject.SetActive(!isPlaying);
        if (flashDano) flashDano.gameObject.SetActive(false);
    }

    List<BalloonController> CreateItems(int count)
    {
        float w = container.rect.width, h = container.rect.height;
        var list = new List<BalloonController>();
        var positions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(itemTemplate, container);
            go.name = "Item_" + (i + 1); go.SetActive(true);
            var rt = go.GetComponent<RectTransform>();
            
            // Lógica para evitar solapamiento
            Vector2 pos = Vector2.zero;
            bool valid = false;
            int attempts = 0;
            while (!valid && attempts < 50) {
                pos = new Vector2(UnityEngine.Random.Range(-w/2 + 120, w/2 - 120), UnityEngine.Random.Range(-h/2 + 180, h/2 - 180));
                valid = true;
                foreach (var p in positions) {
                    if (Vector2.Distance(pos, p) < 180f) { // Distancia mínima entre naves
                        valid = false;
                        break;
                    }
                }
                attempts++;
            }
            positions.Add(pos);
            rt.anchoredPosition = pos;
            
            var ctrl = go.GetComponent<BalloonController>();
            if (ctrl == null) ctrl = go.AddComponent<BalloonController>();
            ctrl.Init(i + 1, 130f, this, useEyeTracking);
            list.Add(ctrl);
        }
        return list;
    }

    public void OnItemInteracted(BalloonController item)
    {
        if (_state != GameState.Playing) return;

        if (item.Numero == _itemsPopped + 1)
        {
            _itemsPopped++;
            _activeItems.Remove(item);
            item.Pop();
            if (_itemsPopped >= cantidadItems) EndGame(true);
        }
        else
        {
            _vidasRestantes--;
            _errors++;
            UpdateFallosUI();
            
            if (_vidasRestantes > 0 && _vidasRestantes <= _lifeIcons.Count)
            {
                StartCoroutine(RutinaTintineoVida(_lifeIcons[_vidasRestantes]));
            }

            StartCoroutine(DoFlashDano());
            item.FlashError();
            if (_vidasRestantes <= 0) EndGame(false);
        }
    }

    void UpdateFallosUI() { if (fallosText) fallosText.text = "Vidas: " + _vidasRestantes; }

    IEnumerator DoFlashDano()
    {
        if (flashDano == null) yield break;
        flashDano.gameObject.SetActive(true);
        flashDano.color = new Color(1, 0, 0, 0.4f);
        yield return new WaitForSeconds(0.2f);
        flashDano.gameObject.SetActive(false);
    }

    private IEnumerator RutinaTintineoVida(Image icon)
    {
        if (icon == null) yield break;

        // Tintineo (Flicker)
        for (int i = 0; i < 4; i++)
        {
            icon.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSeconds(0.05f);
            icon.color = new Color(1, 1, 1, 1.0f);
            yield return new WaitForSeconds(0.05f);
        }

        // Fade out
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            icon.color = new Color(1, 1, 1, 1 - t);
            yield return null;
        }
        icon.gameObject.SetActive(false);
    }

    void EndGame(bool success)
    {
        _timerRunning = false;
        float elapsed = Time.time - _startTime;
        float scoreFinal = 0f;
        string resultTitle = "";
        string resultMsg = "";

        if (!success) 
        {
            scoreFinal = Mathf.Max(0, 30f - (_errors * 5f));
            resultTitle = "¡Misión Fallida!";
            if (_vidasRestantes <= 0)
                resultMsg = "Cometiste demasiados errores. La precisión es clave, ¡concéntrate y vuelve a intentarlo!";
            else
                resultMsg = "Se agotó el tiempo. ¡Debes ser más rápido en la próxima misión!";
        }
        else 
        {
            if (elapsed <= 15f) 
            {
                if (_errors == 0) 
                {
                    scoreFinal = 100f;
                    resultTitle = "¡Perfección Galáctica!";
                    resultMsg = "¡Impresionante! Destruiste todas las naves en tiempo récord y sin un solo fallo.";
                }
                else 
                {
                    scoreFinal = Mathf.Max(60f, 100f - (_errors * 15f));
                    resultTitle = "¡Misión Cumplida!";
                    resultMsg = "Fuiste increíblemente rápido, pero cometiste algunos errores. ¡Casi perfecto!";
                }
            }
            else 
            {
                float timePenalty = Mathf.Lerp(0f, 50f, (elapsed - 15f) / (tiempoSesion - 15f));
                scoreFinal = 90f - timePenalty - (_errors * 10f);
                scoreFinal = Mathf.Max(40f, scoreFinal); 
                
                resultTitle = "¡Buen Trabajo!";
                resultMsg = "Has logrado despejar el sector. Entrena tus reflejos para mejorar tu tiempo en el próximo intento.";
            }
        }
        
        scoreFinal = Mathf.Clamp(scoreFinal, 0, 100);

        if (GestorPaciente.Instance != null)
            GestorPaciente.Instance.GuardarPartida("Explosión Estelar", Mathf.RoundToInt(scoreFinal), 1, (_itemsPopped / (float)Mathf.Max(1, _itemsPopped + _errors)) * 100f, success, elapsed, _errors);

        if (finalScoreText) finalScoreText.text = Mathf.RoundToInt(scoreFinal).ToString();
        if (finalErrorsText) finalErrorsText.text = _errors.ToString();
        if (finalTimeText) finalTimeText.text = elapsed.ToString("F1") + "s";
        
        if (finalResultText) finalResultText.text = resultTitle;
        if (finalMessageText) finalMessageText.text = resultMsg;

        _finalScore = scoreFinal;
        SetState(GameState.Results);

        // Si el puntaje es 100, ocultar botón de reintento y su overlay
        if (_finalScore >= 100f) 
        {
            if (retryBtn != null) retryBtn.gameObject.SetActive(false);
            if (overlayRetry != null) overlayRetry.SetActive(false);
        }
        
        ClearItems();
    }

    void ClearItems() { foreach (Transform t in container) if (t.name.StartsWith("Item_")) Destroy(t.gameObject); }
}

// ─────────────────────────────────────────────────────────────────────────────
// BALLOON CONTROLLER (Refactored to be a child of Manager file)
// ─────────────────────────────────────────────────────────────────────────────

public class BalloonController : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    public int Numero { get; private set; }
    private ExplosionGlobosManager _mgr;
    private Image _img;
    private float _timer, _dwell;
    private bool _eye;
    private bool _isPopping;

    public void Init(int num, float size, ExplosionGlobosManager mgr, bool eye)
    {
        Numero = num; _mgr = mgr; _eye = eye;
        var rt = GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(size, size);
        _img = GetComponent<Image>();
        _img.color = Color.HSVToRGB((num * 0.15f) % 1f, 0.7f, 1f);

        var txt = GetComponentInChildren<TextMeshProUGUI>();
        if (txt == null) {
            var goTxt = new GameObject("Num");
            goTxt.transform.SetParent(transform, false);
            txt = goTxt.AddComponent<TextMeshProUGUI>();
        }
        txt.text = num.ToString();
        txt.fontSize = size * 0.4f; txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white; txt.fontStyle = FontStyles.Bold;
        
        // Aseguramos que tenga Raycast Target
        if (_img) _img.raycastTarget = true;
    }

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (_isPopping) return;
        _mgr.OnItemInteracted(this);
    }

    void Update()
    {
        if (_mgr == null || !_eye || _isPopping || TobiiGazeProvider.Instance == null) return;
        var tobii = TobiiGazeProvider.Instance;
        bool inside = RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), tobii.GazePositionScreen, null);

        if (inside && tobii.EyeDataValid)
        {
            _timer += Time.deltaTime;
            if (_timer > 0.1f)
            {
                _dwell += Time.deltaTime;
                if (_dwell >= 0.6f) 
                {
                    _mgr.OnItemInteracted(this);
                    _timer = 0; _dwell = 0; // RESET para evitar spam de errores
                }
            }
        }
        else { _timer = 0; _dwell = 0; }
    }

    public void Pop() 
    { 
        if (_isPopping) return;
        _isPopping = true;
        StartCoroutine(PopAnim()); 
    }

    IEnumerator PopAnim()
    {
        float t = 0;
        Vector3 startScale = transform.localScale;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            transform.localScale = Vector3.Lerp(startScale, Vector3.one * 1.5f, t);
            if (_img) _img.color = new Color(_img.color.r, _img.color.g, _img.color.b, 1-t);
            yield return null;
        }
        Destroy(gameObject);
    }

    public void FlashError() { StartCoroutine(Shake()); }

    IEnumerator Shake()
    {
        Vector3 pos = transform.localPosition;
        for (int i = 0; i < 5; i++)
        {
            transform.localPosition = pos + (Vector3)UnityEngine.Random.insideUnitCircle * 10f;
            _img.color = Color.red;
            yield return new WaitForSeconds(0.04f);
        }
        transform.localPosition = pos;
        _img.color = Color.HSVToRGB((Numero * 0.15f) % 1f, 0.7f, 1f);
    }
}
