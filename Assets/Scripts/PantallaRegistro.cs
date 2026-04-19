using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PantallaRegistro : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField campoDNI;
    public TMP_InputField campoNombre;
    public Button botonContinuar;
    public TMP_Text mensajeError;
    
    [Header("Ayuda Tooltip")]
    public Button botonAyuda;
    public GameObject panelAyuda;

    void Start()
    {

        // 1. Asegurar EventSystem
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem_Auto");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // 2. Rastreo de escena AGRESIVO (Senior Scan)
        Button[] todosLosBotones = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        Debug.Log($"<color=orange>Escaneando {todosLosBotones.Length} botones en total...</color>");

        foreach (var b in todosLosBotones)
        {
            if (string.IsNullOrEmpty(b.gameObject.scene.name)) continue;

            string nombreLimpio = b.name.ToLower().Trim();
            
            if (nombreLimpio.Contains("continuar") || nombreLimpio.Contains("start") || nombreLimpio.Contains("iniciar"))
                botonContinuar = b;
            
            if (nombreLimpio.Contains("ayuda") || nombreLimpio.Contains("info") || nombreLimpio.Contains("help"))
                botonAyuda = b;
        }

        // 3. Búsqueda exhaustiva del Panel (Incluso si está oculto)
        if (panelAyuda == null)
        {
            GameObject[] todosLosObjetos = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in todosLosObjetos)
            {
                if (obj.name.ToLower().Contains("ayudapanel") || obj.name.ToLower().Contains("panelayuda"))
                {
                    panelAyuda = obj;
                    break;
                }
            }
        }

        // 3. Vincular funciones
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(Registrar);
            botonContinuar.interactable = false;
        }

        if (botonAyuda != null)
        {
            botonAyuda.onClick.RemoveAllListeners();
            botonAyuda.onClick.AddListener(AlternarAyuda);
        }

        // 4. Búsqueda AGRESIVA de Inputs (DNI y Nombre)
        if (campoDNI == null || campoNombre == null)
        {
            TMP_InputField[] todosLosInputs = Resources.FindObjectsOfTypeAll<TMP_InputField>();
            foreach (var input in todosLosInputs)
            {
                if (string.IsNullOrEmpty(input.gameObject.scene.name)) continue;

                string n = input.name.ToLower();
                if (n.Contains("dni")) 
                {
                    campoDNI = input;
                    Debug.Log("<color=cyan>DNI INPUT ENCONTRADO:</color> " + input.name);
                }
                if (n.Contains("nombre") || n.Contains("name") || n.Contains("paciente")) 
                {
                    campoNombre = input;
                    Debug.Log("<color=cyan>NOMBRE INPUT ENCONTRADO:</color> " + input.name);
                }
            }
        }

        if (campoDNI != null)
        {
            campoDNI.onValueChanged.RemoveAllListeners();
            campoDNI.onValueChanged.AddListener(ValidarDNI);
            campoDNI.Select();
        }

        if (campoNombre != null)
        {
            campoNombre.onValueChanged.RemoveAllListeners();
            campoNombre.onValueChanged.AddListener(ValidarNombre);
            campoNombre.interactable = false;
        }

        if (panelAyuda != null) panelAyuda.SetActive(false);
    }

    void ValidarDNI(string dni)
    {
        Debug.Log("DNI cambiado: " + dni);
        // El DNI siempre debe ser interactuable
        if (campoDNI != null) campoDNI.interactable = true;

        if (string.IsNullOrEmpty(dni) || dni.Length < 1) 
        {
            ResetearCampos(false);
            return;
        }

        var paciente = GestorPaciente.Instance.BuscarPacientePorDNI(dni);
        if (paciente != null)
        {
            // PACIENTE EXISTE: Autocompletar
            campoNombre.text = paciente.nombre;
            campoNombre.interactable = true;
            botonContinuar.interactable = true;
        }
        else
        {
            // PACIENTE NUEVO: Permitir escribir nombre
            campoNombre.interactable = true;
            // No borramos el nombre si el usuario está a mitad de escribirlo
            ValidarNombre(campoNombre.text); 
        }
    }

    void ValidarNombre(string nombre)
    {
        // Botón habilitado solo si ambos tienen info
        if (botonContinuar != null)
            botonContinuar.interactable = !string.IsNullOrEmpty(campoDNI.text) && !string.IsNullOrEmpty(nombre);
    }

    void ResetearCampos(bool limpiarTodo)
    {
        if (limpiarTodo) campoDNI.text = "";
        campoNombre.text = "";
        campoNombre.interactable = false;
        botonContinuar.interactable = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            NavegarConTab();
        }

        // Acceso rápido: Enter para registrar
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Registrar();
        }
    }

    void NavegarConTab()
    {
        if (EventSystem.current == null) return;

        GameObject actual = EventSystem.current.currentSelectedGameObject;
        
        if (actual == campoDNI.gameObject)
            campoNombre.Select();
        else if (actual == campoNombre.gameObject)
            botonContinuar.Select();
        else
            campoDNI.Select();
    }

    public void Registrar()
    {
        Debug.Log("PantallaRegistro: Botón INICIAR clicado");
        string dni = campoDNI.text;
        string nombre = campoNombre.text;

        if (string.IsNullOrEmpty(dni) || string.IsNullOrEmpty(nombre))
        {
            MostrarError("¡Completa todos los campos!");
            return;
        }

        if (GestorPaciente.Instance != null)
        {
            // Feedback de carga
            if (botonContinuar != null)
            {
                botonContinuar.interactable = false;
                var txt = botonContinuar.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = "Iniciando...";
            }

            GestorPaciente.Instance.RegistrarPaciente(dni, nombre);
            Debug.Log($"Registrado: {nombre} (DNI: {dni})");
            
            // Un pequeño delay para que se vea el "Iniciando..." antes de cambiar de escena
            Invoke("IrAlMenu", 0.5f);
        }
        else
        {
            Debug.LogError("GestorPaciente no encontrado en la escena.");
            MostrarError("Error: Gestor de datos no disponible.");
        }
    }

    public void IrAlMenu()
    {
        SceneManager.LoadScene("MenuPrincipal");
    }

    void MostrarError(string mensaje)
    {
        if (mensajeError != null)
        {
            mensajeError.text = mensaje;
            mensajeError.gameObject.SetActive(true);
            CancelInvoke("OcultarError");
            Invoke("OcultarError", 3f);
        }
    }

    void OcultarError()
    {
        if (mensajeError != null)
            mensajeError.gameObject.SetActive(false);
    }

    public void AlternarAyuda()
    {
        Debug.Log("PantallaRegistro: Botón AYUDA clicado");
        if (panelAyuda != null)
        {
            bool estaActivo = panelAyuda.activeSelf;
            panelAyuda.SetActive(!estaActivo);
            Debug.Log($"Tooltip de Registro: {(!estaActivo ? "Abierto" : "Cerrado")}");
        }
    }
}