using UnityEngine;
using System;

public class GameTimer : MonoBehaviour
{
    public static event Action<float> OnTimerUpdated;
    public static event Action        OnTimeUp;
    public static event Action<float> OnPenaltyApplied;

    public float TimeRemaining { get; private set; }
    public float TimeLimit     { get; private set; }

    private float _penalty;
    private bool  _running;

    public void StartTimer(float limit, float penalty)
    {
        TimeLimit     = limit;
        TimeRemaining = limit;
        _penalty      = penalty;
        _running      = true;
    }

    public void StopTimer() => _running = false;

    public void ApplyPenalty()
    {
        if (!_running) return;
        TimeRemaining = Mathf.Max(0f, TimeRemaining - _penalty);
        OnPenaltyApplied?.Invoke(_penalty);
        if (TimeRemaining <= 0f) { _running = false; OnTimeUp?.Invoke(); }
    }

    void Update()
    {
        if (!_running) return;
        TimeRemaining -= Time.deltaTime;
        OnTimerUpdated?.Invoke(TimeRemaining);
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            _running      = false;
            OnTimeUp?.Invoke();
        }
    }
}
