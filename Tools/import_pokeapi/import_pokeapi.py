"""
Descarga de PokeAPI los datos de un rango de Pokémon (por defecto la 1.ª generación, 1–151)
y los guarda en un JSON que lee Unity.

Solo usa la biblioteca estándar de Python (3.8 o superior).

Uso:
    python import_pokeapi.py                 # 1–151
    python import_pokeapi.py --range 1-251   # hasta la 2.ª generación
    python import_pokeapi.py --workers 4     # menos peticiones en paralelo

Las respuestas se guardan en cache/, así que volver a ejecutarlo no repite las descargas.
"""

import argparse
import json
import re
import sys
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
from pathlib import Path

API = "https://pokeapi.co/api/v2/"
USER_AGENT = "PokeMoba-fan-project/0.1 (importador de datos)"

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[1]
CACHE_DIR = SCRIPT_DIR / "cache"
# En Resources para que Unity pueda cargarlos también en el APK (Resources.Load).
DATA_DIR = REPO_ROOT / "UnityProject" / "Assets" / "_Project" / "Resources" / "Data"

# Nombres de tipo tal como están en el enum PokemonType de Unity.
TYPE_NAMES = {
    "normal": "Normal", "fire": "Fuego", "water": "Agua", "grass": "Planta",
    "electric": "Electrico", "ice": "Hielo", "fighting": "Lucha", "poison": "Veneno",
    "ground": "Tierra", "flying": "Volador", "psychic": "Psiquico", "bug": "Bicho",
    "rock": "Roca", "ghost": "Fantasma", "dragon": "Dragon", "dark": "Siniestro",
    "steel": "Acero", "fairy": "Hada",
}

STAT_NAMES = {
    "hp": "hp", "attack": "attack", "defense": "defense",
    "special-attack": "spAttack", "special-defense": "spDefense", "speed": "speed",
}

# Juego del que se toman los movimientos que aprende cada especie, en orden de preferencia.
# Se usa el primero en el que la especie tenga movimientos por nivel.
VERSION_GROUP_PREFERENCE = [
    "legends-za",
    "scarlet-violet",
    "sword-shield",
    "lets-go-pikachu-lets-go-eevee",
    "ultra-sun-ultra-moon",
    "omega-ruby-alpha-sapphire",
    "x-y",
    "black-2-white-2",
    "heartgold-soulsilver",
    "firered-leafgreen",
    "red-blue",
]

# Megapiedras conocidas (la forma Mega de PokeAPI → objeto). Para las que no estén aquí
# se prueba con "<especie>ite" y "<especie>nite".
MEGA_STONES = {
    "venusaur-mega": "venusaurite",
    "charizard-mega-x": "charizardite-x",
    "charizard-mega-y": "charizardite-y",
    "blastoise-mega": "blastoisinite",
    "beedrill-mega": "beedrillite",
    "pidgeot-mega": "pidgeotite",
    "alakazam-mega": "alakazite",
    "slowbro-mega": "slowbronite",
    "gengar-mega": "gengarite",
    "kangaskhan-mega": "kangaskhanite",
    "pinsir-mega": "pinsirite",
    "gyarados-mega": "gyaradosite",
    "aerodactyl-mega": "aerodactylite",
    "mewtwo-mega-x": "mewtwonite-x",
    "mewtwo-mega-y": "mewtwonite-y",
    # Nuevas en Pokémon Legends: Z-A
    "clefable-mega": "clefablite",
    "victreebel-mega": "victreebelite",
    "starmie-mega": "starminite",
    "dragonite-mega": "dragoninite",
}


# Zonas de Kanto donde aparece cada especie en estado salvaje (GDD §6). 0 = inicial; 17 = evento de Mew.
ZONES = {
    0: ["bulbasaur", "charmander", "squirtle"],
    1: ["pidgey", "rattata", "spearow", "poliwag"],
    2: ["caterpie", "metapod", "weedle", "kakuna", "pikachu", "beedrill"],
    3: ["geodude", "sandshrew", "ekans", "jigglypuff", "nidoran-f", "nidoran-m"],
    4: ["zubat", "clefairy", "paras", "onix", "omanyte", "kabuto", "magikarp"],
    5: ["oddish", "bellsprout", "abra", "mankey", "psyduck", "goldeen", "meowth"],
    6: ["diglett", "drowzee", "krabby", "tentacool", "shellder", "farfetchd"],
    7: ["machop", "cubone", "gastly", "haunter", "kadabra", "snorlax"],
    8: ["vulpix", "growlithe", "tangela", "eevee", "porygon"],
    9: ["rhyhorn", "chansey", "kangaskhan", "scyther", "pinsir", "tauros", "exeggcute", "dratini",
        "venonat", "doduo", "lickitung"],
    10: ["mr-mime", "hitmonlee", "hitmonchan", "lapras"],
    11: ["magnemite", "voltorb", "electabuzz", "electrode", "zapdos"],
    12: ["seel", "dewgong", "slowpoke", "horsea", "staryu", "jynx", "articuno"],
    13: ["ponyta", "magmar", "grimer", "koffing", "aerodactyl", "ditto"],
    14: ["nidorino", "nidorina", "dugtrio"],
    15: ["machoke", "golbat", "marowak", "moltres"],
    16: ["mewtwo"],
    17: ["mew"],
}

# Dieta (GDD §14.3): primero excepciones por especie, luego por tipo principal.
DIET_BY_SPECIES = {
    "DeTodo": ["snorlax", "rattata", "raticate", "meowth", "persian", "kangaskhan"],
    "Pescado": ["seel", "dewgong", "psyduck", "golduck"],
    "EnergiaEspiritual": ["drowzee", "hypno"],
    "Residuos": ["grimer", "muk", "koffing", "weezing"],
    "NectarPolen": ["butterfree", "beedrill", "venomoth", "venonat"],
    "BayasFrutas": ["pikachu", "raichu", "clefairy", "clefable", "jigglypuff", "wigglytuff",
                    "eevee", "vaporeon", "jolteon", "flareon"],
    "InsectosSemillas": ["farfetchd", "doduo", "dodrio"],
}
DIET_BY_TYPE = {
    "Fuego": "FuegoCalor", "Agua": "AlgasPlancton", "Planta": "HojasPlantas", "Electrico": "Electricidad",
    "Roca": "Minerales", "Tierra": "Minerales", "Acero": "Minerales", "Fantasma": "EnergiaEspiritual",
    "Volador": "InsectosSemillas", "Bicho": "HojasPlantas", "Lucha": "DeTodo", "Dragon": "AlgasPlancton",
}

SUPPORT_SPECIES = {"chansey", "clefairy", "clefable", "jigglypuff", "wigglytuff", "venusaur",
                   "butterfree", "mr-mime"}


# --------------------------------------------------------------------------- red y caché

def fetch(path_or_url):
    """GET a PokeAPI con caché en disco y reintentos. Devuelve None si no existe (404)."""
    url = path_or_url if path_or_url.startswith("http") else API + path_or_url.strip("/") + "/"
    key = re.sub(r"[^a-z0-9]+", "_", url[len(API):].lower()).strip("_") or "root"
    cache_file = CACHE_DIR / f"{key}.json"

    if cache_file.exists():
        data = json.loads(cache_file.read_text(encoding="utf-8"))
        return None if data == {"__missing__": True} else data

    for attempt in range(5):
        try:
            request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(request, timeout=30) as response:
                data = json.loads(response.read().decode("utf-8"))
            cache_file.write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
            return data
        except urllib.error.HTTPError as error:
            if error.code == 404:
                cache_file.write_text(json.dumps({"__missing__": True}), encoding="utf-8")
                return None
            wait = 2 ** attempt
            print(f"  HTTP {error.code} en {url}, reintento en {wait} s", file=sys.stderr)
            time.sleep(wait)
        except (urllib.error.URLError, TimeoutError) as error:
            wait = 2 ** attempt
            print(f"  Error de red en {url} ({error}), reintento en {wait} s", file=sys.stderr)
            time.sleep(wait)

    raise RuntimeError(f"No se pudo descargar {url}")


def fetch_many(paths, workers):
    paths = list(dict.fromkeys(paths))
    with ThreadPoolExecutor(max_workers=workers) as pool:
        return dict(zip(paths, pool.map(fetch, paths)))


def id_from_url(url):
    return int(url.rstrip("/").rsplit("/", 1)[-1])


# --------------------------------------------------------------------------- textos

def localized(entries, field="name", language="es"):
    for entry in entries:
        if entry["language"]["name"] == language:
            return entry[field]
    return None


def name_es(resource):
    """Nombre en español con respaldo en inglés y luego la clave."""
    if resource is None:
        return None
    names = resource.get("names", [])
    return localized(names, language="es") or localized(names, language="en") or resource["name"]


def clean_text(text):
    return re.sub(r"\s+", " ", text.replace("\f", " ").replace("­", "")).strip() if text else None


def latest_flavor_text(entries, language="es"):
    texts = [e for e in entries if e["language"]["name"] == language]
    if not texts:
        return None
    # Especies, movimientos y habilidades usan "flavor_text"; los objetos, "text".
    return clean_text(texts[-1].get("flavor_text") or texts[-1].get("text"))


def short_effect(resource):
    """Descripción en español: resumen de PokeAPI si existe; si no, el texto del juego más reciente."""
    entries = resource.get("effect_entries", [])
    text = (localized(entries, "short_effect", "es")
            or latest_flavor_text(resource.get("flavor_text_entries", []))
            or localized(entries, "short_effect", "en"))
    chance = resource.get("effect_chance")
    if text and chance is not None:
        text = text.replace("$effect_chance", str(chance))
    return clean_text(text)


# --------------------------------------------------------------------------- conversión

def convert_stats(pokemon):
    return {STAT_NAMES[s["stat"]["name"]]: s["base_stat"] for s in pokemon["stats"]}


def convert_types(pokemon):
    return [type_name(t["type"]["name"]) for t in sorted(pokemon["types"], key=lambda t: t["slot"])]


def choose_learnset(pokemon):
    """Elige el juego preferido en el que la especie aprende movimientos por nivel."""
    by_group = {}
    for entry in pokemon["moves"]:
        move_key = entry["move"]["name"]
        for detail in entry["version_group_details"]:
            group = detail["version_group"]["name"]
            method = detail["move_learn_method"]["name"]
            by_group.setdefault(group, []).append((move_key, method, detail["level_learned_at"]))

    for group in VERSION_GROUP_PREFERENCE:
        entries = by_group.get(group, [])
        if any(method == "level-up" for _, method, _ in entries):
            level_up = sorted(
                {(level, key) for key, method, level in entries if method == "level-up"},
                key=lambda pair: (pair[0], pair[1]),
            )
            machine = sorted({key for key, method, _ in entries if method == "machine"})
            return {
                "versionGroup": group,
                "levelUp": [{"level": level, "move": key} for level, key in level_up],
                "machine": machine,
            }

    return {"versionGroup": None, "levelUp": [], "machine": []}


def convert_evolution_detail(detail):
    def key(field):
        value = detail.get(field)
        return value["name"] if isinstance(value, dict) else value

    result = {
        "trigger": key("trigger"),
        "minLevel": detail.get("min_level"),
        "item": key("item"),
        "heldItem": key("held_item"),
        "minHappiness": detail.get("min_happiness"),
        "timeOfDay": detail.get("time_of_day") or None,
        "knownMove": key("known_move"),
        "knownMoveType": type_name(key("known_move_type")) if key("known_move_type") else None,
        "location": key("location"),
        "gender": detail.get("gender"),
    }
    return {k: v for k, v in result.items() if v is not None}


def walk_chain(node, parent_id, out):
    """Recorre la cadena evolutiva: out[id] = {"from": id_padre, "to": [evoluciones]}."""
    species_id = id_from_url(node["species"]["url"])
    entry = out.setdefault(species_id, {"from": parent_id, "to": []})
    entry["from"] = parent_id
    for child in node["evolves_to"]:
        child_id = id_from_url(child["species"]["url"])
        details = []
        for detail in child["evolution_details"]:
            converted = convert_evolution_detail(detail)
            if converted not in details:  # PokeAPI repite métodos iguales de distintos juegos
                details.append(converted)
        details = details or [{}]
        entry["to"].append({"species": child_id, "methods": details})
        walk_chain(child, species_id, out)


def type_name(key):
    return TYPE_NAMES.get(key, key.capitalize())


def damage_class(move):
    return (move.get("damage_class") or {}).get("name", "status")


def suggested_shape(move):
    """Plantilla de forma sugerida (GDD §8.4). Se puede corregir en overrides.json."""
    category = damage_class(move)
    target = move["target"]["name"]
    meta = move.get("meta") or {}
    multi = target in ("all-opponents", "all-other-pokemon", "entire-field", "opponents-field")

    if category == "status":
        return "Self" if target in ("user", "users-field", "user-and-allies", "ally") else "GroundArea" if multi else "Projectile"
    if category == "physical":
        if multi:
            return "GroundArea"
        if move.get("priority", 0) > 0 or (meta.get("drain") or 0) < 0:
            return "Dash"
        return "Melee"
    return "Cone" if multi else "Projectile"


def convert_move(move):
    meta = move.get("meta") or {}
    ailment = (meta.get("ailment") or {}).get("name")
    return {
        "id": move["id"],
        "key": move["name"],
        "name": name_es(move),
        "nameEn": localized(move["names"], language="en") or move["name"],
        "type": type_name(move["type"]["name"]),
        "category": {"physical": "Fisico", "special": "Especial", "status": "Estado"}[damage_class(move)],
        "power": move.get("power"),
        "accuracy": move.get("accuracy"),
        "pp": move.get("pp"),
        "priority": move.get("priority", 0),
        "target": move["target"]["name"],
        "ailment": None if ailment in (None, "none") else ailment,
        "ailmentChance": meta.get("ailment_chance") or 0,
        "flinchChance": meta.get("flinch_chance") or 0,
        "critRate": meta.get("crit_rate") or 0,
        "drain": meta.get("drain") or 0,
        "healing": meta.get("healing") or 0,
        "statChanges": [{"stat": c["stat"]["name"], "change": c["change"]} for c in move.get("stat_changes", [])],
        "statChance": meta.get("stat_chance") or 0,
        "suggestedShape": suggested_shape(move),
        "shortEffect": short_effect(move),
    }


# --------------------------------------------------------------------------- overrides

def default_role(species):
    """Rol del MOBA sugerido según estadísticas base (GDD §7.3)."""
    if species["key"] in SUPPORT_SPECIES:
        return "Soporte"
    s = species["baseStats"]
    offense = max(s["attack"], s["spAttack"])
    guard = (s["defense"] + s["spDefense"]) / 2
    bulk = s["hp"] + s["defense"] + s["spDefense"]
    sturdy = guard >= offense + 15 or (bulk >= 230 and bulk >= 2.6 * offense)
    if s["speed"] < 90 and bulk >= 190 and sturdy:
        return "Defensor"
    if s["spAttack"] >= s["attack"] + 10 and s["spAttack"] >= 60:
        return "Atacante"
    if s["speed"] >= 90:
        return "Veloz"
    return "Luchador" if s["attack"] >= s["spAttack"] else "Atacante"


def default_diet(species):
    for diet, keys in DIET_BY_SPECIES.items():
        if species["key"] in keys:
            return diet
    return DIET_BY_TYPE.get(species["types"][0], "BayasFrutas")


def build_overrides(species_list):
    zone_of = {key: zone for zone, keys in ZONES.items() for key in keys}
    species = {}
    for s in species_list:
        species[s["key"]] = {
            "role": default_role(s),
            "diet": default_diet(s),
            "favoriteFlavor": None,
            # null = no aparece en estado salvaje (se obtiene evolucionando) o falta asignarle zona
            "zone": zone_of.get(s["key"]),
            # Movimientos del combate en tiempo real (GDD §8.4); null = se eligen automáticamente
            "basicMove": None,
            "skills": [],
            "ultimate": None,
        }
    return {
        "_info": "Ajustes a mano que no vienen de PokeAPI. El importador no sobrescribe este archivo "
                 "(salvo con --reset-overrides). En 'moves' se puede corregir la forma sugerida, "
                 "por ejemplo: \"tackle\": {\"shape\": \"Dash\"}.",
        "species": species,
        "moves": {},
    }


# --------------------------------------------------------------------------- proceso principal

def parse_range(text):
    match = re.fullmatch(r"(\d+)-(\d+)", text)
    if not match:
        raise argparse.ArgumentTypeError("El rango tiene que ser del tipo 1-151")
    first, last = int(match.group(1)), int(match.group(2))
    if first < 1 or last < first:
        raise argparse.ArgumentTypeError("Rango no válido")
    return first, last


def main():
    parser = argparse.ArgumentParser(description="Descarga datos de Pokémon desde PokeAPI para PokeMoba.")
    parser.add_argument("--range", type=parse_range, default=(1, 151), help="Rango de números de Pokédex (por defecto 1-151)")
    parser.add_argument("--workers", type=int, default=6, help="Peticiones en paralelo (por defecto 6)")
    parser.add_argument("--output", type=Path, default=None, help="Archivo de salida")
    parser.add_argument("--reset-overrides", action="store_true", help="Regenera overrides.json (pierde los cambios a mano)")
    args = parser.parse_args()

    first, last = args.range
    ids = list(range(first, last + 1))
    output = args.output or DATA_DIR / f"pokeapi_{first:03d}-{last:03d}.json"
    CACHE_DIR.mkdir(parents=True, exist_ok=True)
    output.parent.mkdir(parents=True, exist_ok=True)

    print(f"1/6 Especies {first}–{last}…")
    species_raw = fetch_many([f"pokemon-species/{i}" for i in ids], args.workers)
    pokemon_raw = fetch_many([f"pokemon/{i}" for i in ids], args.workers)

    print("2/6 Cadenas evolutivas…")
    chain_urls = {species_raw[f"pokemon-species/{i}"]["evolution_chain"]["url"] for i in ids}
    chains = fetch_many(sorted(chain_urls), args.workers)
    evolution = {}
    for chain in chains.values():
        walk_chain(chain["chain"], None, evolution)

    print("3/6 Formas Mega y Gigamax…")
    mega_paths, gmax = [], {}
    for i in ids:
        for variety in species_raw[f"pokemon-species/{i}"]["varieties"]:
            form = variety["pokemon"]["name"]
            if "-mega" in form:
                mega_paths.append(f"pokemon/{form}")
            elif form.endswith("-gmax"):
                gmax[i] = True
    megas_raw = fetch_many(mega_paths, args.workers)

    stone_keys = {}
    for path in mega_paths:
        form = path.split("/", 1)[1]
        if form in MEGA_STONES:
            stone_keys[form] = MEGA_STONES[form]
            continue
        base = form.split("-mega", 1)[0]
        suffix = form.split("-mega", 1)[1]  # "", "-x", "-y"…
        for candidate in (f"{base}ite{suffix}", f"{base}nite{suffix}"):
            if fetch(f"item/{candidate}") is not None:
                stone_keys[form] = candidate
                break

    print("4/6 Movimientos y habilidades…")
    learnsets = {i: choose_learnset(pokemon_raw[f"pokemon/{i}"]) for i in ids}
    move_keys = set()
    for learnset in learnsets.values():
        move_keys.update(entry["move"] for entry in learnset["levelUp"])
        move_keys.update(learnset["machine"])
    moves_raw = fetch_many([f"move/{key}" for key in sorted(move_keys)], args.workers)

    ability_keys = set()
    for pokemon in list(pokemon_raw.values()) + [m for m in megas_raw.values() if m]:
        ability_keys.update(a["ability"]["name"] for a in pokemon["abilities"])
    abilities_raw = fetch_many([f"ability/{key}" for key in sorted(ability_keys)], args.workers)

    print("5/6 Objetos de evolución y megapiedras…")
    item_keys = set(stone_keys.values())
    for entry in evolution.values():
        for target in entry["to"]:
            for method in target["methods"]:
                item_keys.update(k for k in (method.get("item"), method.get("heldItem")) if k)
    items_raw = fetch_many([f"item/{key}" for key in sorted(item_keys)], args.workers)

    print("6/6 Generando JSON…")
    in_range = set(ids)
    species_out = []
    for i in ids:
        species = species_raw[f"pokemon-species/{i}"]
        pokemon = pokemon_raw[f"pokemon/{i}"]
        evo = evolution.get(i, {"from": None, "to": []})

        megas = []
        for variety in species["varieties"]:
            form = variety["pokemon"]["name"]
            mega = megas_raw.get(f"pokemon/{form}")
            if mega is None:
                continue
            megas.append({
                "key": form,
                "types": convert_types(mega),
                "baseStats": convert_stats(mega),
                "ability": mega["abilities"][0]["ability"]["name"] if mega["abilities"] else None,
                "stone": stone_keys.get(form),
                "heightM": mega["height"] / 10,
                "weightKg": mega["weight"] / 10,
            })

        species_out.append({
            "id": i,
            "key": species["name"],
            "name": name_es(species),
            "nameEn": localized(species["names"], language="en") or species["name"],
            "genus": localized(species["genera"], "genus", "es") or localized(species["genera"], "genus", "en"),
            "flavorText": latest_flavor_text(species["flavor_text_entries"]),
            "types": convert_types(pokemon),
            "baseStats": convert_stats(pokemon),
            "abilities": [
                {"ability": a["ability"]["name"], "hidden": a["is_hidden"]}
                for a in sorted(pokemon["abilities"], key=lambda a: a["slot"])
            ],
            "captureRate": species["capture_rate"],
            "baseExperience": pokemon.get("base_experience"),
            "baseHappiness": species.get("base_happiness"),
            "growthRate": species["growth_rate"]["name"],
            "genderRate": species["gender_rate"],  # -1 sin género; si no, octavos de probabilidad de ser hembra
            "heightM": pokemon["height"] / 10,
            "weightKg": pokemon["weight"] / 10,
            "habitat": (species.get("habitat") or {}).get("name"),
            "color": species["color"]["name"],
            "shape": (species.get("shape") or {}).get("name"),
            "eggGroups": [g["name"] for g in species["egg_groups"]],
            "isLegendary": species["is_legendary"],
            "isMythical": species["is_mythical"],
            "evolvesFrom": evo["from"] if evo["from"] in in_range else None,
            "evolutions": [t for t in evo["to"] if t["species"] in in_range],
            "learnset": learnsets[i],
            "megas": megas,
            "hasGigantamax": gmax.get(i, False),
        })

    moves_out = sorted((convert_move(m) for m in moves_raw.values() if m), key=lambda m: m["id"])
    abilities_out = sorted(
        (
            {
                "id": a["id"],
                "key": a["name"],
                "name": name_es(a),
                "nameEn": localized(a["names"], language="en") or a["name"],
                "shortEffect": short_effect(a),
            }
            for a in abilities_raw.values() if a
        ),
        key=lambda a: a["id"],
    )
    items_out = sorted(
        (
            {"key": it["name"], "name": name_es(it), "category": it["category"]["name"], "shortEffect": short_effect(it)}
            for it in items_raw.values() if it
        ),
        key=lambda it: it["key"],
    )

    result = {
        "source": "https://pokeapi.co",
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "range": [first, last],
        "species": species_out,
        "moves": moves_out,
        "abilities": abilities_out,
        "items": items_out,
    }
    output.write_text(json.dumps(result, ensure_ascii=False, indent=1), encoding="utf-8")

    overrides_file = DATA_DIR / "overrides.json"
    if args.reset_overrides or not overrides_file.exists():
        overrides = build_overrides(species_out)
        overrides_file.write_text(json.dumps(overrides, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"Creado {overrides_file}")
    else:
        overrides = json.loads(overrides_file.read_text(encoding="utf-8"))
        missing = [s["key"] for s in species_out if s["key"] not in overrides.get("species", {})]
        if missing:
            print(f"  Aviso: especies sin entrada en overrides.json: {', '.join(missing)}")

    without_learnset = [s["key"] for s in species_out if s["learnset"]["versionGroup"] is None]
    no_zone = [
        s["key"] for s in species_out
        if s["evolvesFrom"] is None and overrides.get("species", {}).get(s["key"], {}).get("zone") is None
    ]
    print(f"Listo: {output}")
    print(f"  {len(species_out)} especies, {len(moves_out)} movimientos, {len(abilities_out)} habilidades, "
          f"{len(items_out)} objetos, {sum(len(s['megas']) for s in species_out)} formas Mega")
    if without_learnset:
        print(f"  Aviso: sin movimientos por nivel en ningún juego preferido: {', '.join(without_learnset)}")
    if no_zone:
        print(f"  Pendiente: especies base sin zona asignada en overrides.json: {', '.join(no_zone)}")


if __name__ == "__main__":
    main()
