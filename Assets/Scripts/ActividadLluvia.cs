using UnityEngine;
using TMPro;
using System.Collections;

public class ActividadLluvia : BaseActividad
{
    [Header("Configuración")]
    public GameObject regaloPrefab;
    public float velocidadCaida = 2.0f;
    public float tiempoAparicion = 1.5f;
    public int totalRegalos = 15;

    private int regalosAtrapados = 0;
    private int regalosGenerados = 0;

    protected override void Start()
    {
        base.Start();
        Time.timeScale = 0;
    }

    public override void IniciarJuego()
    {
        base.IniciarJuego();
        StartCoroutine(GenerarRegalos());
    }

    IEnumerator GenerarRegalos()
    {
        while (regalosGenerados < totalRegalos)
        {
            if (!juegoPausado)
            {
                float x = Random.Range(-7f, 7f);
                GameObject r = Instantiate(regaloPrefab, new Vector3(x, 6f, 0), Quaternion.identity);
                regalosGenerados++;
                StartCoroutine(ControlarRegalo(r));
            }
            yield return new WaitForSeconds(tiempoAparicion);
        }
        
        // Wait a bit for last gifts to fall
        yield return new WaitForSeconds(3f);
        FinalizarActividad("Lluvia de Regalos");
    }

    IEnumerator ControlarRegalo(GameObject regalo)
    {
        while (regalo != null && regalo.transform.position.y > -6f)
        {
            if (!juegoPausado)
            {
                regalo.transform.Translate(Vector3.down * velocidadCaida * Time.deltaTime);

                if (TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
                {
                    Vector2 gazePos = TobiiGazeProvider.Instance.GazePositionScreen;
                    Ray ray = Camera.main.ScreenPointToRay(gazePos);
                    RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

                    if (hit.collider != null && hit.collider.gameObject == regalo)
                    {
                        regalosAtrapados++;
                        puntuacion += 20;
                        ActualizarPuntuacionUI();
                        Destroy(regalo);
                        yield break;
                    }
                }
            }
            yield return null;
        }
        if (regalo != null) Destroy(regalo);
    }

    protected override void MostrarInfo()
    {
        panelInfo.Mostrar("LLUVIA DE REGALOS", "¡Están cayendo regalos del cielo! Míralos con tus ojos para atraparlos antes de que lleguen al suelo.");
    }
}