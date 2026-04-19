using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement; 
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TMProMigrator : EditorWindow
{
    [MenuItem("Tools/Migrar Escena a TextMeshPro")]
    public static void Migrate()
    {
        GameObject[] anyObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CalibradorKids calibrador = GameObject.FindFirstObjectByType<CalibradorKids>();
        
        int count = 0;
        foreach (GameObject obj in anyObjects)
        {
            Text oldText = obj.GetComponent<Text>();
            if (oldText != null)
            {
                string oldContent = oldText.text;
                Color oldColor = oldText.color;
                int oldSize = oldText.fontSize;
                TextAnchor oldAnchor = oldText.alignment;

                Undo.DestroyObjectImmediate(oldText);
                TextMeshProUGUI newText = obj.AddComponent<TextMeshProUGUI>();
                newText.text = oldContent;
                newText.color = oldColor;
                newText.fontSize = oldSize;
                
                // Mapeo básico de alineación
                newText.alignment = TextAlignmentOptions.Center;
                
                Debug.Log($"Migrado: {obj.name} a TextMeshPro.");
                count++;
            }
        }

        if (calibrador != null)
        {
            EditorUtility.SetDirty(calibrador);
            // El script CalibradorKids se auto-vinculará en su Awake() gracias a nuestra lógica de FindObjectByName
            Debug.Log("CalibradorKids marcado para actualización de referencias.");
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Migración Completada", $"Se han convertido {count} elementos a TextMeshPro con éxito.", "¡Genial!");
    }

    [MenuItem("Tools/Preparar Escena de Actividad")]
    public static void SetupActivityScene()
    {
        GameObject overlay = GameObject.Find("OverlayInicio");
        if (overlay == null) overlay = GameObject.Find("PanelInicio");

        if (overlay != null)
        {
            GameObject startMsgObj = GameObject.Find("StartMessage");
            if (startMsgObj == null)
            {
                startMsgObj = new GameObject("StartMessage");
                startMsgObj.transform.SetParent(overlay.transform, false);
                
                TextMeshProUGUI tmp = startMsgObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "Buscando tus ojos...";
                tmp.fontSize = 32;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                RectTransform rect = startMsgObj.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0, -150); // Posición debajo del botón
                rect.sizeDelta = new Vector2(600, 100);

                Undo.RegisterCreatedObjectUndo(startMsgObj, "Create StartMessage");
                Debug.Log("StartMessage creado exitosamente en el Overlay.");
            }
            else {
                Debug.Log("StartMessage ya existe.");
            }
        }
        else {
            EditorUtility.DisplayDialog("Error", "No se encontró un objeto llamado 'OverlayInicio'. Asegúrate de estar en la escena de una actividad.", "Ok");
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Generar Layout de Aventuras")]
    public static void BuildSelectorCards()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        
        // 1. Título
        GameObject titleObj = new GameObject("MainTitle");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Elige tu Aventura";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.8f, 0.4f, 1f); // Púrpura/Lavanda
        titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 400);

        // 2. Contenedor de Tarjetas (Doble Panel para Marco Redondeado)
        GameObject gridObj = new GameObject("AdventureCardsGrid");
        gridObj.transform.SetParent(canvasObj.transform, false);
        
        // --- MARCO EXTERIOR (El color del borde) ---
        var outerImg = gridObj.AddComponent<Image>();
        outerImg.color = new Color(1f, 1f, 1f, 0.4f); // Color del borde (Blanco traslúcido)
        outerImg.type = Image.Type.Sliced; // Para que el redondeado no se estire
        // Aquí deberías asignar tu sprite de borde redondeado en el Inspector
        
        // --- PANEL INTERIOR (El "agujero" central) ---
        GameObject innerObj = new GameObject("InnerHole");
        innerObj.transform.SetParent(gridObj.transform, false);
        var innerImg = innerObj.AddComponent<Image>();
        innerImg.color = new Color(0.02f, 0.02f, 0.05f, 1f); // Mismo color que el fondo de la escena
        innerImg.type = Image.Type.Sliced;
        
        RectTransform innerRect = innerObj.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero; innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(4, 4); // Grosor del borde (4px)
        innerRect.offsetMax = new Vector2(-4, -4);

        // Layout sobre el panel interno
        var layout = innerObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        
        RectTransform gridRect = gridObj.GetComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(1300, 650);
        gridRect.anchoredPosition = new Vector2(0, -50);

        string[] titulos = { "Estrella Lineal", "Planeta Circular", "Cometa Cuadrado", "Meteoro Zigzag" };
        string[] descs = { "Sigue la estrella en línea recta", "Sigue el planeta en círculos", "Sigue el cometa en forma de cuadrado", "Sigue el meteoro en zigzag" };
        Color[] colores = { Color.cyan, new Color(1, 0, 1), new Color(1, 0.5f, 0), Color.yellow };

        for (int i = 0; i < 4; i++)
        {
            GameObject card = new GameObject("Card_" + titulos[i]);
            card.transform.SetParent(innerObj.transform, false);
            
            // Fondo de la tarjeta (Usamos el redondeado que tenemos)
            var img = card.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 1f); // Gris oscuro
            
            // Borde Neón (Un objeto hijo para que brille)
            GameObject border = new GameObject("Border");
            border.transform.SetParent(card.transform, false);
            var bImg = border.AddComponent<Image>();
            bImg.color = colores[i];
            RectTransform bRect = border.GetComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = new Vector2(10, 10); // Un poco más grande para el borde

            // Contenido Interno (Icono, Título, Desc)
            CreateTextInCard(card, titulos[i], 32, new Vector2(0, 0), FontStyles.Bold);
            CreateTextInCard(card, descs[i], 18, new Vector2(0, -60), FontStyles.Normal);
            
            // Botón Play
            GameObject playBtn = new GameObject("PlayButton");
            playBtn.transform.SetParent(card.transform, false);
            var pBtn = playBtn.AddComponent<Button>();
            var pImg = playBtn.AddComponent<Image>();
            pImg.color = colores[i];
            RectTransform pRect = playBtn.GetComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0, -180);
            pRect.sizeDelta = new Vector2(80, 80);
        }

        Undo.RegisterCreatedObjectUndo(titleObj, "Build Adventure Scene");
        Debug.Log("Layout de Aventuras generado con éxito.");
    }

    private static void CreateTextInCard(GameObject parent, string text, float size, Vector2 pos, FontStyles style)
    {
        GameObject obj = new GameObject("Text_" + text);
        obj.transform.SetParent(parent.transform, false);
        var t = obj.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = style;
        t.color = Color.white;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(250, 100);
    }

    [MenuItem("Tools/Construir Pantalla de Resultados Dashboard")]
    public static void BuildResultsDashboard()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        // 1. Título Resultados
        GameObject titleObj = new GameObject("Title_Resultados");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Resultados";
        titleText.fontSize = 80;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.6f, 0.2f); // Naranja
        titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 420);

        // 2. Dashboard Panel
        GameObject dashObj = new GameObject("StatsDashboard");
        dashObj.transform.SetParent(canvasObj.transform, false);
        var dashImg = dashObj.AddComponent<Image>();
        dashImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
        RectTransform dashRect = dashObj.GetComponent<RectTransform>();
        dashRect.sizeDelta = new Vector2(1200, 400);
        dashRect.anchoredPosition = new Vector2(0, 180);

        var layout = dashObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true; layout.childControlHeight = true;

        // Crear las 3 columnas
        string[] labels = { "Precisión Promedio", "Ejercicios Completados", "Tiempo Total" };
        string[] values = { "0%", "0", "0min" };
        for (int i = 0; i < 3; i++)
        {
            GameObject col = new GameObject("Col_" + i);
            col.transform.SetParent(dashObj.transform, false);
            var colLayout = col.AddComponent<VerticalLayoutGroup>();
            colLayout.childAlignment = TextAnchor.MiddleCenter;
            colLayout.spacing = 15;

            // Icono Placeholder
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(col.transform, false);
            icon.AddComponent<Image>().color = new Color(1, 1, 1, 0.2f);
            icon.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);

            // Label
            GameObject lab = new GameObject("Label");
            lab.transform.SetParent(col.transform, false);
            var labT = lab.AddComponent<TextMeshProUGUI>();
            labT.text = labels[i];
            labT.fontSize = 28;
            labT.color = new Color(0.7f, 0.7f, 0.8f);
            labT.alignment = TextAlignmentOptions.Center;

            // Value
            GameObject val = new GameObject("Value");
            val.transform.SetParent(col.transform, false);
            var valT = val.AddComponent<TextMeshProUGUI>();
            valT.text = values[i];
            valT.fontSize = 72;
            valT.fontStyle = FontStyles.Bold;
            valT.alignment = TextAlignmentOptions.Center;
            
            // Línea separadora (si no es el último)
            if (i < 2) {
                // Podrías añadir un objeto imagen fino aquí
            }
        }

        // 3. Zona de Mensaje
        GameObject msgZone = new GameObject("MessageZone");
        msgZone.transform.SetParent(canvasObj.transform, false);
        RectTransform msgRect = msgZone.GetComponent<RectTransform>();
        msgRect.anchoredPosition = new Vector2(0, -150);
        msgRect.sizeDelta = new Vector2(800, 300);

        // Cohete (Icono grande)
        GameObject rocket = new GameObject("RocketIcon");
        rocket.transform.SetParent(msgZone.transform, false);
        rocket.AddComponent<Image>().color = new Color(1, 1, 1, 0.5f);
        rocket.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
        rocket.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 150);

        // Texto Principal y Secundario
        CreateTextInCard(msgZone, "¡Excelente aventura hoy!", 42, new Vector2(0, -20), FontStyles.Normal);
        CreateTextInCard(msgZone, "¡Sigue así para mejorar tu visión!", 24, new Vector2(0, -80), FontStyles.Normal);

        // 4. Botón Volver (Footer)
        GameObject backBtn = new GameObject("BackButton");
        backBtn.transform.SetParent(canvasObj.transform, false);
        var bImg = backBtn.AddComponent<Image>();
        bImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        RectTransform bRect = backBtn.GetComponent<RectTransform>();
        bRect.anchoredPosition = new Vector2(0, -420);
        bRect.sizeDelta = new Vector2(300, 80);
        backBtn.AddComponent<Button>();
        CreateTextInCard(backBtn, "←  VOLVER", 28, Vector2.zero, FontStyles.Bold);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Dashboard de Resultados construido. Vincula los componentes TMP en el script para terminar.");
    }

    private static void CreateBorderLine(GameObject parent, string name, Vector2 min, Vector2 max, Vector2 size, Color color)
    {
        GameObject line = new GameObject("Border_" + name);
        line.transform.SetParent(parent.transform, false);
        var img = line.AddComponent<Image>();
        img.color = color;
        RectTransform r = line.GetComponent<RectTransform>();
        r.anchorMin = min; r.anchorMax = max;
        r.sizeDelta = size;
        r.anchoredPosition = Vector2.zero;
    }

    [MenuItem("Tools/Inyectar Mensajes de Resultados")]
    public static void InjectMessageZone()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) return;

        GameObject msgZone = GameObject.Find("MessageZone");
        if (msgZone == null)
        {
            msgZone = new GameObject("MessageZone");
            msgZone.transform.SetParent(canvasObj.transform, false);
            RectTransform r = msgZone.AddComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0, -200);
            r.sizeDelta = new Vector2(1000, 400);
        }

        // 1. Cohete
        GameObject rocket = GameObject.Find("RocketIcon");
        if (rocket == null)
        {
            rocket = new GameObject("RocketIcon");
            rocket.transform.SetParent(msgZone.transform, false);
            var img = rocket.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.8f);
            RectTransform rt = rocket.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 120);
            rt.sizeDelta = new Vector2(180, 180);
        }

        // 2. Texto Principal
        GameObject mainTxt = GameObject.Find("Feedback_Principal");
        if (mainTxt == null)
        {
            mainTxt = new GameObject("Feedback_Principal");
            mainTxt.transform.SetParent(msgZone.transform, false);
            var tmp = mainTxt.AddComponent<TextMeshProUGUI>();
            tmp.text = "¡Excelente aventura hoy!";
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform rt = mainTxt.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -30);
            rt.sizeDelta = new Vector2(800, 100);
        }

        // 3. Texto Secundario
        GameObject secTxt = GameObject.Find("Feedback_Secundario");
        if (secTxt == null)
        {
            secTxt = new GameObject("Feedback_Secundario");
            secTxt.transform.SetParent(msgZone.transform, false);
            var tmp = secTxt.AddComponent<TextMeshProUGUI>();
            tmp.text = "Sigue así para mejorar tu visión";
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.7f, 0.7f, 0.8f);
            RectTransform rt = secTxt.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -90);
            rt.sizeDelta = new Vector2(800, 600);
        }

        Undo.RegisterCreatedObjectUndo(msgZone, "Inyectar Mensajes");
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Elementos inyectados correctamente en la MessageZone.");
    }
}
