using UnityEngine;

public static class GlobosStatsCalculator
{
    public static ResumenData Calcular(int aciertos, int totales, int errores, float tiempoUsado, float tiempoLimite, bool completado, GlobosConfig config)
    {
        // 1. Precisión (Peso según config, original 40%)
        int intentosTotales = aciertos + errores;
        float precision = (intentosTotales > 0) ? (aciertos / (float)intentosTotales) * 100f : 0f;
        precision = Mathf.Clamp(precision, 0f, 100f);

        // 2. Velocidad (Peso según config, original 40%)
        float porcentajeTiempo = tiempoUsado / tiempoLimite;
        float multiplicador;
        string zonaVel = "Lenta";

        if (porcentajeTiempo <= 0.33f)
        {
            multiplicador = 1.0f;
            zonaVel = "Rápida";
        }
        else if (porcentajeTiempo <= 0.66f)
        {
            multiplicador = 0.65f;
            zonaVel = "Media";
        }
        else
        {
            multiplicador = 0.35f;
            zonaVel = "Lenta";
        }

        float velocidad;
        if (completado)
        {
            velocidad = 100f * multiplicador;
        }
        else
        {
            float proporcion = aciertos / (float)totales;
            velocidad = proporcion * 50f * multiplicador;
        }
        velocidad = Mathf.Clamp(velocidad, 0f, 100f);

        // 3. Consistencia (Peso según config, original 20%)
        float consistencia = 100f - (errores * 15f);
        consistencia = Mathf.Clamp(consistencia, 0f, 100f);

        // Puntuación Final
        float puntuacionFinal = (precision * config.pesoPrecision) + 
                                (velocidad * config.pesoVelocidad) + 
                                (consistencia * config.pesoConsistencia);
                                
        puntuacionFinal = Mathf.Round(puntuacionFinal * 10f) / 10f;
        puntuacionFinal = Mathf.Clamp(puntuacionFinal, 0f, 100f);

        return new ResumenData
        {
            precision = precision,
            velocidad = velocidad,
            consistencia = consistencia,
            puntuacionFinal = puntuacionFinal,
            zonaVelocidad = zonaVel,
            aciertos = aciertos,
            totales = totales,
            errores = errores,
            tiempoUsado = tiempoUsado,
            tiempoLimite = tiempoLimite,
            fueCompletado = completado
        };
    }
}
