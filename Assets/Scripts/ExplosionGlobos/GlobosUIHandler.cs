using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Gestiona la visibilidad de los paneles y la actualización de la UI básica.
/// </summary>
public class GlobosUIHandler : MonoBehaviour
{
    [Header("Paneles Principales")]
    [SerializeField] private GameObject panelInicio;
    [SerializeField] private GameObject panelCuentaRegresiva;
    [SerializeField] private GameObject panelJuego;
    [SerializeField] private GameObject panelResumen;

    [Header("Referencias HUD")]
    [SerializeField] private TMP_Text textoTimer;
    [SerializeField] private TMP_Text textoInstrucciones;
    [SerializeField] private TMP_Text textoContadorCountdown;
    [SerializeField] private Image flashRojoOverlay;

    [Header("Prefab Resumen")]
    [SerializeField] private ResumenPanelView resumenViewPrefab;
    private ResumenPanelView _instanciaResumen;

    public void MostrarEstado(GlobosGameLogic.EstadoJuego estado)
    {
        panelInicio.SetActive(estado == GlobosGameLogic.EstadoJuego.Inicio);
        panelCuentaRegresiva.SetActive(estado == GlobosGameLogic.EstadoJuego.CuentaRegresiva);
        panelJuego.SetActive(estado == GlobosGameLogic.EstadoJuego.EnCurso);
        panelResumen.SetActive(estado == GlobosGameLogic.EstadoJuego.Resumen);
        
        if (estado == GlobosGameLogic.EstadoJuego.Inicio)
        {
            if (_instanciaResumen != null) _instanciaResumen.gameObject.SetActive(false);
        }
    }

    public void ActualizarTimer(float tiempo, Color color)
    {
        int minutos = Mathf.FloorToInt(tiempo / 60);
        int segundos = Mathf.FloorToInt(tiempo % 60);
        textoTimer.text = string.Format("{0:00}:{1:00}", minutos, segundos);
        textoTimer.color = color;
    }

    public void ActualizarInstrucciones(int siguiente)
    {
        textoInstrucciones.text = "Busca y explota el número: " + siguiente;
    }

    public void ActualizarCountdown(string texto, Color color)
    {
        textoContadorCountdown.text = texto;
        textoContadorCountdown.color = color;
        // Animación simple de escala
        textoContadorCountdown.transform.localScale = Vector3.one * 1.5f;
    }

    public void TriggerFlashRojo()
    {
        if (flashRojoOverlay != null)
        {
            StopAllCoroutines();
            StartCoroutine(RutinaFlash());
        }
    }

    private System.Collections.IEnumerator RutinaFlash()
    {
        flashRojoOverlay.gameObject.SetActive(true);
        flashRojoOverlay.color = new Color(1, 0, 0, 0.4f);
        yield return new WaitForSeconds(0.2f);
        flashRojoOverlay.gameObject.SetActive(false);
    }

    public void MostrarResumen(ResumenData data)
    {
        if (_instanciaResumen == null)
        {
            _instanciaResumen = Instantiate(resumenViewPrefab, panelResumen.transform);
        }
        
        _instanciaResumen.gameObject.SetActive(true);
        _instanciaResumen.Rellenar(data);
    }
}
