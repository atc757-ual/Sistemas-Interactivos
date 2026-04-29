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
    [SerializeField] int   cantidadItems      = 20;
    [SerializeField] float tiempoSesion       = 60f;
    [SerializeField] int   maxVidas           = 3;
    [SerializeField] bool  useEyeTracking     = true;

    [Header("UI Hierarchy")]
    [SerializeField] GameObject overlayInicio;
    [SerializeField] GameObject overlayFinal;
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
    [SerializeField] Button volverBtn;
    [SerializeField] Button startBtn;
    [SerializeField] Button retryBtn; 
    [SerializeField] Image flashDano;

    private GameState _state;
    private List<BalloonController> _activeItems = new();
    private int   _itemsPopped, _vidasRestantes, _errors;
    private float _startTime, _timeRemaining;
    private bool  _timerRunning;

    void Start()
    {
        AutoBind();
        if (startBtn) startBtn.onClick.AddListener(EmpezarConteo);
        if (retryBtn) retryBtn.onClick.AddListener(ReiniciarJuego);
        if (volverBtn) volverBtn.onClick.AddListener(() => SceneManager.LoadScene("Activities"));
        
        SetState(GameState.Inicio);
    }

    void AutoBind()
    {
        var canvas = GameObject.Find("Canvasa") ?? GameObject.Find("Canvas");
        if (canvas != null)
        {
            if (overlayInicio == null) overlayInicio = canvas.transform.Find("OverlayInicio")?.gameObject;
            if (overlayFinal == null) overlayFinal = canvas.transform.Find("OverlayFinal")?.gameObject;
            if (playerCursor == null) playerCursor = canvas.transform.Find("PlayerCursor")?.gameObject;
            
            // Creamos un clon oculto para usar como plantilla y que no estorbe el original
            if (itemTemplate == null && playerCursor != null) {
                itemTemplate = Instantiate(playerCursor, container);
                itemTemplate.name = "ItemTemplate_Hidden";
                itemTemplate.SetActive(false);
            }
            if (container == null) container = canvas.GetComponent<RectTransform>();
            if (timerText == null) timerText = canvas.transform.Find("TimerText")?.GetComponent<TMP_Text>();
            if (fallosText == null) fallosText = canvas.transform.Find("fallos")?.GetComponent<TMP_Text>();
            if (flashDano == null) flashDano = canvas.transform.Find("FlashDano")?.GetComponent<Image>();
            if (volverBtn == null) volverBtn = canvas.transform.Find("VolverBtn")?.GetComponent<Button>();
            
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
            }
        }

        if (startBtn == null && overlayInicio != null) 
            startBtn = overlayInicio.GetComponentInChildren<Button>(true);

        // Forzamos 20 elementos si el usuario así lo quiere
        if (cantidadItems < 20) cantidadItems = 20;
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
        if (_state == GameState.Playing) return;
        StartCoroutine(RutinaCountdown(overlayInicio, textoContador));
    }

    public void ReiniciarJuego()
    {
        ClearItems();
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

    void Update()
    {
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

    public void IniciarJuego()
    {
        // Forzamos 20 elementos para evitar que el Inspector mande sobre el código
        cantidadItems = 20;

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
            
            var ctrl = go.GetComponent<BalloonController>() ?? go.AddComponent<BalloonController>();
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

    void EndGame(bool success)
    {
        _timerRunning = false;
        float elapsed = Time.time - _startTime;
        
        // PUNTUACIÓN PROFESIONAL:
        // Si termina en 10s o menos -> 100 puntos base.
        // Si tarda más, el puntaje base baja hacia 50 de forma lineal hasta agotar el tiempo.
        float scoreBase = 100f;
        if (elapsed > 10f) {
            scoreBase = Mathf.Lerp(100f, 50f, (elapsed - 10f) / (tiempoSesion - 10f));
        }
        
        // Penalización por errores (10 puntos cada uno)
        float scoreFinal = scoreBase - (_errors * 10f);
        if (!success) scoreFinal *= 0.5f; // Penalización por no terminar o perder vidas
        
        scoreFinal = Mathf.Clamp(scoreFinal, 0, 100);

        if (GestorPaciente.Instance != null)
            GestorPaciente.Instance.GuardarPartida("Globos", Mathf.RoundToInt(scoreFinal), 1, (_itemsPopped / (float)Mathf.Max(1, _itemsPopped + _errors)) * 100f, success, elapsed);

        if (finalScoreText) finalScoreText.text = Mathf.RoundToInt(scoreFinal).ToString();
        if (finalErrorsText) finalErrorsText.text = _errors.ToString();
        if (finalTimeText) finalTimeText.text = elapsed.ToString("F1") + "s";

        SetState(GameState.Results);
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
