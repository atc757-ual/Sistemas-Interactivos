using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.CollectionsCollections;

/// <summary>
/// Maneja la representación visual del panel de resumen.
/// Debe estar adjunto al Prefab "ResumenPanel".
/// </summary>
public class ResumenPanelView : MonoBehaviour
{
    [Header("Textos Principales")]
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoPuntuacionFinal;
    [SerializeField] private TMP_Text textoMetricasDetalle; // Globos: X/Y • Tiempo: Zs • Errores: E

    [Header("Barras de Progreso")]
    [SerializeField] private Image fillPrecision;
    [SerializeField] private TMP_Text valPrecision;
    [SerializeField] private Image fillVelocidad;
    [SerializeField] private TMP_Text valVelocidad;
    [SerializeField] private Image fillConsistencia;
    [SerializeField] private TMP_Text valConsistencia;

    [Header("Histórico")]
    [SerializeField] private GameObject contenedorGrafica;
    [SerializeField] private TMP_Text textoTendencia;

    [Header("Botones")]
    [SerializeField] public Button botonReintentar;
    [SerializeField] public Button botonMenu;

    public void Rellenar(ResumenData data)
    {
        // Título y feedback visual según puntaje
        var (motivacion, colorRango) = GetFeedback(data.puntuacionFinal);
        textoTitulo.text = motivacion;
        textoTitulo.color = colorRango;
        textoPuntuacionFinal.color = colorRango;

        // Métricas
        textoMetricasDetalle.text = $"Globos: {data.aciertos}/{data.totales} • Tiempo: {data.tiempoUsado:F1}s ({data.zonaVelocidad}) • Errores: {data.errores}";

        // Animaciones de barras (manteniendo el efecto visual original)
        StartCoroutine(AnimarBarra(fillPrecision, valPrecision, data.precision, 0.6f));
        StartCoroutine(AnimarBarra(fillVelocidad, valVelocidad, data.velocidad, 0.8f));
        StartCoroutine(AnimarBarra(fillConsistencia, valConsistencia, data.consistencia, 1.0f));
        StartCoroutine(AnimarTextoPuntaje(data.puntuacionFinal, 1.2f));
    }

    private IEnumerator AnimarBarra(Image fill, TMP_Text text, float target, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(0, target, elapsed / duration);
            fill.fillAmount = current / 100f;
            if (text != null) text.text = Mathf.RoundToInt(current).ToString();
            yield return null;
        }
        fill.fillAmount = target / 100f;
        if (text != null) text.text = Mathf.RoundToInt(target).ToString();
    }

    private IEnumerator AnimarTextoPuntaje(float target, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(0, target, elapsed / duration);
            textoPuntuacionFinal.text = current.ToString("F1");
            yield return null;
        }
        textoPuntuacionFinal.text = target.ToString("F1");
    }

    private (string motivacion, Color color) GetFeedback(float score)
    {
        if (score >= 90) return ("¡Rendimiento excepcional!", Color.green);
        if (score >= 70) return ("¡Buen trabajo!", new Color(0.4f, 0.6f, 1f));
        if (score >= 50) return ("¡Sigue practicando!", Color.yellow);
        return ("¡Cada sesión cuenta!", new Color(1f, 0.6f, 0.2f));
    }
}
