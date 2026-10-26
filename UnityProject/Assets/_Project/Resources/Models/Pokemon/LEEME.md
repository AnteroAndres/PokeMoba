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
