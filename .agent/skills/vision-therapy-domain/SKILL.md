---
name: vision-therapy-domain
description: Instrucciones y constantes matemáticas críticas respecto a las dimensiones oftalmológicas de Insuficiencia de Convergencia (CI) a ser usados por cualquier mecánica en entorno 3D.
---

# Vision Therapy Domain

Use when: El proyecto demande posicionar elementos clave del gameplay ("Targets"), calcular distancias entre el usuario interactivo y los modelos 3D del entorno del juego Parallax, o modelar un estado de sesión terapéutica para Insuficiencia de Convergencia (CI).

Eres el especialista clínico validando las distancias de juego para el tratamiento real mediante el espacio virtual de los pacientes. La rigurosidad espacial es obligatoria.

## ESTÁNDAR 1: Normalización Métrica (La Unidad)
- **Regla Estricta:** Todo cálculo clínico interno (como el umbral y distancias biomodulares calculados frente a cara) **debe validarse siempre y expresarse en centímetros (cm)**, aunque la API local de posición 3D en Unit `Vector3` naturalmente opere su escala preestablecida de un mundo general en Metros (meters). 
- Si un componente en tu código va a chequear la cercanía al paciente: convierte los metros de Unity *implícitos* y coméntalo en variables finalizadas con suftijo (e.g., `float targetDistanceCm`).

## ESTÁNDAR 2: Regla de Oro de Seguridad Clínica (Near Point of Convergence - NPC)
El NPC representa qué tan cerca puede mirar cómodamente el paciente antes de perder visión de fusión. (El ojo "Break").
- **Prohibición:** La IA **AQUÍ** o **EN IMPLEMENTACIONES DE UNITY** nunca debe sugerir crear instancias virtuales (spawners), o solicitar posicionar a un objetivo / estímulo móvil, para el entrenamiento de vergencia *a una distancia menor de la documentada históricamente por el perfil NPC del Usuario Actual de juego/terapia*.
- Por debajo de este rango solo promoverá fatiga destructiva, dolor ocular o abandono.

## ESTÁNDAR 3: Fórmula Clínica (Vergence Angle)

Calcula de antemano siempre la desviación a la que estarás obligando mover anatómicamente el grado de ojo del usuario mediante tu programación, utilizando estrictamente trigonometría predefinida:
$$\theta = 2 \cdot \arctan\left(\frac{IPD}{2z}\right)$$

Donde:
*   $\theta$ es tu Ángulo de Vergencia resultativo, usualmente requerido en grados.
*   $IPD$ corresponde a la Distancia Interpupilar en espacio (generalmente en milímetros - *recuerda cuadrar unidades con $z$*).
*   $z$ denota la distancia en su respectiva recta ortogonal hasta el target estático del juego.

Asegúrate de repasar los dominios reales al aplicar esto programáticamente en scripts leyendo: `[VER ARCHIVO: resources/clinical_thresholds.md]`
