# Nuevo paquete de entrenadores

Origen: `C:/Users/andres/Downloads/trainers.7z` (33 modelos TRMDL con sus mallas,
esqueletos, materiales y texturas). Roshi y Ash conservan sus modelos, clips,
texturas y claves de selección.

El selector incluye Roshi, Ash y las 33 entradas de `TrainerCharacterCatalog`.
La lista tiene desplazamiento vertical para mantener las miniaturas legibles.
Las 15 claves anteriores se resuelven hacia su equivalente de este paquete sin
reescribir la partida guardada. Las claves Roshi (`Procedural`) y Ash (`Ash/Ash`)
no se redirigen.

La conversión genera un FBX de máxima resolución por entrenador con su esqueleto.
El shader con paletas del formato original se hornea en texturas de color PNG
para que Unity no dependa del shader de Blender. Se excluyen los LOD de menor
resolución. `PackageTrainerMotion` ajusta la pose de brazos, centra el modelo a
1,95 m y mueve piernas y brazos con la velocidad del jugador; este archivo no
incluye clips de caminar/correr originales en formato TRANM/GFBANM.

Las miniaturas se renderizaron en Unity y están en
`Models/Trainers/Icons/Package/`. `import-report.json` registra cada fuente,
SHA-256, número de vértices, huesos y mapas usados. El 7z original permanece intacto.

## Reproducir la conversión

`Tools/import_trainer_package.py` se ejecuta con Blender 4.5. La fuente extraída y
las dependencias de conversión están aisladas en `tmp/trainers-package/`; no se
instaló ningún complemento global en Blender.

El lector TRMDL utilizado es [Pokémon Switch V3](https://github.com/ChicoEevee/Pokemon-Switch-Model-Importer-Blender),
de ChicoEevee y colaboradores, descargado en `tmp/trainers-package/converter`.
Se impide que realice descargas durante la conversión; utiliza el shader incluido
en su propio repositorio. Dependencias locales: `flatbuffers`, `requests` y NumPy
de Blender. Los créditos del conversor se conservan en su README original.

```powershell
tar -xf C:/Users/andres/Downloads/trainers.7z -C tmp/trainers-package/source
& 'C:/Program Files/Blender Foundation/Blender 4.5/blender.exe' -b -t 2 --python-exit-code 1 --python Tools/import_trainer_package.py
```

En Unity: **PokeMoba → Validar nuevo paquete de entrenadores** comprueba carga desde
el catálogo, materiales, tamaño, apoyo sobre el suelo y deformación al caminar.
También genera una galería y las 33 miniaturas. La prueba se ejecutó en una copia
temporal del proyecto para no cerrar la sesión abierta del editor ni modificar
la partida del usuario.

## Auditoría de animaciones del paquete original

Se revisó también el contenido de `trainers.7z`: contiene tablas de recursos,
estados, parámetros y controles (`.tracr`, `.tracs`, `.tracp`, `.tracn`, `.tracl`,
`.tralk`, `.trbik`), pero ningún archivo de clip `.tranm` ni `.gfbanm`. Las tablas
mencionan animaciones como `tr0036_00_02001_wait01_loop.tranm` y otros movimientos;
esos archivos referenciados no están incluidos en el 7z.

`animation-audit.json` enumera las referencias faltantes de cada modelo. Por eso
no se añadieron ni se simularon como originales clips de carrera, salto o gestos.
Se conserva la locomoción procedural de `PackageTrainerMotion`. Ash mantiene sus
FBX originales de reposo y carrera; Roshi conserva su configuración anterior.

Para repetir la comprobación:

```powershell
python Tools/audit_trainer_animations.py
```

Para importar las animaciones originales hace falta el paquete de clips `.tranm`
correspondiente a estos esqueletos (incluidos los clips compartidos `cb9999`).
