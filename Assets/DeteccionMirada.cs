using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DeteccionMirada : MonoBehaviour
{
    [Header("Configuración")]
    public float tiempoParaActivar = 1.5f;
    public int puntosPorActivacion = 10;

    [Header("Límites de pantalla")]
    public float limiteX = 6f;
    public float limiteY = 3.5f;

    private Renderer miRenderer;
    private float tiempoMirando = 0f;
    private bool mirando = false;
    private static int puntuacionTotal = 0;
    private Text textoPuntuacion;

    void Start()
    {
        miRenderer = GetComponent<Renderer>();
        if (miRenderer != null)
        {
            miRenderer.material.color = Color.gray;
        }

        BuscarTextoPuntuacion();
        ActualizarTexto();
        transform.position = new Vector3(0, 0, 0);

        Debug.Log("🎮 Juego listo - Mantén el MOUSE sobre el cubo hasta que se active");
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (Camera.main == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray rayo = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit impacto;

        if (Physics.Raycast(rayo, out impacto))
        {
            if (impacto.collider.gameObject == this.gameObject)
            {
                // Está mirando el cubo
                if (!mirando)
                {
                    mirando = true;
                    tiempoMirando = 0f;
                    Debug.Log("👀 Comenzó a mirar el cubo");
                }

                tiempoMirando += Time.deltaTime;

                // Mostrar el progreso cada 0.3 segundos
                if (Mathf.Floor(tiempoMirando * 10) % 3 == 0)
                {
                    Debug.Log($"⏱️ Progreso: {tiempoMirando:F1} / {tiempoParaActivar} segundos");
                }

                float progreso = Mathf.Clamp01(tiempoMirando / tiempoParaActivar);

                // Cambiar color según el progreso
                if (miRenderer != null)
                {
                    if (progreso < 0.7f)
                        miRenderer.material.color = Color.Lerp(Color.gray, Color.green, progreso / 0.7f);
                    else
                        miRenderer.material.color = Color.Lerp(Color.green, Color.red, (progreso - 0.7f) / 0.3f);
                }

                // Activar al completar el tiempo
                if (tiempoMirando >= tiempoParaActivar)
                {
                    Debug.Log("🎯 ¡TIEMPO COMPLETADO! Activando...");
                    ActivarYReaparecer();
                }
            }
            else
            {
                if (mirando)
                {
                    Debug.Log("❌ Dejó de mirar el cubo");
                    ResetearProgreso();
                }
            }
        }
        else
        {
            if (mirando)
            {
                Debug.Log("❌ Dejó de mirar el cubo");
                ResetearProgreso();
            }
        }
    }

    void ActivarYReaparecer()
    {
        Debug.Log($"🎉 ACTIVADO! +{puntosPorActivacion} puntos");

        puntuacionTotal += puntosPorActivacion;
        ActualizarTexto();

        // Efecto visual
        if (miRenderer != null)
        {
            miRenderer.material.color = Color.cyan;
        }

        // Mover a posición aleatoria
        float xAleatorio = Random.Range(-limiteX, limiteX);
        float yAleatorio = Random.Range(-limiteY, limiteY);
        transform.position = new Vector3(xAleatorio, yAleatorio, 0);

        Debug.Log($"📦 Cubo movido a: X={xAleatorio:F2}, Y={yAleatorio:F2}");

        mirando = false;
        tiempoMirando = 0f;

        if (miRenderer != null)
        {
            miRenderer.material.color = Color.gray;
        }
    }

    void ResetearProgreso()
    {
        mirando = false;
        tiempoMirando = 0f;

        if (miRenderer != null)
        {
            miRenderer.material.color = Color.gray;
        }
    }

    void BuscarTextoPuntuacion()
    {
        GameObject textoObj = GameObject.Find("TextoPuntuacion");
        if (textoObj != null)
        {
            textoPuntuacion = textoObj.GetComponent<Text>();
            if (textoPuntuacion != null)
            {
                Debug.Log("✅ Texto de puntuación encontrado");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró 'TextoPuntuacion'. Crea un UI > Text");
        }
    }

    void ActualizarTexto()
    {
        if (textoPuntuacion != null)
        {
            textoPuntuacion.text = "Puntuación: " + puntuacionTotal;
            Debug.Log($"📝 Puntuación actual: {puntuacionTotal}");
        }
    }
}