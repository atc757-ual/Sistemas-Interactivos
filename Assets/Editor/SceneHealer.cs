using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SceneHealer : EditorWindow
{
    // [MenuItem("Herramientas/Limpiar Escena y Duplicados")]
    public static void CleanScene()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int brokenCount = 0;
        int duplicateCount = 0;
        bool managerFound = false;

        foreach (GameObject obj in allObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            brokenCount += removed;

            var managers = obj.GetComponents<ActivitiesManager>();
            if (managers.Length > 0)
            {
                for (int i = 0; i < managers.Length; i++)
                {
                    if (managers[i] == null) continue;
                    if (!managerFound) managerFound = true;
                    else { Undo.DestroyObjectImmediate(managers[i]); duplicateCount++; }
                }
            }
        }
        EditorUtility.DisplayDialog("Limpieza Completa", $"Eliminados {brokenCount} rotos y {duplicateCount} duplicados.", "OK");
    }

    // [MenuItem("Herramientas/Crear Panel de Aviso Calibracion")]
    public static void CreateWarningPanel()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) {
            EditorUtility.DisplayDialog("Error", "No hay un Canvas en la escena.", "OK");
            return;
        }

        GameObject aviso = new GameObject("AvisoCalibracion");
        aviso.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = aviso.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 100);
        rt.sizeDelta = new Vector2(650, 80);

        Image img = aviso.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.9f);

        GameObject txtObj = new GameObject("Texto");
        txtObj.transform.SetParent(aviso.transform, false);
        TMP_Text txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = "¡Casi listo! Debes <b>Calibrar</b> tus ojos antes de empezar.";
        txt.fontSize = 28;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;

        aviso.SetActive(false);
        Selection.activeGameObject = aviso;
        
        EditorUtility.DisplayDialog("Hecho", "Panel 'AvisoCalibracion' creado. ¡Ahora el script ya puede mostrarlo!", "¡Genial!");
    }
}
