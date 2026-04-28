using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GloboComponente : MonoBehaviour
{
    public int Numero { get; private set; }
    private Image _img;

    public void Configurar(int num, float size, GlobosConfig config) {
        Numero = num;
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        
        _img = GetComponent<Image>();
        if (_img == null) _img = gameObject.AddComponent<Image>();

        _img.color = new Color(Random.value, Random.value, Random.value, 0.9f);
        
        // El texto y el anillo de progreso deberían estar ya en el Prefab, 
        // pero para compatibilidad los buscamos o creamos.
        TextMeshProUGUI txt = GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = num.ToString();

        // Configurar Tobii Dwell si está el componente
        GazeDwellHandler dwell = GetComponent<GazeDwellHandler>();
        if (dwell != null)
        {
            // Los tiempos vendrían de la config si se pasara, 
            // por ahora mantenemos la lógica de que el InputHandler lo gestione o se asigne en prefab
        }

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => GlobosInputHandler.TriggerInteract(this));
        }
    }

    public void Explotar() { StartCoroutine(AnimExplotar()); }

    private IEnumerator AnimExplotar() {
        Vector3 startScale = transform.localScale;
        for (float t = 0; t < 1f; t += Time.deltaTime * 5f) {
            transform.localScale = startScale * (1f + t);
            if(_img != null) _img.color = new Color(_img.color.r, _img.color.g, _img.color.b, 1f - t);
            yield return null;
        }
        Destroy(gameObject);
    }
}
