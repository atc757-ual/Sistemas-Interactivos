using UnityEngine;
using Tobii.Research.Unity;

public class TobiiGazeProvider : MonoBehaviour
{
    public static TobiiGazeProvider Instance;

    public bool useMouseFallback = true;
    public Vector2 GazePositionScreen { get; private set; }
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
        HasGaze = false;

        // Try to get Tobii Gaze Data
        if (EyeTracker.Instance != null && EyeTracker.Instance.LatestGazeData != null)
        {
            var data = EyeTracker.Instance.LatestGazeData;
            
            if (data.Left.GazePointValid || data.Right.GazePointValid)
            {
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
                // Tobii GazePointOnDisplayArea is normalized (0,0 top-left to 1,1 bottom-right in some SDKs, 
                // but let's assume it's normalized coordinates based on common Tobii Pro usage)
                // In Tobii Pro Unity SDK, GazePointOnDisplayArea is usually 0,0 top-left.
                GazePositionScreen = new Vector2(avgPoint.x * Screen.width, (1f - avgPoint.y) * Screen.height);
                HasGaze = true;
            }
        }

        // Mouse fallback if no Tobii data
        if (!HasGaze && useMouseFallback)
        {
            GazePositionScreen = Input.mousePosition;
            HasGaze = true;
        }
    }
}