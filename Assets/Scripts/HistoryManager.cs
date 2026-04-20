using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HistoryManager : MonoBehaviour
{
    [Header("Stats UI")]
    public TMP_Text valorPrecision; // Col_0 Value
    public TMP_Text valorExitos;    // Col_1 Value
    public TMP_Text valorTiempo;    // Col_2 Value

    [Header("Messages UI")]
    public TMP_Text feedbackPrincipal;
    public TMP_Text feedbackSecundario;

    [Header("Buttons")]
    public Button botonVolver;

    void Start()
    {
        AutoVincular();
        CargarEstadisticas();

        if (botonVolver != null)
            botonVolver.onClick.AddListener(() => SceneManager.LoadScene("Home"));
    }

    void AutoVincular()
    {
        // Buscamos los valores por jerarquía si no están asignados
        valorPrecision = valorPrecision ?? BuscarEnHijo("Col_0", "Value");
        valorExitos = valorExitos ?? BuscarEnHijo("Col_1", "Value");
        valorTiempo = valorTiempo ?? BuscarEnHijo("Col_2", "Value");

        feedbackPrincipal = feedbackPrincipal ?? GameObject.Find("Feedback_Principal")?.GetComponent<TMP_Text>();
        feedbackSecundario = feedbackSecundario ?? GameObject.Find("Feedback_Secundario")?.GetComponent<TMP_Text>();

        botonVolver = botonVolver ?? GameObject.Find("VolverBtn")?.GetComponent<Button>();
    }

    void CargarEstadisticas()
    {
        if (GestorPaciente.Instance == null || GestorPaciente.Instance.pacienteActual == null)
        {
            if (feedbackPrincipal != null) feedbackPrincipal.text = "Sin sesión activa";
            if (feedbackSecundario != null) feedbackSecundario.text = "Inicia sesión para ver tus récords";
            return;
        }

        var p = GestorPaciente.Instance.pacienteActual;
        string nombreCap = !string.IsNullOrEmpty(p.nombre) ? 
            char.ToUpper(p.nombre[0]) + p.nombre.Substring(1).ToLower() : "Astronauta";

        // Feedback Personalizado
        if (feedbackPrincipal != null) feedbackPrincipal.text = "¡Buen trabajo, " + nombreCap + "!";
        if (feedbackSecundario != null) feedbackSecundario.text = "DNI: " + p.dni;

        // Estadísticas Reales
        if (valorPrecision != null) 
            valorPrecision.text = GestorPaciente.Instance.ObtenerPrecisionMedia().ToString("F0") + "%";

        if (valorExitos != null) 
            valorExitos.text = GestorPaciente.Instance.ObtenerMisionesExitosas().ToString();

        if (valorTiempo != null)
        {
            float totalSegundos = GestorPaciente.Instance.ObtenerTiempoTotalDeVuelo();
            int minutos = Mathf.FloorToInt(totalSegundos / 60);
            int segundos = Mathf.FloorToInt(totalSegundos % 60);
            valorTiempo.text = string.Format("{0:00}:{1:00}", minutos, segundos);
        }
    }

    TMP_Text BuscarEnHijo(string colName, string objName)
    {
        GameObject col = GameObject.Find(colName);
        if (col != null)
        {
            Transform t = col.transform.Find(objName);
            if (t != null) return t.GetComponent<TMP_Text>();
        }
        return null;
    }
}