using System;

[Serializable]
public class SessionData
{
    public int   totalBalloons;
    public int   balloonsPopped;
    public int   errors;
    public float timeUsed;
    public float timeLimit;
    public bool  completed;
    public float finalScore;
    public float precisionScore;
    public float velocidadScore;
    public float consistenciaScore;
    public string scoreRange;
    public string scoreMessage;
}
