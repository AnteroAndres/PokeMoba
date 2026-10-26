# PokeMoba

Juego 3D para celular con combate Pokémon en tiempo real: Aventura (captura y medallas), Duelo 1v1 y MOBA.

El diseño completo está en **[GDD.md](GDD.md)**.

## Estado

**Versión jugable con los 4 modos y los 151 Pokémon de Kanto.** Los datos salen de PokeAPI (`Tools/import_pokeapi`).

### Menú principal

| Modo | Qué es | Se abre |
|---|---|---|
| **Aventura** | Estilo Legends Z-A / Arceus (GDD §7.1): **controlas al entrenador**, tu Pokémon te sigue y **obedece tus órdenes** (4 movimientos con recarga contra el objetivo **fijado**); el entrenador esquiva rodando y puede recibir golpes. Creas a tu entrenador (colores de piel, pelo, gorra, camiseta y pantalón), eliges inicial en el laboratorio de Oak y exploras las **16 zonas de Kanto**, cada una con su escenario (pradera, bosque, cueva, ciudad, playa, torre fantasma, safari, central eléctrica, nieve, volcán, Liga). Pokémon salvajes que pasean y pelean, **captura con Poké / Super / Ultra / Master Ball** (probabilidad en pantalla), shinies, experiencia, subida de nivel, **evolución** (Eevee te deja elegir), **8 gimnasios con medallas**, **Liga Pokémon** (Alto Mando + Campeón), legendarios como jefes, Centro Pokémon, tienda, equipo y PC | Siempre |
| **Duelo 1v1** | Tus Pokémon capturados contra la IA a nivel 50, en 10 arenas que se abren con la Aventura | Al tener inicial |
| **MOBA 3v3** | Tú + 2 bots contra 3 bots. 3 carriles, río y jungla, una torre por carril y un núcleo por equipo, oleadas de súbditos, niveles de partida 1–15 con **evolución en la partida**, reaparición, 10 minutos | Medalla Trueno |
| **Práctica** | Cualquiera de los 151 contra cualquiera, sin progreso | Siempre |
| **Pokédex** | Desconocidos, vistos y capturados; ficha con modelo 3D, descripción, estadísticas, dieta, zona, evoluciones y movimientos | Siempre |

**Modo pruebas** (botón del menú principal): abre todas las zonas, arenas, el MOBA y todos los Pokémon en Duelo y MOBA, para probar sin tener que avanzar.

### Modelos

Cada Pokémon se genera con formas simples según **su forma de cuerpo y su color reales** (PokeAPI): cuadrúpedo, bípedo, humanoide, ave, insecto alado, serpiente, pez, bola, gelatina, tentáculos, varias cabezas, caparazón… con detalles por tipo (llama en la cola, bulbo, caparazón, rayo, cuernos de dragón, gema psíquica, púas de veneno…) y animaciones (respirar, aletear, andar, cola, flotar). Los shinies cambian de color.

Para usar un **modelo 3D real**, basta con poner el prefab en `Assets/_Project/Resources/Models/Pokemon/` con el nombre `NNN_clave` (por ejemplo `006_charizard`); ver el `LEEME.md` de esa carpeta.

### Combate (común a todos los modos)

- Elección de tu Pokémon y del rival entre los 151 (o rival aleatorio), y de la dificultad
- Movimientos reales de cada especie según su nivel, convertidos a combate en tiempo real (ajustables en `overrides.json`)
- Joystick flotante (mitad izquierda de la pantalla)
- Ataque básico + 2 habilidades: un toque apunta solo; arrastrar apunta a mano con guía en el suelo
- Esquiva con invulnerabilidad y estamina (toque = hacia donde te mueves; arrastrar = dirección elegida)
- Tabla de tipos moderna, STAB, críticos y fórmula de daño del GDD
- Quemadura y envenenamiento
- Cinco formas de movimiento: cuerpo a cuerpo, proyectil, cono, área con aviso previo y embestida
- IA que se acerca, rodea, ataca y esquiva proyectiles y áreas
- Números de daño, "¡Súper eficaz!" y "¡Esquivado!"

## Preparar el entorno (los dos)

1. **Unity Hub**: descargarlo desde <https://unity.com/download> e iniciar sesión con una cuenta de Unity (la licencia Personal es gratis).
2. **Unity 6 LTS**: en Unity Hub → *Installs* → *Install Editor* → elegir la última versión **6000.x LTS**. Marcar estos módulos:
   - **Android Build Support** (con *OpenJDK* y *Android SDK & NDK Tools*)
   - *iOS Build Support* (solo si se va a compilar para iPhone desde una Mac)
3. **Editor de código**: Visual Studio Community 2022 (se ofrece al instalar Unity) o VS Code con la extensión de C# / Unity.
4. **Git** y **Git LFS**: <https://git-scm.com/> y luego `git lfs install`.
5. **Celular Android** en modo desarrollador con *Depuración USB* activada, para probar los APK.

## Abrir el prototipo

1. Unity Hub → *Projects* → *Add* → *Add project from disk* → elegir la carpeta **`UnityProject`**.
2. Si Unity Hub dice que falta la versión del editor, elegir la **Unity 6 LTS** que instalaste.
3. La primera apertura tarda unos minutos (Unity genera la carpeta `Library`).
4. Si aparece el aviso **"This project is using the new input system package…"**, pulsar **Yes**. Unity se reinicia.
5. Al terminar de cargar, se crea sola la escena `Assets/_Project/Scenes/Prototype.unity` y queda abierta. Si no, menú **PokeMoba → Crear escena del prototipo**.
6. Pulsar **Play**.

**Controles en el editor:** WASD mover · J K L I los 4 movimientos · Espacio esquivar · F (o Tab) fijar objetivo en la Aventura. Para probar los controles táctiles con el ratón, haz clic y arrastra en la mitad izquierda (joystick) o sobre los botones.

## Probar en el celular (Android)

1. *File → Build Profiles* → **Android** → *Switch Platform*.
2. Conectar el celular por USB (con *Depuración USB* activada).
3. *Build and Run*. El juego se abre en horizontal.

## Estructura

```
PokeMoba/
├── GDD.md                Documento de diseño
├── README.md             Este archivo
├── Tools/import_pokeapi/ Descarga los 151 Pokémon de PokeAPI (ver su README)
└── UnityProject/
    ├── Packages/manifest.json
    └── Assets/_Project/
        ├── Resources/
        │   ├── Data/            Datos de PokeAPI (generado) y overrides.json (a mano)
        │   └── Models/Pokemon/  Modelos 3D reales opcionales (NNN_clave)
        ├── Scenes/       Se crea al abrir el proyecto
        └── Scripts/
            ├── Data/       Tipos, especies, movimientos, experiencia, captura, Pokémon del jugador
            │   └── Pokedex/  Carga los JSON y convierte los movimientos a tiempo real
            ├── Combat/     Estadísticas, daño, movimiento, esquiva, habilidades, límites del escenario
            ├── Input/      Joystick, botones de habilidad, apuntado, teclado
            ├── AI/         Rival, salvajes, bots y súbditos del MOBA, torres
            ├── World/      Modelos de Pokémon y entrenador, escenarios por bioma, mapa del MOBA
            ├── Game/       Partida guardada, zonas de Kanto, gimnasios, Liga, desbloqueos, evolución
            ├── Modes/      Aventura, Duelo, MOBA, Práctica, Pokédex y piezas compartidas
            ├── UI/         HUD, menús, cuadrícula de especies, textos flotantes
            ├── Prototype/  Arranque (menú principal), cámara y fábrica de Pokémon
            └── Editor/     Crea la escena y configura Android
```

Toda la escena se construye por código desde `PrototypeGame`, así que no hace falta tocar nada en el editor para probar. La partida se guarda en `Application.persistentDataPath/pokemoba_save.json` (se puede borrar desde el menú).

## Dónde ajustar el combate

- **Números generales** (vida, velocidad, esquiva, estamina, estados): `Scripts/Combat/CombatTuning.cs`
- **Movimientos de cada especie y formas de movimientos concretos**: `Resources/Data/overrides.json` (ver [Tools/import_pokeapi/README.md](Tools/import_pokeapi/README.md))
- **Cómo se convierte un movimiento a tiempo real** (alcance, radio, recarga según forma y potencia): `Scripts/Data/Pokedex/MoveFactory.cs`
- **Dificultad de la IA**: `Difficulty` en `Scripts/Modes/ArenaBattle.cs`
- **Zonas, niveles, gimnasios y Liga**: `Scripts/Game/ZoneCatalog.cs`
- **Experiencia** (multiplicador para que no haga falta entrenar tanto): `Scripts/Data/Leveling.cs`
- **MOBA** (duración, oleadas, niveles de partida, evolución): constantes al principio de `Scripts/Modes/MobaMode.cs`

## Pendiente

Respecto al [GDD](GDD.md#20-hoja-de-ruta), todavía falta:

- **Modelos 3D reales** (ahora son figuras generadas; ver «Modelos»)
- **Megaevolución en combate** (el Mega-Aro ya se consigue al ganar la Liga)
- **Multijugador online** (Duelo y MOBA son contra la IA)
- Comida y bayas, ropa desbloqueable por zona, tareas de la Pokédex, piedras evolutivas en la tienda (ahora los Pokémon que evolucionan con piedra lo hacen a nivel 30 y los de intercambio a nivel 37)
