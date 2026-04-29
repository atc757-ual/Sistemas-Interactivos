using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class HomeManager : MonoBehaviour
{
    [Header("UI Principal")]
    public TMP_Text textoBienvenida;
    public Button botonCalibrar;
    public Button botonVerActividades;
    public Button botonVerResultados;
    public Button botonCerrarSesion;

    [Header("UI Aviso Calibracion")]
    public GameObject panelAvisoCalibracion;
    public TMP_Text textoAviso;

    [Header("Modal Logout")]
    public GameObject panelModalLogout;
    public Button modalBtnConfirmar;
    public Button modalBtnCancelar;

    void Awake()
    {
        if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        VincularElementosAutomatico();
        if (panelModalLogout != null) panelModalLogout.SetActive(false);
        Input.multiTouchEnabled = false;
    }

    void VincularElementosAutomatico()
    {
        if (textoBienvenida == null) textoBienvenida = BuscarObj("WelcomeText")?.GetComponent<TMP_Text>();
        if (botonCalibrar == null) botonCalibrar = BuscarObj("CalibrarBtn")?.GetComponent<Button>();
        if (botonVerActividades == null) botonVerActividades = BuscarObj("VerActividadesBtn")?.GetComponent<Button>();
        if (botonVerResultados == null) botonVerResultados = BuscarObj("ResultadosBtn")?.GetComponent<Button>();
        if (botonCerrarSesion == null) botonCerrarSesion = BuscarObj("LogoutBtn")?.GetComponent<Button>();
        
        if (panelModalLogout == null) panelModalLogout = BuscarObj("ModalCerrarSesion");
        if (panelAvisoCalibracion == null) panelAvisoCalibracion = BuscarObj("AvisoCalibracion");
    }

    GameObject BuscarObj(string nombre)
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name.Trim() == nombre && !string.IsNullOrEmpty(go.scene.name)) return go;
        }
        return null;
    }

    void Start()
    {
        string nombre = GestorPaciente.Instance != null ? GestorPaciente.Instance.GetNombrePacienteFormateado() : "Astronauta";
        
        if (textoBienvenida != null) textoBienvenida.text = $"¡Hola, {nombre}!";

        bool haCalibrado = (GestorPaciente.Instance != null && GestorPaciente.Instance.haCalibradoEnEstaSesion);
        
        ConfigurarBoton(botonCalibrar, () => SceneManager.LoadScene("Calibracion"));
        
        ConfigurarBotonActividades(haCalibrado);

        ConfigurarBoton(botonVerResultados, () => SceneManager.LoadScene("History"));
        ConfigurarBoton(botonCerrarSesion, () => { if (panelModalLogout != null) panelModalLogout.SetActive(true); });
        
        if (panelModalLogout != null)
        {
            var buttons = panelModalLogout.GetComponentsInChildren<Button>(true);
            modalBtnConfirmar = System.Array.Find(buttons, b => b.name.Contains("Confirmar"));
            modalBtnCancelar = System.Array.Find(buttons, b => b.name.Contains("Cancelar"));
            
            ConfigurarBoton(modalBtnCancelar, () => panelModalLogout.SetActive(false));
            ConfigurarBoton(modalBtnConfirmar, () => {
                GestorPaciente.Instance?.CerrarSesion();
                SceneManager.LoadScene("Login");
            });
        }
    }

    void ConfigurarBotonActividades(bool haCalibrado)
    {
        if (botonVerActividades == null) return;

        CanvasGroup cg = botonVerActividades.GetComponent<CanvasGroup>();
        if (cg == null) cg = botonVerActividades.gameObject.AddComponent<CanvasGroup>();
        botonVerActividades.onClick.RemoveAllListeners();

        Debug.Log($"<color=yellow>[HomeManager]</color> Estado calibración: {haCalibrado}");

        if (haCalibrado) {
            cg.alpha = 1.0f;
            botonVerActividades.interactable = true;
            botonVerActividades.onClick.AddListener(() => SceneManager.LoadScene("Activities"));
            
            EventTrigger trigger = botonVerActividades.GetComponent<EventTrigger>();
            if (trigger != null) {
                trigger.triggers.Clear();
                Destroy(trigger);
            }
        } else {
            cg.alpha = 0.4f;
            botonVerActividades.interactable = true;
            // Añadimos Hover (disparador al entrar con el mouse)
            EventTrigger trigger = botonVerActividades.GetComponent<EventTrigger>() ?? botonVerActividades.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();
            
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { MostrarAviso(); });
            trigger.triggers.Add(entry);

            // También mantenemos el clic por si acaso
            botonVerActividades.onClick.AddListener(MostrarAviso);
        }
    }

    void MostrarAviso()
    {
        if (panelAvisoCalibracion == null) panelAvisoCalibracion = BuscarObj("AvisoCalibracion");
        
        if (panelAvisoCalibracion != null) {
            panelAvisoCalibracion.transform.SetAsLastSibling(); // Poner delante de todo
            StopAllCoroutines();
            StartCoroutine(RutinaAviso());
            Debug.Log("<color=cyan><b>[UX]</b> Aviso de calibración activado.</color>");
        } else {
            Debug.LogWarning("¡Aviso! No encuentro el panel 'AvisoCalibracion' en el Home.");
        }
    }

    IEnumerator RutinaAviso()
    {
        panelAvisoCalibracion.SetActive(true);
        if (textoAviso == null && panelAvisoCalibracion != null) textoAviso = panelAvisoCalibracion.GetComponentInChildren<TMP_Text>(true);
        if (textoAviso != null) textoAviso.text = "¡Casi listo! Debes de <b>Calibrar</b> tus ojos antes de jugar.";
        yield return new WaitForSeconds(2.0f);
        panelAvisoCalibracion.SetActive(false);
    }

    void ConfigurarBoton(Button b, UnityEngine.Events.UnityAction accion)
    {
        if (b != null) {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(accion);
        }
    }
}