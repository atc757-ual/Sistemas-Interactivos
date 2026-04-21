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
        // Reparación de entorno UI
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Canvas c = GetComponentInParent<Canvas>() ?? Object.FindFirstObjectByType<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null)
            c.gameObject.AddComponent<GraphicRaycaster>();

        // Auto-vinculación de referencias
        if (campoDNI == null) campoDNI = GameObject.Find("CampoDNI")?.GetComponent<TMP_InputField>();
        if (campoNombre == null) campoNombre = GameObject.Find("CampoNombre")?.GetComponent<TMP_InputField>();
        if (botonContinuar == null) botonContinuar = GameObject.Find("BotonContinuar")?.GetComponent<Button>();
        if (botonAyuda == null) botonAyuda = GameObject.Find("AyudaBtn")?.GetComponent<Button>() ?? GameObject.Find("BotonAyuda")?.GetComponent<Button>();
        if (panelAyuda == null) panelAyuda = GameObject.Find("AyudaPanel");
        if (IconInfo == null) IconInfo = BuscarInactivo("IconInfo") ?? BuscarInactivo("InfoIcon");
        if (IconClose == null) IconClose = BuscarInactivo("IconClose") ?? BuscarInactivo("CloseIcon");

        if (panelAyuda != null) panelAyuda.SetActive(false);
        SincronizarIconos();
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

        if (botonAyuda != null) {
            botonAyuda.onClick.RemoveAllListeners();
            botonAyuda.onClick.AddListener(AlternarAyuda);
        }
    }

    public void AlternarAyuda()
    {
        if (panelAyuda == null) return;
        panelAyuda.SetActive(!panelAyuda.activeSelf);
        SincronizarIconos();
    }

    void SincronizarIconos()
    {
        if (panelAyuda == null) return;
        bool abierto = panelAyuda.activeSelf;
        if (IconClose != null) IconClose.SetActive(abierto);
        if (IconInfo != null) IconInfo.SetActive(!abierto);
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
