using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PanelInfo : MonoBehaviour
{
    public GameObject panelVisual;
    public TMP_Text textoTitulo;
    public TMP_Text textoInstrucciones;
    public Button botonCerrar;

    private float _lastTimeScale = 1f;

    void Start()
    {
        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(Ocultar);
    }

    public void Mostrar(string titulo, string instrucciones)
    {
        if (textoTitulo != null) textoTitulo.text = titulo;
        if (textoInstrucciones != null) textoInstrucciones.text = instrucciones;
        
        if (panelVisual != null)
        {
            panelVisual.SetActive(true);
            panelVisual.transform.SetAsLastSibling();
        }

        _lastTimeScale = Time.timeScale;
        Time.timeScale = 0; 
    }

    public void Ocultar()
    {
        if (panelVisual != null) panelVisual.SetActive(false);
        Time.timeScale = _lastTimeScale;
    }
}