using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HistoryManager : MonoBehaviour
{
    [Header("Stats UI")]
    public TMP_Text valorPrecision; // Col_0 Value (Average Score)
    public TMP_Text valorExitos;    // Col_1 Value (Total Exercises)
    public TMP_Text valorTiempo;    // Col_2 Value (Total Time)

    [Header("Messages UI")]
    public TMP_Text feedbackPrincipal;
    public TMP_Text feedbackSecundario;

    [Header("Detail Overlay")]
    public GameObject overlayDetalle;
    public TMP_Text detNombreJuego; // txtTitle
    public RectTransform tableContainer; // table
    public TMP_Text txtContenido;        // Contenido (dentro de table)
    public Button detBtnClose;

    [Header("Table Settings")]
    public float rowHeight = 40f;
    public int fontSize = 24;

    [Header("Game Buttons")]
    public Button btnLaberinto;
    public Button btnLluvia;
    public Button btnLineal;
    public Button btnExplota;

    [Header("Main Buttons")]
    public Button botonVolver;

    void Start()
    {
        AutoVincular();
        
        // AUTO-LOGIN TEST: Si no hay nadie, logueamos al primero (Alex) para ver los datos
        if (GestorPaciente.Instance != null && GestorPaciente.Instance.pacienteActual == null)
        {
            if (GestorPaciente.Instance.listaPacientes.Count > 0)
            {
                GestorPaciente.Instance.IniciarSesion(GestorPaciente.Instance.listaPacientes[0]);
                Debug.Log("[History] Auto-login de prueba: " + GestorPaciente.Instance.pacienteActual.nombre);
            }
        }

        CargarEstadisticas();

        if (botonVolver != null)
            botonVolver.onClick.AddListener(() => SceneManager.LoadScene("Home"));

        ConfigurarBotonesDetalle();
        
        if (overlayDetalle != null) overlayDetalle.SetActive(false);
    }


    void AutoVincular()
    {
        Debug.Log("[History] Iniciando AutoVincular...");
        
        // Buscamos los valores por jerarquía si no están asignados
        valorPrecision = valorPrecision ?? BuscarEnHijo("Col_0", "Value");
        valorExitos = valorExitos ?? BuscarEnHijo("Col_1", "Value");
        valorTiempo = valorTiempo ?? BuscarEnHijo("Col_2", "Value");

        feedbackPrincipal = feedbackPrincipal ?? GameObject.Find("Feedback_Principal")?.GetComponent<TMP_Text>();
        feedbackSecundario = feedbackSecundario ?? GameObject.Find("Feedback_Secundario")?.GetComponent<TMP_Text>();

        botonVolver = botonVolver ?? GameObject.Find("VolverBtn")?.GetComponent<Button>();

        // Intentamos buscar el Canvas primero para búsquedas más precisas
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas != null)
        {
            Transform c = mainCanvas.transform;
            btnLaberinto = btnLaberinto ?? c.Find("btnLaberinto")?.GetComponent<Button>();
            btnLluvia = btnLluvia ?? c.Find("btnLluvia")?.GetComponent<Button>();
            btnLineal = btnLineal ?? c.Find("btnLineal")?.GetComponent<Button>();
            btnExplota = btnExplota ?? c.Find("btnExplota")?.GetComponent<Button>();
            
            if (overlayDetalle == null) 
            {
                Transform overlayT = c.Find("OverlayDetalle");
                if (overlayT != null) overlayDetalle = overlayT.gameObject;
            }
        }

        // Respaldo por nombre global si falló lo anterior
        btnLaberinto = btnLaberinto ?? GameObject.Find("btnLaberinto")?.GetComponent<Button>();
        btnLluvia = btnLluvia ?? GameObject.Find("btnLluvia")?.GetComponent<Button>();
        btnLineal = btnLineal ?? GameObject.Find("btnLineal")?.GetComponent<Button>();
        btnExplota = btnExplota ?? GameObject.Find("btnExplota")?.GetComponent<Button>();

        if (overlayDetalle == null) overlayDetalle = GameObject.Find("OverlayDetalle");
        
        if (overlayDetalle != null)
        {
            Debug.Log("[History] AutoVincular: Buscando componentes en " + overlayDetalle.name);
            
            // 1. Buscar Título
            Transform titleObj = overlayDetalle.transform.Find("txtTitle");
            if (titleObj == null) titleObj = BuscarHijoRecursivo(overlayDetalle.transform, "txtTitle");
            
            if (titleObj != null)
            {
                Transform nameObj = titleObj.Find("name");
                if (nameObj != null) detNombreJuego = nameObj.GetComponent<TMP_Text>();
                else detNombreJuego = titleObj.GetComponent<TMP_Text>();
            }

            // 2. Buscar Tabla
            if (tableContainer == null)
            {
                Transform t = overlayDetalle.transform.Find("tabla");
                if (t == null) t = BuscarHijoRecursivo(overlayDetalle.transform, "tabla");
                if (t != null) tableContainer = t.GetComponent<RectTransform>();
            }

            // 3. Buscar Botón Cerrar
            if (detBtnClose == null)
            {
                Transform b = overlayDetalle.transform.Find("btnClose");
                if (b == null) b = BuscarHijoRecursivo(overlayDetalle.transform, "btnClose");
                if (b != null) detBtnClose = b.GetComponent<Button>();
            }
            
            // 4. Buscar Contenido (txtContenido)
            if (txtContenido == null && tableContainer != null)
            {
                Transform c = tableContainer.Find("txtContenido");
                if (c == null) c = BuscarHijoRecursivo(tableContainer.transform, "txtContenido");
                if (c != null) txtContenido = c.GetComponent<TMP_Text>();
            }

            // Logs de diagnóstico final
            if (tableContainer == null) Debug.LogError("[History] ERROR: No se encontró 'tabla' en OverlayDetalle");
            if (txtContenido == null) Debug.LogError("[History] ERROR: No se encontró 'txtContenido' en tabla");
        }
    }

    void ConfigurarBotonesDetalle()
    {
        Debug.Log("[History] Configurando botones de detalle...");
        // Ajustamos los nombres de los juegos para que coincidan con la UI y el historial
        if (btnLaberinto != null) { btnLaberinto.onClick.RemoveAllListeners(); btnLaberinto.onClick.AddListener(() => AbrirDetalle("Laberinto Estelar")); }
        if (btnLluvia != null) { btnLluvia.onClick.RemoveAllListeners(); btnLluvia.onClick.AddListener(() => AbrirDetalle("Carrera Espacial")); }
        if (btnLineal != null) { btnLineal.onClick.RemoveAllListeners(); btnLineal.onClick.AddListener(() => AbrirDetalle("Estrella Lineal")); }
        if (btnExplota != null) { btnExplota.onClick.RemoveAllListeners(); btnExplota.onClick.AddListener(() => AbrirDetalle("Explosión Estelar")); }

        if (detBtnClose != null) { detBtnClose.onClick.RemoveAllListeners(); detBtnClose.onClick.AddListener(() => overlayDetalle.SetActive(false)); }
    }

    Transform BuscarHijoRecursivo(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }

    void CargarEstadisticas()
    {
        if (GestorPaciente.Instance == null || GestorPaciente.Instance.pacienteActual == null)
        {
            if (feedbackPrincipal != null) feedbackPrincipal.text = "Sin sesión activa";
            if (feedbackSecundario != null) feedbackSecundario.text = "Inicia sesión para ver tus récords";
            return;
        }

        var p = GestorPaciente.Instance.pacienteActual;
        string nombreCap = GestorPaciente.Instance.GetNombrePacienteFormateado();

        // Feedback Personalizado
        if (feedbackPrincipal != null) feedbackPrincipal.text = "¡Buen trabajo, " + nombreCap + "!";
        if (feedbackSecundario != null) feedbackSecundario.text = "DNI: " + p.dni;

        // Estadísticas Reales (Globales)
        if (valorPrecision != null) 
            valorPrecision.text = GestorPaciente.Instance.ObtenerPrecisionMedia().ToString("F0");

        if (valorExitos != null) 
            valorExitos.text = p.historialPartidas.Count.ToString(); // Total de ejercicios completados

        if (valorTiempo != null)
        {
            float totalSegundos = GestorPaciente.Instance.ObtenerTiempoTotalDeVuelo();
            int minutos = Mathf.FloorToInt(totalSegundos / 60);
            int segundos = Mathf.FloorToInt(totalSegundos % 60);
            valorTiempo.text = string.Format("{0:00}:{1:00}", minutos, segundos);
        }
    }

    void AbrirDetalle(string nombreJuego)
    {
        Debug.Log("[History] Intentando abrir detalle de: " + nombreJuego);
        
        if (overlayDetalle == null || txtContenido == null) AutoVincular();
        if (overlayDetalle == null) return;
        overlayDetalle.SetActive(true);

        if (detNombreJuego != null) detNombreJuego.text = nombreJuego.ToUpper();

        LimpiarTabla();

        int count = 0;
        if (GestorPaciente.Instance != null && GestorPaciente.Instance.pacienteActual != null)
        {
            var pActual = GestorPaciente.Instance.pacienteActual;
            
            // 1. Encabezado "Chulísimo"
            CrearFilaElegante("FECHA", "PUNTAJE", "ERRORES", "TIEMPO", true, -1);

            // 2. Datos con colores alternos
            int rowIdx = 0;
            for (int i = pActual.historialPartidas.Count - 1; i >= 0; i--)
            {
                Partida p = pActual.historialPartidas[i];
                if (p.juego == nombreJuego)
                {
                    CrearFilaElegante(
                        p.fecha, 
                        p.puntuacion.ToString(), 
                        p.errores.ToString(), 
                        p.tiempoJuego.ToString("F1") + "s",
                        false,
                        rowIdx
                    );
                    count++;
                    rowIdx++;
                }
            }
        }

        if (txtContenido != null)
        {
            txtContenido.gameObject.SetActive(count == 0);
            if (count == 0)
            {
                txtContenido.text = (GestorPaciente.Instance?.pacienteActual != null)
                    ? "Aún no has completado esta misión"
                    : "SESIÓN NO INICIADA";
            }
        }
    }

    void CrearFilaElegante(string c1, string c2, string c3, string c4, bool esEncabezado, int index)
    {
        if (tableContainer == null) return;

        // Crear contenedor de fila
        GameObject filaObj = new GameObject("Fila_" + (esEncabezado ? "Header" : index.ToString()));
        filaObj.transform.SetParent(tableContainer, false);
        
        RectTransform rtFila = filaObj.AddComponent<RectTransform>();
        rtFila.sizeDelta = new Vector2(0, 45); // Altura de fila

        // Fondo de la fila
        Image imgFila = filaObj.AddComponent<Image>();
        if (esEncabezado)
            imgFila.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        else
            imgFila.color = (index % 2 == 0) ? new Color(1, 1, 1, 0.05f) : new Color(0, 0, 0, 0.1f);

        // Layout horizontal
        HorizontalLayoutGroup hlg = filaObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.spacing = 10;

        // Crear celdas
        CrearCeldaElegante(filaObj.transform, c1, esEncabezado, TextAlignmentOptions.Left);
        CrearCeldaElegante(filaObj.transform, c2, esEncabezado, TextAlignmentOptions.Center);
        CrearCeldaElegante(filaObj.transform, c3, esEncabezado, TextAlignmentOptions.Center);
        CrearCeldaElegante(filaObj.transform, c4, esEncabezado, TextAlignmentOptions.Right);
    }

    void CrearCeldaElegante(Transform parent, string texto, bool negrita, TextAlignmentOptions alig)
    {
        GameObject celdaObj = new GameObject("Celda");
        celdaObj.transform.SetParent(parent, false);
        
        TMP_Text t = celdaObj.AddComponent<TextMeshProUGUI>();
        t.text = negrita ? $"<b>{texto}</b>" : texto;
        t.alignment = alig;
        t.fontSize = negrita ? 18 : 20; // Cambiado de 16 a 20 para el contenido
        t.color = Color.white;
        
        // Copiar fuente del original si existe
        if (txtContenido != null) {
            t.font = txtContenido.font;
            t.fontSharedMaterial = txtContenido.fontSharedMaterial;
        }
    }

    void LimpiarTabla()
    {
        if (tableContainer == null) return;
        
        // Aseguramos que tenga Layout para las filas
        VerticalLayoutGroup vlg = tableContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = tableContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        
        vlg.childControlHeight = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 5;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        foreach (Transform child in tableContainer)
        {
            if (child.name != "txtContenido")
                Destroy(child.gameObject);
        }
    }

    void DibujarEncabezado()
    {
        CrearFilaRaw("FECHA", "PUNTAJE", "ERRORES", "TIEMPO", true);
    }

    void CrearFila(Partida p)
    {
        string fechaFormateada = "";
        try {
            System.DateTime dt = System.DateTime.Parse(p.fecha);
            fechaFormateada = dt.ToString("dd/MM HH:mm");
        } catch { fechaFormateada = p.fecha; }

        int mins = Mathf.FloorToInt(p.tiempoJuego / 60);
        int segs = Mathf.FloorToInt(p.tiempoJuego % 60);
        string tiempoStr = string.Format("{0:0}:{1:00}", mins, segs);

        CrearFilaRaw(fechaFormateada, p.puntuacion.ToString(), p.errores.ToString(), tiempoStr, false);
    }

    void CrearFilaRaw(string c1, string c2, string c3, string c4, bool esHeader)
    {
        if (tableContainer == null) return;

        GameObject fila = new GameObject("Fila_" + c1);
        fila.transform.SetParent(tableContainer, false);
        
        RectTransform rt = fila.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, rowHeight);

        HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.spacing = 10;
        hlg.padding = new RectOffset(10, 10, 0, 0);

        // Añadimos fondo si es header o impar para legibilidad
        if (esHeader) {
            Image img = fila.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.1f);
        }

        Color colorTexto = esHeader ? Color.cyan : Color.white;
        FontStyles estilo = esHeader ? FontStyles.Bold : FontStyles.Normal;

        CrearCelda(fila.transform, c1, colorTexto, estilo, TextAlignmentOptions.Left);
        CrearCelda(fila.transform, c2, colorTexto, estilo, TextAlignmentOptions.Center);
        CrearCelda(fila.transform, c3, colorTexto, estilo, TextAlignmentOptions.Center);
        CrearCelda(fila.transform, c4, colorTexto, estilo, TextAlignmentOptions.Right);
    }

    void CrearFilaVacia(string msg)
    {
        if (tableContainer == null) return;
        GameObject fila = new GameObject("Fila_Empty");
        fila.transform.SetParent(tableContainer, false);
        RectTransform rt = fila.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, rowHeight);
        CrearCelda(fila.transform, msg, Color.gray, FontStyles.Italic, TextAlignmentOptions.Center);
    }

    void CrearCelda(Transform parent, string texto, Color color, FontStyles estilo, TextAlignmentOptions align)
    {
        GameObject celda = new GameObject("Celda");
        celda.transform.SetParent(parent, false);
        TMP_Text t = celda.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.color = color;
        t.fontSize = fontSize;
        t.fontStyle = estilo;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
    }

    TMP_Text BuscarEnHijo(string colName, string objName)
    {
        GameObject col = GameObject.Find(colName);
        if (col != null)
        {
            Transform t = col.transform.Find(objName);
            if (t != null) return t.GetComponent<TMP_Text>();
        }
        return null;
    }
}