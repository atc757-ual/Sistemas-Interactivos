using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Bloque 2 — Pruebas de presencia de elementos UI críticos en cada actividad.
///
/// HALLAZGOS DE ANÁLISIS (bugs documentados, no corregidos):
///  - botonPausar (PauseButton): NULL en las 3 escenas → los tests de pausa FALLAN
///  - botonInfo   (InfoButton):  NULL en las 3 escenas → los tests de info FALLAN
///  - EstrellaLineal: botonSalir e botonIniciar son NULL en editor
///    (se asignan en runtime pero GameObject 'VolverBtn' sí existe)
///
/// Cada test reporta la causa exacta del fallo para guiar la corrección.
/// </summary>
[TestFixture]
public class UIPresenceTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // DATOS — rutas y nombres de GOs extraídos del análisis real
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Par {ruta, nombre mostrable} de cada escena de actividad.</summary>
    private static readonly string[][] ActivityScenes =
    {
        new[] { "Assets/Scenes/EstrellaLineal.unity",  "EstrellaLineal"  },
        new[] { "Assets/Scenes/MeteoroZigzag.unity",   "MeteoroZigzag"   },
        new[] { "Assets/Scenes/CometaCuadrado.unity",  "CometaCuadrado"  },
    };

    // Nombres de GameObject buscados por BaseActividad (fuente: BaseActividad.cs L33-36)
    private const string GO_PAUSE   = "PauseButton";
    private const string GO_BACK    = "BackButton";
    private const string GO_INFO    = "InfoButton";
    private const string GO_START   = "StartButton";

    // Nombres alternativos usados en las escenas reales (del análisis)
    private static readonly string[] AltBack  = { "BackButton",  "VolverBtn" };
    private static readonly string[] AltStart = { "StartButton" };

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Busca un GameObject por cualquiera de los nombres dados
    /// dentro de la escena actualmente cargada.
    /// </summary>
    private static GameObject BuscarGO(params string[] nombres)
    {
        foreach (var n in nombres)
        {
            var go = GameObject.Find(n);
            if (go != null) return go;
        }
        return null;
    }

    /// <summary>
    /// Busca recursivamente un Button cuyo nombre contenga la palabra dada.
    /// Fallback cuando el nombre exacto no coincide.
    /// </summary>
    private static Button BuscarButtonConNombre(string fragmento)
    {
        var todos = Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in todos)
            if (b.name.ToLower().Contains(fragmento.ToLower())) return b;
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2.1 — Botón SALIDA presente con componente Button activo
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("Assets/Scenes/EstrellaLineal.unity",  "EstrellaLineal")]
    [TestCase("Assets/Scenes/MeteoroZigzag.unity",   "MeteoroZigzag")]
    [TestCase("Assets/Scenes/CometaCuadrado.unity",  "CometaCuadrado")]
    public void Escena_TieneBotonSalida(string scenePath, string sceneName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            // BaseActividad busca primero "BackButton", luego buscamos "VolverBtn" como alternativa real
            var go = BuscarGO("BackButton", "VolverBtn");

            Assert.IsNotNull(go,
                $"[{sceneName}] Botón Salida no encontrado. " +
                $"BaseActividad espera 'BackButton' o 'VolverBtn'. " +
                $"Crear un GameObject con ese nombre y componente Button.");

            var btn = go.GetComponent<Button>();
            Assert.IsNotNull(btn,
                $"[{sceneName}] GameObject '{go.name}' existe pero no tiene componente Button.");

            Assert.IsTrue(go.activeSelf,
                $"[{sceneName}] El botón Salida '{go.name}' está desactivado (SetActive=false).");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2.2 — Botón PAUSA presente con componente Button activo
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("Assets/Scenes/EstrellaLineal.unity",  "EstrellaLineal")]
    [TestCase("Assets/Scenes/MeteoroZigzag.unity",   "MeteoroZigzag")]
    [TestCase("Assets/Scenes/CometaCuadrado.unity",  "CometaCuadrado")]
    public void Escena_TieneBotonPausa(string scenePath, string sceneName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            // BaseActividad.cs L34: GameObject.Find("PauseButton")
            var go = BuscarGO("PauseButton", "PauseBtn", "BtnPause");

            // FALLO ESPERADO según análisis: PauseButton no existe en ninguna escena
            Assert.IsNotNull(go,
                $"[{sceneName}] BUG: Botón Pausa no encontrado. " +
                $"BaseActividad busca 'PauseButton' (L34 de BaseActividad.cs). " +
                $"Resultado: botonPausar=NULL → AlternarPausa() nunca se conecta. " +
                $"ACCIÓN REQUERIDA: Añadir botón 'PauseButton' con componente Button.");

            var btn = go?.GetComponent<Button>();
            Assert.IsNotNull(btn,
                $"[{sceneName}] '{go?.name}' no tiene componente Button.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2.3 — Botón INFO presente con componente Button activo
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("Assets/Scenes/EstrellaLineal.unity",  "EstrellaLineal")]
    [TestCase("Assets/Scenes/MeteoroZigzag.unity",   "MeteoroZigzag")]
    [TestCase("Assets/Scenes/CometaCuadrado.unity",  "CometaCuadrado")]
    public void Escena_TieneBotonInfo(string scenePath, string sceneName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            // BaseActividad.cs L33: GameObject.Find("InfoButton")
            var go = BuscarGO("InfoButton", "InfoBtn", "BtnInfo");

            // FALLO ESPERADO según análisis: InfoButton no existe en ninguna escena
            Assert.IsNotNull(go,
                $"[{sceneName}] BUG: Botón Info no encontrado. " +
                $"BaseActividad busca 'InfoButton' (L33 de BaseActividad.cs). " +
                $"Resultado: botonInfo=NULL → MostrarInfo() nunca se conecta. " +
                $"ACCIÓN REQUERIDA: Añadir botón 'InfoButton' con componente Button.");

            var btn = go?.GetComponent<Button>();
            Assert.IsNotNull(btn,
                $"[{sceneName}] '{go?.name}' no tiene componente Button.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2.4 — Botón INICIAR presente
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("Assets/Scenes/EstrellaLineal.unity",  "EstrellaLineal")]
    [TestCase("Assets/Scenes/MeteoroZigzag.unity",   "MeteoroZigzag")]
    [TestCase("Assets/Scenes/CometaCuadrado.unity",  "CometaCuadrado")]
    public void Escena_TieneBotonIniciar(string scenePath, string sceneName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            // BaseActividad.cs L36: GameObject.Find("StartButton")
            var go = BuscarGO("StartButton");

            Assert.IsNotNull(go,
                $"[{sceneName}] Botón Iniciar ('StartButton') no encontrado. " +
                $"BaseActividad busca 'StartButton' (L36 de BaseActividad.cs).");

            var btn = go?.GetComponent<Button>();
            Assert.IsNotNull(btn,
                $"[{sceneName}] 'StartButton' existe pero no tiene componente Button.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2.5 — Exactamente 1 componente BaseActividad en cada escena
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("Assets/Scenes/EstrellaLineal.unity",  "EstrellaLineal")]
    [TestCase("Assets/Scenes/MeteoroZigzag.unity",   "MeteoroZigzag")]
    [TestCase("Assets/Scenes/CometaCuadrado.unity",  "CometaCuadrado")]
    public void Escena_TieneExactamenteUnaBaseActividad(string scenePath, string sceneName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            // Usar reflexión para encontrar MonoBehaviours que hereden de BaseActividad
            // sin necesitar referencia directa al Assembly-CSharp en el asmdef
            System.Type baseType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                baseType = asm.GetType("BaseActividad");
                if (baseType != null) break;
            }

            Assert.IsNotNull(baseType,
                "No se encontró el tipo 'BaseActividad' en ningún assembly cargado.");

            var todos = Object.FindObjectsByType(baseType,
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Filtrar solo los de esta escena
            var deEstaEscena = new List<Component>();
            foreach (var obj in todos)
            {
                var c = obj as Component;
                if (c != null && c.gameObject.scene.name == sceneName)
                    deEstaEscena.Add(c);
            }

            Assert.AreEqual(1, deEstaEscena.Count,
                $"[{sceneName}] Se esperaba exactamente 1 BaseActividad. " +
                $"Encontrados: {deEstaEscena.Count}. " +
                $"Tipos: [{string.Join(", ", deEstaEscena.ConvertAll(b => b.GetType().Name))}]");

            // Verificar que el componente está habilitado
            var behaviour = deEstaEscena[0] as MonoBehaviour;
            Assert.IsNotNull(behaviour, $"[{sceneName}] El componente no es MonoBehaviour.");
            Assert.IsTrue(behaviour.enabled,
                $"[{sceneName}] El componente {behaviour.GetType().Name} está deshabilitado.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2.6 — Objeto Objetivo (target de seguimiento) presente
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("Assets/Scenes/MeteoroZigzag.unity",  "MeteoroZigzag")]
    [TestCase("Assets/Scenes/CometaCuadrado.unity", "CometaCuadrado")]
    public void Escena_TieneObjetivoDeSegumiento(string scenePath, string sceneName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            // En las nuevas escenas el GO se llama "Objetivo" (del análisis)
            var go = BuscarGO("Objetivo", "Star", "Bubble");

            Assert.IsNotNull(go,
                $"[{sceneName}] Objeto de seguimiento no encontrado. " +
                $"Se busca un GameObject llamado 'Objetivo', 'Star' o 'Bubble'.");

            Assert.IsTrue(go.activeSelf,
                $"[{sceneName}] El objeto '{go.name}' existe pero está desactivado.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2.7 — OverlayInicio existe y está activo al cargar la escena
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("Assets/Scenes/MeteoroZigzag.unity",  "MeteoroZigzag")]
    [TestCase("Assets/Scenes/CometaCuadrado.unity", "CometaCuadrado")]
    public void Escena_TieneOverlayInicio(string scenePath, string sceneName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            var go = GameObject.Find("OverlayInicio");

            Assert.IsNotNull(go,
                $"[{sceneName}] 'OverlayInicio' no encontrado. " +
                $"BaseActividad depende de este panel para el bio-trigger inicial.");

            Assert.IsTrue(go.activeSelf,
                $"[{sceneName}] 'OverlayInicio' existe pero NO está activo al cargar la escena. " +
                $"Debe estar activo para que el flujo de inicio funcione correctamente.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2.8 — Canvas con CanvasScaler presente en cada actividad
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    [TestCase("Assets/Scenes/EstrellaLineal.unity",  "EstrellaLineal")]
    [TestCase("Assets/Scenes/MeteoroZigzag.unity",   "MeteoroZigzag")]
    [TestCase("Assets/Scenes/CometaCuadrado.unity",  "CometaCuadrado")]
    public void Escena_TieneCanvasConScaler(string scenePath, string sceneName)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.IsNotNull(canvas,
                $"[{sceneName}] No se encontró ningún Canvas en la escena.");

            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            Assert.IsNotNull(scaler,
                $"[{sceneName}] El Canvas no tiene CanvasScaler. " +
                $"La UI puede no escalar correctamente en diferentes resoluciones.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
