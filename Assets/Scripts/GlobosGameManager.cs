using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

public enum GameState { Inicio, Countdown, Playing, Results }

/// <summary>
/// Coordinador principal de ExplosionGlobos.
/// Maneja la state machine, delega spawn/timer/UI/datos a sus componentes hermanos.
/// </summary>
public class GlobosGameManager : MonoBehaviour
{
    [Header("Configuración de sesión")]
    [SerializeField] int   cantidadGlobos       = 5;   // 3–10
    [SerializeField] float tiempoLimiteSegundos = 60f;
    [SerializeField] float penalizacionSegundos = 5f;

    [Header("Tobii")]
    [SerializeField] bool  useEyeTracking = false;
    [SerializeField] float onsetDelayMs   = 175f;
    [SerializeField] float dwellTimeMs    = 600f;

    // ─── Eventos globales ─────────────────────────────────────────────────────

    public static event Action<GameState>   OnStateChanged;
    public static event Action<int>         OnBalloonPopped;    // número del globo
    public static event Action              OnWrongBalloon;
    public static event Action<SessionData> OnGameOver;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private GameState _state;
    private List<BalloonController> _balloons = new();
    private int   _balloonsPopped;
    private int   _errors;
    private float _startTime;
    private bool  _gameOverFired;

    // ─── Componentes hermanos (añadidos automáticamente si faltan) ────────────

    private GlobosUIManager     _ui;
    private BalloonSpawner      _spawner;
    private GameTimer           _timer;
    private PatientDataManager  _patientData;

    // ─────────────────────────────────────────────────────────────────────────
    // Ciclo de vida Unity
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        _ui          = GetComponent<GlobosUIManager>()    ?? gameObject.AddComponent<GlobosUIManager>();
        _spawner     = GetComponent<BalloonSpawner>()     ?? gameObject.AddComponent<BalloonSpawner>();
        _timer       = GetComponent<GameTimer>()          ?? gameObject.AddComponent<GameTimer>();
        _patientData = GetComponent<PatientDataManager>() ?? gameObject.AddComponent<PatientDataManager>();
    }

    void Start()
    {
        // Botones
        _ui.RegisterBtnIniciar(OnBtnIniciarClicked);
        _ui.RegisterBtnVolver(() => { Time.timeScale = 1f; SceneManager.LoadScene("Activities"); });
        _ui.RegisterBtnReintentar(Reiniciar);
        _ui.RegisterBtnVolverResults(() => { Time.timeScale = 1f; SceneManager.LoadScene("Activities"); });

        // Suscribir eventos del timer
        GameTimer.OnTimeUp         += OnTimerUp;
        GameTimer.OnTimerUpdated   += _ui.UpdateTimer;
        GameTimer.OnPenaltyApplied += OnPenaltyFeedback;

        SetState(GameState.Inicio);
    }

    void OnDestroy()
    {
        GameTimer.OnTimeUp         -= OnTimerUp;
        GameTimer.OnTimerUpdated   -= _ui.UpdateTimer;
        GameTimer.OnPenaltyApplied -= OnPenaltyFeedback;

        // Limpiar eventos estáticos al descargar la escena
        OnStateChanged  = null;
        OnBalloonPopped = null;
        OnWrongBalloon  = null;
        OnGameOver      = null;
    }

    void Update()
    {
        if (_state != GameState.Playing) return;
        if (TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
            _ui.UpdateGazeCursor(TobiiGazeProvider.Instance.GazePositionScreen);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State machine
    // ─────────────────────────────────────────────────────────────────────────

    void SetState(GameState next)
    {
        _state = next;
        _ui.ShowState(next);
        OnStateChanged?.Invoke(next);
    }

    void OnBtnIniciarClicked()
    {
        if (_state != GameState.Inicio) return;
        StartCoroutine(RunCountdown());
    }

    IEnumerator RunCountdown()
    {
        SetState(GameState.Countdown);
        yield return StartCoroutine(_ui.AnimateCountdown());
        StartPlaying();
    }

    void StartPlaying()
    {
        _balloonsPopped = 0;
        _errors         = 0;
        _startTime      = Time.time;
        _gameOverFired  = false;

        // Dar un frame para que el layout del Canvas calcule el rect correcto
        StartCoroutine(SpawnAfterLayout());
    }

    IEnumerator SpawnAfterLayout()
    {
        SetState(GameState.Playing);   // activar PanelGame antes de spawnar
        yield return null;             // esperar un frame para que el layout calcule rect
        _balloons = _spawner.Spawn(cantidadGlobos, this, useEyeTracking, onsetDelayMs, dwellTimeMs);
        _timer.StartTimer(tiempoLimiteSegundos, penalizacionSegundos);
        _ui.UpdateInstruccion(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Interacción con globos (llamado desde BalloonController)
    // ─────────────────────────────────────────────────────────────────────────

    public void OnBalloonInteracted(BalloonController balloon)
    {
        if (_state != GameState.Playing) return;

        int expected = _balloonsPopped + 1;

        if (balloon.Numero == expected)
        {
            _balloonsPopped++;
            _balloons.Remove(balloon);
            balloon.Pop();
            OnBalloonPopped?.Invoke(balloon.Numero);

            if (_balloonsPopped >= cantidadGlobos)
                EndGame();
            else
                _ui.UpdateInstruccion(_balloonsPopped + 1);
        }
        else
        {
            _errors++;
            balloon.FlashError();
            balloon.ResetActivated();
            _timer.ApplyPenalty();
            OnWrongBalloon?.Invoke();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Final de partida
    // ─────────────────────────────────────────────────────────────────────────

    void OnTimerUp()
    {
        if (_state == GameState.Playing) EndGame();
    }

    void OnPenaltyFeedback(float seconds)
    {
        Debug.Log($"[GlobosGameManager] Penalización -{seconds}s aplicada.");
    }

    void EndGame()
    {
        if (_gameOverFired) return;
        _gameOverFired = true;

        _timer.StopTimer();
        float timeUsed = Time.time - _startTime;
        var data = SessionScorer.Calculate(_balloonsPopped, _errors, timeUsed, tiempoLimiteSegundos, cantidadGlobos);

        _patientData.SaveSession(data);
        OnGameOver?.Invoke(data);

        SetState(GameState.Results);
        StartCoroutine(_ui.AnimateResults(data));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reiniciar (sin recargar escena: reset in-place al estado Inicio)
    // ─────────────────────────────────────────────────────────────────────────

    void Reiniciar()
    {
        _timer.StopTimer();
        _spawner.ClearContainer();
        _balloons.Clear();
        _gameOverFired = false;
        Time.timeScale = 1f;
        SetState(GameState.Inicio);
    }
}
