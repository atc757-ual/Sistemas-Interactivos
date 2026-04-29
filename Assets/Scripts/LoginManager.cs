using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class LoginManager : MonoBehaviour
{
    [Header("Panel de Ayuda")]
    public GameObject panelAyuda;
    public GameObject IconInfo;
    public GameObject IconClose;

    [Header("Configuración UI")]
    public TMP_InputField campoDNI;
    public TMP_InputField campoNombre;
    public Button botonContinuar;
    public Button botonAyuda;

    private bool estaCargando = false;

    void Awake()
    {
        // 1. Reparación de entorno UI (Detección robusta)
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            Debug.Log("<color=yellow>LOGIN: No se detectó EventSystem, creando uno...</color>");
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        Canvas c = GetComponentInParent<Canvas>() ?? Object.FindFirstObjectByType<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null)
            c.gameObject.AddComponent<GraphicRaycaster>();

        // 2. Vinculación robusta (incluso si están inactivos)
        if (campoDNI == null) campoDNI = BuscarComponente<TMP_InputField>("CampoDNI");
        if (campoNombre == null) campoNombre = BuscarComponente<TMP_InputField>("CampoNombre");
        if (botonContinuar == null) botonContinuar = BuscarComponente<Button>("BotonContinuar");
        if (botonAyuda == null) botonAyuda = BuscarComponente<Button>("AyudaBtn") ?? BuscarComponente<Button>("BotonAyuda");
        if (panelAyuda == null) panelAyuda = BuscarInactivo("AyudaPanel");
        
        if (IconInfo == null) IconInfo = BuscarInactivo("IconInfo") ?? BuscarInactivo("InfoIcon");
        if (IconClose == null) IconClose = BuscarInactivo("IconClose") ?? BuscarInactivo("CloseIcon");

        if (panelAyuda != null) {
            CanvasGroup cg = panelAyuda.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelAyuda.AddComponent<CanvasGroup>();
            
            cg.alpha = 0;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            panelAyuda.SetActive(false);
        }
        SincronizarIconos();
    }

    T BuscarComponente<T>(string nombre) where T : Component
    {
        GameObject go = BuscarInactivo(nombre);
        return go != null ? go.GetComponent<T>() : null;
    }

    GameObject BuscarInactivo(string nombre)
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name.Trim() == nombre && !string.IsNullOrEmpty(go.gameObject.scene.name)) return go;
        }
        return null;
    }

    void Start()
    {
        if (campoDNI != null) {
            campoDNI.onValueChanged.RemoveAllListeners();
            campoDNI.text = "";
            campoDNI.onValueChanged.AddListener(ValidarDNI);
            campoDNI.ActivateInputField();
        }

        if (campoNombre != null) {
            campoNombre.onValueChanged.RemoveAllListeners();
            campoNombre.text = "";
            campoNombre.interactable = false;
            campoNombre.onValueChanged.AddListener(_ => ActualizarBotonContinuar());
        }

        if (botonContinuar != null) {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(IniciarSesion);
        }

        // Hover en lugar de clic para el panel de ayuda
        if (botonAyuda != null) {
            botonAyuda.onClick.RemoveAllListeners(); // ya no usamos clic
            var trigger = botonAyuda.gameObject.GetComponent<EventTrigger>() 
                       ?? botonAyuda.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var entrar = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entrar.callback.AddListener(_ => MostrarAyuda(true));
            trigger.triggers.Add(entrar);

            var salir = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            salir.callback.AddListener(_ => MostrarAyuda(false));
            trigger.triggers.Add(salir);
        }
    }

    // ── Fade suave del panel ───────────────────────────────────────────────────
    CanvasGroup _ayudaCG;
    Coroutine   _fadeCo;

    void MostrarAyuda(bool mostrar)
    {
        if (panelAyuda == null) return;

        if (_ayudaCG == null) {
            _ayudaCG = panelAyuda.GetComponent<CanvasGroup>();
            if (_ayudaCG == null) _ayudaCG = panelAyuda.AddComponent<CanvasGroup>();
        }

        // Bloquear raycasts solo cuando se muestra
        _ayudaCG.blocksRaycasts = mostrar;
        _ayudaCG.interactable = mostrar;

        // Icono cambia al instante
        if (IconClose != null) IconClose.SetActive(mostrar);
        if (IconInfo  != null) IconInfo.SetActive(!mostrar);

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadePanel(mostrar));
    }

    IEnumerator FadePanel(bool aparecer)
    {
        panelAyuda.SetActive(true);
        float desde = _ayudaCG.alpha;
        float hasta = aparecer ? 1f : 0f;
        float dur   = 0.2f;
        float t     = 0f;

        while (t < dur) {
            t += Time.unscaledDeltaTime;
            _ayudaCG.alpha = Mathf.Lerp(desde, hasta, t / dur);
            yield return null;
        }

        _ayudaCG.alpha = hasta;
        if (!aparecer) panelAyuda.SetActive(false);
    }

    // Mantener compatibilidad si algo llama AlternarAyuda por código antiguo
    public void AlternarAyuda() => MostrarAyuda(panelAyuda != null && !panelAyuda.activeSelf);

    void SincronizarIconos()
    {
        if (panelAyuda == null) return;
        bool abierto = panelAyuda.activeSelf;
        if (IconClose != null) IconClose.SetActive(abierto);
        if (IconInfo  != null) IconInfo.SetActive(!abierto);
    }

    void ValidarDNI(string dni)
    {
        // Limitar a 8 caracteres
        if (dni.Length > 8) {
            dni = dni.Substring(0, 8);
            campoDNI.text = dni;
        }

        bool completo = dni.Length >= 8;
        
        if (completo && GestorPaciente.Instance != null)
        {
            var p = GestorPaciente.Instance.BuscarPacientePorDNI(dni);
            if (p != null)
            {
                // PACIENTE EXISTE: Cargar y bloquear
                if (campoNombre != null) {
                    campoNombre.text = p.nombre;
                    campoNombre.interactable = false;
                }
            }
            else
            {
                // PACIENTE NUEVO: Limpiar y habilitar
                if (campoNombre != null) {
                    campoNombre.text = "";
                    campoNombre.interactable = true;
                    // Opcional: auto-enfocar el nombre si es nuevo
                    campoNombre.ActivateInputField();
                }
            }
        }
        else
        {
            // DNI INCOMPLETO: Bloquear y limpiar nombre
            if (campoNombre != null) {
                campoNombre.text = "";
                campoNombre.interactable = false;
            }
        }

        ActualizarBotonContinuar();
    }

    void ActualizarBotonContinuar()
    {
        if (botonContinuar != null)
            botonContinuar.interactable = campoDNI.text.Length >= 8 && !string.IsNullOrEmpty(campoNombre.text);
    }

    public void IniciarSesion()
    {
        if (estaCargando) return;
        StartCoroutine(CargandoStep());
    }

    IEnumerator CargandoStep()
    {
        estaCargando = true;
        if (botonContinuar != null) botonContinuar.interactable = false;
        var t = botonContinuar?.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = "INICIANDO...";
        
        yield return new WaitForSeconds(0.4f);
        GestorPaciente.Instance?.RegistrarPaciente(campoDNI.text, campoNombre.text);
        SceneManager.LoadScene("Home");
    }

    void Update()
    {
        // --- SISTEMA DE RESCATE DE CLIC ---
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null) return;
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                string n = results[0].gameObject.name.ToLower();
                if (n.Contains("ayuda") || n.Contains("info")) AlternarAyuda();
                if (n.Contains("continuar") && (botonContinuar != null && botonContinuar.interactable)) IniciarSesion();
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (campoDNI != null && campoDNI.isFocused) campoNombre?.Select();
            else if (campoNombre != null && campoNombre.isFocused) botonContinuar?.Select();
            else campoDNI?.Select();
        }

        if (Input.GetKeyDown(KeyCode.Return) && botonContinuar != null && botonContinuar.interactable) IniciarSesion();
    }
}
