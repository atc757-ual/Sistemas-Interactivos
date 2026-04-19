using UnityEngine;
using TMPro;
using System.Collections;

public class ActividadDestellos : BaseActividad
{
    [Header("Configuración")]
    public GameObject destelloPrefab;
    public int totalDestellos = 12;
    public float tiempoVida = 2.0f;
    public float tiempoPausaEntreDestellos = 1.0f;

    private int destellosAtrapados = 0;
    private int destellosGenerados = 0;

    protected override void Start()
    {
        base.Start();
        Time.timeScale = 0;
    }

    public override void IniciarJuego()
    {
        base.IniciarJuego();
        StartCoroutine(JuegoDestellos());
    }

    IEnumerator JuegoDestellos()
    {
        while (destellosGenerados < totalDestellos)
        {
            if (!juegoPausado)
            {
                float x = Random.Range(-7f, 7f);
                float y = Random.Range(-4f, 4f);
                GameObject d = Instantiate(destelloPrefab, new Vector3(x, y, 0), Quaternion.identity);
                destellosGenerados++;
                
                StartCoroutine(ControlarDestello(d));
                yield return new WaitForSeconds(tiempoVida + tiempoPausaEntreDestellos);
            }
            yield return null;
        }
        FinalizarActividad("Destellos Fugaces");
    }

    IEnumerator ControlarDestello(GameObject destello)
    {
        float timer = 0;
        bool atrapado = false;

        while (timer < tiempoVida && !atrapado && destello != null)
        {
            if (!juegoPausado && TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
            {
                Vector2 gazePos = TobiiGazeProvider.Instance.GazePositionScreen;
                Ray ray = Camera.main.ScreenPointToRay(gazePos);
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

                if (hit.collider != null && hit.collider.gameObject == destello)
                {
                    atrapado = true;
                    destellosAtrapados++;
                    puntuacion += 15;
                    ActualizarPuntuacionUI();
                    Destroy(destello);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (destello != null) Destroy(destello);
    }

    protected override void MostrarInfo()
    {
        panelInfo.Mostrar("DESTELLOS", "¡Mira los destellos mágicos que aparecen y desaparecen! Debes ser muy rápido para atraparlos a todos.");
    }
}