using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GlobosManager : BaseActividad
{
    [Header("Ajustes de Juego")]
    public int cantidadGlobos = 5;
    public float tamanoGlobo = 100f;
    public Color colorResaltado = Color.yellow;
    
    [Header("Referencias")]
    public RectTransform contenedorGlobos;
    public GameObject overlayResultado;
    public TMP_Text textoInstrucciones;

    private int _siguienteNumero = 1;
    private List<GloboComponente> _globosActivos = new List<GloboComponente>();
    private bool _juegoTerminado = false;

    protected override void Start()
    {
        usarValidacionOjos = false; // Permitir jugar con ratón
        base.Start();
        
        if (overlayResultado != null) overlayResultado.SetActive(false);
        PrepararNivel();
    }

    void PrepararNivel()
    {
        _siguienteNumero = 1;
        _juegoTerminado = false;
        LimpiarGlobos();
        
        for (int i = 1; i <= cantidadGlobos; i++)
        {
            CrearGlobo(i);
        }
        
        ActualizarInstrucciones();
    }

    void CrearGlobo(int numero)
    {
        GameObject go = new GameObject("Globo_" + numero);
        go.transform.SetParent(contenedorGlobos, false);
        
        GloboComponente globo = go.AddComponent<GloboComponente>();
        globo.Configurar(numero, tamanoGlobo, this);
        
        // Posición aleatoria en pantalla (dentro de márgenes)
        RectTransform rt = go.GetComponent<RectTransform>();
        float margin = 100f;
        float x = Random.Range(-Screen.width/2 + margin, Screen.width/2 - margin);
        float y = Random.Range(-Screen.height/2 + margin, Screen.height/2 - margin);
        rt.anchoredPosition = new Vector2(x, y);
        
        _globosActivos.Add(globo);
    }

    public void IntentarExplotar(GloboComponente globo)
    {
        if (_juegoTerminado) return;

        if (globo.Numero == _siguienteNumero)
        {
            // ¡Correcto!
            _siguienteNumero++;
            _globosActivos.Remove(globo);
            globo.Explotar();
            
            if (_globosActivos.Count == 0)
            {
                GanarJuego();
            }
            else
            {
                ActualizarInstrucciones();
            }
        }
        else
        {
            // ¡Error!
            Debug.Log("Orden incorrecto. Debes explotar el " + _siguienteNumero);
            // Podríamos restar vidas aquí si quisiéramos
        }
    }

    void ActualizarInstrucciones()
    {
        if (textoInstrucciones != null)
            textoInstrucciones.text = "Busca y explota el número: " + _siguienteNumero;
    }

    void GanarJuego()
    {
        _juegoTerminado = true;
        
        // Si no está asignado, intentamos buscarlo por nombre
        if (overlayResultado == null) overlayResultado = GameObject.Find("OverlayResultado");

        // SI SIGUE SIN EXISTIR, LO CREAMOS POR CÓDIGO
        if (overlayResultado == null) 
        {
            overlayResultado = CrearPanelVictoriaDinamico();
        }

        if (overlayResultado != null) 
        {
            overlayResultado.SetActive(true);
            TMP_Text resText = overlayResultado.GetComponentInChildren<TMP_Text>();
            if (resText != null) resText.text = "¡EXCELENTE TRABAJO!\nGlobos completados";
        }
    }

    GameObject CrearPanelVictoriaDinamico()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return null;

        GameObject panel = new GameObject("OverlayResultado");
        panel.transform.SetParent(canvas.transform, false);
        
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0.6f, 0, 0.9f);
        
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

        GameObject txtObj = new GameObject("TextWin");
        txtObj.transform.SetParent(panel.transform, false);
        var txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = "¡COMPLETADO!";
        txt.fontSize = 60;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        
        return panel;
    }

    void LimpiarGlobos()
    {
        foreach (var g in _globosActivos) if(g != null) Destroy(g.gameObject);
        _globosActivos.Clear();
    }

    public override void ReiniciarJuego()
    {
        base.ReiniciarJuego();
    }
}

// Componente auxiliar para cada globo
public class GloboComponente : MonoBehaviour
{
    public int Numero { get; private set; }
    private GlobosManager _manager;
    private Image _img;
    private Color _colorOriginal;

    public void Configurar(int num, float size, GlobosManager manager)
    {
        Numero = num;
        _manager = manager;
        
        RectTransform rt = gameObject.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        
        _img = gameObject.AddComponent<Image>();
        _colorOriginal = new Color(Random.value, Random.value, Random.value, 0.8f);
        _img.color = _colorOriginal;
        
        // Círculo (necesita un sprite circular o lo simulamos con un cuadrado redondeado)
        // Por ahora usamos el sprite por defecto de UI que es un cuadrado, pero se puede cambiar
        
        GameObject txtGo = new GameObject("Num");
        txtGo.transform.SetParent(transform, false);
        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = num.ToString();
        txt.fontSize = size * 0.5f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;

        // Botón invisible para detectar clic de ratón
        Button btn = gameObject.AddComponent<Button>();
        btn.onClick.AddListener(() => _manager.IntentarExplotar(this));
        
        // Efecto hover (resaltado)
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        
        ColorBlock cb = btn.colors;
        cb.highlightedColor = Color.yellow;
        btn.colors = cb;
    }

    public void Explotar()
    {
        // Animación simple de escala antes de desaparecer
        StartCoroutine(AnimExplotar());
    }

    private IEnumerator AnimExplotar()
    {
        Vector3 startScale = transform.localScale;
        for (float t = 0; t < 1f; t += Time.deltaTime * 5f)
        {
            transform.localScale = startScale * (1f + t);
            _img.color = new Color(_img.color.r, _img.color.g, _img.color.b, 1f - t);
            yield return null;
        }
        Destroy(gameObject);
    }
}
