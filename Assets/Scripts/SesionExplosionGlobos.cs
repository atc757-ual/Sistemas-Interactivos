using System;

[Serializable]
public class SesionExplosionGlobos
{
    public string fechaHora;
    public int globosTotales;
    public int globosExplotados;
    public int errores;
    public float tiempoUsado;
    public float tiempoLimite;
    public bool completado;
    public string zonaVelocidad; // "Rápida" | "Media" | "Lenta"
    
    // Componentes de la puntuación
    public float componentePrecision;
    public float componenteVelocidad;
    public float componenteConsistencia;
    public float puntuacionFinal;

    public SesionExplosionGlobos() { }

    public SesionExplosionGlobos(int totales, int explotados, int errores, float usado, float limite, bool completado, 
                                string zona, float prec, float vel, float cons, float final)
    {
        this.fechaHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        this.globosTotales = totales;
        this.globosExplotados = explotados;
        this.errores = errores;
        this.tiempoUsado = usado;
        this.tiempoLimite = limite;
        this.completado = completado;
        this.zonaVelocidad = zona;
        
        this.componentePrecision = prec;
        this.componenteVelocidad = vel;
        this.componenteConsistencia = cons;
        this.puntuacionFinal = final;
    }
}
