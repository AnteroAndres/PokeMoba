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
