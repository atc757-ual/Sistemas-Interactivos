using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ActividadLaberinto : BaseActividad
{
    [Header("Configuración")]
    public GameObject puntoPrefab;
    public float velocidadPunto = 3.0f;
    public List<Vector3> camino = new List<Vector3>();

    private GameObject puntoActual;
    private int indiceCamino = 0;
    private float tiempoMirando = 0f;
    // private float tiempoParaActivar = 0.5f; // Comentado para evitar warning CS0414

    protected override void Start()
    {
        base.Start();
        Time.timeScale = 0;
        
        // Default path if empty
        if (camino.Count == 0)
        {
            camino.Add(new Vector3(-6, -3, 0));
            camino.Add(new Vector3(-4, 3, 0));
            camino.Add(new Vector3(-2, -3, 0));
            camino.Add(new Vector3(0, 3, 0));
            camino.Add(new Vector3(2, -3, 0));
            camino.Add(new Vector3(4, 3, 0));
            camino.Add(new Vector3(6, -3, 0));
        }
    }

    public override void IniciarJuego()
    {
        base.IniciarJuego();
        if (puntoActual == null && puntoPrefab != null)
        {
            puntoActual = Instantiate(puntoPrefab, camino[0], Quaternion.identity);
            indiceCamino = 1;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!juegoIniciado || juegoPausado || puntoActual == null) return;

        // Move target along path
        Vector3 target = camino[indiceCamino];
        puntoActual.transform.position = Vector3.MoveTowards(puntoActual.transform.position, target, velocidadPunto * Time.deltaTime);

        if (Vector3.Distance(puntoActual.transform.position, target) < 0.1f)
        {
            indiceCamino++;
            if (indiceCamino >= camino.Count)
            {
                puntuacion += 100;
                FinalizarActividad("Laberinto Visual");
                return;
            }
        }

        // Must keep gaze on it
        if (TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
        {
            Vector2 gazePos = TobiiGazeProvider.Instance.GazePositionScreen;
            Ray ray = Camera.main.ScreenPointToRay(gazePos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            if (hit.collider != null && hit.collider.gameObject == puntoActual)
            {
                tiempoMirando += Time.deltaTime;
                if (tiempoMirando >= 0.1f) puntuacion += 1;
                ActualizarPuntuacionUI();
            }
        }
    }

    public override void MostrarInfo()
    {
        panelInfo.Mostrar("LABERINTO", "¡Sigue al punto mágico por todo el laberinto sin quitarle los ojos de encima! Si lo sigues bien, sumarás muchos puntos.");
    }
}