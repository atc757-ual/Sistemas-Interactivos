using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class LaberintoManager : BaseActividad
{
    [Header("Ajustes Laberinto")]
    public float velocidadSuavizado = 15f;
    public RectTransform cursorVisual;
    public RectTransform puntoInicio; 
    public RectTransform puntoMeta;
    public Color colorRastro = Color.yellow;
    public Color colorCorrecto = Color.green;
    public Color colorError = Color.red;

    [Header("UI Laberinto")]
    public TMP_Text textoTimer; // Restaurado para el editor
    public float tiempoLimite = 60f;
    
    [Header("Feedback")]
    public GameObject overlayGanaste;
    public Image flashDano;

    private Vector2 _posicionActual;
    private List<Vector2Int> _nodosValidados = new List<Vector2Int>(); 
    private GeneradorLaberinto _generador;
    private bool _enMeta = false;
    private float _tiempoRestante;

    protected override void Start()
    {
        usarValidacionOjos = false; // Desactivamos validación de ojos para que el botón funcione siempre
        base.Start();
        _tiempoRestante = tiempoLimite;

        if (overlayGanaste != null) overlayGanaste.SetActive(false);
        if (flashDano != null) flashDano.color = new Color(1, 0, 0, 0);

        _generador = GetComponent<GeneradorLaberinto>();
        if (_generador != null) _generador.Generar();
        
        _nodosValidados.Clear();
        _nodosValidados.Add(new Vector2Int(0, 1)); 
        
        ReiniciarPosicion();
    }

    public override void ReiniciarJuego()
    {
        // En lugar de recargar escena, reseteamos variables para mantener el generador
        _enMeta = false;
        _tiempoRestante = tiempoLimite;
        juegoIniciado = true;
        juegoPausado = false;
        
        if (overlayGanaste != null) overlayGanaste.SetActive(false);
        if (_generador != null) _generador.Generar();
        
        _nodosValidados.Clear();
        _nodosValidados.Add(new Vector2Int(0, 1));
        
        ReiniciarPosicion();
    }

    protected override void Update()
    {
        base.Update(); // Importante para la clase base

        if (_enMeta || !juegoIniciado || juegoPausado) return;

        // Gestión del Tiempo
        _tiempoRestante -= Time.deltaTime;
        if (textoTimer != null) 
            textoTimer.text = Mathf.Max(0, _tiempoRestante).ToString("F0") + "s";

        if (_tiempoRestante <= 0)
        {
            FinalizarPorTiempo();
            return;
        }

        // Cálculo robusto usando el padre del cursor (que siempre es un RectTransform)
        RectTransform referenceRT = cursorVisual != null ? cursorVisual.parent as RectTransform : null;
        if (referenceRT == null) referenceRT = transform.parent as RectTransform;

        if (referenceRT == null) return; // Si no hay referencia válida, no podemos calcular

        Vector2 localPos;
        bool hit = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            referenceRT, 
            UnityEngine.Input.mousePosition, 
            null, 
            out localPos
        );

        if (!hit) return;

        _posicionActual = Vector2.Lerp(_posicionActual, localPos, Time.deltaTime * velocidadSuavizado);
        
        if (cursorVisual != null)
            cursorVisual.anchoredPosition = _posicionActual;

        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            MarcarPuntoClick();
        }

        ChequearColisiones();
    }

    void MarcarPuntoClick()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = UnityEngine.Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        if (EventSystem.current != null)
        {
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                if (r.gameObject.name.Contains("Path"))
                {
                    Image img = r.gameObject.GetComponent<Image>();
                    Vector2Int coords = EncontrarCoordenadas(r.gameObject);
                    if (coords.x != -1)
                    {
                        bool esSolucion = _generador.EsParteDeLaSolucion(coords.x, coords.y);
                        if (esSolucion) {
                            if (img.color != colorCorrecto) img.color = colorRastro; 
                        } else {
                            img.color = colorError; 
                        }
                    }
                }
            }
        }
    }

    void ChequearColisiones()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = cursorVisual.position;
        List<RaycastResult> results = new List<RaycastResult>();
        if (EventSystem.current != null)
        {
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                if (r.gameObject.name.Contains("Path"))
                {
                    Image img = r.gameObject.GetComponent<Image>();
                    Vector2Int coords = EncontrarCoordenadas(r.gameObject);
                    if (coords.x != -1)
                    {
                        if (_generador.EsParteDeLaSolucion(coords.x, coords.y))
                        {
                            Vector2Int ultimo = _nodosValidados[_nodosValidados.Count - 1];
                            if (Vector2Int.Distance(coords, ultimo) <= 1.1f)
                            {
                                if (!_nodosValidados.Contains(coords)) _nodosValidados.Add(coords);
                                img.color = colorCorrecto;
                            }
                        } else {
                            img.color = colorError;
                        }

                        // NUEVA LÓGICA: Si llegamos a la última columna de la derecha, ganamos
                        if (coords.x == _generador.columnas - 1)
                        {
                            Ganar();
                        }
                    }
                }
                if (r.gameObject.name == "GoalPoint") Ganar();
            }
        }

        // RESPALDO: Detección por distancia si el raycast falla
        if (puntoMeta != null && !_enMeta)
        {
            float dist = Vector2.Distance(cursorVisual.anchoredPosition, puntoMeta.anchoredPosition);
            if (dist < _generador.anchoCelda * 0.8f)
            {
                Ganar();
            }
        }
    }

    Vector2Int EncontrarCoordenadas(GameObject obj)
    {
        if (_generador == null) return new Vector2Int(-1,-1);
        for (int x = 0; x < _generador.columnas; x++) {
            for (int y = 0; y < _generador.filas; y++) {
                if (_generador.GetObjetoEn(x, y) == obj) return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1);
    }

    void ReiniciarPosicion()
    {
        if (puntoInicio != null)
        {
            _posicionActual = puntoInicio.anchoredPosition;
            if (cursorVisual != null) cursorVisual.anchoredPosition = _posicionActual;
        }
    }

    void FinalizarPorTiempo()
    {
        if (_enMeta) return;
        _enMeta = true;
        juegoIniciado = false;
        MostrarOverlayFinal("TIEMPO AGOTADO", Color.red);
        // FinalizarActividad("TIEMPO AGOTADO"); // Comentado para evitar redirección
    }

    void Ganar()
    {
        if (_enMeta) return;
        _enMeta = true;
        juegoIniciado = false;
        MostrarOverlayFinal("¡ÉXITO CUMPLIDO!", Color.green);
        // FinalizarActividad("¡ÉXITO!"); // Comentado para evitar redirección
    }

    void MostrarOverlayFinal(string mensaje, Color colorTexto)
    {
        if (overlayGanaste == null)
        {
            // Intentar buscar el Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject panel = new GameObject("FinalOverlay_Dynamic");
                panel.transform.SetParent(canvas.transform, false);
                RectTransform rt = panel.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                
                Image img = panel.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.85f);

                GameObject textoGO = new GameObject("FinalText");
                textoGO.transform.SetParent(panel.transform, false);
                RectTransform textRT = textoGO.AddComponent<RectTransform>();
                textRT.sizeDelta = new Vector2(1200, 300); // Ancho aumentado para que el texto no se junte
                
                var text = textoGO.AddComponent<TextMeshProUGUI>();
                text.text = mensaje;
                text.fontSize = 80;
                text.alignment = TextAlignmentOptions.Center;
                text.color = colorTexto;
                text.textWrappingMode = TextWrappingModes.NoWrap; // Nueva forma de evitar el salto de línea
                
                overlayGanaste = panel;
            }
        }

        if (overlayGanaste != null) 
        {
            overlayGanaste.SetActive(true);
            var txt = overlayGanaste.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) {
                txt.text = mensaje;
                txt.color = colorTexto;
            }
        }
    }
}
