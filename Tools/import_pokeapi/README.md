# Importador de PokeAPI

Descarga de [PokeAPI](https://pokeapi.co/) los datos de los Pokémon y los guarda en un JSON que lee Unity.

## Requisitos

**Python 3.8 o superior** (<https://www.python.org/downloads/>; al instalar, marcar *Add python.exe to PATH*). No necesita librerías extra.

## Uso

Desde la carpeta `PokeMoba`:

```bash
python Tools/import_pokeapi/import_pokeapi.py
```

La primera vez hace unas 900 peticiones a PokeAPI y tarda unos minutos. Las respuestas se guardan en `Tools/import_pokeapi/cache/`, así que volver a ejecutarlo es casi instantáneo.

Opciones:

| Opción | Para qué |
|---|---|
| `--range 1-251` | Otro rango de la Pokédex (por ejemplo, hasta la 2.ª generación) |
| `--workers 4` | Menos peticiones en paralelo si PokeAPI da errores |
| `--reset-overrides` | Vuelve a generar `overrides.json` desde cero (**se pierden los cambios a mano**) |

## Qué genera

Los dos archivos van en `UnityProject/Assets/_Project/Resources/Data/` para que Unity los cargue también en el APK (`PokemonDatabase` los lee al arrancar).

### `pokeapi_001-151.json`

Se regenera cada vez; no editarlo a mano.

| Sección | Contenido |
|---|---|
| `species` | Número, nombre en español e inglés, categoría, descripción de la Pokédex, tipos, estadísticas base, habilidades, tasa de captura, experiencia, crecimiento, género, altura, peso, hábitat, color, grupos huevo, legendario/singular, evoluciones (con método: nivel, piedra, intercambio, amistad…), movimientos que aprende, formas Mega (tipos, estadísticas y megapiedra) y si tiene Gigamax |
| `moves` | Nombre, tipo, categoría (Fisico, Especial, Estado), potencia, precisión, PP, prioridad, estado que provoca y probabilidad, cambios de estadísticas, drenaje, curación, **forma sugerida** para el combate en tiempo real y descripción corta |
| `abilities` | Nombre y descripción |
| `items` | Piedras evolutivas, objetos de intercambio y megapiedras con nombre en español |

- Los **tipos** usan los mismos nombres que el enum `PokemonType` de Unity (`Fuego`, `Electrico`, `Psiquico`…).
- Los **movimientos de cada especie** se toman del juego más reciente que los tenga (Legends: Z-A, luego Escarlata/Púrpura, Espada/Escudo, Let's Go…). El campo `learnset.versionGroup` dice de cuál salió.
- Las **evoluciones** solo incluyen especies dentro del rango (por ejemplo, Golbat → Crobat no aparece con 1–151).

### `overrides.json`

Ajustes **hechos a mano** que no vienen de PokeAPI. Solo se crea si no existe; después el importador nunca lo toca (salvo con `--reset-overrides`).

```jsonc
"charmander": {
  "role": "Atacante",          // Atacante, Luchador, Defensor, Veloz, Soporte (rol del MOBA)
  "diet": "FuegoCalor",        // dieta (GDD §14.3)
  "favoriteFlavor": null,      // picante, seco, dulce, amargo, ácido
  "zone": 0,                   // zona de Kanto (GDD §6); 0 = inicial; null = se obtiene evolucionando
  "basicMove": null,           // ataque básico (clave de PokeAPI, p. ej. "scratch"); null = automático
  "skills": [],                // 2 habilidades, p. ej. ["ember", "flamethrower"]; vacío = automático
  "ultimate": null             // definitiva (todavía no se usa)
}
```

**Elección automática de movimientos** (cuando no se indican): se toman los movimientos de daño que aprende hasta el nivel 50 (y los de MT si son pocos).
- **Ataque básico**: el de mejor potencia × STAB entre los de potencia ≤ 60 que sean cuerpo a cuerpo o proyectil.
- **Habilidades**: las 2 de mejor potencia × STAB × precisión; la segunda, de otra forma si no pierde mucho daño.
- Si no hay suficientes (Ditto, Kakuna…), se rellena con **Forcejeo**.

La forma decide alcance, radio y preparación, y la potencia decide la recarga (`MoveFactory.cs`).

En `moves` se corrige la forma de un movimiento si la sugerida no convence:

```json
"moves": {
  "tackle": { "shape": "Dash" },
  "flamethrower": { "shape": "Cone", "cooldown": 6 }
}
```

Campos admitidos en `moves`: `shape` (Melee, Projectile, Cone, GroundArea, Dash), `range`, `radius` (en Cone es el ángulo) y `cooldown`.

Al terminar, el script avisa de las especies base que **todavía no tienen zona** para repartirlas.

## Nota

PokeAPI es gratuito y pide un uso responsable: no subir `--workers` por encima de 6 ni borrar la caché sin motivo.
