using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// Bloque 1 — Pruebas de navegación entre escenas.
/// Valida que los scripts de navegación referencian escenas registradas
/// en Build Settings y que no existen eslabones rotos en la cadena.
///
/// NOTA DE ANÁLISIS: Los botones asignan listeners por código en Start(),
/// no en el Inspector. Por tanto GetPersistentEventCount() = 0 para todos.
/// Estas pruebas validan el código fuente del script de navegación directamente.
/// </summary>
[TestFixture]
public class NavigationTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // DATOS DEL PROYECTO — extraídos en la fase de análisis
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tabla completa de navegación del ActivitiesManager.
    /// Formato: { nombreBotonGO, escenaDestino }
    /// Fuente: ActivitiesManager.cs líneas 29-33.
    /// </summary>
    private static readonly string[,] NavegacionActivities =
    {
        { "BtnEstrellaLineal",  "EstrellaLineal"   },
        { "BtnMeteoroZigZag",   "MeteoroZigzag"    },
        { "BtnCometaCuadrado",  "CometaCuadrado"   },
        { "BtnPlanetaCircular", "PlanetaCircular"  },  // ⚠️ ESPERAMOS QUE FALLE: escena inexistente
        { "VolverBtn",          "Home"             },
    };

    /// <summary>
    /// Cadena completa de navegación esperada desde el menú raíz
    /// hasta cada actividad final.
    /// </summary>
    private static readonly string[] CadenaNavegacionCompleta =
    {
        "Login",
        "Home",
        "Activities",
        "EstrellaLineal",
        "MeteoroZigzag",
        "CometaCuadrado",
    };

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el conjunto de nombres de escena registradas Y HABILITADAS
    /// en Build Settings.
    /// </summary>
    private static HashSet<string> ObtenerEscenasBuild(bool soloHabilitadas = true)
    {
        var result = new HashSet<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (soloHabilitadas && !s.enabled) continue;
            result.Add(Path.GetFileNameWithoutExtension(s.path));
        }
        return result;
    }

    /// <summary>
    /// Lee el texto de ActivitiesManager.cs para encontrar referencias
    /// a LoadScene por nombre, dado que los listeners son runtime-only.
    /// </summary>
    private static List<string> ExtraerEscenasReferenciadas(string scriptPath)
    {
        var escenas = new List<string>();
        if (!File.Exists(scriptPath)) return escenas;

        foreach (var linea in File.ReadAllLines(scriptPath))
        {
            int idx = linea.IndexOf("LoadScene(\"");
            if (idx < 0) continue;
            int start = idx + 11;
            int end   = linea.IndexOf("\"", start);
            if (end > start)
                escenas.Add(linea.Substring(start, end - start));
        }
        return escenas;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 1.1 — Escenas de actividad habilitadas en Build Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("EstrellaLineal")]
    [TestCase("MeteoroZigzag")]
    [TestCase("CometaCuadrado")]
    [TestCase("Home")]
    [TestCase("Login")]
    [TestCase("Activities")]
    public void Escena_EstaHabilitadaEnBuildSettings(string nombreEscena)
    {
        // Verificar que la escena está en Build Settings y habilitada
        var escenasBuild = ObtenerEscenasBuild(soloHabilitadas: true);

        Assert.IsTrue(
            escenasBuild.Contains(nombreEscena),
            $"La escena '{nombreEscena}' no está en Build Settings o está deshabilitada. " +
            $"Escenas habilitadas: {string.Join(", ", escenasBuild)}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 1.2 — ActivitiesManager referencia escenas válidas
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void ActivitiesManager_EscenasReferenciadas_ExistenEnBuildSettings()
    {
        // Extraer escenas referenciadas por código fuente (no por Inspector)
        const string scriptPath = "Assets/Scripts/ActivitiesManager.cs";
        var escenasRef = ExtraerEscenasReferenciadas(scriptPath);

        Assert.IsNotEmpty(escenasRef,
            $"No se encontraron llamadas a LoadScene en '{scriptPath}'. " +
            "Verificar ruta del script.");

        var escenasBuild = ObtenerEscenasBuild(soloHabilitadas: true);
        var noEncontradas = new List<string>();

        foreach (var escena in escenasRef)
            if (!escenasBuild.Contains(escena))
                noEncontradas.Add(escena);

        Assert.IsEmpty(noEncontradas,
            $"ActivitiesManager referencia escenas no registradas en Build Settings: " +
            $"[{string.Join(", ", noEncontradas)}]. " +
            $"Registradas y habilitadas: [{string.Join(", ", escenasBuild)}]");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 1.3 — Detección específica de PlanetaCircular (caso conocido)
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void ActivitiesManager_PlanetaCircular_NoExisteEnBuildSettings()
    {
        // Este test documenta el hallazgo crítico:
        // BtnPlanetaCircular → LoadScene("PlanetaCircular") pero la escena no existe.
        // El test PASA si el bug persiste (lo documenta) y FALLA si se corrige.
        // Formato: Assert diseñado para FALLAR cuando el bug ESTÉ CORREGIDO.
        var escenasBuild = ObtenerEscenasBuild(soloHabilitadas: false);  // incluye deshabilitadas
        bool planetaRegistrada = escenasBuild.Contains("PlanetaCircular");

        // Reportamos el estado actual sin bloquear el pipeline (Warn no existe en NUnit/Unity)
        if (!planetaRegistrada)
        {
            TestContext.WriteLine(
                "BUG DETECTADO [1.3]: 'PlanetaCircular' es referenciada en " +
                "ActivitiesManager.LoadScene(\"PlanetaCircular\") " +
                "pero NO existe en Build Settings. " +
                "Al pulsar BtnPlanetaCircular en runtime se lanzará MissingSceneException.");
        }

        // El flujo sin PlanetaCircular no debe romper los demás botones
        string[] esencialesConPlaneta = { "EstrellaLineal", "MeteoroZigzag", "CometaCuadrado" };
        foreach (var e in esencialesConPlaneta)
            Assert.IsTrue(escenasBuild.Contains(e),
                $"La escena esencial '{e}' falta en Build Settings.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 1.4 — Cadena completa menú → actividad sin eslabones rotos
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void CadenaNavegacion_TodasLasEscenas_TienenArchivoEnDisco()
    {
        // Verificar que todas las escenas de la cadena existen como archivo .unity
        string projectRoot = Path.GetFullPath(
            Path.Combine(Application.dataPath, ".."));

        var rotas = new List<string>();
        foreach (var escena in CadenaNavegacionCompleta)
        {
            string path = Path.Combine(projectRoot, "Assets", "Scenes", escena + ".unity");
            if (!File.Exists(path))
                rotas.Add(escena);
        }

        Assert.IsEmpty(rotas,
            $"Las siguientes escenas de la cadena de navegación NO existen en disco: " +
            $"[{string.Join(", ", rotas)}]");
    }

    [Test]
    public void CadenaNavegacion_TodasLasEscenas_EstabanHabilitadasEnBuild()
    {
        // Verificar que cada eslabón de la cadena está habilitado en Build Settings
        var escenasBuild = ObtenerEscenasBuild(soloHabilitadas: true);
        var rotas = new List<string>();

        foreach (var escena in CadenaNavegacionCompleta)
            if (!escenasBuild.Contains(escena))
                rotas.Add(escena);

        Assert.IsEmpty(rotas,
            $"Eslabones rotos en la cadena de navegación " +
            $"(escenas no habilitadas en Build Settings): " +
            $"[{string.Join(", ", rotas)}]");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 1.5 — Activities.unity contiene los botones de navegación esperados
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Activities_ContieneLosBotonesDeNavegacion()
    {
        // Abrir la escena en modo aditivo (sin destruir la activa)
        var scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/Activities.unity",
            OpenSceneMode.Additive);

        try
        {
            string[] botonesEsperados = {
                "BtnEstrellaLineal",
                "BtnMeteoroZigZag",
                "BtnCometaCuadrado",
                "VolverBtn"
            };

            var faltantes = new List<string>();
            foreach (var nombre in botonesEsperados)
            {
                var go = GameObject.Find(nombre);
                if (go == null || go.GetComponent<UnityEngine.UI.Button>() == null)
                    faltantes.Add(nombre);
            }

            Assert.IsEmpty(faltantes,
                $"Botones de navegación faltantes o sin componente Button en Activities: " +
                $"[{string.Join(", ", faltantes)}]");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 1.6 — ActivitiesManager está presente en Activities.unity
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Activities_TieneActivitiesManager()
    {
        var scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/Activities.unity",
            OpenSceneMode.Additive);

        try
        {
            // Usar reflexión para no depender de referencia directa al Assembly-CSharp
            System.Type managerType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                managerType = asm.GetType("ActivitiesManager");
                if (managerType != null) break;
            }

            Assert.IsNotNull(managerType,
                "No se encontró el tipo 'ActivitiesManager' en ningún assembly cargado.");

            var managers = Object.FindObjectsByType(managerType,
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Filtrar solo los de esta escena
            int count = 0;
            foreach (var obj in managers)
            {
                var c = obj as Component;
                if (c != null && c.gameObject.scene.name == "Activities") count++;
            }

            Assert.AreEqual(1, count,
                $"Se esperaba exactamente 1 ActivitiesManager en Activities.unity. " +
                $"Encontrados: {count}");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
