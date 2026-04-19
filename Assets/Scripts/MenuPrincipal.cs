using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        // Auto-asignación Senior para mayor robustez
        if (botonCalibrar == null) botonCalibrar = GameObject.Find(" Calibrar")?.GetComponent<Button>();
        if (botonVerActividades == null) botonVerActividades = GameObject.Find("VerActividadesBtn")?.GetComponent<Button>();
        if (botonVerResultados == null) botonVerResultados = GameObject.Find("VerResultadosBtn")?.GetComponent<Button>();
        if (botonCerrarSesion == null) botonCerrarSesion = GameObject.Find("CerrarSesionBtn")?.GetComponent<Button>();
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
            botonVerActividades.onClick.AddListener(() => SceneManager.LoadScene("SelectorActividades"));

        if (botonVerResultados != null)
            botonVerResultados.onClick.AddListener(() => SceneManager.LoadScene("PantallaResultados"));

        if (botonCerrarSesion != null)
            botonCerrarSesion.onClick.AddListener(() => SceneManager.LoadScene("PantallaRegistro"));
    }
}