using UnityEngine;

[CreateAssetMenu(fileName = "GlobosConfig", menuName = "VisionTherapy/ExplosionGlobosConfig")]
public class GlobosConfig : ScriptableObject
{
    [Header("Ajustes de Juego")]
    public int cantidadGlobos = 5;
    public float tamanoGloboBase = 100f;
    public Color[] coloresGlobos;
    
    [Header("Dificultad")]
    public float tiempoLimiteSegundos = 60f;
    public float penalizacionSegundos = 5f;
    public float jitterFactor = 0.15f;

    [Header("Tobii / Eye Tracking")]
    public float onsetDelayMs = 175f;
    public float dwellTimeMs = 600f;

    [Header("Pesos de Puntuación (Sumar 1.0)")]
    public float pesoPrecision = 0.40f;
    public float pesoVelocidad = 0.40f;
    public float pesoConsistencia = 0.20f;
}
