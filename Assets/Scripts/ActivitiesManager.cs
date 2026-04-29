using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ActivitiesManager : MonoBehaviour
{
    [Header("UI DE ACTIVIDADES")]
    public Button btnLaberinto;
    public Button btnCarrera;
    public Button btnExplosion;
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
        ConfigurarBoton(btnLaberinto, () => SceneManager.LoadScene("LaberintoVisual"));
        ConfigurarBoton(btnCarrera, () => SceneManager.LoadScene("CarreraOcular"));
        ConfigurarBoton(btnExplosion, () => SceneManager.LoadScene("ExplosionGlobos"));
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
        if (btnLaberinto == null) btnLaberinto = BuscarBoton("Card_LaberintoEstelar") ?? BuscarPorContenido("planeta") ?? BuscarPorContenido("laberinto");
        if (btnCarrera == null) btnCarrera = BuscarBoton("Card_CarreraOcular") ?? BuscarPorContenido("cometa") ?? BuscarPorContenido("carrera");
        if (btnExplosion == null) btnExplosion = BuscarBoton("Card_ExplosiónEstelar") ?? BuscarBoton("Card_ExplosionEstelar") ?? BuscarPorContenido("estrella") ?? BuscarPorContenido("explos");
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