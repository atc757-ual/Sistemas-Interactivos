using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GlobosUIManager : MonoBehaviour
{
    [Header("Paneles principales")]
    [SerializeField] GameObject panelInicio;
    [SerializeField] GameObject panelCountdown;
    [SerializeField] GameObject panelGame;
    [SerializeField] GameObject panelResults;
    [SerializeField] GameObject gazeCursor;

    [Header("PanelInicio")]
    [SerializeField] Button btnIniciar;
    [SerializeField] Button btnVolver;

    [Header("PanelCountdown")]
    [SerializeField] TMP_Text textoContador;

    [Header("PanelGame — HUD")]
    [SerializeField] TMP_Text textoTimer;
    [SerializeField] TMP_Text textoInstruccion;

    [Header("PanelResultados — asignar en el Inspector")]
    [SerializeField] TMP_Text textoTitulo;
    [SerializeField] TMP_Text textoMensaje;
    [SerializeField] TMP_Text textoPuntuacion;
    [SerializeField] Image    barPrecision;
    [SerializeField] Image    barVelocidad;
    [SerializeField] Image    barConsistencia;
    [SerializeField] TMP_Text valorPrecision;
    [SerializeField] TMP_Text valorVelocidad;
    [SerializeField] TMP_Text valorConsistencia;
    [SerializeField] Button   btnReintentar;
    [SerializeField] Button   btnVolverResults;

    [Header("PanelResultados — fila métricas (opcional)")]
    [SerializeField] TMP_Text textoFilaGlobos;
    [SerializeField] TMP_Text textoFilaTiempo;
    [SerializeField] TMP_Text textoFilaErrores;
    [SerializeField] GameObject seccionHistorico;

    private Canvas _rootCanvas;

    void Awake()
    {
        AutoBind();
        SetAllPanels(false);
    }

    // ─── Auto-binding (solo paneles y HUD; resultados se asignan en Inspector) ─

    void AutoBind()
    {
        _rootCanvas = Object.FindFirstObjectByType<Canvas>();

        if (panelInicio    == null) panelInicio    = FindByName("PanelInicio")    ?? FindByName("OverlayInicio");
        if (panelCountdown == null) panelCountdown = FindByName("PanelCountdown");
        if (panelGame      == null) panelGame      = FindByName("PanelGame");
        if (panelResults   == null) panelResults   = FindByName("PanelResultados") ?? FindByName("PanelResults");
        if (gazeCursor     == null) gazeCursor     = FindByName("GazeCursor");

        if (panelInicio != null)
        {
            if (btnIniciar == null) btnIniciar = FindChildByName<Button>(panelInicio, "BtnIniciar")
                                             ?? FindChildByName<Button>(panelInicio, "BotonInicio");
            if (btnVolver  == null) btnVolver  = FindChildByName<Button>(panelInicio, "BtnVolver")
                                             ?? FindChildByName<Button>(panelInicio, "VolverBtn");
        }

        if (panelCountdown != null && textoContador == null)
            textoContador = panelCountdown.GetComponentInChildren<TMP_Text>(true);

        if (panelGame != null)
        {
            if (textoTimer       == null) textoTimer       = FindChildByName<TMP_Text>(panelGame, "TimerText");
            if (textoInstruccion == null) textoInstruccion = FindChildByName<TMP_Text>(panelGame, "InstruccionText");
        }

        if (panelResults != null)
        {
            if (textoTitulo       == null) textoTitulo       = FindChildByName<TMP_Text>(panelResults, "TituloResultado")
                                                            ?? FindChildByName<TMP_Text>(panelResults, "TituloRango");
            if (textoMensaje      == null) textoMensaje      = FindChildByName<TMP_Text>(panelResults, "TextoMensaje");
            if (textoPuntuacion   == null) textoPuntuacion   = FindChildByName<TMP_Text>(panelResults, "TextoPuntuacion");
            if (barPrecision      == null) barPrecision      = FindChildByName<Image>(panelResults, "BarPrecision");
            if (barVelocidad      == null) barVelocidad      = FindChildByName<Image>(panelResults, "BarVelocidad");
            if (barConsistencia   == null) barConsistencia   = FindChildByName<Image>(panelResults, "BarConsistencia");
            if (valorPrecision    == null) valorPrecision    = FindChildByName<TMP_Text>(panelResults, "ValorPrecision");
            if (valorVelocidad    == null) valorVelocidad    = FindChildByName<TMP_Text>(panelResults, "ValorVelocidad");
            if (valorConsistencia == null) valorConsistencia = FindChildByName<TMP_Text>(panelResults, "ValorConsistencia");
            if (btnReintentar     == null) btnReintentar     = FindChildByName<Button>(panelResults, "BtnReintentar");
            if (btnVolverResults  == null) btnVolverResults  = FindChildByName<Button>(panelResults, "BtnVolver");
            if (textoFilaGlobos   == null) textoFilaGlobos   = FindChildByName<TMP_Text>(panelResults, "TextoGlobos")
                                                            ?? FindChildByName<TMP_Text>(panelResults, "FilaGlobos");
            if (textoFilaTiempo   == null) textoFilaTiempo   = FindChildByName<TMP_Text>(panelResults, "TextoTiempo")
                                                            ?? FindChildByName<TMP_Text>(panelResults, "FilaTiempo");
            if (textoFilaErrores  == null) textoFilaErrores  = FindChildByName<TMP_Text>(panelResults, "TextoErrores")
                                                            ?? FindChildByName<TMP_Text>(panelResults, "FilaErrores");
            if (seccionHistorico  == null)
            {
                var t = FindChildByName<RectTransform>(panelResults, "SeccionHistorico");
                if (t != null) seccionHistorico = t.gameObject;
            }
        }

        if (btnReintentar    == null) Debug.LogWarning("[GlobosUI] btnReintentar no encontrado — verificar nombre 'BtnReintentar' en PanelResultados.");
        if (btnVolverResults == null) Debug.LogWarning("[GlobosUI] btnVolverResults no encontrado — verificar nombre 'BtnVolver' en PanelResultados.");
        if (barPrecision     == null) Debug.LogWarning("[GlobosUI] barPrecision no encontrado — verificar nombre 'BarPrecision'.");
        if (barVelocidad     == null) Debug.LogWarning("[GlobosUI] barVelocidad no encontrado — verificar nombre 'BarVelocidad'.");
        if (barConsistencia  == null) Debug.LogWarning("[GlobosUI] barConsistencia no encontrado — verificar nombre 'BarConsistencia'.");
    }

    // ─── Registro de botones ──────────────────────────────────────────────────

    public void RegisterBtnIniciar(UnityEngine.Events.UnityAction a)
    {
        if (btnIniciar != null) { btnIniciar.onClick.RemoveAllListeners(); btnIniciar.onClick.AddListener(a); }
    }

    public void RegisterBtnVolver(UnityEngine.Events.UnityAction a)
    {
        if (btnVolver != null) { btnVolver.onClick.RemoveAllListeners(); btnVolver.onClick.AddListener(a); }
    }

    public void RegisterBtnReintentar(UnityEngine.Events.UnityAction a)
    {
        if (btnReintentar != null) { btnReintentar.onClick.RemoveAllListeners(); btnReintentar.onClick.AddListener(a); }
    }

    public void RegisterBtnVolverResults(UnityEngine.Events.UnityAction a)
    {
        if (btnVolverResults != null) { btnVolverResults.onClick.RemoveAllListeners(); btnVolverResults.onClick.AddListener(a); }
    }

    // ─── Control de paneles ───────────────────────────────────────────────────

    void SetAllPanels(bool active)
    {
        if (panelInicio    != null) panelInicio.SetActive(active);
        if (panelCountdown != null) panelCountdown.SetActive(active);
        if (panelGame      != null) panelGame.SetActive(active);
        if (panelResults   != null) panelResults.SetActive(active);
        if (gazeCursor     != null) gazeCursor.SetActive(active);
    }

    public void ShowState(GameState state)
    {
        SetAllPanels(false);
        switch (state)
        {
            case GameState.Inicio:
                if (panelInicio    != null) panelInicio.SetActive(true);
                break;
            case GameState.Countdown:
                if (panelCountdown != null) panelCountdown.SetActive(true);
                break;
            case GameState.Playing:
                if (panelGame  != null) panelGame.SetActive(true);
                if (gazeCursor != null && TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
                    gazeCursor.SetActive(true);
                break;
            case GameState.Results:
                if (panelResults != null) panelResults.SetActive(true);
                break;
        }
    }

    // ─── Actualizaciones HUD ─────────────────────────────────────────────────

    public void UpdateTimer(float remaining)
    {
        if (textoTimer == null) return;
        textoTimer.text  = Mathf.CeilToInt(Mathf.Max(0, remaining)) + "s";
        textoTimer.color = remaining <= 10f ? Color.red : remaining <= 25f ? new Color(1f, 0.55f, 0f) : Color.white;
    }

    public void UpdateInstruccion(int nextNumber)
    {
        if (textoInstruccion != null)
            textoInstruccion.text = "Busca el número: <b>" + nextNumber + "</b>";
    }

    public void UpdateGazeCursor(Vector2 screenPos)
    {
        if (gazeCursor == null) return;
        var rt = gazeCursor.GetComponent<RectTransform>();
        if (rt == null || _rootCanvas == null) return;
        Camera cam = (_rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _rootCanvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(), screenPos, cam, out Vector2 local);
        rt.anchoredPosition = local;
    }

    // ─── Cuenta atrás ─────────────────────────────────────────────────────────

    public IEnumerator AnimateCountdown()
    {
        for (int i = 3; i > 0; i--)
        {
            if (textoContador != null) textoContador.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }
        if (textoContador != null) textoContador.text = "¡Ya!";
        yield return new WaitForSecondsRealtime(0.7f);
    }

    // ─── Animación resultados ─────────────────────────────────────────────────

    public IEnumerator AnimateResults(SessionData data)
    {
        ShowState(GameState.Results);

        Debug.Log($"[GlobosUI] Resultados — Precisión={data.precisionScore:F1} Velocidad={data.velocidadScore:F1} Consistencia={data.consistenciaScore:F1} Final={data.finalScore:F1}");

        if (textoTitulo     != null) { textoTitulo.text = data.scoreRange; textoTitulo.color = GetRangeColor(data.finalScore); }
        if (textoMensaje    != null) textoMensaje.text    = data.scoreMessage;
        if (textoPuntuacion != null) textoPuntuacion.text = "0";

        if (textoFilaGlobos  != null) textoFilaGlobos.text  = $"{data.balloonsPopped}/{data.totalBalloons} globos";
        if (textoFilaTiempo  != null) textoFilaTiempo.text  = $"{data.timeUsed:F1}s";
        if (textoFilaErrores != null) textoFilaErrores.text = $"{data.errors} error{(data.errors != 1 ? "es" : "")}";

        if (seccionHistorico != null)
        {
            int hist = PatientDataManager.Instance != null ? PatientDataManager.Instance.GetGlobosHistory().Count : 0;
            seccionHistorico.SetActive(hist >= 2);
        }

        // Reconstruir layout ANTES del fade para que el panel tenga altura correcta
        Canvas.ForceUpdateCanvases();
        if (panelResults != null) LayoutRebuilder.ForceRebuildLayoutImmediate(panelResults.GetComponent<RectTransform>());

        if (btnReintentar    != null) btnReintentar.gameObject.SetActive(false);
        if (btnVolverResults != null) btnVolverResults.gameObject.SetActive(false);

        var cg = panelResults != null
            ? (panelResults.GetComponent<CanvasGroup>() ?? panelResults.AddComponent<CanvasGroup>())
            : null;
        if (cg != null) { cg.alpha = 0f; yield return StartCoroutine(FadeTo(cg, 1f, 0.3f)); }

        yield return new WaitForSeconds(0.3f);

        // fillAmount requiere 0–1; los scores de SessionData son 0–100
        yield return StartCoroutine(AnimFill(barPrecision,    valorPrecision,    data.precisionScore    / 100f, 0.5f));
        yield return StartCoroutine(AnimFill(barVelocidad,    valorVelocidad,    data.velocidadScore    / 100f, 0.5f));
        yield return StartCoroutine(AnimFill(barConsistencia, valorConsistencia, data.consistenciaScore / 100f, 0.5f));

        if (textoPuntuacion != null)
            yield return StartCoroutine(CountUp(textoPuntuacion, 0f, data.finalScore, 0.6f));

        if (textoPuntuacion != null)
            textoPuntuacion.color = GetRangeColor(data.finalScore);

        yield return new WaitForSeconds(0.4f);

        if (btnReintentar    != null) btnReintentar.gameObject.SetActive(true);
        if (btnVolverResults != null) btnVolverResults.gameObject.SetActive(true);

        if (panelResults != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelResults.GetComponent<RectTransform>());
        }
    }

    // ─── Helpers de animación ─────────────────────────────────────────────────

    static Color GetRangeColor(float score)
    {
        if (score >= 90f) return new Color(0.2f, 0.85f, 0.3f);
        if (score >= 70f) return new Color(0.3f, 0.6f,  1.0f);
        if (score >= 50f) return new Color(1.0f, 0.8f,  0.2f);
        return new Color(1.0f, 0.6f, 0.2f);
    }

    IEnumerator FadeTo(CanvasGroup cg, float target, float dur)
    {
        float start = cg.alpha;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            cg.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        cg.alpha = target;
    }

    IEnumerator AnimFill(Image bar, TMP_Text label, float target, float dur)
    {
        if (bar == null) yield break;
        bar.type       = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = (int)Image.OriginHorizontal.Left;
        bar.fillAmount = 0f;
        Debug.Log($"[GlobosUI] AnimFill {bar.name} → target={target:F2} ({Mathf.RoundToInt(target * 100f)})");
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float v = Mathf.Lerp(0f, target, Mathf.Clamp01(elapsed / dur));
            bar.fillAmount = v;
            if (label != null) label.text = Mathf.RoundToInt(v * 100f).ToString();
            yield return null;
        }
        bar.fillAmount = target;
        if (label != null) label.text = Mathf.RoundToInt(target * 100f).ToString();
    }

    IEnumerator CountUp(TMP_Text txt, float from, float to, float dur)
    {
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            txt.text = Mathf.Lerp(from, to, t / dur).ToString("F1");
            yield return null;
        }
        txt.text = to.ToString("F1");
    }

    // ─── Utilidades ───────────────────────────────────────────────────────────

    static GameObject FindByName(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && !string.IsNullOrEmpty(t.gameObject.scene.name)) return t.gameObject;
        return null;
    }

    static T FindChildByName<T>(GameObject parent, string name) where T : Component
    {
        if (parent == null) return null;
        foreach (var c in parent.GetComponentsInChildren<T>(true))
            if (c.gameObject.name == name) return c;
        return null;
    }
}
