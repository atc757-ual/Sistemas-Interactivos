using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class DatosPaciente
{
    public string dni;
    public string nombre;
    public string fechaSesion;
    public int puntuacionTotal;
    public List<Partida> historialPartidas = new List<Partida>();
}

[System.Serializable]
public class Partida
{
    public string juego;
    public int puntuacion;
    public string fecha;
}

public class GestorPaciente : MonoBehaviour
{
    private static GestorPaciente _instance;
    public static GestorPaciente Instance
    {
        get
        {
            if (_instance == null)
            {
                // Auto-creation if not found in scene
                GameObject go = new GameObject("GestorPaciente_AutoCreated");
                _instance = go.AddComponent<GestorPaciente>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Paciente Actual")]
    public DatosPaciente pacienteActual;
    
    [Header("Lista de Todos los Pacientes")]
    public List<DatosPaciente> listaPacientes = new List<DatosPaciente>();
    
    private string rutaArchivo;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            rutaArchivo = Path.Combine(Application.persistentDataPath, "pacientes_data.json");
            CargarTodosLosPacientes();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void RegistrarPaciente(string dni, string nombre)
    {
        // Search if patient already exists in the list
        pacienteActual = listaPacientes.Find(p => p.dni == dni);

        if (pacienteActual == null)
        {
            pacienteActual = new DatosPaciente();
            pacienteActual.dni = dni;
            pacienteActual.nombre = nombre;
            pacienteActual.fechaSesion = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            listaPacientes.Add(pacienteActual);
            Debug.Log($"Nuevo paciente creado: {nombre}");
        }
        else
        {
            pacienteActual.nombre = nombre; // Update name if different
            Debug.Log($"Paciente existente cargado: {nombre}");
        }

        GuardarTodosLosDatos();
    }

    public void GuardarPartida(string nombreJuego, int puntuacion)
    {
        if (pacienteActual == null) return;

        Partida nuevaPartida = new Partida
        {
            juego = nombreJuego,
            puntuacion = puntuacion,
            fecha = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        pacienteActual.historialPartidas.Add(nuevaPartida);
        pacienteActual.puntuacionTotal += puntuacion;

        GuardarTodosLosDatos();
    }

    public void GuardarTodosLosDatos()
    {
        string json = JsonUtility.ToJson(new Wrapper<List<DatosPaciente>> { items = listaPacientes }, true);
        File.WriteAllText(rutaArchivo, json);
        Debug.Log($"Datos guardados en: {rutaArchivo}");
    }

    private void CargarTodosLosPacientes()
    {
        if (File.Exists(rutaArchivo))
        {
            string json = File.ReadAllText(rutaArchivo);
            Wrapper<List<DatosPaciente>> wrapper = JsonUtility.FromJson<Wrapper<List<DatosPaciente>>>(json);
            if (wrapper != null && wrapper.items != null)
            {
                listaPacientes = wrapper.items;
            }
        }
    }

    [System.Serializable]
    class Wrapper<T>
    {
        public T items;
    }
}