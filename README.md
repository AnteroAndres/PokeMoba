# PokeMoba

Juego 3D para celular con combate Pokémon en tiempo real: Aventura (captura y medallas), Duelo 1v1 y MOBA.

El diseño completo está en **[GDD.md](GDD.md)**.

## Estado

**Fase 2 en curso.** Una arena 1v1 contra la IA con **cualquiera de los 151 Pokémon de Kanto** (modelos provisionales hechos con formas simples). Los datos salen de PokeAPI (`Tools/import_pokeapi`).

Qué incluye:

- Elección de tu Pokémon y del rival entre los 151 (o rival aleatorio), y de la dificultad
- Movimientos reales de cada especie a nivel 50, convertidos a combate en tiempo real (ajustables en `overrides.json`)
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

**Controles en el editor:** WASD mover · J ataque · K / L habilidades · Espacio esquivar. Para probar los controles táctiles con el ratón, haz clic y arrastra en la mitad izquierda (joystick) o sobre los botones.

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
        ├── Resources/Data/ Datos de PokeAPI (generado) y overrides.json (a mano)
        ├── Scenes/       Se crea al abrir el proyecto
        └── Scripts/
            ├── Data/       Tipos, tabla de tipos, especies y movimientos
            │   └── Pokedex/  Carga los JSON y convierte los movimientos a tiempo real
            ├── Combat/     Estadísticas, daño, movimiento, esquiva, habilidades, proyectiles, áreas
            ├── Input/      Joystick, botones de habilidad, apuntado, teclado
            ├── AI/         IA del rival
            ├── UI/         HUD, textos flotantes, utilidades de interfaz
            ├── Prototype/  Arranque de la escena, arena, cámara y modelos provisionales
            └── Editor/     Crea la escena y configura Android
```

Toda la escena se construye por código desde `PrototypeGame`, así que no hace falta tocar nada en el editor para probar.

## Dónde ajustar el combate

- **Números generales** (vida, velocidad, esquiva, estamina, estados): `Scripts/Combat/CombatTuning.cs`
- **Movimientos de cada especie y formas de movimientos concretos**: `Resources/Data/overrides.json` (ver [Tools/import_pokeapi/README.md](Tools/import_pokeapi/README.md))
- **Cómo se convierte un movimiento a tiempo real** (alcance, radio, recarga según forma y potencia): `Scripts/Data/Pokedex/MoveFactory.cs`
- **Dificultad de la IA**: `Difficulties` en `Scripts/Prototype/PrototypeGame.cs`

## Siguiente paso

Probar en el celular y ajustar hasta que el combate se sienta bien. Después, resto de la fase 2 del [GDD](GDD.md#20-hoja-de-ruta): shinies y elección del inicial dentro de la Aventura.
