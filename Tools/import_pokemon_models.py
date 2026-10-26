"""Import the user-supplied Gen1 ZIP without requiring Cinema4D or Blender.

One Unity-compatible FBX/OBJ/DAE per dex entry, its texture files, and explicit
FBX material/texture bindings are retained. Existing different model/texture
files are never overwritten; generated JSON import metadata is refreshed. Run with --check to audit an existing import.
"""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'UnityProject/Assets/_Project/Resources/Models/Pokemon/Imported'
PREFIX = 'PoGo-3D-Assets-master/Pokemon-Gen1/'


def fbx_nodes(data):
    """Read FBX node metadata; skip geometry arrays (no decompression needed)."""
    if not data.startswith(b'Kaydara FBX Binary'):
        raise ValueError('Expected binary FBX')
    wide = struct.unpack_from('<I', data, 23)[0] >= 7500
    header, header_size = ('<QQQB', 25) if wide else ('<IIIB', 13)

    def node(offset):
        end, count, _, name_len = struct.unpack_from(header, data, offset)
        if not end:
            return None, offset + header_size
        pos = offset + header_size
        name = data[pos:pos+name_len].decode('utf8', errors='replace')
        pos += name_len
        props = []
        for _ in range(count):
            kind = chr(data[pos]); pos += 1
            formats = {'Y': '<h', 'C': '<?', 'I': '<i', 'F': '<f', 'D': '<d', 'L': '<q'}
            if kind in formats:
                fmt = formats[kind]
                props.append(struct.unpack_from(fmt, data, pos)[0]); pos += struct.calcsize(fmt)
            elif kind in ('S', 'R'):
                length = struct.unpack_from('<I', data, pos)[0]; pos += 4
                raw = data[pos:pos+length]; pos += length
                props.append(raw.decode('utf8', errors='replace') if kind == 'S' else None)
            elif kind in 'fdlibc':
                length, encoding, compressed = struct.unpack_from('<III', data, pos); pos += 12
                pos += compressed
                props.append(None)
            else:
                raise ValueError(f'Unsupported FBX property {kind!r}')
        children = []
        while pos < end - header_size:
            child, pos = node(pos)
            if child is None:
                break
            children.append(child)
        return (name, props, children), end

    result, offset = [], 27
    while offset + header_size < len(data):
        item, offset = node(offset)
        if item is None:
            break
        result.append(item)
    return result


def clean_name(name):
    return name.split('\x00')[0].removeprefix('Material::').removeprefix('Texture::')


def material_bindings(data):
    nodes = {n[0]: n for n in fbx_nodes(data)}
    materials, textures = {}, {}
    for name, props, children in nodes.get('Objects', ('', [], []))[2]:
        if name == 'Material':
            materials[props[0]] = clean_name(props[1])
        elif name == 'Texture':
            paths = {c[0]: c[1][0] for c in children if c[0] in ('RelativeFilename', 'FileName') and c[1]}
            path = paths.get('RelativeFilename') or paths.get('FileName')
            if path:
                textures[props[0]] = path.replace('\\', '/').rsplit('/', 1)[-1]
    connections = nodes.get('Connections', ('', [], []))[2]
    # C4D connects Texture -> LayeredTexture -> Material. Resolve the intermediary.
    for _ in range(4):
        for _, props, _ in connections:
            if len(props) >= 3 and props[0] == 'OO' and props[1] in textures and props[2] not in materials:
                textures.setdefault(props[2], textures[props[1]])
    bindings = []
    for _, props, _ in connections:
        if len(props) >= 3 and props[1] in textures and props[2] in materials:
            channel = props[3] if len(props) > 3 else ''
            if not channel or any(x in channel.lower() for x in ('diffuse', 'color', 'transparent')):
                bindings.append({'material': materials[props[2]], 'texture': textures[props[1]], 'channel': channel})
    return bindings


def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists() and path.read_bytes() != data and path.suffix != '.json':
        raise FileExistsError(f'Refusing to replace a different file: {path}')
    if not path.exists() or path.suffix == '.json':
        path.write_bytes(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    dex = json.loads((ROOT / 'UnityProject/Assets/_Project/Resources/Data/pokeapi_001-151.json').read_text('utf8'))
    keys = {s['id']: s['key'] for s in dex['species']}
    report = []
    with zipfile.ZipFile(args.archive) as z:
        groups = {}
        for entry in z.infolist():
            if not entry.filename.startswith(PREFIX) or entry.is_dir():
                continue
            relative = entry.filename[len(PREFIX):]
            folder, name = relative.split('/', 1)
            if not re.match(r'^\d{3}', folder):
                continue
            groups.setdefault(folder, []).append((name, entry))
        for folder, entries in sorted(groups.items()):
            number = int(folder[:3])
            candidates = [(name, e) for name, e in entries if name.lower().endswith('.fbx') and '/' not in name]
            if not candidates:
                candidates = [(name, e) for name, e in entries if '/' not in name and name.lower().endswith('.dae')]
            if not candidates:
                candidates = [(name, e) for name, e in entries if name.lower().startswith('source/') and name.lower().endswith('.obj')]
            candidates = [(n,e) for n,e in candidates if not any(t in n.lower() for t in ('_split', '_anim'))]
            if len(candidates) != 1:
                raise ValueError(f'Ambiguous model: {folder}: {[n for n,e in candidates]}')
            name, entry = candidates[0]
            target = DEST / f'{number:03}_{keys[number]}'
            payload = z.read(entry)
            bindings = material_bindings(payload) if name.lower().endswith('.fbx') else []
            mtls = [(n,e) for n,e in entries if n.lower().endswith('.mtl') and name.lower().endswith('.obj')]
            for n,e in mtls:
                material = ''
                for line in z.read(e).decode('utf8', errors='replace').splitlines():
                    if line.startswith('newmtl '): material = line[7:].strip()
                    if line.startswith('map_Kd '):
                        bindings.append({'material': material, 'texture': line[7:].strip().replace('\\','/').rsplit('/',1)[-1], 'channel': 'DiffuseColor'})
            model_file = 'Model' + PurePosixPath(name).suffix.lower()
            if model_file.endswith('.obj'):
                assert b'\nf ' in payload, f'No OBJ geometry: {name}'
            if model_file.endswith('.fbx'):
                objects = next(n for n in fbx_nodes(payload) if n[0] == 'Objects')
                assert any(n[0] == 'Geometry' and n[1][2] == 'Mesh' for n in objects[2]), f'No FBX mesh: {name}'
            images = [(n,e) for n,e in entries if PurePosixPath(n).suffix.lower() in ('.png','.tga','.jpg') and not n.startswith('placeholder/')]
            by_name = {}
            for n,e in images:
                by_name.setdefault(PurePosixPath(n).name.lower(), []).append((n,e))
            missing = []
            for binding in bindings:
                matches = by_name.get(binding['texture'].lower(), [])
                if matches:
                    # Prefer prepared tex/ over duplicate source textures.
                    n,e = min(matches, key=lambda pair: (not pair[0].startswith('tex/'), len(pair[0])))
                    binding['texture'] = n
                else:
                    missing.append(binding['texture'])
            record = {'dex': number, 'key': keys[number], 'sourceModel': entry.filename,
                      'sha256': hashlib.sha256(payload).hexdigest(), 'placeholder': False, 'modelFile': model_file,
                      'bindings': bindings, 'missingTextures': sorted(set(missing)), 'textureCount': len(images)}
            if not args.check:
                save(target/model_file, payload)
                for n,e in mtls:
                    save(target/PurePosixPath(n).name, z.read(e))
                for n,e in images:
                    relative = PurePosixPath(n)
                    if relative.is_absolute() or '..' in relative.parts:
                        raise ValueError(f'Unsafe archive path: {n}')
                    save(target.joinpath(*relative.parts), z.read(e))
                save(target/'import.json', (json.dumps(record, indent=2, ensure_ascii=False)+'\n').encode('utf8'))
            else:
                assert (target/model_file).read_bytes() == payload, target
                for n,e in mtls:
                    assert (target/PurePosixPath(n).name).read_bytes() == z.read(e), n
                for n,e in images:
                    assert target.joinpath(*PurePosixPath(n).parts).read_bytes() == z.read(e), n
                assert json.loads((target/'import.json').read_text('utf8')) == record
            report.append(record)
        assert {r['dex'] for r in report} == set(range(1,152)), 'Incomplete Gen1 coverage'
        assert not any(r['missingTextures'] for r in report), 'Missing texture dependencies'
        if not args.check:
            save(DEST/'SOURCE_README.md', z.read('PoGo-3D-Assets-master/README.md'))
            save(DEST/'import-report.json', (json.dumps(report, indent=2, ensure_ascii=False)+'\n').encode('utf8'))
    print(f'{"Verified" if args.check else "Imported"} {len(report)} models and {sum(r["textureCount"] for r in report)} textures')
    print('Formats:', {ext: sum(Path(r['modelFile']).suffix == ext for r in report) for ext in ('.fbx', '.obj', '.dae')})
    for record in report:
        if record['missingTextures']:
            print(f'Missing source textures {record["dex"]}: {record["missingTextures"]}')


if __name__ == '__main__':
    main()
