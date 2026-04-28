using UnityEngine;
using System.Collections.Generic;

public class PatientDataManager : MonoBehaviour
{
    public static PatientDataManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    public void SaveSession(SessionData data)
    {
        if (GestorPaciente.Instance == null) return;
        int   intentos  = data.balloonsPopped + data.errors;
        float precision = intentos > 0 ? data.balloonsPopped / (float)intentos * 100f : 100f;
        GestorPaciente.Instance.GuardarPartida(
            "Globos",
            Mathf.RoundToInt(data.finalScore),
            1,
            precision,
            data.completed,
            data.timeUsed
        );
    }

    public List<Partida> GetGlobosHistory()
    {
        if (GestorPaciente.Instance?.pacienteActual == null) return new List<Partida>();
        return GestorPaciente.Instance.pacienteActual.historialPartidas
               .FindAll(p => p.juego == "Globos");
    }

    public string GetPatientName() =>
        GestorPaciente.Instance?.pacienteActual?.nombre ?? "Astronauta";
}
