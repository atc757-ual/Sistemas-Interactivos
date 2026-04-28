using System;

[Serializable]
public struct ResumenData
{
    public float precision;
    public float velocidad;
    public float consistencia;
    public float puntuacionFinal;
    public string zonaVelocidad;
    public int aciertos;
    public int totales;
    public int errores;
    public float tiempoUsado;
    public float tiempoLimite;
    public bool fueCompletado;
}
