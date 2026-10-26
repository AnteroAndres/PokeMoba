# Unity Character Customizer — prototipo uGUI

## Instalación
1. Descomprime y copia `Assets/CharacterCustomizer` dentro de `Assets/` de tu proyecto Unity.
2. En tu escena existente crea un GameObject vacío `CharacterCustomizer`.
3. Añade el componente `CharacterCustomizerUI`.
4. Arrastra tu personaje 3D al campo `Character Root` y los Renderers de piel y cabello a los campos correspondientes.
5. Si tienes peinados como GameObjects, asígnalos en `Hairstyles`.
6. Configura `On Save` y `On Cancel` desde el Inspector según el flujo de tu juego.
7. Ejecuta en modo **Landscape**. La interfaz se crea automáticamente al iniciar.

## Incluido
- Layout adaptable uGUI para móvil horizontal: cabecera, roles, categorías, paleta de piel/cabello, opciones de peinado, botones y rotación.
- Integración opcional con personaje existente; la escena 3D permanece visible detrás de la UI.
- Guardado local de selección con PlayerPrefs y eventos UnityEvent.
- Imagen de referencia en `Documentation/referencia.png` (solo referencia, no se usa como UI).

## Limitaciones reales
- No incluye modelos 3D, escenario, texturas originales, tipografías licenciadas ni iconografía exacta.
- Los controles de ropa y accesorios son espacios de integración, no sistemas completos.
- Las miniaturas de peinado son marcadores numéricos hasta que se conecten recursos reales.
- PlayerPrefs guarda índices, no persistencia segura ni sincronización con servidor; debes aplicar las selecciones al cargar.
- Para shaders URP/HDRP, configura el nombre de propiedad de color correspondiente en el Inspector.
- Usa Unity uGUI (`UnityEngine.UI`) y el sistema EventSystem de Unity; si la escena no tiene uno, crea `GameObject > UI > Event System`.
- Si tu proyecto usa el nuevo Input System, verifica que el EventSystem tenga `InputSystemUIInputModule`.

## Arquitectura recomendada
Separa después este prototipo en `CharacterAppearanceState`, `AppearancePresenter`, `CharacterPreviewAdapter` y un repositorio de persistencia. El script actual es un punto de partida visual, no una arquitectura de producción.
