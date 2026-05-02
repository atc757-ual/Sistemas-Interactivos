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
    private const string KEY_DEV_DNI = "DevLastDNI";

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
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
        if (string.IsNullOrEmpty(dni)) return null;
        string normalizedDni = dni.ToUpper().Trim();
        return listaPacientes.Find(p => p.dni.ToUpper().Trim() == normalizedDni);
    }

    public void IniciarSesion(DatosPaciente paciente)
    {
        pacienteActual = paciente;
        inicioSesion = System.DateTime.Now;
        haCalibradoEnEstaSesion = false; // Reset cada vez que inicia sesión nueva
        
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
            // Intentamos recuperar la última sesión del editor
            string lastDni = PlayerPrefs.GetString(KEY_DEV_DNI, "");
            if (!string.IsNullOrEmpty(lastDni))
            {
                CargarTodosLosPacientes(); // Asegurar datos frescos
                DatosPaciente p = BuscarPacientePorDNI(lastDni.Trim());
                if (p != null)
                {
                    Debug.Log($"<color=cyan>[GestorPaciente] SESIÓN RESTAURADA: {p.nombre} ({p.dni}) - {p.historialPartidas.Count} partidas encontradas.</color>");
                    IniciarSesion(p);
                    // haCalibradoEnEstaSesion = false; // Por defecto es false al iniciar
                    return true;
                }
            }

            // 3. Si llegamos aquí y no hay paciente, forzamos ir al Login
            if (pacienteActual == null)
            {
                Debug.Log("<color=red>[GestorPaciente] SESIÓN NO ENCONTRADA. Redirigiendo a Login...</color>");
                // Solo redirigimos si no estamos ya en la escena de Login para evitar bucles
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Login")
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
                }
                return false;
            }

            return true;
#else
            // En build final, si no hay sesión, vamos a Login
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Login")
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
            }
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
            pacienteActual.fechaRegistro = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            pacienteActual.fechaSesion = pacienteActual.fechaRegistro;
            listaPacientes.Add(pacienteActual);
            Debug.Log($"Nuevo paciente registrado el: {pacienteActual.fechaRegistro}");
        }
        else
        {
            pacienteActual.nombre = nombre;
            pacienteActual.fechaSesion = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Debug.Log($"Paciente existente. Última sesión hoy: {pacienteActual.fechaSesion}");
        }

        IniciarSesion(pacienteActual);
        GuardarTodosLosDatos();
    }

    public void GuardarPartida(string nombreJuego, int puntuacion, int nivel, float precision = 0, bool exito = false, float tiempo = 0, int errores = 0)
    {
        if (pacienteActual == null)
        {
            Debug.LogWarning("[GestorPaciente] GuardarPartida ignorada: sin sesión activa.");
            return;
        }

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
        
        // No sumamos la puntuación de calibración al total del paciente
        if (nombreJuego != "Calibración")
            pacienteActual.puntuacionTotal += puntuacion;

        GuardarTodosLosDatos();
    }

    // MÉTODOS DE CÁLCULO PARA EL HISTORIAL
    public float ObtenerPrecisionMedia()
    {
        if (pacienteActual == null) return 0;
        float suma = 0;
        int count = 0;
        foreach (var p in pacienteActual.historialPartidas) 
        {
            if (p.juego != "Calibración")
            {
                suma += p.precision;
                count++;
            }
        }
        return count == 0 ? 0 : suma / count;
    }

    public int ObtenerMisionesExitosas()
    {
        if (pacienteActual == null) return 0;
        int count = 0;
        foreach (var p in pacienteActual.historialPartidas) 
        {
            if (p.juego != "Calibración" && p.exito) count++;
        }
        return count;
    }

    public int ObtenerConteoEjercicios()
    {
        if (pacienteActual == null) return 0;
        int count = 0;
        foreach (var p in pacienteActual.historialPartidas) 
        {
            if (p.juego != "Calibración") count++;
        }
        return count;
    }

    public float ObtenerTiempoTotalDeVuelo()
    {
        if (pacienteActual == null) return 0;
        float suma = 0;
        foreach (var p in pacienteActual.historialPartidas) 
        {
            if (p.juego != "Calibración") suma += p.tiempoJuego;
        }
        return suma;
    }

    public void GuardarTodosLosDatos()
    {
        string json = JsonUtility.ToJson(new WrapperPacientes { items = listaPacientes }, true);
        File.WriteAllText(rutaArchivo, json);
        Debug.Log($"Datos guardados en: {rutaArchivo}");
    }

    public string GetNombrePacienteFormateado()
    {
        if (pacienteActual == null || string.IsNullOrWhiteSpace(pacienteActual.nombre)) return "Astronauta";
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pacienteActual.nombre.ToLower());
    }

    private void CargarTodosLosPacientes()
    {
        if (string.IsNullOrEmpty(rutaArchivo))
            rutaArchivo = Path.Combine(Application.persistentDataPath, "pacientes_data.json");

        Debug.Log("[GestorPaciente] Cargando datos desde: " + rutaArchivo);
        if (File.Exists(rutaArchivo))
        {
            try 
            {
                string json = File.ReadAllText(rutaArchivo);
                Debug.Log($"[GestorPaciente] JSON leído ({json.Length} chars). Primeros 100: {(json.Length > 100 ? json.Substring(0, 100) : json)}");
                
                WrapperPacientes wrapper = JsonUtility.FromJson<WrapperPacientes>(json);
                if (wrapper != null && wrapper.items != null)
                {
                    Dictionary<string, DatosPaciente> mapaPacientes = new Dictionary<string, DatosPaciente>();
                    foreach (var p in wrapper.items)
                    {
                        if (string.IsNullOrEmpty(p.dni)) continue;
                        string key = p.dni.ToUpper().Trim();
                        
                        if (mapaPacientes.ContainsKey(key))
                        {
                            Debug.Log($"<color=orange>[GestorPaciente] FUSIONANDO: DNI {key} tenía {mapaPacientes[key].historialPartidas.Count}, sumando {p.historialPartidas.Count} más.</color>");
                            foreach (var partida in p.historialPartidas)
                            {
                                if (!mapaPacientes[key].historialPartidas.Exists(ex => ex.fecha == partida.fecha && ex.juego == partida.juego))
                                {
                                    mapaPacientes[key].historialPartidas.Add(partida);
                                }
                            }
                            mapaPacientes[key].puntuacionTotal = Mathf.Max(mapaPacientes[key].puntuacionTotal, p.puntuacionTotal);
                        }
                        else
                        {
                            mapaPacientes[key] = p;
                        }
                    }
                    listaPacientes = new List<DatosPaciente>(mapaPacientes.Values);
                    Debug.Log($"<color=green>[GestorPaciente] Carga completa: {listaPacientes.Count} pacientes únicos cargados.</color>");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[GestorPaciente] Error crítico al cargar JSON: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("[GestorPaciente] No existe archivo JSON. Se creará uno nuevo al guardar.");
            listaPacientes = new List<DatosPaciente>();
        }
    }

    [System.Serializable]
    class WrapperPacientes
    {
        public List<DatosPaciente> items;
    }
}