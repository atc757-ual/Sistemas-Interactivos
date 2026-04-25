using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;

public class CarreraOcularSceneBuilder : EditorWindow
{
    [MenuItem("Tools/Vision Therapy/Build Carrera Ocular Scene")]
    public static void BuildScene()
    {
        // 1. Create New Scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CarreraOcular";

        // 2. Camera
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // 3. EventSystem
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<StandaloneInputModule>();

        // 4. Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 5. Hierarchy
        // Background
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = new Vector2(Screen.width * 2, 0); // Doble ancho para scroll
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/activityBackground.jpg");
        bgImg.type = Image.Type.Tiled; 
        bgImg.color = Color.white; 
        bgRT.anchoredPosition = Vector2.zero;
        // Player
        GameObject playerObj = new GameObject("Player");
        playerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform playerRT = playerObj.AddComponent<RectTransform>();
        playerRT.sizeDelta = new Vector2(120, 120);
        playerRT.anchoredPosition = new Vector2(-Screen.width * 0.4f, 0);
        Image playerImg = playerObj.AddComponent<Image>();
        Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/MenuIcons/Orbe_Pediatrica_Transparente.png");
        if (playerSprite != null) playerImg.sprite = playerSprite;
        else playerImg.color = Color.cyan;

        // Obstacles Container
        GameObject obsObj = new GameObject("Obstacles");
        obsObj.transform.SetParent(canvasObj.transform, false);
        RectTransform obsRT = obsObj.AddComponent<RectTransform>();
        obsRT.anchorMin = Vector2.zero;
        obsRT.anchorMax = Vector2.one;
        obsRT.sizeDelta = Vector2.zero;

        // UI Layer
        GameObject uiObj = new GameObject("UI");
        uiObj.transform.SetParent(canvasObj.transform, false);
        RectTransform uiRT = uiObj.AddComponent<RectTransform>();
        uiRT.anchorMin = Vector2.zero;
        uiRT.anchorMax = Vector2.one;
        uiRT.sizeDelta = Vector2.zero;

        // Timer Text
        GameObject timerObj = new GameObject("Timer");
        timerObj.transform.SetParent(uiObj.transform, false);
        TextMeshProUGUI timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "00:00";
        timerText.fontSize = 36;
        timerText.alignment = TextAlignmentOptions.TopRight;
        RectTransform timerRT = timerObj.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(1, 1);
        timerRT.anchorMax = new Vector2(1, 1);
        timerRT.pivot = new Vector2(1, 1);
        timerRT.anchoredPosition = new Vector2(-30, -30);
        timerRT.sizeDelta = new Vector2(200, 50);

        // Lives Text
        // Start Button
        GameObject startBtnObj = new GameObject("StartButton");
        startBtnObj.transform.SetParent(uiObj.transform, false);
        Button startBtn = startBtnObj.AddComponent<Button>();
        Image startBtnImg = startBtnObj.AddComponent<Image>();
        startBtnImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        RectTransform startBtnRT = startBtnObj.GetComponent<RectTransform>();
        startBtnRT.sizeDelta = new Vector2(250, 80);
        GameObject startBtnTextObj = new GameObject("Text");
        startBtnTextObj.transform.SetParent(startBtnObj.transform, false);
        TextMeshProUGUI startBtnText = startBtnTextObj.AddComponent<TextMeshProUGUI>();
        startBtnText.text = "INICIAR";
        startBtnText.fontSize = 40;
        startBtnText.alignment = TextAlignmentOptions.Center;
        startBtnText.color = Color.white;
        startBtnText.GetComponent<RectTransform>().sizeDelta = startBtnRT.sizeDelta;

        // Back Button
        GameObject backBtnObj = new GameObject("BackButton");
        backBtnObj.transform.SetParent(uiObj.transform, false);
        Button backBtn = backBtnObj.AddComponent<Button>();
        Image backBtnImg = backBtnObj.AddComponent<Image>();
        backBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        RectTransform backBtnRT = backBtnObj.GetComponent<RectTransform>();
        backBtnRT.anchorMin = new Vector2(0, 0);
        backBtnRT.anchorMax = new Vector2(0, 0);
        backBtnRT.pivot = new Vector2(0, 0);
        backBtnRT.anchoredPosition = new Vector2(30, 30);
        backBtnRT.sizeDelta = new Vector2(150, 50);
        GameObject backBtnTextObj = new GameObject("Text");
        backBtnTextObj.transform.SetParent(backBtnObj.transform, false);
        TextMeshProUGUI backBtnText = backBtnTextObj.AddComponent<TextMeshProUGUI>();
        backBtnText.text = "SALIR";
        backBtnText.fontSize = 24;
        backBtnText.alignment = TextAlignmentOptions.Center;
        backBtnText.color = Color.white;
        backBtnText.GetComponent<RectTransform>().sizeDelta = backBtnRT.sizeDelta;

        // Overlay Inicio (Mínimo para BaseActividad)
        GameObject overlayObj = new GameObject("OverlayInicio");
        overlayObj.transform.SetParent(uiObj.transform, false);
        RectTransform overlayRT = overlayObj.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        overlayObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        
        GameObject startMsgObj = new GameObject("StartMessage");
        startMsgObj.transform.SetParent(overlayObj.transform, false);
        TextMeshProUGUI startMsgText = startMsgObj.AddComponent<TextMeshProUGUI>();
        startMsgText.text = "Haz clic en INICIAR para jugar";
        startMsgText.alignment = TextAlignmentOptions.Center;
        startMsgText.fontSize = 30;

        // Lives Text
        GameObject livesObj = new GameObject("Lives");
        livesObj.transform.SetParent(uiObj.transform, false);
        TextMeshProUGUI livesText = livesObj.AddComponent<TextMeshProUGUI>();
        livesText.text = "Vidas: 3";
        livesText.fontSize = 36;
        livesText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform livesRT = livesObj.GetComponent<RectTransform>();
        livesRT.anchorMin = new Vector2(0, 1);
        livesRT.anchorMax = new Vector2(0, 1);
        livesRT.pivot = new Vector2(0, 1);
        livesRT.anchoredPosition = new Vector2(30, -30);
        livesRT.sizeDelta = new Vector2(200, 50);
        
        // Mover botón iniciar dentro del overlay para que se vea
        startBtnObj.transform.SetParent(overlayObj.transform, false);
        startBtnRT.anchoredPosition = new Vector2(0, -100);

        // Vidas con Corazones
        GameObject heartsObj = new GameObject("HeartsContainer");
        heartsObj.transform.SetParent(uiObj.transform, false);
        HorizontalLayoutGroup hlg = heartsObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = false;
        hlg.childControlWidth = false;
        
        RectTransform heartsRT = heartsObj.GetComponent<RectTransform>();
        heartsRT.anchorMin = new Vector2(0, 1);
        heartsRT.anchorMax = new Vector2(0, 1);
        heartsRT.pivot = new Vector2(0, 1);
        heartsRT.anchoredPosition = new Vector2(150, -35);
        heartsRT.sizeDelta = new Vector2(300, 60);

        System.Collections.Generic.List<Image> iconosVidas = new System.Collections.Generic.List<Image>();
        for (int i = 0; i < 3; i++)
        {
            GameObject heart = new GameObject("Heart_" + i);
            heart.transform.SetParent(heartsObj.transform, false);
            Image hImg = heart.AddComponent<Image>();
            hImg.color = Color.red; // Color inicial rojo
            heart.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
            iconosVidas.Add(hImg);
        }

        // Overlay Resultado (Mínimo para el fin)
        GameObject resultObj = new GameObject("OverlayResultado");
        resultObj.transform.SetParent(uiObj.transform, false);
        RectTransform resultRT = resultObj.AddComponent<RectTransform>();
        resultRT.anchorMin = Vector2.zero;
        resultRT.anchorMax = Vector2.one;
        resultRT.sizeDelta = Vector2.zero;
        resultObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        resultObj.SetActive(false); // Inicia oculto

        GameObject resTextObj = new GameObject("ResultText");
        resTextObj.transform.SetParent(resultObj.transform, false);
        TextMeshProUGUI resText = resTextObj.AddComponent<TextMeshProUGUI>();
        resText.text = "ACTIVIDAD FINALIZADA";
        resText.alignment = TextAlignmentOptions.Center;
        resText.fontSize = 45;
        resText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

        // Botón Reintentar
        GameObject retryBtnObj = new GameObject("RetryButton");
        retryBtnObj.transform.SetParent(resultObj.transform, false);
        Button retryBtn = retryBtnObj.AddComponent<Button>();
        retryBtnObj.AddComponent<Image>().color = Color.blue;
        RectTransform retryRT = retryBtnObj.GetComponent<RectTransform>();
        retryRT.sizeDelta = new Vector2(200, 60);
        retryRT.anchoredPosition = new Vector2(0, -50);
        GameObject retryTextObj = new GameObject("Text");
        retryTextObj.transform.SetParent(retryBtnObj.transform, false);
        TextMeshProUGUI retryText = retryTextObj.AddComponent<TextMeshProUGUI>();
        retryText.text = "REINTENTAR";
        retryText.alignment = TextAlignmentOptions.Center;
        retryText.fontSize = 24;

        // Manager
        GameObject managerObj = new GameObject("GameManager");
        CarreraOcularManager manager = managerObj.AddComponent<CarreraOcularManager>();
        manager.jugador = playerRT;
        manager.backgroundScroll = bgRT;
        manager.contenedorObstaculos = obsRT;
        manager.textoTimer = timerText;
        manager.textoVidas = livesText; // Vinculamos el texto de vidas
        manager.iconosVidas = iconosVidas;
        manager.botonIniciar = startBtn;
        manager.botonSalir = backBtn;
        manager.overlayInicio = overlayObj;
        manager.textoMensajeInicio = startMsgText;
        manager.overlayResult = resultObj;

        // Vincular eventos manualmente en el builder
        retryBtn.onClick.AddListener(manager.ReiniciarJuego);
        
        // Add Tobii Eye Tracker
        if (Object.FindFirstObjectByType<Tobii.Research.Unity.EyeTracker>() == null)
        {
            GameObject tobiiObj = new GameObject("EyeTracker");
            tobiiObj.AddComponent<Tobii.Research.Unity.EyeTracker>();
        }

        // Save Scene
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
            
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CarreraOcular.unity");
        Debug.Log("Escena CarreraOcular creada con éxito.");
    }
}
