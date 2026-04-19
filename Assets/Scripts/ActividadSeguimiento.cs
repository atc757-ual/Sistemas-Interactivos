using UnityEngine;
using UnityEngine.UI;
using TMPro; // Requerido para look Premium
using System.Collections;

public class ActividadSeguimiento : BaseActividad
{
    [Header("Estrella Lineal - Referencias UI")]
    public RectTransform estrella;
    public RectTransform distractor;
    public Image barraProgreso;
    public TMP_Text textoPrecision;
    public TMP_Text textoAvance;

    [Header("Configuración")]
    [Range(15f, 60f)]
    public float duracionSesion = 22f;      // la estrella cruza la pantalla en ~22s (más rápida)
    public float umbralAcierto = 180f;      // radio de tolerancia en píxeles

    // ─── estado interno ───────────────────────────────────────────────────────
    private float _t = 0f;
    private float _totalSamples = 0f;
    private float _puntosAcierto = 0f;
    private bool  _activo = false;

    // ─── cached canvas reference ──────────────────────────────────────────────
    private Canvas _canvas;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        _canvas = FindFirstObjectByType<Canvas>();
        if (estrella  != null) estrella.gameObject.SetActive(false);
        if (distractor!= null) distractor.gameObject.SetActive(false);
        if (barraProgreso != null) barraProgreso.fillAmount = 0f;
        if (textoPrecision != null) textoPrecision.text = "Precisión: --";
        if (textoAvance    != null) textoAvance.text    = "0%";
        Time.timeScale = 0f;
    }

    // ─── IniciarJuego ─────────────────────────────────────────────────────────
    public override void IniciarJuego()
    {
        base.IniciarJuego();
        _t             = 0f;
        _totalSamples  = 0f;
        _puntosAcierto = 0f;
        _activo        = true;
        _canvas        = _canvas != null ? _canvas : FindFirstObjectByType<Canvas>();

        if (estrella  != null) estrella.gameObject.SetActive(true);
        if (distractor!= null) distractor.gameObject.SetActive(true);
    }

    // ─── Update  ──────────────────────────────────────────────────────────────
    void Update()
    {
        if (!juegoIniciado || juegoPausado || !_activo) return;

        _t += Time.deltaTime / duracionSesion;
        _t  = Mathf.Clamp01(_t);

        float sw = Screen.width;
        float sh = Screen.height;

        // ── Mover estrella linealmente (de borde izq al borde der) ─────────
        float starScreenX = Mathf.Lerp(sw * 0.05f, sw * 0.95f, _t);
        float starScreenY = sh * 0.50f;

        // Convertir posición de pantalla a posición del canvas (Screen Space Overlay)
        if (estrella != null)
        {
            Vector2 canvPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                new Vector2(starScreenX, starScreenY),
                null,
                out canvPos);
            estrella.localPosition = canvPos;
        }

        // ── Distractor errático ────────────────────────────────────────────
        if (distractor != null)
        {
            float dScreenX = Mathf.Lerp(sw * 0.90f, sw * 0.10f, _t)
                             + Mathf.Sin(Time.time * 2.0f) * 70f;
            float dScreenY = sh * 0.50f + Mathf.Cos(Time.time * 2.8f) * 240f;
            dScreenY = Mathf.Clamp(dScreenY, sh * 0.1f, sh * 0.9f);

            Vector2 dCanvPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                new Vector2(dScreenX, dScreenY),
                null,
                out dCanvPos);
            distractor.localPosition = dCanvPos;
        }

        // ── HUD: barra + chips ─────────────────────────────────────────────
        if (barraProgreso != null) barraProgreso.fillAmount = _t;
        if (textoAvance   != null) textoAvance.text = Mathf.RoundToInt(_t * 100f) + "%";

        // ── Precisión: comparar gaze vs posición de pantalla de la estrella ─
        if (TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
        {
            _totalSamples++;
            Vector2 gazeScr = TobiiGazeProvider.Instance.GazePositionScreen;
            float dist = Vector2.Distance(gazeScr, new Vector2(starScreenX, starScreenY));
            if (dist < umbralAcierto) _puntosAcierto++;

            if (textoPrecision != null)
            {
                float prec = (_puntosAcierto / _totalSamples) * 100f;
                textoPrecision.text = $"Precisión: {Mathf.RoundToInt(prec)}%";
                puntuacion = Mathf.RoundToInt(prec);
                ActualizarPuntuacionUI(); // Refrescar HUD base
            }
        }

        // ── Fin de recorrido ───────────────────────────────────────────────
        if (_t >= 1f)
        {
            _activo = false;
            StartCoroutine(FinalizarConDelay());
        }
    }

    IEnumerator FinalizarConDelay()
    {
        if (estrella  != null) estrella.gameObject.SetActive(false);
        if (distractor!= null) distractor.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.2f);
        FinalizarActividad("Estrella Lineal");
    }

    protected override void MostrarInfo()
    {
        panelInfo.Mostrar("ESTRELLA LINEAL",
            "Mira fijamente la estrella azul y síguela mientras se mueve de izquierda a derecha. " +
            "¡No te dejes distraer por el círculo verde!");
    }
}