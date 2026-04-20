using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class SceneHealer : EditorWindow
{
    [MenuItem("Herramientas/Reparar Scripts de Escena")]
    public static void RepararScripts()
    {
        int reparados = 0;
        int totalMissing = 0;
        GameObject[] todos = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var go in todos)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    totalMissing++;
                    string nombre = go.name.ToLower();
                    bool sanado = false;

                    // Lógica por nombre de objeto y contexto de escena
                    string activeScene = EditorSceneManager.GetActiveScene().name.ToLower();

                    bool esManager = nombre.Contains("activities") || nombre.Contains("actividades") || 
                                     nombre.Contains("manager") || nombre.Contains("canvas") || 
                                     nombre.Contains("main");

                    if (esManager && (activeScene.Contains("activities") || activeScene.Contains("activid")))
                    {
                        Undo.RegisterCompleteObjectUndo(go, "Reparar Script");
                        GameObject.DestroyImmediate(components[i]);
                        go.AddComponent<ActivitiesManager>();
                        sanado = true;
                    }
                    else if (activeScene.Contains("estrella") || activeScene.Contains("lineal"))
                    {
                        // En la escena de EstrellaLineal, el script suele estar en el Canvas o en un Manager
                        if (esManager || nombre.Contains("estrella") || nombre.Contains("star"))
                        {
                            Undo.RegisterCompleteObjectUndo(go, "Reparar Script");
                            GameObject.DestroyImmediate(components[i]);
                            go.AddComponent<EstrellaLinealManager>();
                            sanado = true;
                        }
                    }

                    if (sanado)
                    {
                        reparados++;
                        Debug.Log($"<color=green>✓ SceneHealer:</color> Sanado correctamente en {go.name} (Escena: {activeScene})");
                    }
                }
            }
        }

        if (totalMissing == 0) {
            EditorUtility.DisplayDialog("Scene Healer", "No se han detectado componentes 'Missing' en esta escena. ¡Todo parece estar sano!", "OK");
        } else {
            EditorUtility.DisplayDialog("Scene Healer", 
                $"Se encontraron {totalMissing} scripts rotos.\nReparados automáticamente: {reparados}.\n\nRevisa la Consola (Ctrl+Shift+C) para ver los nombres de los objetos que siguen rotos.", 
                "OK");
        }
    }
}
