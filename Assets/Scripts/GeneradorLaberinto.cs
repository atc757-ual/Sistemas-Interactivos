using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class GeneradorLaberinto : MonoBehaviour
{
    [Header("Configuración")]
    public int columnas = 19;
    public int filas = 11;
    public float anchoCelda = 60f;
    [Range(0.1f, 1.0f)]
    public float grosorPared = 0.2f; // 0.2 = Paredes muy finas, 1.0 = Bloques completos
    public Color colorPared = Color.white; 
    public Color colorCamino = new Color(0, 0, 0, 0);


    [Header("Referencias")]
    public RectTransform contenedor;
    public RectTransform puntoInicio;
    public RectTransform puntoMeta;

    private int[,] _grid; 
    private List<Vector2Int> _caminoSolucion = new List<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> _objetosCamino = new Dictionary<Vector2Int, GameObject>();

    public bool EsParteDeLaSolucion(int x, int y) => _caminoSolucion.Contains(new Vector2Int(x, y));
    public List<Vector2Int> GetCaminoOrdenado() => _caminoSolucion;
    public GameObject GetObjetoEn(int x, int y) => _objetosCamino.ContainsKey(new Vector2Int(x, y)) ? _objetosCamino[new Vector2Int(x, y)] : null;

    public int GetCantidadNodosSolucion() => _caminoSolucion.Count;

    public void Generar()
    {
        // Forzamos los colores por si el Inspector tiene valores antiguos
        colorPared = Color.white;
        colorCamino = new Color(0, 0, 0, 0);

        // Limpiar
        _objetosCamino.Clear();
        _caminoSolucion.Clear();
        foreach (Transform child in contenedor) {
            if (child.gameObject.name != "StartPoint" && child.gameObject.name != "Goal_Point")
                Destroy(child.gameObject);
        }

        _grid = new int[columnas, filas];
        GenerarCamino(1, 1);
        
        // Entrada y Salida (Asegurar que estén talladas en el grid)
        Vector2Int inicioPos = new Vector2Int(0, 1);
        Vector2Int metaPos = new Vector2Int(columnas - 1, 5);
        _grid[inicioPos.x, inicioPos.y] = 1;
        _grid[metaPos.x, metaPos.y] = 1;
        
        // Forzar conexión de la meta con el interior
        _grid[columnas - 2, 5] = 1; 

        // Calcular Camino Feliz (BFS)
        CalcularSolucion(inicioPos, metaPos);

        DibujarLaberinto();
        ConfigurarMarcadores(inicioPos, metaPos);
    }

    void CalcularSolucion(Vector2Int inicio, Vector2Int meta)
    {
        Queue<List<Vector2Int>> queue = new Queue<List<Vector2Int>>();
        queue.Enqueue(new List<Vector2Int> { inicio });
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        while (queue.Count > 0)
        {
            List<Vector2Int> path = queue.Dequeue();
            Vector2Int current = path[path.Count - 1];

            if (current == meta) {
                _caminoSolucion = path; // Guardamos el orden exacto del BFS
                return;
            }

            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs) {
                Vector2Int next = current + d;
                if (next.x >= 0 && next.x < columnas && next.y >= 0 && next.y < filas && _grid[next.x, next.y] == 1 && !visited.Contains(next)) {
                    visited.Add(next);
                    List<Vector2Int> newPath = new List<Vector2Int>(path);
                    newPath.Add(next);
                    queue.Enqueue(newPath);
                }
            }
        }
    }

    void DibujarLaberinto()
    {
        float offsetVertical = -30f; // Bajar un poquito
        float offsetStartX = -(columnas * anchoCelda) / 2f;
        float offsetStartY = -(filas * anchoCelda) / 2f + offsetVertical;

        for (int x = 0; x < columnas; x++) {
            for (int y = 0; y < filas; y++) {
                GameObject celda = new GameObject("Cell_" + x + "_" + y);
                celda.transform.SetParent(contenedor, false);
                celda.AddComponent<Image>().color = (_grid[x, y] == 1) ? colorCamino : colorPared;
                celda.name = (_grid[x, y] == 1) ? "Path" : "Wall";

                RectTransform rt = celda.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(offsetStartX + x * anchoCelda + anchoCelda/2, offsetStartY + y * anchoCelda + anchoCelda/2);
                
                // TODO EL LABERINTO UNIFORME
                float thickness = anchoCelda * 0.2f; // Grosor 20% uniforme
                rt.sizeDelta = new Vector2(anchoCelda + 1, thickness); // Líneas horizontales perfectas
                
                // Si es vertical, invertimos
                if (y + 1 < filas && _grid[x, y+1] == 0) {
                     // Solo un ejemplo, simplificamos a bloques sólidos si prefieres uniformidad total
                }
                
                // REVERSIÓN A BLOQUES SÓLIDOS LIMPIOS PARA UNIFORMIDAD TOTAL
                rt.sizeDelta = new Vector2(anchoCelda + 1, anchoCelda + 1);
                if (_grid[x, y] == 1) _objetosCamino[new Vector2Int(x, y)] = celda;
            }
        }
    }

    void ConfigurarMarcadores(Vector2Int inicio, Vector2Int meta)
    {
        float offsetVertical = -30f; // Sincronizado con DibujarLaberinto
        float offsetStartX = -(columnas * anchoCelda) / 2f;
        float offsetStartY = -(filas * anchoCelda) / 2f + offsetVertical;

        // Auto-crear marcadores si son nulos (Fallback)
        if (puntoInicio == null) {
            GameObject go = new GameObject("StartPoint");
            go.transform.SetParent(contenedor.parent, false);
            puntoInicio = go.AddComponent<Image>().rectTransform;
        }
        if (puntoMeta == null) {
            GameObject go = new GameObject("Goal_Point");
            go.transform.SetParent(contenedor.parent, false);
            puntoMeta = go.AddComponent<Image>().rectTransform;
        }

        if (puntoInicio != null) {
            puntoInicio.sizeDelta = new Vector2(anchoCelda, anchoCelda);
            puntoInicio.anchoredPosition = new Vector2(offsetStartX + inicio.x * anchoCelda + anchoCelda/2, offsetStartY + inicio.y * anchoCelda + anchoCelda/2);
            
            // Inicio: Transparente -> 4 Rayas Blancas -> Texto Blanco
            Image imgIni = puntoInicio.GetComponent<Image>();
            if (imgIni != null) imgIni.color = Color.clear; 
            var txtObj = CrearTexto(puntoInicio.gameObject, "INICIO", Color.white);
            // Mover a la IZQUIERDA y separar de las barras
            txtObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-45, 0);
            CrearRayas(puntoInicio.gameObject, Color.white, 10); // Desplazar barras a la derecha (+10)

            puntoInicio.SetAsLastSibling();
        }
        if (puntoMeta != null) {
            puntoMeta.sizeDelta = new Vector2(anchoCelda, anchoCelda);
            puntoMeta.anchoredPosition = new Vector2(offsetStartX + meta.x * anchoCelda + anchoCelda/2, offsetStartY + meta.y * anchoCelda + anchoCelda/2);
            puntoMeta.gameObject.name = "Goal_Point"; 
            
            // Salida: Transparente -> 4 Rayas Blancas -> Texto Blanco
            Image imgMeta = puntoMeta.GetComponent<Image>();
            if (imgMeta != null) imgMeta.color = Color.clear; 
            var txtObj = CrearTexto(puntoMeta.gameObject, "SALIDA", Color.white);
            // Mover a la DERECHA y separar de las barras
            txtObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(45, 0);
            CrearRayas(puntoMeta.gameObject, Color.white, -10); // Desplazar barras a la izquierda (-10)
            
            puntoMeta.SetAsLastSibling();
        }
    }

    void CrearRayas(GameObject parent, Color c, float offsetX = 0)
    {
        // 5 Rayas horizontales, más estrechas y con espacio
        for (int i = 0; i < 5; i++) {
            GameObject raya = new GameObject("Raya");
            raya.transform.SetParent(parent.transform, false);
            Image img = raya.AddComponent<Image>();
            img.color = new Color(c.r, c.g, c.b, 0.6f); 
            RectTransform rt = raya.GetComponent<RectTransform>();
            // Más estrechas (0.6f) para dejar aire
            rt.sizeDelta = new Vector2(anchoCelda * 0.6f, 7f); 
            rt.anchoredPosition = new Vector2(offsetX, (i - 2f) * 14);
            rt.localRotation = Quaternion.identity; 
        }
    }

    GameObject CrearTexto(GameObject parent, string s, Color c)
    {
        GameObject t = new GameObject("Label");
        t.transform.SetParent(parent.transform, false);
        var txt = t.AddComponent<TextMeshProUGUI>();
        txt.text = s; txt.fontSize = 18; txt.color = c; txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold; // Texto más grueso
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        rt.localRotation = Quaternion.Euler(0, 0, -90); // Rotar de lado
        return t;
    }

    void GenerarCamino(int x, int y)
    {
        _grid[x, y] = 1;
        List<Vector2Int> dirs = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int i = 0; i < dirs.Count; i++) {
            int r = Random.Range(i, dirs.Count);
            var temp = dirs[i]; dirs[i] = dirs[r]; dirs[r] = temp;
        }
        foreach (var d in dirs) {
            int nx = x + d.x * 2, ny = y + d.y * 2;
            if (nx > 0 && nx < columnas - 1 && ny > 0 && ny < filas - 1 && _grid[nx, ny] == 0) {
                _grid[x + d.x, y + d.y] = 1;
                GenerarCamino(nx, ny);
            }
        }
    }
}
