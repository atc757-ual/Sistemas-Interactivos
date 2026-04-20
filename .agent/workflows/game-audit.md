---
description: Realiza una auditoría automatizada del código C# de Unity basándose en las skills TDD, Performance y Clean Architecture.
---

# Game Code Audit Workflow

Este workflow está diseñado para ejecutarse cuando el usuario escribe `/audit`. Escaneará todo el código de producción en el workspace de Unity y generará recomendaciones estructuradas para mejorar su calidad y acoplamiento según la filosofía de Antigravity Unity.

El alcance es principalmente la carpeta `Assets/` (y usualmente en `Assets/Scripts/` si se siguen convenciones modulares).

1. Usa la terminal bash del usuario (herramientas `list_dir`, `find_by_name`, `grep_search` o similares si están disponibles, u obtén un listado nativamente para enumerar tus intervenciones) para identificar la ubicación de todos los archivos `.cs` en el proyecto. Excluye carpetas ocultas y archivos asociados a paquetes (`Library`, `Packages`).
2. Examina porciones representativas o problemáticas buscando:
    - Uso de LINQ repetitivo o constructores innecesarios en ciclos `Update()` / `FixedUpdate()` **(unity-performance-optimizer)**.
    - Ausencia de interfaces o uso reiterado de "God-Classes" que heredan directamente con lógica embebida de Unity en vez de puramente C# **(unity-clean-arch)**.
    - Presencia o refactorización para Singletons acoplados, sugiriendo Scriptable Objects Architecture **(unity-clean-arch)**.
    - Valida si existen carpetas `EditMode` y `PlayMode`; en su ausencia o la de las contrapartes de prueba, levanta alertas **(unity-tdd-guardian)**.
3. Escribe las salidas y hallazgos en un artefacto estructurado o directamente al usuario detallando:
   - **Clases Críticas Detectables:** (p.e. `PlayerController.cs` con exceso de trabajo)
   - **Problemas de Performance Potenciales**
   - **Fallas de Acoplamiento y Singletons**
   - **Cobertura de Tests Ausente o Deficiente**
4. Para al menos 1 clase encontrada, propón y muestra al usuario una visión refactorizada incorporando `Interface Dependency`, Mocks listos para los tests y extracción del peso del `Update()`.
