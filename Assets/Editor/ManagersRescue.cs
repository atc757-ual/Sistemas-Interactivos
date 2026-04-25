using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Events;

public class ManagersRescue : Editor
{
    // [MenuItem("Tobii Pro/🚀 RESUCITAR MANAGERS")]
    public static void ResucitarTodo()
    {
        Debug.Log("<color=cyan>Iniciando protocolo de resurrección de Managers...</color>");

        // 0. Asegurar EventSystem
        AsegurarEventSystem();

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentScene == "Login") RepararLogin();
        else if (currentScene == "Home") RepararHome();
        else if (currentScene == "Activities") RepararActivities();
        else if (currentScene == "History") RepararHistory();
        else Debug.LogWarning("Escena no reconocida para reparación automática.");
    }

    static void AsegurarEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Crear EventSystem");
            Debug.Log("<color=yellow>EventSystem creado automáticamente.</color>");
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("<color=yellow>GraphicRaycaster añadido al Canvas principal.</color>");
        }
    }

    static void RepararLogin()
    {
        GameObject go = LimpiarYObtenerManager("Manager_Login");
        LoginManager lm = go.GetComponent<LoginManager>() ?? go.AddComponent<LoginManager>();
        
        lm.campoDNI = BuscarObjeto("CampoDNI", "DNI", "InputField")?.GetComponent<TMP_InputField>();
        lm.campoNombre = BuscarObjeto("CampoNombre", "Nombre", "NameField")?.GetComponent<TMP_InputField>();
        lm.botonContinuar = BuscarObjeto("BotonContinuar", "Continuar", "Continue")?.GetComponent<Button>();
        lm.botonAyuda = BuscarObjeto("AyudaBtn", "BotonAyuda", "InfoBtn")?.GetComponent<Button>();
        lm.panelAyuda = BuscarObjeto("AyudaPanel", "PanelAyuda", "Help");
        lm.IconInfo = BuscarObjeto("IconInfo", "InfoIcon");
        lm.IconClose = BuscarObjeto("IconClose", "CloseIcon");

        if (lm.botonContinuar != null) {
            while (lm.botonContinuar.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(lm.botonContinuar.onClick, 0);
            UnityEventTools.AddPersistentListener(lm.botonContinuar.onClick, lm.IniciarSesion);
        }
        if (lm.botonAyuda != null) {
            while (lm.botonAyuda.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(lm.botonAyuda.onClick, 0);
            UnityEventTools.AddPersistentListener(lm.botonAyuda.onClick, lm.AlternarAyuda);
        }
        
        FinalizarReparacion(go);
    }

    static void RepararHome()
    {
        GameObject go = LimpiarYObtenerManager("Manager_Home");
        HomeManager hm = go.GetComponent<HomeManager>() ?? go.AddComponent<HomeManager>();
        hm.textoBienvenida = GameObject.Find("TextoBienvenida")?.GetComponent<TMP_Text>();
        FinalizarReparacion(go);
    }

    static void RepararActivities()
    {
        GameObject go = LimpiarYObtenerManager("Manager_Activities");
        ActivitiesManager am = go.GetComponent<ActivitiesManager>() ?? go.AddComponent<ActivitiesManager>();
        FinalizarReparacion(go);
    }

    static void RepararHistory()
    {
        GameObject go = LimpiarYObtenerManager("Manager_History");
        HistoryManager hm = go.GetComponent<HistoryManager>() ?? go.AddComponent<HistoryManager>();
        FinalizarReparacion(go);
    }

    static GameObject BuscarObjeto(params string[] nombres)
    {
        foreach (string n in nombres)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null) return obj;
            
            // Intento búsqueda en inactivos
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == n && !string.IsNullOrEmpty(go.gameObject.scene.name)) return go;
            }
        }
        return null;
    }

    static GameObject LimpiarYObtenerManager(string nombre)
    {
        // Limpiar scripts rotos en toda la escena
        foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
        }
        
        GameObject go = GameObject.Find(nombre);
        if (go == null)
        {
            go = new GameObject(nombre);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        }
        return go;
    }

    static void FinalizarReparacion(GameObject go)
    {
        EditorUtility.SetDirty(go);
        Selection.activeGameObject = go;
        Debug.Log("<color=green>¡Reparación completada con éxito!</color> Revisa las asignaciones en el Inspector.");
    }
}
