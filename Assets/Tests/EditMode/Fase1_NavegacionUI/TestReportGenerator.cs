using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Genera un reporte en texto plano con el resultado de todos los tests de Fase 1.
/// Ejecutar desde el menú: Tests > Generar Reporte Fase 1
/// </summary>
public static class TestReportGenerator
{
    private const string MENU_PATH    = "Tests/Generar Reporte Fase 1";
    private const string REPORT_DIR   = "Assets/Tests/Reports";
    private const string REPORT_PREFIX = "Fase1_Report";

    // [MenuItem(MENU_PATH)]
    public static void GenerarReporte()
    {
        // Asegurar que el directorio de reportes existe
        if (!Directory.Exists(REPORT_DIR))
            Directory.CreateDirectory(REPORT_DIR);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string reportPath = Path.Combine(REPORT_DIR, $"{REPORT_PREFIX}_{timestamp}.txt");

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();

        var callbacks = new TestCallbacks();
        api.RegisterCallbacks(callbacks);

        var filter = new Filter
        {
            testMode = TestMode.EditMode,
            assemblyNames = new[] { "Fase1_NavegacionUI" }
        };

        api.Execute(new ExecutionSettings(filter));

        // El reporte se guarda cuando los tests terminan (callback OnRunFinished)
        callbacks.ReportPath = reportPath;

        Debug.Log($"<color=cyan>[ReportGenerator]</color> Tests de Fase 1 iniciados. " +
                  $"El reporte se guardará en: {reportPath}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Callbacks
    // ─────────────────────────────────────────────────────────────────────────

    private class TestCallbacks : ICallbacks
    {
        public string ReportPath { get; set; }

        private readonly List<ITestResultAdaptor> _resultados = new List<ITestResultAdaptor>();
        private DateTime _inicio;

        public void RunStarted(ITestAdaptor testsToRun)
        {
            _inicio = DateTime.Now;
            _resultados.Clear();
            Debug.Log("[ReportGenerator] Suite iniciada.");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            EscribirReporte(result);
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (!result.HasChildren)
                _resultados.Add(result);
        }

        private void EscribirReporte(ITestResultAdaptor suiteResult)
        {
            var sb = new StringBuilder();
            string sep = new string('─', 70);

            sb.AppendLine("╔══════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║          REPORTE QA — FASE 1: NAVEGACIÓN Y UI                      ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════════╝");
            sb.AppendLine($"Generado : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Duración : {(DateTime.Now - _inicio).TotalSeconds:F2}s");
            sb.AppendLine($"Total    : {_resultados.Count} tests");
            sb.AppendLine();

            int pass = 0, fail = 0, skip = 0;
            var fallidos = new List<ITestResultAdaptor>();

            foreach (var r in _resultados)
            {
                switch (r.TestStatus)
                {
                    case TestStatus.Passed:  pass++;  break;
                    case TestStatus.Failed:  fail++;  fallidos.Add(r); break;
                    default:                 skip++;  break;
                }
            }

            // ── RESUMEN ──
            sb.AppendLine(sep);
            sb.AppendLine("RESUMEN");
            sb.AppendLine(sep);
            sb.AppendLine($"  ✅ PASS  : {pass}");
            sb.AppendLine($"  ❌ FAIL  : {fail}");
            sb.AppendLine($"  ⏭️  SKIP  : {skip}");
            sb.AppendLine();

            float pct = _resultados.Count > 0 ? (pass / (float)_resultados.Count) * 100f : 0;
            string estado = (fail == 0) ? "🏆 FASE 1 SUPERADA" : $"⚠️  FASE 1 INCOMPLETA ({pct:F0}% PASS)";
            sb.AppendLine($"  ESTADO  : {estado}");
            sb.AppendLine();

            // ── DETALLE POR TEST ──
            sb.AppendLine(sep);
            sb.AppendLine("DETALLE COMPLETO");
            sb.AppendLine(sep);
            foreach (var r in _resultados)
            {
                string icono = r.TestStatus == TestStatus.Passed ? "✅" :
                               r.TestStatus == TestStatus.Failed  ? "❌" : "⏭️ ";
                sb.AppendLine($"{icono} {r.Test.FullName}");
                if (r.TestStatus == TestStatus.Failed && !string.IsNullOrEmpty(r.Message))
                    sb.AppendLine($"     FALLO: {r.Message.Trim()}");
            }

            // ── BUGS DETECTADOS ──
            if (fallidos.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(sep);
                sb.AppendLine("BUGS DETECTADOS (acciones requeridas)");
                sb.AppendLine(sep);
                int n = 1;
                foreach (var r in fallidos)
                {
                    sb.AppendLine($"[BUG-{n++:D2}] {r.Test.Name}");
                    if (!string.IsNullOrEmpty(r.Message))
                        sb.AppendLine($"  → {r.Message.Trim()}");
                    sb.AppendLine();
                }
            }

            // ── ESCENAS VALIDADAS ──
            sb.AppendLine(sep);
            sb.AppendLine("ESCENAS VALIDADAS");
            sb.AppendLine(sep);
            string[] escenas = { "EstrellaLineal", "MeteoroZigzag", "CometaCuadrado", "Activities" };
            foreach (var e in escenas)
                sb.AppendLine($"  • {e}");

            sb.AppendLine();
            sb.AppendLine("FIN DEL REPORTE");

            // Escribir archivo
            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            Debug.Log($"<color=lime>[ReportGenerator]</color> Reporte escrito en: {ReportPath}");
            Debug.Log($"<color=lime>[ReportGenerator]</color> {estado} — {pass}/{_resultados.Count} tests pasaron.");
        }
    }
}
