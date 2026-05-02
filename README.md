# Galaxy Eye 🌌👁️

**Galaxy Eye** es una suite de terapia visual interactiva desarrollada en Unity, dirigida a pacientes con Insuficiencia de Convergencia (CI) y otros trastornos oculomotores. Utiliza tecnología de seguimiento ocular avanzada para proponer ejercicios interactivos gamificados, que fomentan la adherencia y mejoran los resultados clínicos.

> Proyecto desarrollado para la asignatura **Sistemas Interactivos**, Grado en [Ingeniería Informática], Universidad de Almería.

---

## 👥 Autores

- **Johan Cala**
- **Jesús Ortega**
- **Alex Taquila**

---

## 🚀 Tecnologías Utilizadas

- **Engine**: [Unity 2022.3 LTS](https://unity.com/releases/editor/whats-new/2022.3.0)
  - Render Pipeline: **Universal Render Pipeline (URP)** v17.3.0
  - **Sistema de Input**: Unity Input System (activo junto a API `Input` Legacy)
- **Lenguajes**: C# (principal), ShaderLab, HLSL, HTML, PowerShell
- **SDK de Seguimiento Ocular**: [Tobii Pro SDK for Unity](https://www.tobiipro.com/product-listing/tobii-pro-sdk/)
  - Compatibilidad: **Tobii Eye Tracker 5** y modelos profesionales compatibles
- **UI**: uGUI (Canvas), [TextMesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest)
- **Persistencia**: Sistema de perfiles paciente en JSON sobre `Application.persistentDataPath`
- **Frameworks de Pruebas**: Unity Test Runner (Edit Mode)

---

## 🛠️ Requisitos Técnicos

- **Unity**: Versión 2022.3 LTS o superior *(recomendado 2022.3.x)*
- **Hardware**: Tobii Eye Tracker 5, compatible con SDK oficial de Tobii Pro
- **Sistema Operativo**: Windows 10/11
- **Otros**: Drivers oficiales de Tobii y software de calibración
- **Recomendado**: Pantalla de al menos 22’’ y buena iluminación frontal

---

## 🎮 Actividades Incluidas

1. **🌀 Laberinto Estelar:** Rastreo ocular a través de un camino laberíntico, entrenando precisión y planificación de movimientos.
2. **✨ Estrella Lineal:** Ejercicio de movimientos sacádicos y seguimiento sobre trayectorias predefinidas.
3. **🏁 Carrera Ocular:** Reto de rapidez ocular, saltando entre objetivos con clicks o fijación.
4. **🎈 Explosión Estelar:** Fijación y respuesta rápida explotando objetos con la mirada.

---

## 📦 Estructura del Proyecto

- `Assets/Scripts/`: Lógica C#, manejo de actividades, datos y flujo de escenas.
- `Assets/Scenes/`: Escenas del juego, menús, procesos de login y calibración.
- `Assets/Prefabs/`: UI y elementos interactivos reutilizables.
- `Assets/TobiiPro/`: SDK de integración con hardware Tobii Eye Tracker.
- `Assets/Tests/EditMode/`: Pruebas automáticas por flujo y parsing directo de scripts.
- `persistentDataPath/Pacientes/`: Progreso y registros individuales en JSON (uno por paciente).

---

## 🔧 Instalación y Puesta en Marcha

1. Clona este repositorio:
    ```sh
    git clone https://github.com/atc757-ual/Sistemas-Interactivos.git
    ```
2. Abre el proyecto desde Unity Hub en la versión recomendada.
3. Instala el **Tobii Pro SDK for Unity** (carpeta `Assets/TobiiPro/` ya incluida).
4. Conecta y calibra el Tobii Eye Tracker usando el software oficial.
5. Abre y ejecuta la escena `Login` para comenzar.
6. Asegúrate de que todas las escenas principales estén registradas en el **Build Settings**.

---

## ✅ Pruebas Automáticas

Ejecuta los tests desde la raíz del proyecto (requiere Unity en el PATH):

```sh
Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath . -logFile -
```
Archivos de test: `Assets/Tests/EditMode/Fase1_NavegacionUI/`

---

## 💡 Notas Técnicas

- **GestorPaciente:** Singleton persistente que lleva el estado y el historial entre escenas.
- **Actividades:** Derivan de `BaseActividad`, auto-vinculan su UI por nombre y aseguran lógica desacoplada.
- **Fallback mouse:** El sistema puede funcionar con ratón en ausencia de hardware Tobii (útil para pruebas).
- **Time.timeScale:** Siempre debe ser reiniciado a `1` al navegar entre escenas.
- **Dual Input System:** Compatibilidad asegurada con New Input System y API clásica para máxima robustez.

---

## 📄 Licencia

Este proyecto está licenciado bajo los términos de la licencia MIT. Consulta el archivo [LICENSE](LICENSE) para más información.

---

© 2026 Galaxy Eye Team — Terapia Visual de Próxima Generación.