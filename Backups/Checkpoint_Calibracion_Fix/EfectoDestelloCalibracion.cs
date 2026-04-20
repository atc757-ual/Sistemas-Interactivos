using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class EfectoDestelloCalibracion : MonoBehaviour
{
    [Header("Glow & Pulsing Effect")]
    public float pulseSpeed = 3f;
    public float minScale = 0.9f;
    public float maxScale = 1.1f;
    public float minAlpha = 0.7f;
    public float maxAlpha = 1.0f;

    [Header("Trail Effect")]
    public bool enableTrail = true;
    public float trailSpawnRate = 0.08f;
    public float trailLifeTime = 0.6f;

    private float _lastSpawnTime;
    private Image _image;

    void Start()
    {
        _image = GetComponent<Image>();
        _lastSpawnTime = Time.time;
    }

    void Update()
    {
        // 1. Pulse (Scale & Alpha)
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(scale, scale, 1);

        if (_image != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, alpha);
        }

        // 2. Spawn Trail
        if (enableTrail)
        {
            if (Time.time - _lastSpawnTime > trailSpawnRate)
            {
                SpawnTrail();
                _lastSpawnTime = Time.time;
            }
        }
    }

    void SpawnTrail()
    {
        // Just create a copy of the current image as the trail
        GameObject ghost = new GameObject("Trail_Partical", typeof(RectTransform));
        ghost.transform.SetParent(transform.parent, false);
        ghost.transform.SetSiblingIndex(transform.GetSiblingIndex()); // Behind
        
        var ghostRt = ghost.GetComponent<RectTransform>();
        ghostRt.position = transform.position;
        ghostRt.localScale = transform.localScale;
        ghostRt.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        var img = ghost.AddComponent<Image>();
        img.sprite = _image.sprite;
        img.color = new Color(_image.color.r, _image.color.g, _image.color.b, 0.4f);

        // Add fader script
        var fader = ghost.AddComponent<DestelloFader>();
        fader.StartFade(trailLifeTime);
    }
}

public class DestelloFader : MonoBehaviour
{
    private float _duration;
    private float _timer;
    private Image _img;
    private Vector3 _startScale;

    public void StartFade(float duration)
    {
        _duration = duration;
        _timer = duration;
        _img = GetComponent<Image>();
        _startScale = transform.localScale;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            Destroy(gameObject);
            return;
        }

        float factor = _timer / _duration;
        if (_img != null)
        {
            _img.color = new Color(_img.color.r, _img.color.g, _img.color.b, factor * 0.4f);
        }
        
        // Shrink slightly
        transform.localScale = _startScale * factor;
    }
}
