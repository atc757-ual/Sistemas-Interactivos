using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ActivitiesManager : MonoBehaviour
{
    [Header("UI DE ACTIVIDADES")]
    //public Button btnPlaneta;
    public Button btnMeteoro;
    public Button btnCometa;
    public Button btnEstrella;
    public Button btnLaberinto;
    public Button btnVolver;

    void Awake()
    {
        if (EventSystem.current == null) 
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        VincularElementosManual();
        Input.multiTouchEnabled = false;
    }

    void Start()
    {
        //ConfigurarBoton(btnPlaneta, () => SceneManager.LoadScene("SateliteOrbita"));
        ConfigurarBoton(btnMeteoro, () => SceneManager.LoadScene("MeteoroZigzag"));
        ConfigurarBoton(btnCometa, () => SceneManager.LoadScene("CometaCuadrado"));
        ConfigurarBoton(btnEstrella, () => SceneManager.LoadScene("EstrellaLineal"));
        ConfigurarBoton(btnLaberinto, () => SceneManager.LoadScene("LaberintoVisual"));
        ConfigurarBoton(btnVolver, () => SceneManager.LoadScene("Home"));

        Debug.Log("<color=cyan><b>[Activities]</b> Navegación lista.</color>");
    }

    void ConfigurarBoton(Button b, UnityEngine.Events.UnityAction accion)
    {
        if (b != null) {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(accion);
        }
    }

    void VincularElementosManual()
    {
        //if (btnPlaneta == null) btnPlaneta = BuscarBoton("BtnPlanetaCircular") ?? BuscarPorContenido("planeta");
        if (btnMeteoro == null) btnMeteoro = BuscarBoton("BtnMeteoroZigZag") ?? BuscarPorContenido("meteoro");
        if (btnCometa == null) btnCometa = BuscarBoton("BtnCometaCuadrado") ?? BuscarPorContenido("cometa");
        if (btnEstrella == null) btnEstrella = BuscarBoton("BtnEstrellaLineal") ?? BuscarPorContenido("estrella") ?? BuscarPorContenido("lineal");
        if (btnLaberinto == null) btnLaberinto = BuscarBoton("BtnLaberintoEstelar") ?? BuscarPorContenido("laberinto") ?? BuscarPorContenido("estelar");
        if (btnVolver == null) btnVolver = BuscarBoton("VolverBtn") ?? BuscarPorContenido("volver");
    }

    Button BuscarBoton(string nombre)
    {
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.name.Trim() == nombre && !string.IsNullOrEmpty(b.gameObject.scene.name)) return b;
        }
        return null;
    }

    Button BuscarPorContenido(string palabra)
    {
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.name.ToLower().Contains(palabra.ToLower()) && !string.IsNullOrEmpty(b.gameObject.scene.name)) return b;
        }
        return null;
    }
}