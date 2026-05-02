using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class DatosPaciente
{
    public string dni;
    public string nombre;
    public string fechaRegistro; // Nueva: fija desde el primer día
    public string fechaSesion;  // Actualizarda: fecha del último login
    public int puntuacionTotal;
    public List<Partida> historialPartidas = new List<Partida>();
}

[System.Serializable]
public class Partida
{
    public string juego;
    public int puntuacion;
    public float precision; // Nueva: 0 a 100
    public bool exito;      // Nueva: si llegó al final
    public float tiempoJuego; // Nueva: en segundos
    public int nivel;       // NUEVO: Nivel alcanzado (1, 2, 3...)
    public int errores;     // NUEVO: Cantidad de fallos/colisiones
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
                if (Application.isPlaying) DontDestroyOnLoad(go);
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
    private string carpetaPacientes;
    private const string KEY_DEV_DNI = "DevLastDNI";

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            
            // Configurar rutas
            rutaArchivo = Path.Combine(Application.persistentDataPath, "pacientes_data.json");
            carpetaPacientes = Path.Combine(Application.persistentDataPath, "Pacientes");
            
            if (!Directory.Exists(carpetaPacientes))
                Directory.CreateDirectory(carpetaPacientes);

            // Migrar si existe el archivo viejo
            MigrarAArchivosIndividuales();
            
            CargarTodosLosPacientes();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void MigrarAArchivosIndividuales()
    {
        if (File.Exists(rutaArchivo))
        {
            Debug.Log("<color=orange>[GestorPaciente] Detectado archivo antiguo. Iniciando migración...</color>");
            try
            {
                string json = File.ReadAllText(rutaArchivo);
                WrapperPacientes wrapper = JsonUtility.FromJson<WrapperPacientes>(json);
                
                if (wrapper != null && wrapper.items != null)
                {
                    int migrados = 0;
                    foreach (var p in wrapper.items)
                    {
                        if (string.IsNullOrEmpty(p.dni)) continue;
                        GuardarDatosPaciente(p);
                        migrados++;
                    }
                    Debug.Log($"<color=green>[GestorPaciente] Migración exitosa: {migrados} pacientes movidos a archivos individuales.</color>");
                    
                    // Renombrar archivo viejo para no repetir migración
                    string backupPath = rutaArchivo + ".bak";
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    File.Move(rutaArchivo, backupPath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[GestorPaciente] Error durante la migración: " + e.Message);
            }
        }
    }

    public DatosPaciente BuscarPacientePorDNI(string dni)
    {
        if (string.IsNullOrEmpty(dni)) return null;
        string normalizedDni = dni.ToUpper().Trim();
        
        // 1. Buscar en memoria primero (caché)
        var p = listaPacientes.Find(paciente => paciente.dni.ToUpper().Trim() == normalizedDni);
        if (p != null) return p;

        // 2. Si no está en memoria, intentar cargar el archivo directamente (por si acaso)
        string path = GetPathPaciente(normalizedDni);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                DatosPaciente nuevoP = JsonUtility.FromJson<DatosPaciente>(json);
                if (nuevoP != null)
                {
                    listaPacientes.Add(nuevoP);
                    return nuevoP;
                }
            }
            catch { /* Ignorar errores de carga individual */ }
        }

        return null;
    }

    public void IniciarSesion(DatosPaciente paciente)
    {
        pacienteActual = paciente;
        inicioSesion = System.DateTime.Now;
        haCalibradoEnEstaSesion = false; 
        
#if UNITY_EDITOR
        PlayerPrefs.SetString(KEY_DEV_DNI, paciente.dni);
        PlayerPrefs.Save();
#endif
        Debug.Log($"Sesión iniciada para {paciente.nombre} a las {inicioSesion}");
    }

    public bool EsSesionValida()
    {
        if (pacienteActual == null) 
        {
#if UNITY_EDITOR
            string lastDni = PlayerPrefs.GetString(KEY_DEV_DNI, "");
            if (!string.IsNullOrEmpty(lastDni))
            {
                DatosPaciente p = BuscarPacientePorDNI(lastDni.Trim());
                if (p != null)
                {
                    Debug.Log($"<color=cyan>[GestorPaciente] SESIÓN RESTAURADA: {p.nombre} ({p.dni})</color>");
                    IniciarSesion(p);
                    return true;
                }
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Login")
                UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
            return false;
#else
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Login")
                UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
            return false;
#endif
        }
        
        System.TimeSpan transcurrido = System.DateTime.Now - inicioSesion;
        return transcurrido.TotalMinutes < TIEMPO_SESION_MINUTOS;
    }

    public void CerrarSesion()
    {
        pacienteActual = null;
        haCalibradoEnEstaSesion = false;
        
#if UNITY_EDITOR
        PlayerPrefs.DeleteKey(KEY_DEV_DNI);
        PlayerPrefs.Save();
#endif
    }

    public void RegistrarPaciente(string dni, string nombre)
    {
        pacienteActual = BuscarPacientePorDNI(dni);

        if (pacienteActual == null)
        {
            pacienteActual = new DatosPaciente();
            pacienteActual.dni = dni.ToUpper().Trim();
            pacienteActual.nombre = nombre;
            pacienteActual.fechaRegistro = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            pacienteActual.fechaSesion = pacienteActual.fechaRegistro;
            listaPacientes.Add(pacienteActual);
        }
        else
        {
            pacienteActual.nombre = nombre;
            pacienteActual.fechaSesion = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }

        IniciarSesion(pacienteActual);
        GuardarDatosPaciente(pacienteActual);
    }

    public void GuardarPartida(string nombreJuego, int puntuacion, int nivel, float precision = 0, bool exito = false, float tiempo = 0, int errores = 0)
    {
        if (pacienteActual == null) return;

        Partida nuevaPartida = new Partida
        {
            juego = nombreJuego,
            puntuacion = puntuacion,
            precision = precision,
            exito = exito,
            tiempoJuego = tiempo,
            nivel = nivel,
            errores = errores,
            fecha = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        pacienteActual.historialPartidas.Add(nuevaPartida);
        
        if (nombreJuego != "Calibración")
            pacienteActual.puntuacionTotal += puntuacion;

        GuardarDatosPaciente(pacienteActual);
    }

    // MÉTODOS DE CÁLCULO
    public float ObtenerPrecisionMedia()
    {
        if (pacienteActual == null) return 0;
        float suma = 0; int count = 0;
        foreach (var p in pacienteActual.historialPartidas) {
            if (p.juego != "Calibración") { suma += p.precision; count++; }
        }
        return count == 0 ? 0 : suma / count;
    }

    public int ObtenerMisionesExitosas()
    {
        if (pacienteActual == null) return 0;
        int count = 0;
        foreach (var p in pacienteActual.historialPartidas) if (p.juego != "Calibración" && p.exito) count++;
        return count;
    }

    public int ObtenerConteoEjercicios()
    {
        if (pacienteActual == null) return 0;
        int count = 0;
        foreach (var p in pacienteActual.historialPartidas) if (p.juego != "Calibración") count++;
        return count;
    }

    public float ObtenerTiempoTotalDeVuelo()
    {
        if (pacienteActual == null) return 0;
        float suma = 0;
        foreach (var p in pacienteActual.historialPartidas) if (p.juego != "Calibración") suma += p.tiempoJuego;
        return suma;
    }

    private string GetPathPaciente(string dni)
    {
        return Path.Combine(carpetaPacientes, $"paciente_{dni.ToUpper().Trim()}.json");
    }

    public void GuardarDatosPaciente(DatosPaciente p)
    {
        if (p == null || string.IsNullOrEmpty(p.dni)) return;
        
        string path = GetPathPaciente(p.dni);
        string json = JsonUtility.ToJson(p, true);
        File.WriteAllText(path, json);
        Debug.Log($"[GestorPaciente] Datos de {p.nombre} guardados en: {path}");
    }

    // Deprecated but kept for compatibility in case another script calls it
    public void GuardarTodosLosDatos()
    {
        if (pacienteActual != null) GuardarDatosPaciente(pacienteActual);
    }

    public string GetNombrePacienteFormateado()
    {
        if (pacienteActual == null || string.IsNullOrWhiteSpace(pacienteActual.nombre)) return "Astronauta";
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pacienteActual.nombre.ToLower());
    }

    private void CargarTodosLosPacientes()
    {
        listaPacientes.Clear();
        if (!Directory.Exists(carpetaPacientes)) return;

        string[] archivos = Directory.GetFiles(carpetaPacientes, "paciente_*.json");
        Debug.Log($"[GestorPaciente] Cargando {archivos.Length} archivos de pacientes...");

        foreach (string path in archivos)
        {
            try
            {
                string json = File.ReadAllText(path);
                DatosPaciente p = JsonUtility.FromJson<DatosPaciente>(json);
                if (p != null && !string.IsNullOrEmpty(p.dni))
                {
                    listaPacientes.Add(p);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GestorPaciente] Error cargando paciente en {path}: {e.Message}");
            }
        }
        Debug.Log($"<color=green>[GestorPaciente] Carga finalizada: {listaPacientes.Count} pacientes en memoria.</color>");
    }

    [System.Serializable]
    class WrapperPacientes
    {
        public List<DatosPaciente> items;
    }
}