using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections; // Requerido para Corrutinas (IEnumerator)
using Tobii.Research.Unity;

public abstract class BaseActividad : MonoBehaviour
{
    [Header("Common UI")]
    public TMP_Text textoPuntuacion;
    public Button botonIniciar;
    public TMP_Text textoMensajeInicio; // NEW: Para "Pestañea para empezar"
    public Button botonPausar;
    public Button botonSalir;
    public Button botonReiniciar;
    public Button botonInfo;
    public PanelInfo panelInfo;
    public GameObject overlayInicio;

    [Header("Pause Icons")]
    public Sprite iconPlay;
    public Sprite iconPause;

    protected bool usarValidacionOjos = true; // Permite desactivar el bloqueo por ojos
    protected int puntuacion = 0;
    protected bool juegoIniciado = false;
    protected bool juegoPausado = false;

    // GC-safe cache para el raycast de emergencia en Update()
    private PointerEventData _basePointerEvent;
    private System.Collections.Generic.List<RaycastResult> _baseRaycastResults = new System.Collections.Generic.List<RaycastResult>();

    protected virtual void Start()
    {
        // GUARDIA DE SESIÓN: si no hay login válido, volver al Login
        if (GestorPaciente.Instance == null || !GestorPaciente.Instance.EsSesionValida())
        {
            Debug.LogWarning("[BaseActividad] Sin sesión válida. Redirigiendo a Login.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
            return;
        }

        // Auto-link components if missing
        if (panelInfo == null) panelInfo = GetComponentInChildren<PanelInfo>(true);
        if (botonInfo == null) botonInfo = BuscarObjetoInactivo("InfoButton")?.GetComponent<Button>() ?? BuscarObjetoInactivo("BotonInfo")?.GetComponent<Button>();
        if (botonPausar == null) botonPausar = BuscarObjetoInactivo("PauseButton")?.GetComponent<Button>() ?? BuscarObjetoInactivo("BotonPausa")?.GetComponent<Button>();
        if (botonSalir == null) botonSalir = BuscarObjetoInactivo("VolverBtn")?.GetComponent<Button>() ?? BuscarObjetoInactivo("BackBtn")?.GetComponent<Button>() ?? BuscarObjetoInactivo("SalirBtn")?.GetComponent<Button>() ?? BuscarObjetoInactivo("BackButton")?.GetComponent<Button>();
        if (botonIniciar == null) botonIniciar = BuscarObjetoInactivo("StartButton")?.GetComponent<Button>() ?? BuscarObjetoInactivo("BotonInicio")?.GetComponent<Button>();
        if (botonReiniciar == null) botonReiniciar = BuscarObjetoInactivo("RetryButton")?.GetComponent<Button>() ?? BuscarObjetoInactivo("ReiniciarBtn")?.GetComponent<Button>();
        if (textoMensajeInicio == null) textoMensajeInicio = BuscarObjetoInactivo("StartMessage")?.GetComponent<TMP_Text>() ?? BuscarObjetoInactivo("MensajeInicio")?.GetComponent<TMP_Text>();
        
        if (botonIniciar != null) { botonIniciar.onClick.RemoveAllListeners(); botonIniciar.onClick.AddListener(IniciarJuego); }
        if (botonReiniciar != null) { botonReiniciar.onClick.RemoveAllListeners(); botonReiniciar.onClick.AddListener(ReiniciarJuego); }
        if (botonPausar != null) { botonPausar.onClick.RemoveAllListeners(); botonPausar.onClick.AddListener(AlternarPausa); }
        if (botonSalir != null) { botonSalir.onClick.RemoveAllListeners(); botonSalir.onClick.AddListener(SalirAlMenu); }
        if (botonInfo != null && panelInfo != null) { botonInfo.onClick.RemoveAllListeners(); botonInfo.onClick.AddListener(MostrarInfo); }

        // Estilo Senior: Botón Salir siempre vibrante y disponible
        if (botonSalir != null) {
            botonSalir.interactable = true; 
        }

        if (overlayInicio != null) overlayInicio.SetActive(true);
        
        ActualizarPuntuacionUI();
        UpdatePauseUI();
        
        // Iniciar monitoreo de ojos para el botón INICIAR
        StartCoroutine(RutinaValidacionOjosInicio());
    }

    protected GameObject BuscarObjetoInactivo(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        string busqueda = nombre.ToLower();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            if (t.name.Trim().ToLower() == busqueda) return t.gameObject;
        }
        return null;
    }

    private IEnumerator RutinaValidacionOjosInicio()
    {
        while (!juegoIniciado)
        {
            bool ojosDetectados = false;
            if (EyeTracker.Instance != null)
            {
                var gaze = EyeTracker.Instance.LatestGazeData;
                ojosDetectados = gaze != null && (gaze.Left.GazeOriginValid || gaze.Right.GazeOriginValid);
            }

            if (botonIniciar != null) 
            {
                if (usarValidacionOjos) botonIniciar.interactable = ojosDetectados;
                else botonIniciar.interactable = true;
            }

            // Dejamos que el hijo maneje el texto si quiere, 
            // solo escribimos aquí si el hijo no lo está haciendo
            if (textoMensajeInicio != null && GetType() == typeof(BaseActividad))
            {
                textoMensajeInicio.text = ojosDetectados ? "¡OJOS LISTOS!\nHaz clic para empezar" : "Buscando tus ojos...\nMira al sensor";
                textoMensajeInicio.color = ojosDetectados ? Color.cyan : Color.white;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    public virtual void IniciarJuego()
    {
        juegoIniciado = true;
        juegoPausado = false;
        if (overlayInicio != null) overlayInicio.SetActive(false);
        Time.timeScale = 1;
        UpdatePauseUI();
    }

    public virtual void AlternarPausa()
    {
        if (!juegoIniciado) return;
        juegoPausado = !juegoPausado;
        Time.timeScale = juegoPausado ? 0 : 1;
        UpdatePauseUI();
    }

    protected void UpdatePauseUI()
    {
        if (botonPausar == null) return;

        UnityEngine.UI.Image btnImg = botonPausar.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
        if (btnImg != null)
        {
            if (juegoPausado && iconPlay != null) btnImg.sprite = iconPlay;
            else if (!juegoPausado && iconPause != null) btnImg.sprite = iconPause;
        }

        TMP_Text t = botonPausar.GetComponentInChildren<TMP_Text>(true);
        if (t != null)
        {
            t.text = juegoPausado ? "CONTINUAR" : "PAUSAR";
        }
    }

    public virtual void ReiniciarJuego()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public virtual void SalirAlMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Activities");
    }

    public virtual void MostrarInfo()
    {
        if (panelInfo != null)
        {
            panelInfo.Mostrar("Información de la Actividad", "Observa los elementos que aparecen en pantalla y síguelos con la mirada.");
        }
    }

    protected virtual void Update()
    {
        // --- PUENTE DE EMERGENCIA PARA ACTIVIDADES (Migrado a New Input System) ---
        if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
        {
            if (EventSystem.current == null) return;
            if (_basePointerEvent == null) _basePointerEvent = new PointerEventData(EventSystem.current);
            _basePointerEvent.position = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            _baseRaycastResults.Clear();
            EventSystem.current.RaycastAll(_basePointerEvent, _baseRaycastResults);

            foreach (var r in _baseRaycastResults)
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("back") || n.Contains("salir") || n.Contains("menu") || n.Contains("atras") || n.Contains("volver"))
                {
                    SalirAlMenu();
                }
                if (n.Contains("pausar") || n.Contains("pause"))
                {
                    AlternarPausa();
                }
            }
        }
    }

    protected void ActualizarPuntuacionUI()
    {
        if (textoPuntuacion != null) textoPuntuacion.text = $"Puntos: {puntuacion}";
    }

    protected void FinalizarActividad(string nombreJuego, int nivel = 1, float precision = 0, bool exito = false, float tiempoTotal = 0)
    {
        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.GuardarPartida(nombreJuego, puntuacion, nivel, precision, exito, tiempoTotal, 0);
        }
        Time.timeScale = 1;
        SceneManager.LoadScene("History");
    }
}