using UnityEngine;
using UnityEngine.UI;

/**
 * IMPORTANTE - NOTA TÉCNICA SOBRE DPI SCALING:
 * Si la posición del cursor de mirada aparece desplazada respecto al punto real de fijación, 
 * es probable que se deba al Escalado de Pantalla (DPI Scaling) de Windows.
 * 
 * SOLUCIONES RECOMENDADAS:
 * 1. En las propiedades del ejecutable (.exe) -> Compatibilidad -> Cambiar configuración elevada de PPP:
 *    Marcar "Invalidar el comportamiento de ajuste de PPP alto" y seleccionar "Aplicación".
 * 2. Alternativamente, forzar una resolución fija y desactivar "Fullscreen Window" en Player Settings.
 */
public class GazeCursorController : MonoBehaviour
{
    public static GazeCursorController Instance;

    [Header("Settings")]
    public float smoothing = 0.2f;
    public Color cursorColor = new Color(0.5f, 0.8f, 1f, 0.5f);

    private RectTransform _rectTransform;
    private Image _image;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        Instance = this;
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Update()
    {
        if (TobiiGazeProvider.Instance == null || !TobiiGazeProvider.Instance.HasGaze)
        {
            SetVisibility(false);
            return;
        }

        SetVisibility(true);

        Vector2 targetPos = TobiiGazeProvider.Instance.GazePositionScreen;
        
        // Convert screen point to local point in canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Vector2 localPos;
            RectTransform canvasRect = canvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetPos, canvas.worldCamera, out localPos))
            {
                _rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, localPos, smoothing);
            }
        }
    }

    private void SetVisibility(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
