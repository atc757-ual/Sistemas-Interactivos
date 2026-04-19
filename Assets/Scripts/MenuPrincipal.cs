using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MenuPrincipal : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textoBienvenida;
    public Button botonCalibrar;
    public Button botonVerActividades;
    public Button botonVerResultados;
    public Button botonCerrarSesion;

    void Awake()
    {
        VincularBotonesAgresivo();
    }

    void VincularBotonesAgresivo()
    {
        Button[] todos = Resources.FindObjectsOfTypeAll<Button>();
        foreach (var b in todos)
        {
            if (string.IsNullOrEmpty(b.gameObject.scene.name)) continue;

            string n = b.name.ToLower();
            if (n.Contains("calibrar")) botonCalibrar = b;
            if (n.Contains("actividades") || n.Contains("play")) botonVerActividades = b;
            if (n.Contains("resultados") || n.Contains("dashboard")) botonVerResultados = b;
            if (n.Contains("cerrar") || n.Contains("salir") || n.Contains("logout")) botonCerrarSesion = b;
        }

        if (botonCalibrar != null) { }
        if (botonVerActividades != null) { }
    }

    void Start()
    {
        if (GestorPaciente.Instance != null && GestorPaciente.Instance.pacienteActual != null)
        {
            textoBienvenida.text = $"¡Hola, {GestorPaciente.Instance.pacienteActual.nombre}!";
        }

        if (botonCalibrar != null)
        {
            botonCalibrar.onClick.RemoveAllListeners(); // Limpiar por si acaso
            botonCalibrar.onClick.AddListener(() => SceneManager.LoadScene("Calibracion"));
        }

        if (botonVerActividades != null)
        {
            // Solo se habilita si la sesión es válida y ya calibró
            bool puedeEntrar = GestorPaciente.Instance != null && 
                               GestorPaciente.Instance.EsSesionValida() && 
                               GestorPaciente.Instance.haCalibradoEnEstaSesion;
            
            botonVerActividades.interactable = puedeEntrar;
            
            // Opcional: Feedback visual de que está bloqueado (sin cambiar el front)
            if (botonVerActividades.image != null)
                botonVerActividades.image.color = puedeEntrar ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            botonVerActividades.onClick.AddListener(() => SceneManager.LoadScene("SelectorActividades"));
        }
        
        ActualizarEstadoBotones();

        if (botonVerResultados != null)
            botonVerResultados.onClick.AddListener(() => SceneManager.LoadScene("PantallaResultados"));

        if (botonCerrarSesion != null)
        {
            botonCerrarSesion.onClick.AddListener(() => {
                if (GestorPaciente.Instance != null) GestorPaciente.Instance.CerrarSesion();
                SceneManager.LoadScene("PantallaRegistro");
            });
        }
    }

    void ActualizarEstadoBotones()
    {
        if (botonVerActividades != null)
        {
            bool yaCalibro = GestorPaciente.Instance != null && GestorPaciente.Instance.haCalibradoEnEstaSesion;
            botonVerActividades.interactable = yaCalibro;
            
            // Visual feedback
            if (botonVerActividades.image != null)
                botonVerActividades.image.color = yaCalibro ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);
        }
    }

    void Update()
    {
        // Puente de Emergencia para clics
        if (Input.GetMouseButtonDown(0))
        {
            DetectarClicManual();
        }
    }

    void DetectarClicManual()
    {
        if (EventSystem.current == null) return;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            string n = r.gameObject.name.ToLower();
            if (n.Contains("calibrar")) SceneManager.LoadScene("Calibracion");
            if (n.Contains("actividades") && (botonVerActividades != null && botonVerActividades.interactable)) 
                SceneManager.LoadScene("SelectorActividades");
            if (n.Contains("resultados")) SceneManager.LoadScene("PantallaResultados");
            if (n.Contains("cerrar") || n.Contains("salir") || n.Contains("logout")) 
            {
                if (GestorPaciente.Instance != null) GestorPaciente.Instance.CerrarSesion();
                SceneManager.LoadScene("PantallaRegistro");
            }
        }
    }
}