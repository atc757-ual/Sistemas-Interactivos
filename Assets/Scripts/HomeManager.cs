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

    [Header("Animación de Menú (auto-detectado)")]
    [Tooltip("Raíz que contiene las tarjetas/botones del menú. Si está vacío se buscará 'MenuLayout'.")]
    public RectTransform menuLayoutRoot;

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

        if (menuLayoutRoot == null)
        {
            var ml = BuscarObj("MenuLayout");
            if (ml != null) menuLayoutRoot = ml.GetComponent<RectTransform>();
        }
    }

    GameObject BuscarObj(string nombre)
    {
        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
            modalBtnCancelar  = System.Array.Find(buttons, b => b.name.Contains("Cancelar"));
            ConfigurarBoton(modalBtnCancelar,  () => panelModalLogout.SetActive(false));
            ConfigurarBoton(modalBtnConfirmar, () => { GestorPaciente.Instance?.CerrarSesion(); SceneManager.LoadScene("Login"); });
        }

        // Registrar animaciones de hover en los botones del MenuLayout
        RegistrarHoversDeMenu();
    }

    // ── Sistema de Hover en imágenes del menú ────────────────────────────────

    void RegistrarHoversDeMenu()
    {
        Transform raiz = menuLayoutRoot != null
            ? (Transform)menuLayoutRoot
            : (BuscarObj("MenuLayout")?.transform ?? transform.root);

        foreach (var btn in raiz.GetComponentsInChildren<Button>(true))
        {
            RectTransform imagenRT = BuscarImagenAsociada(btn);
            if (imagenRT == null) continue;

            RectTransform target = imagenRT; // captura local para la lambda

            var trigger = btn.gameObject.GetComponent<EventTrigger>()
                       ?? btn.gameObject.AddComponent<EventTrigger>();

            AgregarTrigger(trigger, EventTriggerType.PointerEnter,
                _ => StartCoroutine(EscalarImagen(target, target.localScale.x, 1.08f, 0.18f)));
            AgregarTrigger(trigger, EventTriggerType.PointerExit,
                _ => StartCoroutine(EscalarImagen(target, target.localScale.x, 1.00f, 0.15f)));
        }
    }

    /// <summary>
    /// Busca la imagen decorativa más cercana a un botón:
    /// 1) Hijo con "img/image" en el nombre
    /// 2) Hermano que comparte prefijo con el botón o tiene "img/image" en el nombre
    /// 3) El propio contenedor padre (card)
    /// </summary>
    RectTransform BuscarImagenAsociada(Button btn)
    {
        // 1. Hijo directo con nombre de imagen
        foreach (Transform child in btn.transform)
        {
            string cn = child.name.ToLower();
            if (child.GetComponent<Image>() != null && (cn.Contains("img") || cn.Contains("image")))
                return child.GetComponent<RectTransform>();
        }

        string prefijo = btn.name.Replace("Btn", "").Replace("Button", "").ToLower();

        if (btn.transform.parent != null)
        {
            // 2. Hermano con prefijo o nombre de imagen
            foreach (Transform sibling in btn.transform.parent)
            {
                if (sibling == btn.transform) continue;
                string sn = sibling.name.ToLower();
                if (sibling.GetComponent<Image>() != null &&
                    (sn.Contains(prefijo) || sn.Contains("img") || sn.Contains("image")))
                    return sibling.GetComponent<RectTransform>();
            }

            // 3. El contenedor padre (la "tarjeta" completa)
            var parentImg = btn.transform.parent.GetComponent<Image>();
            if (parentImg != null)
                return btn.transform.parent.GetComponent<RectTransform>();
        }

        return null;
    }

    void AgregarTrigger(EventTrigger trigger, EventTriggerType tipo, UnityEngine.Events.UnityAction<BaseEventData> accion)
    {
        var entry = new EventTrigger.Entry { eventID = tipo };
        entry.callback.AddListener(accion);
        trigger.triggers.Add(entry);
    }

    IEnumerator EscalarImagen(RectTransform rt, float desde, float hasta, float dur)
    {
        if (rt == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f); // ease-out cubic
            float s = Mathf.Lerp(desde, hasta, eased);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = new Vector3(hasta, hasta, 1f);
    }

    // ── Botón de actividades ──────────────────────────────────────────────────

    void ConfigurarBotonActividades(bool haCalibrado)
    {
        if (botonVerActividades == null) return;

        CanvasGroup cg = botonVerActividades.GetComponent<CanvasGroup>();
        if (cg == null) cg = botonVerActividades.gameObject.AddComponent<CanvasGroup>();
        botonVerActividades.onClick.RemoveAllListeners();

        Debug.Log($"<color=yellow>[HomeManager]</color> Estado calibración: {haCalibrado}");

        if (haCalibrado)
        {
            cg.alpha = 1.0f;
            botonVerActividades.interactable = true;
            botonVerActividades.onClick.AddListener(() => SceneManager.LoadScene("Activities"));

            EventTrigger t = botonVerActividades.GetComponent<EventTrigger>();
            if (t != null) { t.triggers.Clear(); Destroy(t); }
        }
        else
        {
            cg.alpha = 0.4f;
            botonVerActividades.interactable = true;

            EventTrigger trigger = botonVerActividades.GetComponent<EventTrigger>();
            if (trigger == null) trigger = botonVerActividades.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => MostrarAviso());
            trigger.triggers.Add(entry);

            botonVerActividades.onClick.AddListener(MostrarAviso);
        }
    }

    void MostrarAviso()
    {
        if (panelAvisoCalibracion == null) panelAvisoCalibracion = BuscarObj("AvisoCalibracion");

        if (panelAvisoCalibracion != null)
        {
            panelAvisoCalibracion.transform.SetAsLastSibling();
            StopAllCoroutines();
            StartCoroutine(RutinaAviso());
            Debug.Log("<color=cyan><b>[UX]</b> Aviso de calibración activado.</color>");
        }
        else
        {
            Debug.LogWarning("¡Aviso! No encuentro el panel 'AvisoCalibracion' en el Home.");
        }
    }

    IEnumerator RutinaAviso()
    {
        panelAvisoCalibracion.SetActive(true);
        if (textoAviso == null) textoAviso = panelAvisoCalibracion.GetComponentInChildren<TMP_Text>(true);
        if (textoAviso != null) textoAviso.text = "¡Casi listo! Debes de <b>Calibrar</b> tus ojos antes de jugar.";
        yield return new WaitForSeconds(2.0f);
        panelAvisoCalibracion.SetActive(false);
    }

    void ConfigurarBoton(Button b, UnityEngine.Events.UnityAction accion)
    {
        if (b != null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(accion); }
    }
}