using UnityEngine;
using System.Collections.Generic;

public class BalloonSpawner : MonoBehaviour
{
    [SerializeField] RectTransform container;

    void Awake()
    {
        if (container == null)
            container = FindByName("BalloonsContainer")?.GetComponent<RectTransform>();
    }

    public List<BalloonController> Spawn(int count, GlobosGameManager manager,
                                          bool useEyeTracking, float onsetMs, float dwellMs)
    {
        ClearContainer();

        if (container == null)
        {
            container = FindByName("BalloonsContainer")?.GetComponent<RectTransform>();
            if (container == null) { Debug.LogError("[BalloonSpawner] 'BalloonsContainer' no encontrado."); return new(); }
        }

        float size = count <= 5 ? 120f : count <= 8 ? 100f : 85f;

        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);

        // Esperar un frame para que el layout calcule rect correcto se hace en GameManager con coroutine
        float areaW = container.rect.width  > 10f ? container.rect.width  : Screen.width;
        float areaH = container.rect.height > 10f ? container.rect.height : Screen.height;

        float marginTop  = 80f;
        float marginSide = 40f;
        float cellW = Mathf.Max(size * 1.8f, (areaW - marginSide * 2f) / cols);
        float cellH = Mathf.Max(size * 1.8f, (areaH - marginTop - marginSide) / rows);

        // Posiciones de celda con shuffle Fisher-Yates
        var cells = new List<Vector2>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                cells.Add(new Vector2(
                    marginSide + cellW * c + cellW * 0.5f - areaW * 0.5f,
                    -marginTop - cellH * r - cellH * 0.5f + areaH * 0.5f
                ));

        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }

        float maxJitter = cellW * 0.2f;
        var result = new List<BalloonController>();

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Balloon_" + (i + 1));
            go.transform.SetParent(container, false);
            var balloon = go.AddComponent<BalloonController>();
            balloon.Init(i + 1, size, manager, useEyeTracking, onsetMs, dwellMs);

            Vector2 base_ = i < cells.Count ? cells[i] : Vector2.zero;
            Vector2 jitter = new Vector2(Random.Range(-maxJitter, maxJitter), Random.Range(-maxJitter, maxJitter));
            go.GetComponent<RectTransform>().anchoredPosition = base_ + jitter;
            result.Add(balloon);
        }
        return result;
    }

    public void ClearContainer()
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    static GameObject FindByName(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && !string.IsNullOrEmpty(t.gameObject.scene.name)) return t.gameObject;
        return null;
    }
}
