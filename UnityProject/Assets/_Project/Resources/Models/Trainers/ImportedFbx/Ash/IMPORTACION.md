# Ash con animaciones originales

Fuente local: `Gen1Recomp-Character-Selector-main.zip`, carpeta `source/ash`.
Los dos FBX se conservan sin convertir ni alterar sus esqueletos:

- `Ash.fbx`: `Standing Idle.fbx`, reposo (5,72 segundos).
- `Run.fbx`: `Slow Run.fbx`, carrera suave (0,72 segundos).
- Tres atlas PNG originales para cara, cuerpo y brazos/gorra/cabello.

Selecciona **Aventura → Personalizar personaje → Elige tu personaje → Ash** y
pulsa **Guardar apariencia**. La clave guardada es `Ash/Ash`; no sustituye al
personaje que ya esté elegido en la partida.

`AshModelImporter` importa los clips como Legacy y asigna los atlas dentro de
esta carpeta. `AshTrainerMotion` cambia entre reposo y carrera según la velocidad
real del entrenador. El movimiento horizontal de la animación se mantiene en el
sitio para que no desplace al personaje independientemente de los controles.
El salto de exploración usa el arco del controlador existente; este paquete no
incluye clips específicos de salto, ataque o esquiva.

La miniatura del selector está en `Models/Trainers/Icons/Ash/Ash.png` y se generó
renderizando el modelo en Unity. `source-manifest.json` registra los SHA-256 de
los originales; `SOURCE_README.md` y `SOURCE_LICENSE_NOTES.md` conservan la
documentación suministrada.

**PokeMoba → Validar Ash animado** comprueba la carga desde TrainerModelBuilder,
materiales, altura, deformación de la malla con la carrera y ausencia de avance
horizontal por el clip. Para ejecutar por lotes sobre un proyecto de prueba:

```powershell
Unity.exe -batchmode -quit -projectPath RUTA_PROYECTO -executeMethod PokeMoba.Editor.AshImportValidation.Run -ashValidationOutput RUTA_INFORME
```
