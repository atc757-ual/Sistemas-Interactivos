using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// Tests de EditMode para la lógica de dominio de GestorPaciente.
/// Usa reflexión para acceder a Assembly-CSharp sin referencia directa de assembly.
/// Ejecutar desde: Window > General > Test Runner > EditMode.
/// </summary>
public class GestorPacienteTests
{
    // ─── Helpers de reflexión ─────────────────────────────────────────────────

    private System.Type GetType(string typeName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }
        return null;
    }

    /// <summary>Crea un GestorPaciente como MonoBehaviour en un GameObject temporal.</summary>
    private (GameObject go, MonoBehaviour gestor) CrearGestor()
    {
        var go = new GameObject("GestorTest");
        var tipo = GetType("GestorPaciente");
        Assert.IsNotNull(tipo, "No se encontró el tipo GestorPaciente en los assemblies cargados.");
        var gestor = go.AddComponent(tipo) as MonoBehaviour;
        return (go, gestor);
    }

    /// <summary>Crea un DatosPaciente de prueba via reflexión.</summary>
    private object CrearPacienteTest(string nombre = "Tester", string dni = "12345678A")
    {
        var tipo = GetType("DatosPaciente");
        Assert.IsNotNull(tipo, "No se encontró el tipo DatosPaciente.");
        var p = System.Activator.CreateInstance(tipo);
        tipo.GetField("nombre").SetValue(p, nombre);
        tipo.GetField("dni").SetValue(p, dni);
        tipo.GetField("fechaRegistro").SetValue(p, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        tipo.GetField("fechaSesion").SetValue(p, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        // Inicializar historialPartidas como lista vacía
        var listaTipo = typeof(List<>).MakeGenericType(GetType("Partida"));
        tipo.GetField("historialPartidas").SetValue(p, System.Activator.CreateInstance(listaTipo));
        return p;
    }

    private void SetPaciente(MonoBehaviour gestor, object paciente)
        => gestor.GetType().GetField("pacienteActual").SetValue(gestor, paciente);

    private object GetPaciente(MonoBehaviour gestor)
        => gestor.GetType().GetField("pacienteActual").GetValue(gestor);

    private bool InvocarEsSesionValida(MonoBehaviour gestor)
        => (bool)gestor.GetType().GetMethod("EsSesionValida").Invoke(gestor, null);

    private void InvocarGuardarPartida(MonoBehaviour gestor, string juego, int puntos, int nivel,
        float precision = 0, bool exito = false, float tiempo = 0, int errores = 0)
    {
        gestor.GetType().GetMethod("GuardarPartida")
              .Invoke(gestor, new object[] { juego, puntos, nivel, precision, exito, tiempo, errores });
    }

    // ─────────────────────────────────────────────
    // Tests: GuardarPartida
    // ─────────────────────────────────────────────

    [Test]
    public void GuardarPartida_SinPaciente_NoLanzaExcepcion()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, null);

        Assert.DoesNotThrow(() => InvocarGuardarPartida(gestor, "Globos", 50, 1, 80f, true, 30f, 2));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void GuardarPartida_ConPaciente_AgregaPartida()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, CrearPacienteTest());

        InvocarGuardarPartida(gestor, "Globos", 75, 1, 90f, true, 45f, 1);

        var paciente = GetPaciente(gestor);
        var historial = paciente.GetType().GetField("historialPartidas").GetValue(paciente) as System.Collections.IList;
        Assert.AreEqual(1, historial.Count);

        var partida = historial[0];
        Assert.AreEqual("Globos", partida.GetType().GetField("juego").GetValue(partida));
        Assert.AreEqual(75, partida.GetType().GetField("puntuacion").GetValue(partida));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void GuardarPartida_SumaPuntuacionTotal()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, CrearPacienteTest());

        InvocarGuardarPartida(gestor, "Laberinto", 60, 1);
        InvocarGuardarPartida(gestor, "Laberinto", 40, 1);

        var paciente = GetPaciente(gestor);
        int total = (int)paciente.GetType().GetField("puntuacionTotal").GetValue(paciente);
        Assert.AreEqual(100, total);

        Object.DestroyImmediate(go);
    }

    // ─────────────────────────────────────────────
    // Tests: EsSesionValida
    // ─────────────────────────────────────────────

    [Test]
    public void EsSesionValida_SinPaciente_RetornaFalse()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, null);

        Assert.IsFalse(InvocarEsSesionValida(gestor));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void EsSesionValida_SesionReciente_RetornaTrue()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, CrearPacienteTest());
        gestor.GetType().GetField("inicioSesion").SetValue(gestor, System.DateTime.Now);

        Assert.IsTrue(InvocarEsSesionValida(gestor));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void EsSesionValida_SesionExpirada_RetornaFalse()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, CrearPacienteTest());
        gestor.GetType().GetField("inicioSesion").SetValue(gestor, System.DateTime.Now.AddHours(-2));

        Assert.IsFalse(InvocarEsSesionValida(gestor));

        Object.DestroyImmediate(go);
    }

    // ─────────────────────────────────────────────
    // Tests: ObtenerPrecisionMedia
    // ─────────────────────────────────────────────

    [Test]
    public void ObtenerPrecisionMedia_SinPartidas_RetornaCero()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, CrearPacienteTest());

        float media = (float)gestor.GetType().GetMethod("ObtenerPrecisionMedia").Invoke(gestor, null);
        Assert.AreEqual(0f, media);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void ObtenerPrecisionMedia_ConPartidas_CalculaCorrectamente()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, CrearPacienteTest());

        InvocarGuardarPartida(gestor, "Globos",    50, 1, precision: 80f);
        InvocarGuardarPartida(gestor, "Laberinto", 60, 1, precision: 60f);

        float media = (float)gestor.GetType().GetMethod("ObtenerPrecisionMedia").Invoke(gestor, null);
        Assert.AreEqual(70f, media, 0.01f);

        Object.DestroyImmediate(go);
    }

    // ─────────────────────────────────────────────
    // Tests: GetNombrePacienteFormateado
    // ─────────────────────────────────────────────

    [Test]
    public void GetNombrePacienteFormateado_SinPaciente_RetornaAstronauta()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, null);

        string resultado = (string)gestor.GetType().GetMethod("GetNombrePacienteFormateado").Invoke(gestor, null);
        Assert.AreEqual("Astronauta", resultado);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void GetNombrePacienteFormateado_FormatoTitleCase()
    {
        var (go, gestor) = CrearGestor();
        SetPaciente(gestor, CrearPacienteTest(nombre: "ALEX GARCIA"));

        string resultado = (string)gestor.GetType().GetMethod("GetNombrePacienteFormateado").Invoke(gestor, null);
        Assert.AreEqual("Alex Garcia", resultado);

        Object.DestroyImmediate(go);
    }

    // ─────────────────────────────────────────────
    // Test de Contrato: nombres de juego para HistoryManager
    // ─────────────────────────────────────────────

    [Test]
    public void NombresJuegos_SonLosEsperadosPorHistoryManager()
    {
        // Contrato: estos nombres EXACTOS son los que HistoryManager.ConfigurarBotonesDetalle() espera.
        // Si algún manager cambia su string en GuardarPartida, este test lo detecta.
        string[] nombresEsperados = { "Explosión Estelar", "Laberinto Estelar", "Carrera Ocular", "Estrella Lineal" };

        foreach (var nombre in nombresEsperados)
        {
            Assert.IsNotEmpty(nombre, $"El nombre de juego '{nombre}' no debe estar vacío.");
            Assert.IsFalse(nombre.StartsWith(" "), $"'{nombre}' no debe empezar con espacio.");
            Assert.IsFalse(nombre.EndsWith(" "),   $"'{nombre}' no debe terminar con espacio.");
        }

        // Verificar que HistoryManager tiene la referencia correcta leyendo el código fuente
        string historyScript = "Assets/Scripts/HistoryManager.cs";
        if (System.IO.File.Exists(historyScript))
        {
            string contenido = System.IO.File.ReadAllText(historyScript);
            foreach (var nombre in nombresEsperados)
                Assert.IsTrue(contenido.Contains($"\"{nombre}\""),
                    $"HistoryManager.cs no contiene el nombre de juego esperado: \"{nombre}\"");
        }
    }
}
