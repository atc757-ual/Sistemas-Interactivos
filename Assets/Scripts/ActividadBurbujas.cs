using UnityEngine;
using TMPro;
using System.Collections;

public class ActividadBurbujas : BaseActividad
{
    [Header("Configuración")]
    public GameObject burbujaPrefab;
    public int totalBurbujas = 10;
    public float tiempoAparicion = 2.0f;
    public float tiempoParaExplotar = 1.0f;

    public TMP_Text textoVidas;
    private int burbujasExplotadas = 0;
    private int fallos = 0;
    private int maxFallos = 3;
    private bool juegoTerminado = false;

    protected override void Start()
    {
        base.Start();
        Time.timeScale = 0;
    }

    public override void IniciarJuego()
    {
        base.IniciarJuego();
        StartCoroutine(GenerarBurbujas());
        ActualizarUI();
    }

    IEnumerator GenerarBurbujas()
    {
        while (!juegoTerminado && burbujasExplotadas < totalBurbujas)
        {
            if (!juegoPausado)
            {
                float x = Random.Range(-7f, 7f);
                float y = Random.Range(-4f, 2f);
                GameObject b = Instantiate(burbujaPrefab, new Vector3(x, y, 0), Quaternion.identity);
                StartCoroutine(ControlarBurbuja(b));
            }
            yield return new WaitForSeconds(tiempoAparicion);
        }
    }

    IEnumerator ControlarBurbuja(GameObject burbuja)
    {
        float timer = 0;
        bool explotada = false;
        float duracionBurbuja = tiempoParaExplotar * 1.5f; // Faster than before

        while (timer < duracionBurbuja && !explotada && burbuja != null)
        {
            if (!juegoPausado && TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
            {
                Vector2 gazePos = TobiiGazeProvider.Instance.GazePositionScreen;
                Ray ray = Camera.main.ScreenPointToRay(gazePos);
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

                if (hit.collider != null && hit.collider.gameObject == burbuja)
                {
                    timer += Time.deltaTime;
                    burbuja.transform.localScale = Vector3.one * (1f + (timer / tiempoParaExplotar) * 0.2f);
                    
                    if (timer >= tiempoParaExplotar)
                    {
                        Explotar(burbuja);
                        explotada = true;
                    }
                }
                else
                {
                    timer = Mathf.Max(0, timer - Time.deltaTime * 0.5f);
                }
            }
            yield return null;
        }

        if (!explotada && burbuja != null)
        {
            Destroy(burbuja);
            fallos++;
            ActualizarUI();
            if (fallos >= maxFallos) Finalizar(false);
        }
    }

    void Explotar(GameObject b)
    {
        Destroy(b);
        burbujasExplotadas++;
        puntuacion += 10;
        ActualizarUI();
        if (burbujasExplotadas >= totalBurbujas) Finalizar(true);
    }

    void ActualizarUI()
    {
        ActualizarPuntuacionUI();
        if (textoVidas != null) textoVidas.text = $"Vidas: {maxFallos - fallos}";
    }

    void Finalizar(bool exito)
    {
        juegoTerminado = true;
        FinalizarActividad("Explota Burbujas");
    }

    protected override void MostrarInfo()
    {
        panelInfo.Mostrar("BURBUJAS", "¡Aparecerán burbujas mágicas! Míralas fijamente para que exploten antes de que se vayan. ¡No dejes que se escapen muchas!");
    }
}