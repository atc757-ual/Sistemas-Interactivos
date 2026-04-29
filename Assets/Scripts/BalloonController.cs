using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BalloonController : MonoBehaviour
{
    public int Numero { get; private set; }

    // Set by BalloonSpawner.Spawn()
    [HideInInspector] public float onsetDelayMs = 175f;
    [HideInInspector] public float dwellTimeMs  = 600f;
    [HideInInspector] public bool  useEyeTracking;

    private GlobosGameManager _manager;
    private Image _bodyImg;
    private Image _dwellRing;
    private float _onsetTimer;
    private float _dwellTimer;
    private bool  _gazeInside;
    private bool  _inDwell;
    private bool  _activated;

    public void Init(int num, float size, GlobosGameManager manager, bool eyeTracking, float onsetMs, float dwellMs)
    {
        Numero         = num;
        _manager       = manager;
        useEyeTracking = eyeTracking;
        onsetDelayMs   = onsetMs;
        dwellTimeMs    = dwellMs;

        // Body
        _bodyImg       = gameObject.AddComponent<Image>();
        float h        = (num - 1) * 0.13f % 1f;
        _bodyImg.color = Color.HSVToRGB(h, 0.65f, 0.95f);
        var rt         = GetComponent<RectTransform>();
        rt.sizeDelta   = new Vector2(size, size);

        // Number label
        var txtGo = new GameObject("Num");
        txtGo.transform.SetParent(transform, false);
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text      = num.ToString();
        txt.fontSize  = size * 0.44f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = Color.white;
        txt.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);

        // Dwell progress ring
        var ringGo     = new GameObject("DwellRing");
        ringGo.transform.SetParent(transform, false);
        _dwellRing            = ringGo.AddComponent<Image>();
        _dwellRing.color      = new Color(1f, 0.95f, 0.2f, 0.85f);
        _dwellRing.type       = Image.Type.Filled;
        _dwellRing.fillMethod = Image.FillMethod.Radial360;
        _dwellRing.fillAmount = 0f;
        var ringRt            = ringGo.GetComponent<RectTransform>();
        ringRt.sizeDelta      = new Vector2(size + 14f, size + 14f);
        ringRt.anchoredPosition = Vector2.zero;
        ringGo.SetActive(false);

        // Click button (active in mouse mode; skipped in eye-tracking mode)
        var btn = gameObject.AddComponent<Button>();
        var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
        var cb  = btn.colors;
        cb.highlightedColor  = new Color(1f, 1f, 0.5f, 1f);
        cb.pressedColor      = new Color(0.8f, 1f, 0.5f, 1f);
        btn.colors           = cb;
        btn.onClick.AddListener(OnClickInteract);

        transform.localScale = Vector3.zero;
        StartCoroutine(AnimEntrada());
    }

    void OnClickInteract()
    {
        if (_activated) return;
        Activate();
    }

    void Activate()
    {
        if (_activated) return;
        _activated = true;
        _manager.OnBalloonInteracted(this);
    }

    void Update()
    {
        if (!useEyeTracking || _activated || _manager == null) return;

        Vector2 gazeScreen = TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze
            ? TobiiGazeProvider.Instance.GazePositionScreen
            : (Vector2)Input.mousePosition;

        Canvas c    = GetComponentInParent<Canvas>();
        Camera cam  = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;
        bool inside = RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), gazeScreen, cam);

        if (!_gazeInside && inside)
        {
            _gazeInside  = true;
            _onsetTimer  = 0f;
        }
        else if (_gazeInside && !inside)
        {
            ResetGaze();
            return;
        }

        if (!_gazeInside) return;

        _onsetTimer += Time.deltaTime * 1000f;
        if (_onsetTimer < onsetDelayMs) return;

        if (!_inDwell) { _inDwell = true; _dwellRing.gameObject.SetActive(true); }

        _dwellTimer += Time.deltaTime * 1000f;
        _dwellRing.fillAmount = Mathf.Clamp01(_dwellTimer / dwellTimeMs);

        if (_dwellTimer >= dwellTimeMs) Activate();
    }

    void ResetGaze()
    {
        _gazeInside  = false;
        _inDwell     = false;
        _onsetTimer  = 0f;
        _dwellTimer  = 0f;
        if (_dwellRing != null) { _dwellRing.fillAmount = 0f; _dwellRing.gameObject.SetActive(false); }
    }

    public void Pop() { StopAllCoroutines(); StartCoroutine(AnimPop()); }

    public void FlashError() { StartCoroutine(AnimFlash()); }

    public void ResetActivated() { _activated = false; ResetGaze(); }

    private IEnumerator AnimEntrada()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private IEnumerator AnimPop()
    {
        Vector3 s0 = transform.localScale;
        Color   c0 = _bodyImg.color;
        for (float t = 0f; t < 1f; t += Time.deltaTime * 5f)
        {
            transform.localScale = s0 * (1f + t * 0.6f);
            _bodyImg.color       = new Color(c0.r, c0.g, c0.b, 1f - t);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator AnimFlash()
    {
        Color c0 = _bodyImg.color;
        for (int i = 0; i < 3; i++)
        {
            _bodyImg.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            yield return new WaitForSeconds(0.07f);
            _bodyImg.color = c0;
            yield return new WaitForSeconds(0.07f);
        }
    }
}
