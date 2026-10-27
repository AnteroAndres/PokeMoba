"""Audit converted animated Pokemon against the supplied GLB archive (no Unity needed)."""
import hashlib,json,struct,sys,zipfile
from pathlib import Path
from import_pokemon_models import fbx_nodes, clean_name

root=Path(__file__).resolve().parents[1]
out=root/'UnityProject/Assets/_Project/Resources/Models/Pokemon/Animated'
archive=Path(sys.argv[1])
records=[]
with zipfile.ZipFile(archive) as z:
    dex=json.loads((root/'UnityProject/Assets/_Project/Resources/Data/pokeapi_001-151.json').read_text('utf8'))
    for species in dex['species']:
        number,key=species['id'],species['key']
        folder=out/f'{number:03}_{key}'
        record=json.loads((folder/'import.json').read_text('utf8'))
        raw=z.read(f'pokemon-master/public/models/{number:03}/model.glb')
        assert record['converterVersion']==3
        assert hashlib.sha256(raw).hexdigest()==record['sha256'],key
        length=struct.unpack_from('<I',raw,12)[0]
        source=json.loads(raw[20:20+length])
        assert set(c['name'] for c in record['clips'])==set(c['name'] for c in source['animations']),key
        objects=next(n[2] for n in fbx_nodes((folder/'Model.fbx').read_bytes()) if n[0]=='Objects')
        meshes=[n for n in objects if n[0]=='Geometry' and n[1][2]=='Mesh']
        assert len(meshes)==len(source['meshes']),(key,len(meshes),len(source['meshes']))
        assert any(n[0]=='Deformer' and n[1][2]=='Skin' for n in objects),f'Missing skin {key}'
        assert any(n[0]=='AnimationCurve' for n in objects),f'Missing animation curves {key}'
        assert len([n for n in objects if n[0]=='AnimationStack'])==1,key
        materials={clean_name(n[1][1]) for n in objects if n[0]=='Material'}
        for binding in record['bindings']:
            assert binding['material'] in materials,(key,binding['material'])
            image=folder/binding['texture']; assert image.is_file(),image
            assert image.read_bytes()[:8]==b'\x89PNG\r\n\x1a\n',image
        assert len(list((folder/'tex').glob('*.png')))==len(source.get('images',[])),key
        previous=-1
        for clip in record['clips']:
            assert clip['firstFrame']>previous and clip['lastFrame']>clip['firstFrame'],key
            previous=clip['lastFrame']
        assert (folder/'icon.png').read_bytes()==z.read(f'pokemon-master/public/models/{number:03}/icon.png')
        records.append(record)
assert len(records)==151
assert sum(len(r['clips']) for r in records)==869
print(f'PASS: {len(records)} models, {sum(len(r["clips"]) for r in records)} clips, {sum(r["textureCount"] for r in records)} textures; meshes, skins, animation curves, source hashes and all texture references verified.')
