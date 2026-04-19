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
    
    [Header("Estado de Sesión Temporal")]
    public System.DateTime inicioSesion;
    public bool haCalibradoEnEstaSesion = false;
    private const int TIEMPO_SESION_MINUTOS = 60;

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

    public DatosPaciente BuscarPacientePorDNI(string dni)
    {
        return listaPacientes.Find(p => p.dni == dni);
    }

    public void IniciarSesion(DatosPaciente paciente)
    {
        pacienteActual = paciente;
        inicioSesion = System.DateTime.Now;
        haCalibradoEnEstaSesion = false; // Reset cada vez que inicia sesión nueva
        Debug.Log($"Sesión iniciada para {paciente.nombre} a las {inicioSesion}");
    }

    public bool EsSesionValida()
    {
        if (pacienteActual == null) return false;
        
        System.TimeSpan transcurrido = System.DateTime.Now - inicioSesion;
        return transcurrido.TotalMinutes < TIEMPO_SESION_MINUTOS;
    }

    public void CerrarSesion()
    {
        pacienteActual = null;
        haCalibradoEnEstaSesion = false;
        Debug.Log("Sesión cerrada y datos temporales borrados.");
    }

    public void RegistrarPaciente(string dni, string nombre)
    {
        pacienteActual = BuscarPacientePorDNI(dni);

        if (pacienteActual == null)
        {
            pacienteActual = new DatosPaciente();
            pacienteActual.dni = dni;
            pacienteActual.nombre = nombre;
            pacienteActual.fechaSesion = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            listaPacientes.Add(pacienteActual);
            Debug.Log($"Nuevo paciente creado y guardado: {nombre}");
        }
        else
        {
            pacienteActual.nombre = nombre;
            Debug.Log($"Paciente existente cargado: {nombre}");
        }

        IniciarSesion(pacienteActual);
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