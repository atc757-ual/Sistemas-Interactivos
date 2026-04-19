using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PantallaResultados : MonoBehaviour
{
    [Header("UI Actual")]
    [Header("UI Dashboard (Nuevo)")]
    public TMP_Text textoPrecisionPromedio;
    public TMP_Text textoEjerciciosCompletados;
    public TMP_Text textoTiempoTotal;
    public TMP_Text textoMensajePrincipal;
    public TMP_Text textoMensajeSecundario;
    public Image iconoMensaje; // El cohete

    [Header("Historial")]
    public GameObject elementoHistorialPrefab;
    public Transform contenedorHistorial;

    [Header("Botones")]
    public Button botonMenuPrincipal;
    public Button botonReintentar;

    void Start()
    {
        if (GestorPaciente.Instance != null && GestorPaciente.Instance.pacienteActual != null)
        {
            var paciente = GestorPaciente.Instance.pacienteActual;
            var partidas = paciente.historialPartidas;

            // --- CÁLCULOS DINÁMICOS ---
            int totalPartidas = partidas.Count;
            float sumaPrecision = 0;
            foreach (var p in partidas) sumaPrecision += p.puntuacion;
            float promedio = totalPartidas > 0 ? sumaPrecision / totalPartidas : 0;

            // --- ASIGNACIÓN UI ---
            if (textoPrecisionPromedio != null) textoPrecisionPromedio.text = $"{Mathf.RoundToInt(promedio)}%";
            if (textoEjerciciosCompletados != null) textoEjerciciosCompletados.text = totalPartidas.ToString();
            if (textoTiempoTotal != null) textoTiempoTotal.text = $"{totalPartidas * 3}min"; // Estimación: 3 min por actividad

            // Mensaje Central
            if (totalPartidas == 0)
            {
                if (textoMensajePrincipal != null) textoMensajePrincipal.text = "Aún no hay ejercicios completados";
                if (textoMensajeSecundario != null) textoMensajeSecundario.text = "¡Empieza tu aventura espacial!";
            }
            else
            {
                if (textoMensajePrincipal != null) textoMensajePrincipal.text = "¡Buen trabajo hoy!";
                if (textoMensajeSecundario != null) textoMensajeSecundario.text = "Sigue así para mejorar tu visión";
            }

            // Mostrar historial detallado en la lista (opcional/médico)
            CargarHistorial(partidas);
        }

        if (botonMenuPrincipal != null)
            botonMenuPrincipal.onClick.AddListener(() => SceneManager.LoadScene("MenuPrincipal"));

        if (botonReintentar != null)
            botonReintentar.onClick.AddListener(() => SceneManager.LoadScene("ActividadSeguimiento"));
    }

    void CargarHistorial(List<Partida> partidas)
    {
        if (contenedorHistorial == null || elementoHistorialPrefab == null) return;

        // Limpiar
        foreach (Transform child in contenedorHistorial)
            Destroy(child.gameObject);

        // Mostrar las últimas 5 partidas
        int count = 0;
        for (int i = partidas.Count - 1; i >= 0 && count < 5; i--)
        {
            GameObject item = Instantiate(elementoHistorialPrefab, contenedorHistorial);
            TMP_Text txt = item.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = $"{partidas[i].fecha}: {partidas[i].juego} - {partidas[i].puntuacion} pts";
            }
            count++;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null) return;
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("menu") || n.Contains("atras") || n.Contains("salir"))
                {
                    SceneManager.LoadScene("MenuPrincipal");
                }
                if (n.Contains("reintentar") || n.Contains("jugar"))
                {
                    SceneManager.LoadScene("SelectorActividades");
                }
            }
        }
    }
}