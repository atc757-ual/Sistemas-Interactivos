using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class ConfiguradorActividadesVacias
{
    [MenuItem("Tobii Pro/2. Generar Escenas de Prueba para Actividades")]
    public static void GenerarEscenasPrueba()
    {
        // Las escenas que el SelectorActividades necesita llamar
        string[] escenasRequeridas = new string[] 
        { 
            "EstrellaLineal"
        };

        string rutaBase = "Assets/Scenes/ActividadesPrueba";
        if (!AssetDatabase.IsValidFolder(rutaBase))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            
            AssetDatabase.CreateFolder("Assets/Scenes", "ActividadesPrueba");
        }

        List<EditorBuildSettingsScene> escenasBuild = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool huboCambios = false;

        foreach (string nom in escenasRequeridas)
        {
            // Primero, comprobamos si ya existe en cualquier ruta dentro de BuildSettings
            bool existeEnBuild = false;
            foreach(var s in escenasBuild)
            {
                if (s.path.Contains(nom + ".unity")) { existeEnBuild = true; break; }
            }

            if (!existeEnBuild)
            {
                string path = rutaBase + "/" + nom + ".unity";
                
                // Si la escena no existe físicamente, la creamos vacía con un texto
                if (!System.IO.File.Exists(path))
                {
                    var nuevaEscena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    
                    // Añadimos una cámara
                    GameObject camObj = new GameObject("Main Camera");
                    var cam = camObj.AddComponent<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.1f, 0.4f, 0.6f); // Azul agradable
                    
                    // Añadimos un texto 3D básico para identificar rápidamente la escena
                    GameObject txtObj = new GameObject("Texto Identificador");
                    var textMesh = txtObj.AddComponent<TextMesh>();
                    textMesh.text = "ESCENA DE PRUEBA:\n" + nom + "\n(Presiona M para volver al menú)";
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.fontSize = 24;
                    textMesh.color = Color.white;
                    txtObj.transform.position = new Vector3(0, 0, 5);

                    // Pequeño script improvisado para poder volver pulsando M o Escape
                    GameObject manager = new GameObject("VolverManager");
                    var script = manager.AddComponent<VolverAlMenuTemporal>();

                    EditorSceneManager.SaveScene(nuevaEscena, path);
                }

                escenasBuild.Add(new EditorBuildSettingsScene(path, true));
                huboCambios = true;
                Debug.Log($"<color=cyan>Escena {nom} preparada y enlazada.</color>");
            }
        }

        if (huboCambios)
        {
            EditorBuildSettings.scenes = escenasBuild.ToArray();
            Debug.Log("<color=green>¡Todas las escenas de prueba creadas y añadidas al Build Settings!</color>");
        }
        else
        {
            Debug.Log("Todas las escenas de actividades ya están generadas y en el Build Settings.");
        }
    }
}

// Script auto-generado para poder probar las redirecciones sin Quedarse atrapado
public class VolverAlMenuTemporal : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape))
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
    }
}
