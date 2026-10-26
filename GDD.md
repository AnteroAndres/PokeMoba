# PokeMoba — Documento de Diseño del Juego (GDD)

> Versión 0.1 · 2026-10-23
> Proyecto fan-made, sin fines comerciales. Juego 3D para celular (Android primero) con combate en tiempo real, inspirado en Pokémon Legends: Z-A y Pokémon Unite.

---

## Índice

1. [Visión](#1-visión)
2. [Pilares de diseño](#2-pilares-de-diseño)
3. [Tecnología](#3-tecnología)
4. [Inicio del juego](#4-inicio-del-juego)
5. [Progresión: mapa que crece](#5-progresión-mapa-que-crece)
6. [Kanto: zonas, medallas y desbloqueos](#6-kanto-zonas-medallas-y-desbloqueos)
7. [Modos de juego](#7-modos-de-juego)
8. [Sistema de combate](#8-sistema-de-combate)
9. [Captura y Poké Balls](#9-captura-y-poké-balls)
10. [Shinies](#10-shinies)
11. [Evolución y megaevolución](#11-evolución-y-megaevolución)
12. [Personalización](#12-personalización)
13. [Mochila, objetos y economía](#13-mochila-objetos-y-economía)
14. [Alimentación](#14-alimentación)
15. [Pokédex](#15-pokédex)
16. [Otros sistemas](#16-otros-sistemas)
17. [Arquitectura del proyecto (Unity)](#17-arquitectura-del-proyecto-unity)
18. [Datos de especies y movimientos](#18-datos-de-especies-y-movimientos)
19. [Arte 3D y animación](#19-arte-3d-y-animación)
20. [Hoja de ruta](#20-hoja-de-ruta)
21. [Reparto de trabajo](#21-reparto-de-trabajo)
22. [Ideas para el futuro](#22-ideas-para-el-futuro)
23. [Nota legal](#23-nota-legal)

---

## 1. Visión

Un juego de Pokémon **3D para celular** donde todo el combate es **en tiempo real**: te mueves, apuntas, esquivas y lanzas ataques. No hay turnos.

Tiene tres formas de jugar sobre el mismo sistema de combate:

- **Aventura (Arcade)**: explorar Kanto, capturar Pokémon y ganar medallas.
- **Duelo 1v1**: tu Pokémon contra otro en una arena.
- **MOBA**: batallas por equipos estilo Dota / Pokémon Unite.

**Lo que capturas en la Aventura es lo que puedes usar en el Duelo y en el MOBA.** Así la exploración alimenta a los modos competitivos.

Se empieza solo con la **1.ª generación (151 Pokémon)**. Las demás regiones llegan después como expansiones, con nuevas zonas y especies.

---

## 2. Pilares de diseño

1. **Combate que se siente bien en pantalla táctil.** Si esquivar y apuntar no es divertido con los dedos, nada más importa. Es lo primero que se construye y se prueba.
2. **Un solo núcleo de combate.** Los tres modos usan las mismas criaturas, habilidades y controles. Cada modo solo cambia el mapa y las reglas.
3. **Todo es data-driven.** Agregar un Pokémon, un movimiento, una zona o una prenda es agregar datos y un modelo, sin tocar código.
4. **Progresión visible.** El mapa crece, aparecen Pokémon nuevos y se desbloquea ropa. El jugador siempre ve qué es lo siguiente.
5. **Rendimiento en gama media.** El objetivo es 30 FPS estables en un Android de gama media (y 60 FPS en gama alta).

---

## 3. Tecnología

| Área | Elección | Motivo |
|---|---|---|
| Motor | **Unity 6 LTS** | El más maduro para 3D en celular |
| Render | **URP** (Universal Render Pipeline) | Optimizado para móvil |
| Lenguaje | **C#** | |
| Entrada | **Input System** de Unity + controles táctiles propios | Joystick virtual y botones con arrastre para apuntar |
| Cámara | **Cinemachine** | Cámara isométrica y lock-on |
| Carga de modelos | **Addressables** | No cargar 151 modelos a la vez; descargar regiones después |
| Multijugador | **Photon Fusion 2** (alternativa: Netcode for GameObjects + Relay/Lobby) | Probado en juegos móviles de acción; plan gratis de 20 jugadores simultáneos para pruebas |
| Datos | **PokeAPI** → JSON → ScriptableObjects | Script de importación automático |
| Guardado | JSON local (fase 1) → nube (más adelante) | |
| Control de versiones | **Git + Git LFS** (GitHub o GitLab) | LFS para modelos, texturas y audio |
| Plataforma | Android primero, iOS después | |

---

## 4. Inicio del juego

1. **Pantalla de título** → Nueva partida.
2. **Creación del entrenador** (personalización básica, ver [§12](#12-personalización)):
   - Nombre
   - Cuerpo / tono de piel
   - Peinado y color de pelo
   - Color de ojos
   - Un conjunto de ropa inicial (a elegir entre 2–3)
3. **Laboratorio del Profesor Oak** (Pueblo Paleta): introducción y entrega de la Pokédex.
4. **Elección del Pokémon inicial**:

   | Inicial | Tipo | Estilo de juego |
   |---|---|---|
   | Bulbasaur | Planta / Veneno | Resistente, control de zona (enredaderas, polvos) |
   | Charmander | Fuego | Daño alto a distancia corta, más frágil |
   | Squirtle | Agua | Equilibrado, buen escudo y control |

   *Opcional:* Pikachu como inicial especial desbloqueable al completar la Pokédex.

5. **Tutorial de combate** contra el rival (con el inicial contrario por tipo): moverse, atacar, esquivar y usar habilidades.
6. **Tutorial de captura** en la Ruta 1.

---

## 5. Progresión: mapa que crece

El mapa de Kanto está dividido en **zonas**. Al principio solo está abierta la primera; las demás aparecen bloqueadas (con niebla o una barrera) en el mapa.

### Cómo se abre una zona

Cada zona pide **dos condiciones**:

1. **Pokédex**: haber capturado un número mínimo de especies distintas.
2. **Medalla**: haber vencido al líder de gimnasio de la zona anterior (si lo hay).

Ejemplo: para entrar en el Mt. Moon necesitas **12 especies capturadas** y la **Medalla Roca**.

### Qué da cada zona al abrirse

- **Pokémon nuevos** que aparecen en estado salvaje.
- **Ropa nueva** para el entrenador y accesorios para Pokémon.
- **Objetos nuevos** en la tienda (Poké Balls mejores, pociones).
- Un **gimnasio** o evento especial.

### Conexión con los modos competitivos

- En **Duelo 1v1** y **MOBA** solo puedes usar Pokémon que **hayas capturado** en la Aventura.
- Las **arenas del 1v1** y los **mapas del MOBA** también se desbloquean con las zonas (ver la tabla de [§6](#6-kanto-zonas-medallas-y-desbloqueos)).
- El **nivel no se arrastra** a los modos competitivos (ver [§7.2](#72-duelo-1v1)), para que sean justos.

---

## 6. Kanto: zonas, medallas y desbloqueos

> La lista de especies por zona es una distribución inicial y se puede ajustar. La única regla es que los 151 estén repartidos entre todas las zonas y eventos.

| # | Zona | Para abrirla | Pokémon salvajes (ejemplos) | Gimnasio / evento | Recompensas |
|---|---|---|---|---|---|
| 1 | **Pueblo Paleta y Ruta 1** | Inicio | Pidgey, Rattata, Spearow, Poliwag (pesca) | Tutorial vs. rival | Ropa inicial · Poké Ball |
| 2 | **Bosque Verde** | 3 especies | Caterpie, Metapod, Weedle, Kakuna, Pikachu | Jefe: Beedrill alfa | Gorra de explorador · Arena 1v1 "Bosque" |
| 3 | **Ciudad Plateada y Ruta 3** | 6 especies | Geodude, Sandshrew, Ekans, Jigglypuff, Nidoran♀, Nidoran♂ | **Brock** (Roca) → **Medalla Roca** | Chaqueta de montaña |
| 4 | **Mt. Moon** | 12 especies + Medalla Roca | Zubat, Clefairy, Paras, Onix, Magikarp (lo vende un NPC) | Evento: elegir fósil (Omanyte o Kabuto) | Linterna (accesorio) · Arena 1v1 "Cueva" |
| 5 | **Ciudad Celeste y Rutas 24–25** | 18 especies | Oddish, Bellsprout, Abra, Mankey, Psyduck, Goldeen, Meowth | **Misty** (Agua) → **Medalla Cascada** | Ropa marinera · Super Ball |
| 6 | **Ciudad Carmín y Cueva Diglett** | 25 especies + Medalla Cascada | Diglett, Drowzee, Krabby, Tentacool, Shellder, Farfetch'd | **Lt. Surge** (Eléctrico) → **Medalla Trueno** | Uniforme militar · **Desbloquea MOBA (mapa 1)** |
| 7 | **Túnel Roca, Pueblo Lavanda y Torre Pokémon** | 35 especies + Medalla Trueno | Machop, Cubone, Gastly, Haunter, Kadabra | Evento: Marowak fantasma · Evento: Snorlax dormido en la Ruta 12 (despertarlo con la Pokéflauta) | Capa oscura · Gafas de visión espectral |
| 8 | **Ciudad Azulona** | 45 especies | Vulpix, Growlithe, Tangela, Eevee (regalo), Porygon (premio del casino) | **Erika** (Planta) → **Medalla Arcoíris** | Kimono · Accesorios florales para Pokémon |
| 9 | **Ciudad Fucsia y Zona Safari** | 60 especies + Medalla Arcoíris | Rhyhorn, Chansey, Kangaskhan, Scyther, Pinsir, Tauros, Exeggcute, Dratini, Venonat, Doduo, Lickitung | **Koga** (Veneno) → **Medalla Alma** | Traje ninja · Safari Ball · Arena 1v1 "Safari" |
| 10 | **Ciudad Azafrán y Silph S.A.** | 75 especies + Medalla Alma | Mr. Mime, Hitmonlee / Hitmonchan (a elegir), Lapras (regalo) | **Sabrina** (Psíquico) → **Medalla Pantano** · Jefe: Equipo Rocket | Traje de científico · Master Ball (1) |
| 11 | **Central Energía** | 85 especies | Magnemite, Voltorb, Electabuzz, Electrode | Legendario: **Zapdos** | Casco de obrero |
| 12 | **Islas Espuma** | 90 especies | Seel, Dewgong, Slowpoke, Horsea, Staryu, Jynx | Legendario: **Articuno** | Ropa de nieve |
| 13 | **Isla Canela y Mansión** | 95 especies + Medalla Pantano | Ponyta, Magmar, Grimer, Koffing, Ditto, Aerodactyl (fósil) | **Blaine** (Fuego) → **Medalla Volcán** · Revivir fósiles | Gafas de sol · Ropa de playa |
| 14 | **Ciudad Verde** | 105 especies + Medalla Volcán | Nidorino, Nidorina, Dugtrio | **Giovanni** (Tierra) → **Medalla Tierra** | Traje elegante · **Desbloquea MOBA (mapa 2)** |
| 15 | **Calle Victoria y Liga Pokémon** | 8 medallas | Machoke, Golbat, Marowak, Onix | Legendario: **Moltres** · **Alto Mando + Campeón** | **Mega-Aro** · Capa de campeón |
| 16 | **Cueva Celeste** (post-juego) | Liga completada | Especies evolucionadas de alto nivel | Legendario: **Mewtwo** | Megapiedras de Mewtwo · Arena 1v1 "Cueva Celeste" |
| ★ | **Evento especial** | Pokédex 150/151 | — | Singular: **Mew** | Amuleto Iris (ver [§10](#10-shinies)) |

### Medallas

| Medalla | Líder | Tipo | Efecto en el juego |
|---|---|---|---|
| Roca | Brock | Roca | Abre Mt. Moon · Nivel máximo de obediencia 20 |
| Cascada | Misty | Agua | Abre Carmín · Nivel 30 · Habilidad de campo: **Surf** (cruzar agua) |
| Trueno | Lt. Surge | Eléctrico | Abre Túnel Roca · Nivel 40 · **MOBA** |
| Arcoíris | Erika | Planta | Abre Fucsia · Nivel 50 · Habilidad de campo: **Corte** |
| Alma | Koga | Veneno | Abre Azafrán · Nivel 60 |
| Pantano | Sabrina | Psíquico | Abre Canela · Nivel 70 · Habilidad de campo: **Vuelo** (viaje rápido) |
| Volcán | Blaine | Fuego | Abre Ciudad Verde · Nivel 80 |
| Tierra | Giovanni | Tierra | Abre la Liga · Nivel 100 |

**Nivel de obediencia:** los Pokémon por encima de ese nivel a veces ignoran órdenes (habilidades con más tiempo de recarga). Así no se pueden subir niveles de más para saltarse el progreso.

### Combates de gimnasio

- Formato **equipo contra equipo**: el líder tiene 2–6 Pokémon y tú hasta 6.
- Se pelea en tiempo real **uno contra uno**; cuando uno se debilita, entra el siguiente. Puedes **cambiar** de Pokémon en cualquier momento (con un pequeño tiempo de recarga).
- Cada gimnasio tiene una **mecánica de arena** según su tipo (rocas que caen en el de Brock, zonas de agua que ralentizan en el de Misty, campos eléctricos en el de Lt. Surge, etc.).

---

## 7. Modos de juego

### 7.1 Aventura (Arcade)

- **Cámara**: isométrica en tercera persona, estilo MOBA, con el entrenador y su Pokémon líder siempre visibles.
- **Exploración**: el entrenador camina por la zona; su Pokémon líder lo sigue.
- **Encuentros**: los Pokémon salvajes se ven en el mapa (no son aleatorios). Algunos huyen, otros atacan al verte.
- **Combate**: al acercarte a uno salvaje empieza el combate en el mismo lugar, sin cambio de pantalla. Controlas a tu Pokémon con los controles de combate.
- **Equipo**: hasta 6 Pokémon. El resto va al **PC** (accesible desde los Centros Pokémon).
- **Centros Pokémon**: curar, PC, guardar partida.
- **Tienda**: Poké Balls, pociones, objetos.
- **Nivel persistente**: 1–100, con experiencia y evolución normales.

### 7.2 Duelo 1v1

- Arena pequeña y cerrada. Las arenas se desbloquean con las zonas.
- **Formato estándar**: cada jugador elige **3 Pokémon** y puede cambiar entre ellos. Gana quien debilite a los 3 rivales.
- **Formato rápido**: 1 Pokémon cada uno y una sola ronda.
- **Nivel normalizado**: en el 1v1 todos los Pokémon pelean a **nivel 50** con estadísticas calculadas de forma estándar. Lo que cuenta es la especie, los movimientos elegidos y la habilidad del jugador, no las horas de juego.
- **Rivales**: primero contra **IA** (con niveles de dificultad), luego **online**.
- **Megaevolución**: permitida una vez por duelo si tienes Mega-Aro y la megapiedra.

### 7.3 MOBA (estilo Dota / Pokémon Unite)

- **Equipos**: 5v5 como objetivo final. Primera versión 3v3 para que sea más fácil de probar.
- **Mapa** (estilo Dota adaptado a móvil):
  - 3 carriles (superior, central, inferior) con **2 torres por carril** y una **base** por equipo.
  - **Oleadas**: en lugar de súbditos, avanzan Pokémon salvajes débiles del equipo (Rattata, Pidgey, Caterpie).
  - **Jungla**: Pokémon salvajes neutrales que dan experiencia y mejoras temporales.
  - **Objetivos épicos**: un **Snorlax** que, al vencerlo, da un escudo al equipo, y un **legendario** (Zapdos) que aparece en los últimos minutos y empuja un carril.
- **Victoria**: destruir la base enemiga, o tener más torres destruidas al acabar el tiempo (10 minutos).
- **Progresión en partida**:
  - Todos empiezan en **nivel 1** con su Pokémon en la primera etapa (por ejemplo, Charmander).
  - Al subir de nivel **evolucionan durante la partida** (Charmander → Charmeleon → Charizard) y desbloquean habilidades.
  - Nivel máximo en partida: 15.
- **Megaevolución**: funciona como **definitiva** (se carga con el tiempo), disponible a partir de nivel 12.
- **Selección**: solo puedes elegir Pokémon que hayas capturado en la Aventura.
- **Roles**: cada especie tiene uno, en función de sus estadísticas base:

  | Rol | Ejemplos | Función |
  |---|---|---|
  | Atacante | Charizard, Alakazam, Gengar | Mucho daño a distancia |
  | Luchador | Machamp, Pinsir, Hitmonlee | Daño cuerpo a cuerpo y aguante |
  | Defensor | Snorlax, Blastoise, Onix | Mucha vida, protege al equipo |
  | Veloz | Jolteon, Scyther, Raichu | Movilidad, rematar enemigos |
  | Soporte | Chansey, Clefable, Venusaur | Curación, escudos y control |

### 7.4 Desbloqueo de los modos

| Modo | Se desbloquea al… |
|---|---|
| Aventura | Inicio |
| Duelo 1v1 vs. IA | Vencer al rival en el tutorial |
| Duelo 1v1 online | Obtener la Medalla Roca |
| MOBA (mapa 1) | Obtener la Medalla Trueno |
| MOBA (mapa 2) | Obtener la Medalla Tierra |

---

## 8. Sistema de combate

### 8.1 Controles táctiles

```
┌────────────────────────────────────────────┐
│  [HP/mapa]                       [pausa]   │
│                                            │
│                                  [Hab. 1]  │
│                         [Hab. 2]           │
│   ( joystick )                  [ATAQUE]   │
│                         [Def.]  [Esquiva]  │
│   [cambiar Pkmn]           [Mega]          │
└────────────────────────────────────────────┘
```

- **Joystick izquierdo**: mover.
- **Ataque básico**: toque para atacar al objetivo más cercano.
- **Habilidades**: toque para lanzar automáticamente o **arrastrar para apuntar** (aparece una guía en el suelo). Arrastrar a la esquina cancela.
- **Esquiva**: rodar o saltar en la dirección del joystick; da **0,25 s de invulnerabilidad** y cuesta estamina.
- **Definitiva**: se carga con el combate.
- **Mega**: solo aparece si se puede megaevolucionar.
- En la Aventura y el 1v1 hay un botón para **cambiar de Pokémon**.

### 8.2 Estadísticas

Se usan las 6 estadísticas base de cada especie, traducidas a tiempo real:

| Estadística | En el juego |
|---|---|
| PS (HP) | Vida |
| Ataque | Daño de movimientos físicos |
| Defensa | Reducción de daño físico |
| At. Esp. | Daño de movimientos especiales |
| Def. Esp. | Reducción de daño especial |
| Velocidad | Velocidad de movimiento (con límite) y reducción de tiempos de recarga |

Además hay una **barra de estamina**: la esquiva y el sprint la gastan y se recarga sola.

### 8.3 Fórmula de daño

Basada en la fórmula clásica, simplificada para tiempo real:

```
daño = ((2 × nivel / 5 + 2) × potencia × (Ataque / Defensa) / 50 + 2)
       × STAB × efectividad × crítico × variación
```

- **STAB**: ×1,5 si el tipo del movimiento coincide con el del Pokémon.
- **Efectividad**: tabla moderna de 18 tipos (×0, ×0,25, ×0,5, ×1, ×2, ×4).
- **Crítico**: 6,25 % de probabilidad, ×1,5.
- **Variación**: aleatoria entre 0,9 y 1,0.

Se usa la tabla **moderna** de tipos (con Acero, Siniestro y Hada) porque los tipos modernos de los Pokémon de Kanto la necesitan (Clefairy es Hada, Magnemite es Eléctrico/Acero).

### 8.4 Movimientos → habilidades

Cada Pokémon lleva **4 movimientos** en la Aventura:

- **1 ataque básico** (fijo según la especie).
- **2 habilidades** a elegir entre los movimientos que ha aprendido.
- **1 definitiva** (su movimiento más fuerte o uno característico).

Para no programar 150+ movimientos a mano, cada movimiento usa una **plantilla de forma** y solo cambian los números y los efectos visuales:

| Plantilla | Ejemplos |
|---|---|
| Proyectil | Ascuas, Pistola Agua, Rayo |
| Rayo (línea) | Hiperrayo, Rayo Solar, Hidrobomba |
| Cono | Lanzallamas, Polvo Veneno, Ventisca |
| Área en el suelo (con aviso previo) | Terremoto, Trueno, Avalancha |
| Embestida (dash) | Placaje, Ataque Rápido, Derribo |
| Cuerpo a cuerpo | Arañazo, Puño Fuego, Corte |
| Zona persistente | Drenadoras, Púas, Niebla |
| Autoboost | Danza Espada, Defensa Férrea, Agilidad |
| Escudo / curación | Protección, Recuperación, Descanso |
| Invocación / trampa | Sustituto, Bomba Lodo (charco) |

**Aviso previo (telegraph):** los ataques fuertes muestran una marca en el suelo antes de golpear para que se puedan esquivar. Es la base de que el combate sea de habilidad.

### 8.5 Estados alterados

| Estado | Efecto en tiempo real |
|---|---|
| Quemado | Daño cada segundo · −50 % de daño físico |
| Envenenado | Daño cada segundo (el grave aumenta con el tiempo) |
| Paralizado | −50 % de velocidad · a veces se interrumpe la acción |
| Dormido | No puede actuar 2–3 s (se despierta antes si recibe daño) |
| Congelado | Inmóvil 1,5 s |
| Confuso | Controles invertidos durante 2 s |
| Retroceso | Interrumpe la habilidad que se estaba lanzando |

### 8.6 IA de los Pokémon

- **Salvajes**: patrullar → detectar → atacar o huir, según su carácter (agresivo, tímido o territorial).
- **Líderes y rivales**: árbol de comportamiento que usa tipos a su favor, esquiva los avisos y cambia de Pokémon.
- **Bots del MOBA**: avanzar por carril, retirarse con poca vida, ayudar a aliados y atacar objetivos.

---

## 9. Captura y Poké Balls

### 9.1 Cómo se captura

1. **Debilitar** al Pokémon salvaje (opcional, pero cuanta menos vida tenga, más fácil).
2. **Distraerlo con comida** (opcional): lanzar una baya o un cebo lo calma y sube la probabilidad (ver [§14](#14-alimentación)).
3. Pulsar el botón **Poké Ball** → se abre una **rueda rápida** para elegir el tipo de bola → **arrastrar para apuntar** y soltar.
4. **Por la espalda**: si el Pokémon no te ha visto, la captura tiene bonus (estilo Legends).
5. La bola se sacude hasta 3 veces (4 comprobaciones internas) y se cierra o se abre.

### 9.2 Fórmula

Basada en la fórmula moderna de los juegos principales:

```
a = ((3 × PSmáx − 2 × PSactual) × tasaCaptura × bonusBola) / (3 × PSmáx)
    × bonusEstado × bonusExtra

si a ≥ 255 → capturado
si no:
    b = 65536 / (255 / a)^0,1875
    4 comprobaciones: si un número aleatorio entre 0 y 65535 es menor que b, pasa.
    Si pasan las 4 → capturado.
```

- **tasaCaptura**: valor de la especie (1–255). Ejemplos: Caterpie 255, Pikachu 190, Charmander 45, Snorlax 25, legendarios 3.
- **bonusEstado**: dormido o congelado ×2,5 · paralizado, quemado o envenenado ×1,5.
- **bonusExtra**:

  | Situación | Bonus |
  |---|---|
  | Lanzada por la espalda sin ser visto | ×1,5 |
  | Distraído con una Baya Frambu | ×1,5 |
  | Pokémon enfurecido (tras escapar de una bola) | ×0,5 |

- **Captura crítica**: con más especies capturadas en la Pokédex aumenta la probabilidad de una captura crítica (una sola sacudida y una sola comprobación). Llega al 2,5 % con 150 especies.

### 9.3 Tipos de Poké Ball

| Bola | Bonus | Condición | Se consigue en | Precio |
|---|---|---|---|---|
| **Poké Ball** | ×1 | — | Zona 1 (tienda) | 200 ₽ |
| **Super Ball** | ×1,5 | — | Zona 5 (Celeste) | 600 ₽ |
| **Ultra Ball** | ×2 | — | Zona 8 (Azulona) | 800 ₽ |
| **Master Ball** | Siempre captura | — | Silph S.A. (solo 1) | No se vende |
| **Safari Ball** | ×1,5 | Solo en la Zona Safari | Zona 9 (30 por entrada) | Incluida en la entrada |
| **Honor Ball** | ×1 | Decorativa | Regalo por comprar 10 bolas | — |
| **Sana Ball** | ×1 | Cura del todo al Pokémon capturado | Zona 3 | 300 ₽ |
| **Nido Ball** | (41 − nivel) / 10, mínimo ×1 | Mejor con Pokémon de nivel bajo | Zona 2 | 1000 ₽ |
| **Ocaso Ball** | ×3 | De noche o en cuevas | Zona 4 | 1000 ₽ |
| **Malla Ball** | ×3,5 | Pokémon de tipo Agua o Bicho | Zona 6 | 1000 ₽ |
| **Lujo Ball** | ×1 | La amistad sube el doble de rápido | Zona 8 | 3000 ₽ |
| **Acopio Ball** | ×3,5 | Especie ya capturada antes | Zona 9 | 1000 ₽ |
| **Turno Ball** | ×1 + 0,3 cada 10 s de combate, máximo ×4 | Combates largos | Zona 10 | 1000 ₽ |
| **Buceo Ball** | ×3,5 | Pokémon en el agua (surfeando o pescando) | Zona 12 | 1000 ₽ |
| **Veloz Ball** | ×5 | Solo en los primeros 5 s del combate | Zona 13 | 1000 ₽ |

**Bolas de artesanía** (post-Liga, se fabrican con **Bonguris** que se recogen de árboles en las zonas):

| Bola | Bonus | Condición |
|---|---|---|
| **Nivel Ball** | ×2 / ×4 / ×8 | Si tu Pokémon tiene más nivel / el doble / el cuádruple |
| **Cebo Ball** | ×4 | Pokémon pescado |
| **Luna Ball** | ×4 | Especies que evolucionan con Piedra Lunar (Nidoran, Clefairy, Jigglypuff) |
| **Rapid Ball** | ×4 | Especies con Velocidad base ≥ 100 |
| **Peso Ball** | Mejor cuanto más pesado | Por ejemplo, ×4 con Snorlax u Onix |
| **Amor Ball** | ×8 | Misma especie que tu Pokémon y sexo opuesto |
| **Amigo Ball** | ×1 | El capturado empieza con mucha amistad |

- Cada bola tiene su **modelo y efecto visual** propio, al lanzarla y al sacar al Pokémon.
- El Pokémon **recuerda en qué bola fue capturado** y sale siempre de ella.

### 9.4 Lanzamiento

- El lanzamiento sigue un **arco** con guía en el suelo; alcance máximo de 12 m.
- Si el Pokémon se mueve, la bola puede fallar. La bola fallada queda en el suelo **10 s** para recogerla.
- Si la bola golpea al Pokémon mientras **esquiva**, falla.

### 9.5 Otras reglas

- Si escapa, el Pokémon se **enfurece** unos segundos (más daño y velocidad, y peor captura).
- Los **legendarios** solo se pueden capturar después de vencer su fase de jefe.
- Capturar una especie nueva la registra en la **Pokédex** (ver [§15](#15-pokédex)) y suma para abrir zonas.
- Si el equipo está lleno (6), el capturado va al **PC**.

---

## 10. Shinies

- **Probabilidad base**: 1 de cada 4096 encuentros.
- **Amuleto Iris** (al completar la Pokédex de Kanto): 1 de cada 1365.
- **Racha de captura**: capturar seguidos Pokémon de la misma especie sube la probabilidad hasta un máximo (por ejemplo, 1/512 con racha de 30).
- **Cómo se ven**:
  - Paleta de colores alternativa (una textura o un cambio de color en el shader).
  - Destello de estrellas al aparecer y al salir de la Poké Ball.
  - Sonido especial al detectarlo en el mapa, para que el jugador sepa que hay uno cerca.
  - Marca ★ en la Pokédex, el equipo y el PC.
- Los shinies **no son más fuertes**; solo cambia el aspecto. Se pueden usar en el 1v1 y el MOBA para presumir.

---

## 11. Evolución y megaevolución

### Evolución

| Método | Ejemplos de Kanto | En la Aventura |
|---|---|---|
| Nivel | Charmander → Charmeleon (16) → Charizard (36) | Automática al subir de nivel (se puede cancelar) |
| Piedra | Pikachu + Piedra Trueno → Raichu; Eevee → Vaporeon / Jolteon / Flareon | Usar el objeto |
| Intercambio | Kadabra, Machoke, Graveler, Haunter | Se cambia por un **objeto de vínculo** (Cable Unión) que da un NPC, porque no hay intercambio entre jugadores al principio |

En el MOBA la evolución ocurre **durante la partida** según el nivel (ver [§7.3](#73-moba-estilo-dota--pokémon-unite)).

### Megaevolución

- **Requisitos**: tener el **Mega-Aro** (se obtiene al vencer al Campeón) y la **megapiedra** de la especie equipada.
- **Megapiedras**: escondidas en las zonas como recompensa de retos (jefes alfa, eventos, Pokédex).
- **Duración**:
  - Aventura y 1v1: una vez por combate y dura hasta que el Pokémon se debilite.
  - MOBA: como definitiva, durante un tiempo limitado.
- **Efecto**: nuevo modelo, estadísticas base de la forma Mega, a veces nuevo tipo, y una habilidad nueva.

**Megaevoluciones de la 1.ª generación** (21 formas en 17 especies; datos de PokeAPI, incluidas las nuevas de Legends: Z-A):

| Pokémon | Megapiedra | Cambio de tipo |
|---|---|---|
| Venusaur | Venusaurita | — |
| Charizard | Charizardita X / Y | X: Fuego/Dragón · Y: Fuego/Volador |
| Blastoise | Blastoisita | — |
| Beedrill | Beedrillita | — |
| Pidgeot | Pidgeotita | — |
| Raichu | Raichunita X / Y | — |
| Clefable | Clefablita | Hada/Volador |
| Alakazam | Alakazamita | — |
| Victreebel | Victreebelita | — |
| Slowbro | Slowbronita | — |
| Gengar | Gengarita | — |
| Kangaskhan | Kangaskhanita | — |
| Starmie | Starmita | — |
| Pinsir | Pinsirita | Bicho/Volador |
| Gyarados | Gyaradosita | Agua/Siniestro |
| Aerodactyl | Aerodactylita | — |
| Dragonite | Dragonitita | — |
| Mewtwo | Mewtwoita X / Y | X: Psíquico/Lucha |

---

## 12. Personalización

### 12.1 Entrenador

**Al inicio (básica):**

- Cuerpo y tono de piel
- Peinado y color de pelo
- Color de ojos
- 2–3 conjuntos de ropa iniciales

**Ranuras de ropa:**

| Ranura | Ejemplos |
|---|---|
| Cabeza | Gorras, sombreros, cascos |
| Cara | Gafas, máscaras |
| Torso | Camisetas, chaquetas, abrigos |
| Piernas | Pantalones, faldas, shorts |
| Pies | Zapatillas, botas |
| Espalda | Mochilas, capas |
| Manos | Guantes, Mega-Aro (visible) |

- Cada prenda tiene **variantes de color** que se desbloquean aparte.
- La ropa se desbloquea por **zona, medalla, logros de Pokédex, shinies capturados y victorias en el 1v1 y el MOBA**.
- La ropa es **solo estética**; no da ventajas.

### 12.2 Pokémon

- **Una ranura de accesorio** por Pokémon: lazo, sombrero, gafas, bufanda, corona de flores…
- Funciona con **puntos de anclaje** en el esqueleto de cada modelo (cabeza, cuello y espalda), así que cualquier especie puede llevar cualquier accesorio sin hacer versiones especiales.
- **Mote** (nombre personalizado).
- **Poké Ball decorada**: efecto visual al sacar al Pokémon.

### 12.3 Perfil

- Tarjeta de entrenador con medallas, Pokédex, shinies y estadísticas del 1v1 y el MOBA.
- Título elegible ("Campeón de Kanto", "Cazador de shinies", etc.).

---

## 13. Mochila, objetos y economía

### 13.1 Moneda

- **Pokédólares (₽)**: se ganan en combates contra entrenadores, capturas, gimnasios, misiones y vendiendo objetos.
- **Sin compras con dinero real** (es un proyecto fan-made).

### 13.2 Mochila (inventario)

La mochila está dividida en **bolsillos**. Cada bolsillo tiene un límite de huecos que se amplía al abrir zonas (mochila más grande como recompensa).

| Bolsillo | Contenido | Huecos iniciales |
|---|---|---|
| **Botiquín** | Poción, Superpoción, Hiperpoción, Restaurar Todo, Revivir, Antídoto, Antiparalizador, Antiquemar, Despertar, Antihielo, Cura Total, Éter, Elixir | 20 |
| **Poké Balls** | Todas las de [§9.3](#93-tipos-de-poké-ball) | 15 |
| **Comida** | Comida del entrenador y de los Pokémon (ver [§14](#14-alimentación)) | 20 |
| **Bayas** | Bayas recogidas en los árboles | 20 |
| **Ingredientes** | Para cocinar en el campamento: pan, arroz, verduras, condimentos, Bonguris | 30 |
| **Objetos de combate** | Objetos equipables: Restos, Cinta Elegida, Banda Focus, Periscopio, Garra Rápida… | 15 |
| **Evolución y Mega** | Piedras evolutivas, Cable Unión, megapiedras | Sin límite |
| **MT** | Máquinas Técnicas para enseñar movimientos | Sin límite |
| **Objetos clave** | Pokédex, Mapa, Bicicleta, Cañas de pescar, Mega-Aro, Pokéflauta, Pase del Safari, Ticket del S.S. Anne | Sin límite |

- **Acceso rápido en combate**: 4 objetos configurables en una rueda (por ejemplo, Poción, Revivir, Poké Ball favorita y una baya).
- **Ordenar** por tipo, nombre o recientes, y marcar favoritos.
- Lo que no cabe se puede guardar en el **PC** (almacén de objetos), además de las cajas de Pokémon.

### 13.3 Objetos de combate (equipables)

| Objeto | Efecto |
|---|---|
| Restos | Recupera vida poco a poco |
| Cinta Elegida | Más daño, pero el ataque básico no se puede cambiar por habilidades durante 5 s después de usar una |
| Banda Focus | Sobrevive una vez con 1 PS a un golpe que lo debilitaría |
| Garra Rápida | Reduce los tiempos de recarga un 10 % |
| Periscopio | Más probabilidad de crítico |
| Objetos de tipo (Carbón, Agua Mística, Semilla Milagro…) | +20 % de daño a los movimientos de ese tipo |

### 13.4 Tienda

- Cada Centro Pokémon tiene tienda; su catálogo crece con las medallas.
- Hay **tiendas especiales**: Centro Comercial de Azulona (todo tipo de objetos y MT), puestos de comida y el artesano de Poké Balls.

### 13.5 MOBA

En el MOBA no se usa la mochila. Hay **objetos de partida** que se compran con puntos ganados durante la misma partida y se pierden al terminar.

---

## 14. Alimentación

### 14.1 El entrenador

- Tiene una **barra de energía** (hambre) de 0 a 100.
- Baja con el tiempo de exploración (−1 cada 30 s) y más rápido al correr o montar en bici.
- **Efectos de hambre**:

  | Energía | Efecto |
  |---|---|
  | 100–30 | Normal |
  | 30–1 | La estamina del entrenador se recupera más lento y no puede correr |
  | 0 | Camina más lento; un aviso en pantalla pide comer. **Nunca muere** |

- **Comida del entrenador** (bolsillo Comida):

  | Comida | Energía | Extra |
  |---|---|---|
  | Onigiri | +20 | — |
  | Sándwich | +35 | Según los ingredientes, da un bonus temporal |
  | Malasada | +25 | Más experiencia durante 5 min |
  | Leche Mu-mu | +15 | Recupera estamina al instante |
  | Curry (cocinado) | +60 | Cura algo al equipo |

- **Cocinar en el campamento**: en zonas abiertas se puede montar un campamento para cocinar sándwiches o curry con ingredientes y bayas. Según la receta, dan **bonus temporales** (estilo Escarlata/Púrpura):
  - **Encuentro**: aparecen más Pokémon de un tipo.
  - **Captura**: bonus a capturar un tipo.
  - **Brillo**: más probabilidad de shiny de un tipo (recetas difíciles).
  - **Experiencia**: más experiencia en combate.

### 14.2 Los Pokémon

- Cada Pokémon tiene **saciedad** (0–100) y **amistad** (0–255).
- La saciedad baja en la Aventura al combatir. Con poca saciedad, el Pokémon pierde daño y recupera la estamina más lento.
- **Darle de comer** recupera saciedad y sube la amistad (más aún si es su comida favorita).
- **Amistad alta** da ventajas: a veces aguanta un golpe con 1 PS, más críticos, se quita estados solo, y saluda al entrenador en la exploración.
- La saciedad y el hambre **no existen en el Duelo 1v1 ni en el MOBA**.

### 14.3 Qué comen los Pokémon

Cada especie tiene una **dieta** y un **sabor favorito**. Se asigna por defecto según el tipo y se ajusta por especie en `overrides.json`.

| Dieta | Ejemplos | Comida que acepta |
|---|---|---|
| **Bayas y frutas** | Pikachu, Clefairy, Jigglypuff, Eevee | Bayas, Pokécubos, Poffins |
| **Hojas y plantas** | Bulbasaur, Oddish, Tauros, Rhyhorn | Hojas, hierba, bayas amargas |
| **Néctar y polen** | Butterfree, Beedrill, Venomoth | Miel, bayas dulces |
| **Insectos y semillas** | Pidgey, Spearow, Farfetch'd | Semillas, bayas secas |
| **Algas y plancton** | Squirtle, Magikarp, Lapras, Tentacool | Algas, Pokécubos azules |
| **Pescado** | Seel, Psyduck, Golduck | Pescado seco, Pokécubos |
| **Minerales y tierra** | Geodude, Onix, Diglett, Sandshrew | Piedras minerales, Arena fina |
| **Electricidad** | Magnemite, Voltorb, Electabuzz | Pilas, cargarse en la Central Energía |
| **Fuego y calor** | Charmander, Vulpix, Ponyta, Magmar | Bayas picantes, carbón |
| **Energía espiritual** | Gastly, Haunter, Gengar | Incienso, sueños (Drowzee y Hypno también) |
| **Residuos** | Grimer, Muk, Koffing | Lodo, residuos (no hace falta buscarlos) |
| **De todo** | Snorlax, Rattata, Meowth, Kangaskhan | Cualquier comida (Snorlax se come mucha) |

**Sabores** (relacionados con la naturaleza del Pokémon): picante, seco, dulce, amargo y ácido. A cada Pokémon le gusta uno y no le gusta otro, según su naturaleza.

### 14.4 Bayas

| Baya | Sabor | Efecto si se lanza al salvaje | Efecto si se le da al tuyo |
|---|---|---|---|
| Aranja | Varios | Lo calma unos segundos | +10 PS |
| Zidra | Varios | — | +25 % PS |
| Frambu | Dulce/seco | Captura ×1,5 | Amistad |
| Latano | Ácido | Lo frena (más fácil de apuntar) | Amistad |
| Pinia | Dulce | Más probabilidad de que suelte un objeto | Amistad |
| Zreza | Picante | — | Cura parálisis |
| Atania | Seco | — | Cura sueño |
| Meloc | Amargo | — | Cura veneno |
| Safre | Ácido | — | Cura quemaduras |
| Perasi | Dulce | — | Cura congelación |
| Ziuela | Varios | — | Cura cualquier estado |
| Caquic | Picante | Atrae a especies con sabor favorito picante | Amistad |

- Las bayas se recogen de **árboles** en las zonas (vuelven a crecer cada día real).
- **Lanzar comida al suelo** atrae a Pokémon salvajes cercanos que tengan esa dieta. Sirve para capturar sin pelear y para encontrar especies concretas.

---

## 15. Pokédex

### 15.1 Estados de cada especie

| Estado | Qué se ve |
|---|---|
| **Desconocido** | Silueta negra y número |
| **Visto** | Modelo, nombre, tipos, zona donde se vio |
| **Capturado** | Toda la información |

### 15.2 Información de una entrada (capturada)

- Número, nombre, categoría ("Pokémon Semilla"), tipos
- Altura, peso y descripción
- **Modelo 3D** que se puede girar, con formas: macho/hembra, **shiny** (si se ha visto) y **Mega** (si se ha conseguido)
- **Grito** del Pokémon
- **Hábitat**: zonas donde aparece, horario (día/noche) y rareza
- **Alimentación**: dieta y sabor favorito (el favorito se descubre al darle de comer)
- Estadísticas base y gráfico
- Movimientos que aprende (por nivel y por MT)
- Cadena evolutiva y cómo evoluciona
- Rol en el MOBA

### 15.3 Tareas de investigación

Estilo Legends: Arceus. Cada especie tiene tareas que suben su **nivel de investigación** (1 a 10):

- Capturar X ejemplares
- Vencer X ejemplares
- Ver cómo usa un movimiento concreto
- Darle de comer su comida favorita
- Capturarlo sin que te vea
- Evolucionarlo

Al completar el nivel 10 de una especie: recompensa (ropa temática, accesorio o bayas raras).

### 15.4 Recompensas por completar

| Capturados | Recompensa |
|---|---|
| 10 | Mochila más grande |
| 30 | Gorra de investigador |
| 60 | Caña Súper Buena |
| 100 | Traje del Profesor Oak |
| 150 | **Amuleto Iris** (más shinies) |
| 151 | Diploma y título "Maestro Pokémon de Kanto" |

### 15.5 Otras funciones

- Filtros por tipo, zona, dieta, capturado/visto, shiny.
- Buscador por nombre o número.
- **Hábitat en el mapa**: tocar una especie la marca en el mapa.

---

## 16. Otros sistemas

| Sistema | Descripción |
|---|---|
| **PC** | Cajas para guardar Pokémon (30 por caja) y almacén de objetos. En los Centros Pokémon |
| **Centro Pokémon** | Curar al equipo, PC, tienda y guardado |
| **Amistad** | Ver [§14.2](#142-los-pokémon) |
| **Naturaleza** | Cada Pokémon tiene una de las 25 naturalezas: +10 % a una estadística, −10 % a otra, y define su sabor favorito |
| **Habilidad** | Pasiva de cada especie. Ejemplos: Mar Llamas (más daño de Fuego con poca vida), Torrente, Espesura, Electricidad Estática (puede paralizar al tocarlo) |
| **MT** | Máquinas Técnicas para enseñar movimientos. Se encuentran, se compran o se ganan en gimnasios |
| **Pesca** | Caña Vieja, Buena y Súper Buena para encontrar Pokémon de agua. Minijuego en tiempo real |
| **Monturas** | Bicicleta desde la Zona 6. Más adelante, montar en Pokémon: Lapras (Surf), Charizard o Pidgeot (Vuelo) |
| **Día y noche** | Ciclo de 1 h real = 1 día del juego. Cambia qué Pokémon aparecen y algunos bonus (Ocaso Ball) |
| **Clima** | Lluvia, sol, niebla y nieve por zona. Cambia apariciones y potencia los tipos (lluvia: Agua +; sol: Fuego +) |
| **Misiones** | Encargos de NPC: entregar un Pokémon, conseguir comida, vencer a un alfa. Dan dinero, objetos y ropa |
| **Pokémon alfa** | Ejemplares más grandes y fuertes en cada zona; mejores recompensas |
| **Logros** | Para el perfil del entrenador y desbloqueo de títulos |
| **Ajustes** | Calidad gráfica, FPS, sensibilidad del joystick, **modo zurdo** (controles invertidos), tamaño y posición de los botones, idioma, volumen |
| **Guardado** | Automático al entrar en zonas y en los Centros Pokémon; guardado manual en el menú |

---

## 17. Arquitectura del proyecto (Unity)

### 17.1 Estructura de carpetas

```
PokeMoba/
├── GDD.md                       ← este documento
├── README.md
├── Tools/
│   └── import_pokeapi/          ← script que descarga datos de PokeAPI a JSON
└── UnityProject/
    └── Assets/
        └── _Project/
            ├── Art/
            │   ├── Pokemon/       001_bulbasaur/, 004_charmander/… (modelo, texturas, shiny)
            │   ├── Trainer/       cuerpo base + prendas
            │   ├── Accessories/
            │   ├── Environments/  zonas de Kanto, arenas, mapas MOBA
            │   └── VFX/
            ├── Data/              ScriptableObjects generados
            │   ├── Species/
            │   ├── Moves/
            │   ├── Types/
            │   ├── Items/
            │   ├── Zones/
            │   └── Cosmetics/
            ├── Prefabs/
            ├── Scenes/
            │   ├── Boot.unity
            │   ├── MainMenu.unity
            │   ├── Adventure_Kanto.unity
            │   ├── Duel_Arena.unity
            │   └── Moba_Map01.unity
            └── Scripts/
                ├── Core/          arranque, guardado, eventos, servicios
                ├── Data/          definiciones (SpeciesDefinition, MoveDefinition…)
                ├── Combat/        entidades, daño, tipos, estados, habilidades
                ├── Creatures/     instancia de Pokémon, nivel, evolución, shiny
                ├── Input/         joystick y botones táctiles
                ├── AI/            salvajes, líderes, bots del MOBA
                ├── Capture/
                ├── Trainer/       entrenador y personalización
                ├── Modes/
                │   ├── Adventure/
                │   ├── Duel/
                │   └── Moba/
                ├── Net/           multijugador (Photon Fusion)
                └── UI/
```

### 17.2 Clases principales

| Clase | Qué es |
|---|---|
| `SpeciesDefinition` (ScriptableObject) | Datos fijos de una especie: número, nombre, tipos, estadísticas base, tasa de captura, movimientos que aprende, evoluciones, modelo, rol en el MOBA, forma Mega |
| `MoveDefinition` (ScriptableObject) | Tipo, categoría (físico/especial/estado), potencia, plantilla de forma, alcance, tiempo de recarga, efecto, VFX |
| `TypeChart` (ScriptableObject) | Tabla de efectividad de 18 × 18 |
| `PokemonInstance` (clase serializable) | Un Pokémon concreto del jugador: especie, nivel, experiencia, movimientos, shiny, mote, accesorio, objeto equipado |
| `CombatEntity` (MonoBehaviour) | Cualquier cosa que pelea: vida, estadísticas en combate, estados, recibir daño |
| `AbilityRunner` | Ejecuta una `MoveDefinition` según su plantilla |
| `DamageCalculator` | Fórmula de daño (§8.3), sin dependencias de Unity para poder probarla |
| `GameMode` (abstracta) | Reglas del modo: `AdventureMode`, `DuelMode`, `MobaMode` |
| `ProgressionService` | Zonas abiertas, medallas, Pokédex, desbloqueos |
| `SaveService` | Guarda y carga la partida en JSON |
| `CosmeticDefinition` / `TrainerAppearance` | Prendas y lo que lleva puesto el entrenador |

### 17.3 Reglas de código

- La lógica (daño, tipos, captura, progresión) va en **C# puro** sin `MonoBehaviour`, para poder probarla con tests y reutilizarla en el servidor.
- Los `MonoBehaviour` solo conectan la lógica con Unity (animación, física, entrada).
- Nada de números mágicos en el código: todo valor de diseño va en un ScriptableObject.
- Eventos para comunicar sistemas (por ejemplo, `OnPokemonCaptured`) en lugar de referencias directas.

---

## 18. Datos de especies y movimientos

- **Fuente**: [PokeAPI](https://pokeapi.co/) (datos públicos).
- **Proceso**:
  1. Un script en `Tools/import_pokeapi/` descarga las especies 1–151, sus movimientos, tipos y cadenas evolutivas.
  2. Lo guarda como JSON en `Tools/import_pokeapi/output/`.
  3. Un importador dentro de Unity (menú *PokeMoba → Importar datos*) convierte el JSON en ScriptableObjects.
- **Ajustes manuales** (no vienen de PokeAPI): plantilla de forma de cada movimiento, rol en el MOBA, ataque básico, definitiva y zona donde aparece la especie. Van en un archivo `overrides.json` que el importador mezcla con los datos.
- **Nombres en español**: PokeAPI los incluye.
- **Otras generaciones**: el mismo script con otro rango de números.

---

## 19. Arte 3D y animación

### 19.1 Por fases

1. **Provisional (fase 1–2)**: formas simples (cápsulas, esferas) con el color del tipo y el nombre encima. Sirve para probar el combate sin esperar al arte.
2. **Modelos reales**: se cambian uno a uno sin tocar código, porque cada `SpeciesDefinition` apunta a su prefab.

### 19.2 De dónde salen los modelos

- Generadores de 3D con IA (Meshy, Tripo, Rodin) a partir de una imagen de referencia, y luego limpieza en Blender.
- Modelos de la comunidad (revisar siempre la licencia).
- Modelado propio en Blender.

### 19.3 Requisitos de cada modelo de Pokémon

- **Polígonos**: 3.000–8.000 triángulos (hasta 12.000 para legendarios y Megas).
- **Texturas**: 512 × 512 o 1024 × 1024. Una textura extra para el shiny (o una máscara de color).
- **Esqueleto** con huesos de anclaje: `Anchor_Head`, `Anchor_Neck` y `Anchor_Back` para los accesorios.
- **Animaciones mínimas**: reposo, caminar, correr, esquivar, ataque físico, ataque especial, recibir golpe, debilitado y entrada/salida de la Poké Ball.
- Nombre de carpeta: `NNN_nombre` (por ejemplo, `006_charizard`).

### 19.4 Entrenador

- Un cuerpo base con esqueleto humanoide (animaciones de **Mixamo**, gratuitas).
- Cada prenda es una malla aparte que comparte el esqueleto, así se intercambian sin problemas.

---

## 20. Hoja de ruta

| Fase | Objetivo | Se considera terminada cuando… |
|---|---|---|
| **0. Preparación** | Unity instalado, proyecto creado, Git con LFS | Los dos hermanos abren el proyecto y compilan un APK vacío en sus celulares |
| **1. Prototipo de combate** | Una arena, 2 Pokémon provisionales, joystick, ataque, 2 habilidades, esquiva, tipos, IA básica | Se juega en el celular y el combate se siente bien |
| **2. Datos de Kanto** | Importar los 151 con movimientos y tipos; elección del inicial; shinies | Cualquier especie se puede usar en la arena de prueba |
| **3. Aventura (zonas 1–3)** | Paleta, Ruta 1, Bosque Verde, Plateada; captura con Poké Ball, Super Ball, Sana Ball y Nido Ball; Pokédex; mochila; comida y bayas; Centro Pokémon y tienda; gimnasio de Brock; guardado | Se puede jugar de principio a fin hasta la Medalla Roca |
| **4. Personalización** | Creación del entrenador, prendas, accesorios de Pokémon, desbloqueos | La ropa se desbloquea al abrir zonas |
| **5. Duelo 1v1 vs. IA** | Arenas, formato de 3 Pokémon, nivel normalizado | Se puede jugar un duelo completo contra la IA |
| **6. Resto de Kanto** | Zonas 4–16, los 8 gimnasios, Liga, legendarios, Megas | Juego completo de Kanto |
| **7. Online 1v1** | Photon Fusion, emparejamiento | Dos celulares pelean online |
| **8. MOBA** | Mapa, carriles, torres, oleadas, jungla, bots, 3v3 → 5v5 online | Partida completa online |
| **9. Expansiones** | Johto (2.ª gen.) y siguientes | — |

**Regla de oro:** no se pasa a la fase 3 hasta que el combate de la fase 1 sea divertido.

---

## 21. Reparto de trabajo

Una propuesta para dos personas (se ajusta a lo que le guste a cada uno):

| Persona | Áreas |
|---|---|
| **Hermano A** (programación y sistemas) | Combate, IA, captura, progresión, guardado, red |
| **Hermano B** (contenido y arte) | Modelos y animaciones, diseño de zonas, VFX, UI, balance de datos (`overrides.json`) |
| **Los dos** | Pruebas en el celular, balance y diseño de nuevas mecánicas |

**Flujo con Git:**

- Rama `main` siempre funcional.
- Una rama por tarea (`feature/esquiva`, `feature/zona-bosque-verde`).
- No trabajar los dos en la **misma escena** a la vez (los conflictos en escenas de Unity son difíciles de resolver); usar prefabs para repartir.

---

## 22. Ideas para el futuro

- **Dinamax / Gigamax** como evento especial del MOBA (de Kanto: Venusaur, Charizard, Blastoise, Butterfree, Pikachu, Meowth, Machamp, Gengar, Kingler, Lapras, Eevee, Snorlax).
- **Teracristalización** como mecánica alternativa del 1v1.
- **Intercambio** entre jugadores.
- **Formas regionales** (Alola y Galar) de Pokémon de Kanto cuando lleguen esas regiones.
- **Clima y día/noche** que afecten a las apariciones y al daño.
- **Modo cooperativo**: incursiones contra un legendario con 4 jugadores.
- **Torre de batalla**: combates seguidos contra IA con recompensas.
- **Temporadas** con ranking en el 1v1 y el MOBA.

---

## 23. Nota legal

Pokémon y todos sus nombres, personajes y diseños son propiedad de Nintendo, Game Freak y The Pokémon Company. Este proyecto es **fan-made, sin ánimo de lucro**, y sirve como demostración de lo que se puede construir con ayuda de IA.

- No publicar en Google Play ni en App Store con la marca Pokémon.
- No cobrar ni poner anuncios.
- No usar modelos, sonidos ni música extraídos de los juegos oficiales.
- Gracias a la arquitectura data-driven ([§2](#2-pilares-de-diseño)), si algún día se quiere publicar de verdad, se pueden cambiar las especies por **criaturas originales** sin reescribir el código.
