# Galaxy Eye 🌌👁️

**Galaxy Eye** es una suite de terapia visual interactiva desarrollada en Unity, diseñada para pacientes con Insuficiencia de Convergencia (CI) y otros trastornos oculomotores. Utiliza tecnología de seguimiento ocular (Eye Tracking) de **Tobii** para proporcionar ejercicios lúdicos y precisos que ayudan a mejorar la salud visual.

## 👥 Autores
Este proyecto ha sido desarrollado por:
*   **Johan Cala**
*   **Jesús Ortega**
*   **Alex Taquila**

---

## 🚀 Características Principales
*   **Seguimiento Ocular en Tiempo Real**: Integración profunda con Tobii Eye Tracker para una interacción sin manos.
*   **Gamificación de la Terapia**: Ejercicios visuales diseñados como juegos espaciales para aumentar la adherencia al tratamiento.
*   **Gestión de Pacientes Segura**: Sistema de perfiles individuales (JSON) para garantizar la integridad y privacidad de los datos.
*   **Historial de Rendimiento**: Dashboard detallado con métricas de precisión, tiempo de vuelo, puntuación y nivel alcanzado.
*   **Seguridad de Sesión**: Validación obligatoria de login y calibración en todas las actividades.

---

## 🎮 Actividades Incluidas

### 1. 🌀 Laberinto Estelar
Un ejercicio de rastreo ocular donde el paciente debe guiar una nave a través de un camino complejo, mejorando el control de los micromovimientos oculares.

### 2. ✨ Estrella Lineal
Enfocado en movimientos sacádicos y de seguimiento lineal, siguiendo trayectorias predefinidas para fortalecer los músculos oculares.

### 3. 🏁 Carrera Ocular
Desafía la velocidad de reacción y la precisión del salto ocular entre diferentes objetivos en movimiento.

### 4. 🎈 Explosión Estelar
Un ejercicio dinámico de fijación y respuesta rápida donde el paciente debe "explotar" objetivos mediante la mirada.

---

## 🛠️ Requisitos Técnicos
*   **Unity**: Versión 2022.3 LTS o superior.
*   **Hardware**: Tobii Eye Tracker 5 o compatible.
*   **SDK**: Tobii Pro SDK for Unity.

---

## 📂 Estructura del Proyecto
*   `Assets/Scripts/`: Lógica central del juego y gestión de datos.
*   `Assets/Scenes/`: Escenas de juego, menús y calibración.
*   `Assets/Prefabs/`: Elementos reutilizables de la interfaz y gameplay.
*   `persistentDataPath/Pacientes/`: Almacenamiento individual de registros médicos/partidas.

---

## 🔧 Instalación y Uso
1. Clonar el repositorio.
2. Abrir el proyecto en Unity Hub.
3. Asegurarse de tener el hardware Tobii conectado y calibrado mediante el software oficial.
4. Iniciar desde la escena `Login`.

---
© 2026 Galaxy Eye Team - Terapia Visual de Próxima Generación.
