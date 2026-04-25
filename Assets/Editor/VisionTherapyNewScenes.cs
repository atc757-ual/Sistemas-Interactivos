using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class VisionTherapyNewScenes : EditorWindow
{
    // [MenuItem("Tools/Vision Therapy/Build Globos Scene")]
    public static void BuildGlobos()
    {
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Setup UI
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1030);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Añadir EventSystem (indispensable para que los botones funcionen)
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        GameObject uiObj = new GameObject("UI");
        uiObj.transform.SetParent(canvasObj.transform, false);

        // Contenedor de Globos
        GameObject globosCont = new GameObject("BalloonsContainer");
        globosCont.transform.SetParent(canvasObj.transform, false);
        RectTransform globosRT = globosCont.AddComponent<RectTransform>();
        globosRT.anchorMin = Vector2.zero; globosRT.anchorMax = Vector2.one; globosRT.sizeDelta = Vector2.zero;

        // 8. Crear Panel de Éxito (Persistente en Jerarquía)
        GameObject overlay = new GameObject("FinalOverlay");
        overlay.transform.SetParent(canvasObj.transform, false);
        RectTransform rtOver = overlay.AddComponent<RectTransform>();
        rtOver.anchorMin = Vector2.zero;
        rtOver.anchorMax = Vector2.one;
        rtOver.sizeDelta = Vector2.zero;
        
        Image imgOver = overlay.AddComponent<Image>();
        imgOver.color = new Color(0, 0, 0, 0.8f); // Fondo oscuro por defecto
        
        GameObject txtFinalGO = new GameObject("TextoFinal");
        txtFinalGO.transform.SetParent(overlay.transform, false);
        TextMeshProUGUI txtFinal = txtFinalGO.AddComponent<TextMeshProUGUI>();
        txtFinal.text = "¡MENSAJE FINAL!";
        txtFinal.fontSize = 80;
        txtFinal.alignment = TextAlignmentOptions.Center;
        txtFinal.color = Color.white;
        
        overlay.SetActive(false); // Oculto por defecto

        // Manager
        GameObject managerObj = new GameObject("GameManager");
        GlobosManager manager = managerObj.AddComponent<GlobosManager>();
        manager.contenedorGlobos = globosRT;
        manager.overlayGanaste = overlay;

        // Botones Comunes
        SetupCommonUI(canvasObj, manager);

        EditorSceneManager.SaveScene(newScene, "Assets/Scenes/ExplosionGlobos.unity");
    }

    // [MenuItem("Tools/Vision Therapy/Build Laberinto Scene")]
    public static void BuildLaberinto()
    {
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scalerL = canvasObj.AddComponent<CanvasScaler>();
        scalerL.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scalerL.referenceResolution = new Vector2(1920, 1030);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Añadir EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Crear el laberinto (Un camino simple en "S")
        GameObject mazeObj = new GameObject("MazeContainer");
        mazeObj.transform.SetParent(canvasObj.transform, false);
        RectTransform mazeRT = mazeObj.AddComponent<RectTransform>();
        mazeRT.anchorMin = Vector2.zero; mazeRT.anchorMax = Vector2.one; mazeRT.sizeDelta = Vector2.zero;

        // Paredes (Fondo negro, el camino será blanco)
        GameObject bg = new GameObject("BackgroundWalls");
        bg.transform.SetParent(mazeObj.transform, false);
        bg.AddComponent<Image>().color = Color.black;
        bg.name = "Wall_Background";
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;

        // El Camino (Path)
        GameObject path = new GameObject("Path");
        path.transform.SetParent(mazeObj.transform, false);
        path.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        RectTransform pathRT = path.GetComponent<RectTransform>();
        pathRT.sizeDelta = new Vector2(1200, 200); // Pasillo más ancho para 1920x1030
        
        // Inicio y Meta
        GameObject start = new GameObject("StartPoint");
        start.transform.SetParent(mazeObj.transform, false);
        start.AddComponent<Image>().color = Color.blue;
        RectTransform startRT = start.GetComponent<RectTransform>();
        startRT.sizeDelta = new Vector2(100, 100);
        startRT.anchoredPosition = new Vector2(-350, 0);

        GameObject goal = new GameObject("Goal_Point");
        goal.transform.SetParent(mazeObj.transform, false);
        goal.AddComponent<Image>().color = Color.green;
        RectTransform goalRT = goal.GetComponent<RectTransform>();
        goalRT.sizeDelta = new Vector2(100, 100);
        goalRT.anchoredPosition = new Vector2(350, 0);

        // Cursor
        GameObject cursor = new GameObject("PlayerCursor");
        cursor.transform.SetParent(canvasObj.transform, false);
        cursor.AddComponent<Image>().color = Color.cyan;
        RectTransform cursorRT = cursor.GetComponent<RectTransform>();
        cursorRT.sizeDelta = new Vector2(60, 60);

        // Flash de daño
        GameObject flashObj = new GameObject("FlashDano");
        flashObj.transform.SetParent(canvasObj.transform, false);
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(1, 0, 0, 0);
        flashImg.raycastTarget = false;
        RectTransform flashRT = flashObj.GetComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero; flashRT.anchorMax = Vector2.one; flashRT.sizeDelta = Vector2.zero;

        // Manager y Generador
        GameObject managerObj = new GameObject("GameManager");
        LaberintoManager manager = managerObj.AddComponent<LaberintoManager>();
        GeneradorLaberinto generator = managerObj.AddComponent<GeneradorLaberinto>();
        
        manager.playerCursor = cursorRT;
        manager.puntoInicio = startRT;
        manager.puntoMeta = goalRT;
        manager.imageFlashDano = flashImg;

        // Configurar Generador
        generator.contenedor = mazeObj.GetComponent<RectTransform>();
        generator.puntoInicio = startRT;
        generator.puntoMeta = goalRT;
        generator.columnas = 19; // Para que quepa bien en 1920
        generator.filas = 11;    // Para que quepa bien en 1030
        generator.anchoCelda = 90f;
        generator.colorPared = Color.black;
        generator.colorCamino = new Color(0, 0, 0, 0); // Transparente por defecto

        // Texto del Cronómetro
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(canvasObj.transform, false);
        var timerTxt = timerObj.AddComponent<TextMeshProUGUI>();
        timerTxt.text = "TIEMPO: 60s";
        timerTxt.fontSize = 40;
        timerTxt.alignment = TextAlignmentOptions.Center;
        RectTransform timerRT = timerObj.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0.5f, 1); timerRT.anchorMax = new Vector2(0.5f, 1);
        timerRT.anchoredPosition = new Vector2(0, -50);
        manager.timerText = timerTxt;

        SetupCommonUI(canvasObj, manager);

        // 8. Crear Panel de Éxito (Persistente en Jerarquía)
        GameObject overlay = new GameObject("OverlayFinal");
        overlay.transform.SetParent(canvasObj.transform, false);
        RectTransform rtOver = overlay.AddComponent<RectTransform>();
        rtOver.anchorMin = Vector2.zero;
        rtOver.anchorMax = Vector2.one;
        rtOver.sizeDelta = Vector2.zero;
        
        Image imgOver = overlay.AddComponent<Image>();
        imgOver.color = new Color(0, 0, 0, 0.85f); // Un poco más oscuro para legibilidad
        
        // Título (¡ERES UN CRACK!)
        GameObject resultGO = new GameObject("OverlayResult");
        resultGO.transform.SetParent(overlay.transform, false);
        var txtResult = resultGO.AddComponent<TextMeshProUGUI>();
        txtResult.text = "¡ÉXITO!";
        txtResult.fontSize = 100;
        txtResult.alignment = TextAlignmentOptions.Center;
        txtResult.color = Color.green;
        RectTransform rtRes = resultGO.GetComponent<RectTransform>();
        rtRes.anchoredPosition = new Vector2(0, 100); // Posición superior
        
        // Mensaje detallado (Has completado...)
        GameObject messageGO = new GameObject("OverlayMessage");
        messageGO.transform.SetParent(overlay.transform, false);
        var txtMessage = messageGO.AddComponent<TextMeshProUGUI>();
        txtMessage.text = "¡Buen trabajo!";
        txtMessage.fontSize = 45;
        txtMessage.alignment = TextAlignmentOptions.Center;
        txtMessage.color = Color.white;
        RectTransform rtMsg = messageGO.GetComponent<RectTransform>();
        rtMsg.anchoredPosition = new Vector2(0, -50); // Debajo del título

        overlay.SetActive(false); 
        manager.overlayFinal = overlay;
        
        EditorSceneManager.SaveScene(newScene, "Assets/Scenes/LaberintoVisual.unity");
    }

    private static void SetupCommonUI(GameObject canvas, BaseActividad manager)
    {
        // Overlay Inicio
        GameObject overlay = new GameObject("OverlayInicio");
        overlay.transform.SetParent(canvas.transform, false);
        overlay.AddComponent<Image>().color = new Color(0,0,0,0.8f);
        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        manager.overlayInicio = overlay;

        // Botón Iniciar
        GameObject btnObj = new GameObject("BotonInicio");
        btnObj.transform.SetParent(overlay.transform, false);
        btnObj.AddComponent<Image>().color = Color.green;
        Button btn = btnObj.AddComponent<Button>();
        manager.botonIniciar = btn;
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        txtObj.AddComponent<TextMeshProUGUI>().text = "INICIAR";
        txtObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }
}
