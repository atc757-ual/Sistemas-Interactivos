---
name: tobii-sdk-unity
description: Directrices técnicas para la implementación segura del SDK de Tobii Pro en Unity, con foco en Thread Safety y manejo de Gaze Streams.
---

# Tobii Pro SDK en Unity (Parallax Implementation)

Use when: El usuario solicite implementar adquisición de datos de mirada, calibración integrada, o interactuar directamente con el hardware / SDK de Tobii Pro.

Eres el experto técnico a cargo de la ingesta de datos del *Eye Tracker*. Tu prioridad absoluta es mantener la seguridad de sincronización de hilos (Thread Safety) ya que Unity no permite acceder a su API principal desde threads secundarios, donde Tobii entrega sus eventos.

## Reglas de Implementación

### 1. Thread Safety: Encolado Obligatorio (Enqueue / Dequeue)
- **El Problema:** El stream de datos como `Tobii.Research.GazeDataEventArgs` se dispara en un hilo manejado por la API local o remota de Tobii. Tratar de invocar llamadas como `transform.position`, instanciar objetos, o invocar métodos nativos de Unity dentro de este callback resultará en un "Not in main thread context" exception, o peor, *crash silencioso*.
- **La Solución (Regla Estricta):** Implementa siempre una de las siguientes:
  1. Uso de `System.Collections.Concurrent.ConcurrentQueue<T>` para apilar datos entrantes (como structs de GazeData `Vector3`).
  2. Uso de flags volátiles (`volatile bool _newData`) y campos seguros (`lock (_gazeDataLock)`) que se capturen internamente, para luego leer en `Update()`.
- Procesarás la visualización o las lógicas del Game Engine EXCLUSIVAMENTE vaciando la cola dentro de un contexto del Hilo Principal, ya sea invocando en `Update()` or `LateUpdate()`.

### 2. Ciclo de Vida del Eyetracker
- Garantiza que cualquier suscripción a eventos de Stream de Tobii (`EyeTracker.GazeDataReceived += ...`) tenga SU CORRESPONDIENTE DESUSCRIPCIÓN (`EyeTracker.GazeDataReceived -= ...`) dentro del método `OnDestroy()` o `OnDisable()` del componente Unity que actúe de Manager.
- Fallar en esto originará fugas de memoria (Memory Leaks) masivas y Unity Editor colgándose (Freezing) al detener la compilación PlayMode o al detener el juego.

### 3. Namespace y Desacoplamiento
- Minimiza la cantidad de Clases / Scripts que tengan la directiva `using Tobii.Research;`. Centraliza la captura en un único `GazeDataProvider` (u homólogo).

## Recursos Adicionales del Proyecto
Para ejemplos concretos de la API de Tobii, consulta la Referencia API adjunta a la skill: `[VER ARCHIVO: resources/api_reference.md]`
