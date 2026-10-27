# Modelos 3D de Pokémon

Si pones aquí un prefab con el nombre `NNN_clave` (número de Pokédex con 3 cifras y la clave de PokeAPI),
el juego lo usa en lugar del modelo generado con formas simples. Ejemplos:

- `001_bulbasaur.prefab`
- `006_charizard.prefab`
- `025_pikachu.prefab`

Reglas:

- El modelo mira hacia +Z y tiene los pies en Y = 0. El juego lo escala solo a la altura correcta.
- Se le quitan los colliders automáticamente.
- Requisitos de polígonos, texturas, esqueleto y animaciones: GDD §19.3.

## Modelos importados desde PoGo-3D-Assets

El juego carga automáticamente los 151 modelos de Kanto de `Imported/NNN_clave/Model`.
Un prefab personalizado `NNN_clave` en esta carpeta sigue teniendo prioridad.
Los modelos procedurales se usan únicamente si falta el archivo importado.

La importación contiene 136 FBX, 14 OBJ con sus MTL y un DAE (Seaking), además de
677 texturas. Los FBX de las carpetas `placeholder` del ZIP son imágenes planas:
se han usado las mallas OBJ/DAE reales de esas especies. Seaking no incluye textura
en el archivo original; su material usa colores por región.

`PokemonModelImporter` configura las imágenes como texturas 2D y asigna cada mapa
al material exacto de su especie mediante `import.json`. Se conservan el esqueleto,
las UV y las animaciones que incluya el modelo. El adaptador ajusta la altura,
centra el modelo, coloca su base sobre el suelo y añade respiración/balanceo cuando
no hay clips. Este paquete no contiene un conjunto completo de animaciones de
combate ni de texturas shiny; los shiny usan un tinte sobre una copia del material.

Para repetir la importación desde la raíz del repositorio:

```powershell
python Tools/import_pokemon_models.py C:/Users/andres/Downloads/PoGo-3D-Assets-master.zip
python Tools/import_pokemon_models.py C:/Users/andres/Downloads/PoGo-3D-Assets-master.zip --check
```

La comprobación contrasta los modelos y texturas con el ZIP, verifica las 151
claves de la Pokédex, la presencia de geometría y las referencias de texturas.
En Unity: **PokeMoba → Validar modelos de Pokemon importados** comprueba la carga,
los materiales y el tamaño sobre una instancia por especie. El informe se guarda
en `tmp/pokemon-import/unity-validation.txt` en la raíz del repositorio.

`Imported/SOURCE_README.md` conserva la documentación y las procedencias del paquete
suministrado; `Imported/import-report.json` registra el archivo fuente y SHA-256 de
cada modelo. Los originales del ZIP no se modifican.
## Paquete con animaciones (`pokemon-master`)

`Animated/NNN_clave/Model.fbx` contiene los modelos convertidos desde los GLB de
`pokemon-master/public/models`, con sus texturas, esqueleto e icono. El juego da
prioridad al prefab personalizado, luego al paquete Animated, al paquete Imported
y finalmente al modelo procedural.

Los 151 Pokémon tienen `idle`, `walk`, `run`, `attack` y `happy`. 114 también
incluyen `sleep`: se conservan las **869 animaciones originales**, sin fabricar
clips para las especies que no los tienen. `PokemonAnimationDriver` elige reposo,
caminar o correr según la velocidad real y reproduce ataque al recibir el evento
`AbilityCaster.Casted`. El ataque se ajusta a la preparación del movimiento, y una
esquiva lo interrumpe. `Celebrate()` y `SetSleeping(bool)` dejan disponibles alegría
y sueño para los modos que los necesiten. Las animaciones no desplazan la raíz de
combate ni reemplazan los cálculos de daño.

La conversión usa Blender 4.5, que está instalado localmente. Las acciones GLB
contienen múltiples slots: se organizan en una sola línea temporal sincronizada y
se hornean a FBX. `import.json` conserva los intervalos y nombres para que Unity
los separe en clips Legacy. Las texturas base y normales se asignan a cada material;
las imágenes empaquetadas JPEG se recodifican como PNG. Los widgets de huesos que
crea Blender se excluyen del modelo exportado.

```powershell
& 'C:/Program Files/Blender Foundation/Blender 4.5/blender.exe' -b -t 2 --python-exit-code 1 --python Tools/import_animated_pokemon.py -- C:/Users/andres/Downloads/pokemon-master.zip
python Tools/validate_animated_pokemon.py C:/Users/andres/Downloads/pokemon-master.zip
```

En Unity, **PokeMoba → Validar Pokemon con animaciones** comprueba las 151 cargas,
los 869 clips y que muestrear `walk` cambie realmente la jerarquía del modelo.
El informe se guarda en `tmp/animated-pokemon/unity-validation.txt`.
La documentación y licencia suministradas se conservan como `SOURCE_README.md`
y `SOURCE_LICENSE` dentro de Animated. Los ZIP originales se mantienen intactos.

### Corrección de tamaño de las mallas con esqueleto

La medida del modelo se calcula con `SkinnedMeshRenderer.BakeMesh(mesh, true)`:
la compensación de escala evita aplicar de nuevo la escala del esqueleto al pasar
sus vértices a coordenadas del padre. Se muestrea primero la pose `idle`. Esto
corrige los modelos diminutos o aparentemente ausentes tras la importación FBX.
La validación de animaciones comprueba ahora altura y apoyo en suelo para las 151
especies, además de que la malla no colapse al muestrear los 869 clips. El menú
**PokeMoba → Diagnosticar apariencia Pokemon** genera una lámina de 16 especies y
medidas por animación en `tmp/pokemon-visual/`.
