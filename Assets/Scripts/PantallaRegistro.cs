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

    void Start()
    {
        if (botonContinuar != null)
            botonContinuar.onClick.AddListener(Registrar);

        if (mensajeError != null)
            mensajeError.gameObject.SetActive(false);

        // Foco inicial automático en DNI
        if (campoDNI != null)
            campoDNI.Select();
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

    void Registrar()
    {
        string dni = campoDNI.text;
        string nombre = campoNombre.text;

        if (string.IsNullOrEmpty(dni) || string.IsNullOrEmpty(nombre))
        {
            MostrarError("¡Completa todos los campos!");
            return;
        }

        if (GestorPaciente.Instance != null)
        {
            GestorPaciente.Instance.RegistrarPaciente(dni, nombre);
            Debug.Log($"Registrado: {nombre} (DNI: {dni})");
            // Change to MenuPrincipal
            SceneManager.LoadScene("MenuPrincipal");
        }
        else
        {
            Debug.LogError("GestorPaciente no encontrado en la escena.");
            MostrarError("Error: Gestor de datos no disponible.");
        }
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
}