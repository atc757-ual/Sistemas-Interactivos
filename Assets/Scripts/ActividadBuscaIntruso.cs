using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ActividadBuscaIntruso : BaseActividad
{
    [Header("Configuración")]
    public GameObject itemPrefab;
    public Sprite spriteNormal;
    public Sprite spriteIntruso;
    public int totalItems = 12;
    public int totalRondas = 5;

    private List<GameObject> itemGrid = new List<GameObject>();
    private int rondasCompletadas = 0;
    private GameObject intrusoActual;

    protected override void Start()
    {
        base.Start();
        Time.timeScale = 0;
    }

    public override void IniciarJuego()
    {
        base.IniciarJuego();
        NuevaRonda();
    }

    void NuevaRonda()
    {
        foreach (var item in itemGrid) Destroy(item);
        itemGrid.Clear();

        int columnas = 4;
        int filas = 3;
        float spacingX = 2.5f;
        float spacingY = 2.0f;
        float startX = -((columnas - 1) * spacingX) / 2f;
        float startY = -((filas - 1) * spacingY) / 2f;

        int intrusoIndice = Random.Range(0, totalItems);
        int itemContador = 0;

        for (int i = 0; i < filas; i++)
        {
            for (int j = 0; j < columnas; j++)
            {
                if (itemContador >= totalItems) break;

                float x = startX + j * spacingX;
                float y = startY + i * spacingY;
                GameObject item = Instantiate(itemPrefab, new Vector3(x, y, 0), Quaternion.identity);
                itemGrid.Add(item);

                SpriteRenderer sr = item.GetComponent<SpriteRenderer>();
                if (itemContador == intrusoIndice)
                {
                    sr.sprite = spriteIntruso;
                    intrusoActual = item;
                    // Add distinctive color/scale slightly to be an "intruder"
                    item.transform.localScale = Vector3.one * 1.2f;
                }
                else
                {
                    sr.sprite = spriteNormal;
                }
                itemContador++;
            }
        }
    }

    void Update()
    {
        if (!juegoIniciado || juegoPausado || intrusoActual == null) return;

        if (TobiiGazeProvider.Instance != null && TobiiGazeProvider.Instance.HasGaze)
        {
            Vector2 gazePos = TobiiGazeProvider.Instance.GazePositionScreen;
            Ray ray = Camera.main.ScreenPointToRay(gazePos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            if (hit.collider != null && hit.collider.gameObject == intrusoActual)
            {
                puntuacion += 50;
                rondasCompletadas++;
                ActualizarPuntuacionUI();

                if (rondasCompletadas >= totalRondas)
                {
                    FinalizarActividad("Busca al Intruso");
                }
                else
                {
                    NuevaRonda();
                }
            }
        }
    }

    protected override void MostrarInfo()
    {
        panelInfo.Mostrar("BUSCA AL INTRUSO", "¡Uno de los objetos es diferente a los demás! Encuéntralo con tu mirada para pasar a la siguiente ronda.");
    }
}