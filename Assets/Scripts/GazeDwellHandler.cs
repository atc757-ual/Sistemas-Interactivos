using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GazeDwellHandler : MonoBehaviour
{
    private float _onsetDelayMs = 175f;
    private float _dwellTimeMs = 600f;
    private Image _progressImage;
    private System.Action _onDwellComplete;
    private RectTransform _rectTransform;

    private bool _isGazeOver = false;
    private Coroutine _dwellRoutineInstance;

    public void Configurar(float onsetDelay, float dwellTime, Image progressImage, System.Action onComplete)
    {
        _onsetDelayMs = onsetDelay;
        _dwellTimeMs = dwellTime;
        _progressImage = progressImage;
        _onDwellComplete = onComplete;
        _rectTransform = GetComponent<RectTransform>();

        if (_progressImage != null)
        {
            _progressImage.fillAmount = 0;
            _progressImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (TobiiGazeProvider.Instance == null) return;

        Vector2 gazePos = TobiiGazeProvider.Instance.GazePositionScreen;
        bool currentlyOver = RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, gazePos, null);

        if (currentlyOver && !_isGazeOver)
        {
            _isGazeOver = true;
            InternalStartDwell();
        }
        else if (!currentlyOver && _isGazeOver)
        {
            _isGazeOver = false;
            InternalStopDwell();
        }
    }

    private void InternalStartDwell()
    {
        if (_dwellRoutineInstance != null) StopCoroutine(_dwellRoutineInstance);
        _dwellRoutineInstance = StartCoroutine(RutinaDwell());
    }

    private void InternalStopDwell()
    {
        if (_dwellRoutineInstance != null) StopCoroutine(_dwellRoutineInstance);
        _dwellRoutineInstance = null;
        
        if (_progressImage != null)
        {
            _progressImage.fillAmount = 0;
            _progressImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator RutinaDwell()
    {
        // Fase 1: Onset Delay
        yield return new WaitForSeconds(_onsetDelayMs / 1000f);

        // Fase 2: Dwell Activo
        if (_progressImage != null) _progressImage.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = _dwellTimeMs / 1000f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (_progressImage != null)
            {
                _progressImage.fillAmount = elapsed / duration;
            }
            yield return null;
        }

        if (_progressImage != null) _progressImage.fillAmount = 1f;
        _onDwellComplete?.Invoke();
        _dwellRoutineInstance = null;
    }
}
