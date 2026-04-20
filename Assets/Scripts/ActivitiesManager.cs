using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ActivitiesManager : MonoBehaviour
{
    [Header("Botones de Actividades")]
    public Button botonPlaneta;
    public Button botonMeteoro;
    public Button botonCometa;
    public Button botonEstrella;
    public Button botonVolver;

    void Awake()
    {
        if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Canvas c = Object.FindFirstObjectByType<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null) c.gameObject.AddComponent<GraphicRaycaster>();

        VincularBotones();
    }

    void Start()
    {
        ConfigurarBoton(botonPlaneta, () => SceneManager.LoadScene("PlanetaCircular"));
        ConfigurarBoton(botonMeteoro, () => SceneManager.LoadScene("MeteoroZigzag"));
        ConfigurarBoton(botonCometa, () => SceneManager.LoadScene("CometaCuadrado"));
        ConfigurarBoton(botonEstrella, () => SceneManager.LoadScene("EstrellaLineal"));
        ConfigurarBoton(botonVolver, () => SceneManager.LoadScene("Home"));
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
        if (Input.GetMouseButtonDown(0) && EventSystem.current != null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count > 0)
            {
                GameObject obj = results[0].gameObject;
                string n = obj.name.ToLower();
                string p = obj.transform.parent != null ? obj.transform.parent.name.ToLower() : "";

                bool esEstrella = n.Contains("estrella") || p.Contains("estrella") || n.Contains("lineal") || p.Contains("lineal");

                if (n.Contains("planeta") || p.Contains("planeta")) SceneManager.LoadScene("PlanetaCircular");
                else if (n.Contains("meteoro") || p.Contains("meteoro")) SceneManager.LoadScene("MeteoroZigzag");
                else if (n.Contains("cometa") || p.Contains("cometa")) SceneManager.LoadScene("CometaCuadrado");
                else if (esEstrella) SceneManager.LoadScene("EstrellaLineal");
                else if (n.Contains("volver") || p.Contains("volver") || n.Contains("back") || p.Contains("back")) SceneManager.LoadScene("Home");
            }
        }
    }

    void VincularBotones()
    {
        // Optimizamos: Buscamos botones solo dentro del Canvas, no en toda la memoria
        Canvas c = Object.FindFirstObjectByType<Canvas>();
        if (c == null) return;

        Button[] todos = c.GetComponentsInChildren<Button>(true);
        foreach (var b in todos)
        {
            string n = b.name.ToLower();
            if (n.Contains("planeta")) botonPlaneta = b;
            else if (n.Contains("meteoro")) botonMeteoro = b;
            else if (n.Contains("cometa")) botonCometa = b;
            else if (n.Contains("estrella") || n.Contains("lineal")) botonEstrella = b;
            else if (n.Contains("volver") || n.Contains("back")) botonVolver = b;
        }
    }
}