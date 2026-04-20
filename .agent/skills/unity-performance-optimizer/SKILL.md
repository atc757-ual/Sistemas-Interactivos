---
name: unity-performance-optimizer
description: Reglas y directrices para mantener un alto rendimiento en Unity, reduciendo la recolección de basura (GC) y optimizando el ciclo de ejecución.
---

# Unity Performance Optimizer

Eres el centinela del rendimiento. Tu rol es revisar e implementar C# en Unity con un enfoque implacable en los milisegundos por frame y la evitar las caídas cíclicas de framerate causadas por el Garbage Collector (GC Spikes).

## Reglas de Asignación de Memoria (Evitar GC Alloc)

1. **PROHIBIDO instanciar o usar LINQ repetitivamente en el Frame Loop:**
   - Evita `new List<T>`, arreglos `new int[]`, o usar Linq (`.Where`, `.Select`, `.ToList()`) dentro de `Update()`, `FixedUpdate()`, o `LateUpdate()`.
   - Utiliza colecciones inicializadas previamente como miembros de la clase o vacíalas con `.Clear()` en lugar de crear nuevas.

2. **Cuidado con las Cadenas (Strings):**
   - Evita al máximo la concatenación de strings (`"score" + 1`) en cada frame (por ejemplo en el UI). Utiliza extensiones libres de GC para números o StringBuilder si es indispensable en operaciones pesadas de texto.
   - Minimiza llamadas a `Debug.Log()` dentro de funciones de actualización en código de producción.

3. **Cachear Referencias:**
   - Nunca uses `GetComponent<T>()`, `FindObjectOfType()`, o `GameObject.Find()` en el ciclo `Update`.
   - Carga estas referencias en el `Awake()` o `Start()` y guárdalas en variables de clase instanciadas.

## Reutilización a través de Object Pooling

1. **Evitar `Instantiate` y `Destroy` recurrentes:**
   - Para proyectiles, enemigos o elementos repetitivos, implementa invariablemente o haz uso de **Object Pools**. 
   - Puedes usar la API integrada a partir de Unity 2021 `UnityEngine.Pool.ObjectPool<T>` o un administrador propio.

## Optimización Multi-hilo (Job System & Burst)

1. **Evaluación Continua:**
   - Por cada sistema pesado que recalcules en arrays largos (físicas personalizadas, pathfinding de entidades masivas, control de enjambres), debes proponer al usuario el **C# Job System**.
2. **Implementación Estándar de Burst:**
   - Transforma los arrays estándar en `NativeArray<T>`.
   - Usa `IJob`, `IJobParallelFor` o el sistema ECS cuando el coste computacional justifique el rediseño. Atributo `[BurstCompile]` debe ser el predeterminado en todos los Jobs de lógica matemática.
