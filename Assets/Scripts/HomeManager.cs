using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HomeManager : MonoBehaviour
{
    [Header("UI Principal")]
    public TMP_Text textoBienvenida;
    public Button botonCalibrar;
    public Button botonVerActividades;
    public Button botonVerResultados;
    public Button botonCerrarSesion;

    [Header("Modal Logout")]
    public GameObject panelModalLogout;
    public Button modalBtnConfirmar;
    public Button modalBtnCancelar;

    void Awake()
    {
        // 1. Asegurar EventSystem
        if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        
        // 2. Vincular cada botón de forma manual y limpia
        VincularElementosManual();

        // 3. Estado inicial
        if (panelModalLogout != null) panelModalLogout.SetActive(false);
        Input.multiTouchEnabled = false;
    }

    void VincularElementosManual()
    {
        // Texto de bienvenida
        if (textoBienvenida == null) textoBienvenida = GameObject.Find("WelcomeText")?.GetComponent<TMP_Text>();

        // Botones principales
        if (botonCalibrar == null) botonCalibrar = GameObject.Find("CalibrarBtn")?.GetComponent<Button>();
        if (botonVerActividades == null) botonVerActividades = GameObject.Find("VerActividadesBtn")?.GetComponent<Button>();
        if (botonVerResultados == null) botonVerResultados = GameObject.Find("ResultadosBtn")?.GetComponent<Button>();
        if (botonCerrarSesion == null) botonCerrarSesion = GameObject.Find("LogoutBtn")?.GetComponent<Button>();

        // Modal y sus botones
        if (panelModalLogout == null) panelModalLogout = BuscarInactivo("ModalCerrarSesion");
        
        if (panelModalLogout != null)
        {
            // Intentamos buscar por ruta exacta si están dentro de "Ventana"
            if (modalBtnConfirmar == null) modalBtnConfirmar = panelModalLogout.GetComponentsInChildren<Button>(true).Length > 0 ? 
                System.Array.Find(panelModalLogout.GetComponentsInChildren<Button>(true), b => b.name.Contains("Confirmar")) : null;
            
            if (modalBtnCancelar == null) modalBtnCancelar = panelModalLogout.GetComponentsInChildren<Button>(true).Length > 0 ? 
                System.Array.Find(panelModalLogout.GetComponentsInChildren<Button>(true), b => b.name.Contains("Cancelar")) : null;
        }
    }

    GameObject BuscarInactivo(string nombre)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == nombre && !string.IsNullOrEmpty(go.gameObject.scene.name)) return go;
        }
        return null;
    }

    void Start()
    {
        // Saludo personalizado con fallback y capitalización
        string nombre = (GestorPaciente.Instance != null && GestorPaciente.Instance.pacienteActual != null) 
                        ? GestorPaciente.Instance.pacienteActual.nombre : "usuario";
        
        if (!string.IsNullOrEmpty(nombre))
        {
            nombre = char.ToUpper(nombre[0]) + nombre.Substring(1).ToLower();
        }

        if (textoBienvenida != null) textoBienvenida.text = $"¡Hola, {nombre}!";

        // Asignar funciones a los botones con limpieza de listeners previa
        bool haCalibrado = (GestorPaciente.Instance != null && GestorPaciente.Instance.haCalibradoEnEstaSesion);
        
        ConfigurarBoton(botonCalibrar, () => SceneManager.LoadScene("Calibracion"));
        ConfigurarBoton(botonVerActividades, () => SceneManager.LoadScene("Activities"));
        ConfigurarBoton(botonVerResultados, () => SceneManager.LoadScene("History"));
        
        if (botonVerActividades != null) botonVerActividades.interactable = haCalibrado;

        ConfigurarBoton(botonCerrarSesion, () => { if (panelModalLogout != null) panelModalLogout.SetActive(true); });
        
        ConfigurarBoton(modalBtnCancelar, () => { if (panelModalLogout != null) panelModalLogout.SetActive(false); });
        ConfigurarBoton(modalBtnConfirmar, () => {
            GestorPaciente.Instance?.CerrarSesion();
            SceneManager.LoadScene("Login");
        });
    }

    void ConfigurarBoton(Button b, UnityEngine.Events.UnityAction accion)
    {
        if (b != null) {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(accion);
        }
    }

    void Update()
    {
        // Detectar clics para debug o rescate manual si el EventSystem fallara por jerarquía
        if (Input.GetMouseButtonDown(0) && EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            if (results.Count > 0)
            {
                // Obtenemos el objeto clicado y sus padres para no fallar si se toca el icono/texto
                GameObject obj = results[0].gameObject;
                string n = obj.name.ToLower();
                string p = obj.transform.parent != null ? obj.transform.parent.name.ToLower() : "";
                string gp = (obj.transform.parent != null && obj.transform.parent.parent != null) 
                            ? obj.transform.parent.parent.name.ToLower() : "";

                bool esCalibrar = n.Contains("calibrar") || p.Contains("calibrar") || gp.Contains("calibrar");
                bool esActividades = n.Contains("actividades") || p.Contains("actividades") || gp.Contains("actividades");
                bool esResultados = n.Contains("resultados") || p.Contains("resultados") || n.Contains("history") || p.Contains("history");
                bool esLogout = n.Contains("logout") || p.Contains("logout") || n.Contains("cerrar") || p.Contains("cerrar");
                bool esConfirmar = n.Contains("confirmar") || p.Contains("confirmar") || n.Contains("btnconfirmar") || p.Contains("btnconfirmar");
                bool esCancelar = n.Contains("cancelar") || p.Contains("cancelar") || n.Contains("btncancelar") || p.Contains("btncancelar");

                if (esConfirmar) {
                    GestorPaciente.Instance?.CerrarSesion();
                    SceneManager.LoadScene("Login");
                }
                else if (esCancelar) {
                    if (panelModalLogout != null) panelModalLogout.SetActive(false);
                }
                else if (esCalibrar) SceneManager.LoadScene("Calibracion");
                else if (esActividades) {
                    if (GestorPaciente.Instance != null && GestorPaciente.Instance.haCalibradoEnEstaSesion)
                        SceneManager.LoadScene("Activities");
                    else
                        Debug.LogWarning("Acceso denegado: Se requiere calibración previa.");
                }
                else if (esResultados) SceneManager.LoadScene("History");
                else if (esLogout) { if (panelModalLogout != null) panelModalLogout.SetActive(true); }
            }
        }
    }
}