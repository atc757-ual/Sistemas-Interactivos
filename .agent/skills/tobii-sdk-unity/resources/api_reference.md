# Tobii Pro SDK - Unity API Reference

Hoja de referencia rápida para el uso del paquete oficial de Tobii Pro en en el entorno gestionado de .NET.

## 1. Localización del Tracker

```csharp
using Tobii.Research;

// Localizar trackers de manera síncrona
EyeTrackerCollection trackers = EyeTrackingOperations.FindAllEyeTrackers();

if(trackers != null && trackers.Count > 0)
{
    IEyeTracker eyeTracker = trackers[0];
    Debug.Log($"Conectado a {eyeTracker.DeviceName}");
}
```

## 2. Suscripción a Flujos de Datos (Gaze Data)
*Importante: Este código se ejecuta en un Hilo Background.*

```csharp
using Tobii.Research;
using System.Collections.Concurrent;

public ConcurrentQueue<Vector3> GazePositionQueue = new ConcurrentQueue<Vector3>();

private void OnGazeDataReceived(object sender, GazeDataEventArgs e)
{
    // NO SE PUEDE INVOCAR UNITY API AQUI
    var leftEye = e.LeftEye.GazePoint.PositionOnDisplayArea;
    var rightEye = e.RightEye.GazePoint.PositionOnDisplayArea;

    // Calcular punto medio crudo y mandarlo al Main Thread
    Vector3 screenGaze = new Vector3(
        (leftEye.X + rightEye.X) / 2f, 
        (leftEye.Y + rightEye.Y) / 2f, 
        0f);

    GazePositionQueue.Enqueue(screenGaze);
}
```

## 3. Limpieza y Desenlace Seguro
Debe gestionarse cuidadosamente desde Unity MonoBehaviour lifecycle:

```csharp
private IEyeTracker _tracker;

private void OnDisable() // O OnDestroy
{
    if (_tracker != null)
    {
         _tracker.GazeDataReceived -= OnGazeDataReceived;
    }
}
```
