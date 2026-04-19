using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuJuegos : MonoBehaviour
{
    [Header("Información del paciente")]
    public TMP_Text textoBienvenida;

    [Header("Botones")]
    public Button botonCalibrar;
    public Button botonJuegoSeguimiento;
    public Button botonJuegoBurbujas;
    public Button botonVerResultados;
    public Button botonCerrarSesion;

    void Start()
    {
        // Mostrar bienvenida
        if (GestorPaciente.Instance != null && GestorPaciente.Instance.pacienteActual != null)
        {
            textoBienvenida.text = $"¡Hola, {GestorPaciente.Instance.pacienteActual.nombre}!";
        }
        else
        {
            textoBienvenida.text = "¡Hola!";
        }

        if (botonCalibrar != null)
            botonCalibrar.onClick.AddListener(() => SceneManager.LoadScene("Calibracion"));

        if (botonJuegoSeguimiento != null)
            botonJuegoSeguimiento.onClick.AddListener(() => SceneManager.LoadScene("ActividadSeguimiento"));
        
        if (botonJuegoBurbujas != null)
            botonJuegoBurbujas.onClick.AddListener(() => SceneManager.LoadScene("ActividadBurbujas"));
        
        if (botonVerResultados != null)
            botonVerResultados.onClick.AddListener(() => SceneManager.LoadScene("PantallaResultados"));

        if (botonCerrarSesion != null)
            botonCerrarSesion.onClick.AddListener(CerrarSesion);
    }

    void IniciarJuego(string nombreEscena)
    {
        Debug.Log($"Iniciando actividad: {nombreEscena}");
        SceneManager.LoadScene(nombreEscena);
    }

    void CerrarSesion()
    {
        SceneManager.LoadScene("PantallaRegistro");
    }
}