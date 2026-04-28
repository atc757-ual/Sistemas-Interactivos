using UnityEngine;

public static class SessionScorer
{
    public static SessionData Calculate(int balloonsPopped, int errors, float timeUsed, float timeLimit, int totalBalloons)
    {
        bool completed = balloonsPopped >= totalBalloons;

        int   intentos  = balloonsPopped + errors;
        float precision = intentos > 0 ? (balloonsPopped / (float)intentos) * 100f : 100f;

        float ratio      = timeLimit > 0 ? timeUsed / timeLimit : 1f;
        float multiplier = ratio <= 0.33f ? 1.0f : ratio <= 0.66f ? 0.65f : 0.35f;
        float velocidad  = completed
            ? 100f * multiplier
            : (totalBalloons > 0 ? (balloonsPopped / (float)totalBalloons) * 50f * multiplier : 0f);

        float consistencia = Mathf.Clamp(100f - (errors * 15f), 0f, 100f);

        float final = precision * 0.40f + velocidad * 0.40f + consistencia * 0.20f;
        final = Mathf.Round(Mathf.Clamp(final, 0f, 100f) * 10f) / 10f;

        var data = new SessionData
        {
            totalBalloons     = totalBalloons,
            balloonsPopped    = balloonsPopped,
            errors            = errors,
            timeUsed          = timeUsed,
            timeLimit         = timeLimit,
            completed         = completed,
            finalScore        = final,
            precisionScore    = precision,
            velocidadScore    = velocidad,
            consistenciaScore = consistencia,
        };

        if      (final >= 90f) { data.scoreRange = "★★★ Excelente"; data.scoreMessage = "¡Rendimiento excepcional!"; }
        else if (final >= 70f) { data.scoreRange = "★★ Bien";        data.scoreMessage = "¡Buen trabajo!"; }
        else if (final >= 50f) { data.scoreRange = "★ Regular";       data.scoreMessage = "¡Sigue practicando!"; }
        else                   { data.scoreRange = "Iniciando";        data.scoreMessage = "¡Cada sesión cuenta!"; }

        return data;
    }
}
