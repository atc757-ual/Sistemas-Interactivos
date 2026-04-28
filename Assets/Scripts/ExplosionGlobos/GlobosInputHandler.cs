using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// Gestiona la detección de mirada (Tobii) y clics del mouse para los globos.
/// </summary>
public class GlobosInputHandler : MonoBehaviour
{
    public static event Action<GloboComponente> OnGloboInteract;

    [SerializeField] private GlobosConfig config;
    
    // Método estático para que el componente del globo lo llame
    public static void TriggerInteract(GloboComponente globo)
    {
        OnGloboInteract?.Invoke(globo);
    }

    // El Dwell se gestiona dentro de cada componente de globo usando GazeDwellHandler 
    // pero el handler puede configurar los tiempos desde aquí si es necesario.
}
