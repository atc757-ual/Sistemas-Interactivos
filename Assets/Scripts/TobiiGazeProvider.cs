using UnityEngine;
using Tobii.Research.Unity;

public class TobiiGazeProvider : MonoBehaviour
{
    public static TobiiGazeProvider Instance;

    public bool useMouseFallback = true;
    public Vector2 GazePositionScreen { get; private set; }
    public bool EyeDataValid { get; private set; }
    public bool HasGaze { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        EyeDataValid = false;

        // Try to get Tobii Gaze Data
        if (EyeTracker.Instance != null && EyeTracker.Instance.LatestGazeData != null)
        {
            var data = EyeTracker.Instance.LatestGazeData;
            
            if (data.Left.GazePointValid || data.Right.GazePointValid)
            {
                EyeDataValid = true;
                Vector2 avgPoint = Vector2.zero;
                int count = 0;
                
                if (data.Left.GazePointValid)
                {
                    avgPoint += data.Left.GazePointOnDisplayArea;
                    count++;
                }
                if (data.Right.GazePointValid)
                {
                    avgPoint += data.Right.GazePointOnDisplayArea;
                    count++;
                }

                avgPoint /= count;
                GazePositionScreen = new Vector2(avgPoint.x * Screen.width, (1f - avgPoint.y) * Screen.height);
            }
        }

        HasGaze = EyeDataValid || (useMouseFallback && Input.GetMouseButton(0)); // Only "HasGaze" with mouse if clicking or always? 
        
        // Original logic kept HasGaze = true if mouse fallback
        if (!EyeDataValid && useMouseFallback)
        {
            GazePositionScreen = Input.mousePosition;
            HasGaze = true;
        }
    }
}