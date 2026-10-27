"""Run using Blender -b --python Tools/import_animated_pokemon.py -- ZIP [--dex N].
Converts GLBs to FBX with a single baked timeline and explicit Unity clip ranges.
The GLB actions use multiple Blender 4.5 slots; exporting 'all actions' directly
would lose non-armature object tracks. Synchronised NLA strips bake all slots.
"""
import argparse, hashlib, json, math, sys, zipfile
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'UnityProject/Assets/_Project/Resources/Models/Pokemon/Animated'
CLIP_ORDER = ('idle','walk','run','attack','happy','sleep')


def convert(z, number, key):
    destination = OUT/f'{number:03}_{key}'
    record_path = destination/'import.json'
    source = f'pokemon-master/public/models/{number:03}/model.glb'
    raw = z.read(source)
    checksum = hashlib.sha256(raw).hexdigest()
    if record_path.exists():
        record = json.loads(record_path.read_text('utf8'))
        if record.get('sha256') == checksum and record.get('converterVersion') == 3 and (destination/'Model.fbx').exists():
            print(f'SKIP {number:03}', flush=True)
            return record
        if record.get('sha256') != checksum: raise FileExistsError(f'Different existing conversion: {destination}')
    destination.mkdir(parents=True, exist_ok=True)
    temporary = ROOT/f'tmp/animated-pokemon/{number:03}.glb'
    temporary.parent.mkdir(parents=True, exist_ok=True)
    temporary.write_bytes(raw)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = 30
    bpy.ops.import_scene.gltf(filepath=str(temporary))
    # Blender's glTF importer creates Icospheres for bone widgets. They are not Pokemon geometry.
    widgets = {bone.custom_shape for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.pose.bones if bone.custom_shape is not None}
    for widget in widgets: bpy.data.objects.remove(widget, do_unlink=True)
    actions = {a.name:a for a in bpy.data.actions}
    assert set(CLIP_ORDER[:-1]).issubset(actions), (number, list(actions))
    # Preserve each object's action slot before removing the imported overlapping tracks.
    slots = {}
    for obj in list(scene.objects):
        if not obj.animation_data: continue
        for track in obj.animation_data.nla_tracks:
            for strip in track.strips:
                slots[(obj.name, strip.action.name)] = strip.action_slot
        obj.animation_data.action = None
        for track in list(obj.animation_data.nla_tracks): obj.animation_data.nla_tracks.remove(track)
    clips, start = [], 0
    for name in CLIP_ORDER:
        if name not in actions: continue
        action = actions[name]
        first, last = action.frame_range
        duration = max(1, math.ceil(last-first))
        for obj in list(scene.objects):
            slot = slots.get((obj.name, name))
            if slot is None: continue
            track = obj.animation_data.nla_tracks.new()
            track.name = name
            strip = track.strips.new(name, start, action)
            strip.action_slot = slot
            strip.action_frame_start = first
            strip.action_frame_end = last
            strip.frame_start = start
            strip.frame_end = start + duration
            strip.blend_type = 'REPLACE'
            strip.extrapolation = 'NOTHING'
        clips.append({'name':name,'firstFrame':start,'lastFrame':start+duration,'loop':name in ('idle','walk','run','sleep')})
        start += duration + 2
    scene.frame_start = 0
    scene.frame_end = start - 2
    scene.frame_set(0)
    bindings = []
    tex = destination/'tex'; tex.mkdir(exist_ok=True)
    for image in bpy.data.images:
        if image.type != 'IMAGE': continue
        filename = ''.join(c if c.isalnum() or c in '_-' else '_' for c in image.name) + '.png'
        image.filepath_raw = str(tex/filename)
        image.file_format = 'PNG'
        if image.packed_file:
            packed = bytes(image.packed_file.data)
            if packed.startswith(b'\x89PNG\r\n\x1a\n'):
                (tex/filename).write_bytes(packed)
            else:
                _ = image.pixels[0]  # Decode packed JPEG before encoding a real PNG.
                image.save()
        else:
            image.save()
    def image_for_socket(socket, visited=None):
        visited = set() if visited is None else visited
        for link in socket.links:
            node = link.from_node
            if node in visited: continue
            visited.add(node)
            if node.type == 'TEX_IMAGE' and node.image: return node.image
            for input in node.inputs:
                found = image_for_socket(input, visited)
                if found: return found
        return None
    for material in bpy.data.materials:
        if material.node_tree is None: continue
        principled = next((n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if not principled: continue
        for socket, channel in [('Base Color','DiffuseColor'),('Normal','NormalMap')]:
            image = image_for_socket(principled.inputs[socket])
            if image:
                bindings.append({'material':material.name,'texture':'tex/'+Path(image.filepath_raw).name,'channel':channel})
    # Exclude visualization helper objects (lights/cameras) and avoid extra end bones.
    bpy.ops.export_scene.fbx(filepath=str(destination/'Model.fbx'),
        object_types={'ARMATURE','MESH','EMPTY'}, use_selection=False,
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        add_leaf_bones=False, use_armature_deform_only=False,
        bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
        bake_anim_use_all_bones=True, bake_anim_step=1, bake_anim_simplify_factor=0,
        path_mode='RELATIVE', embed_textures=False)
    record = {'converterVersion':3,'dex':number,'key':key,'sourceModel':source,'sha256':checksum,
              'modelFile':'Model.fbx','fps':30,'clips':clips,'bindings':bindings,
              'textureCount':len(list(tex.glob('*.png')))}
    record_path.write_text(json.dumps(record,indent=2)+'\n',encoding='utf8')
    (destination/'icon.png').write_bytes(z.read(f'pokemon-master/public/models/{number:03}/icon.png'))
    temporary.unlink()
    print(f'CONVERTED {number:03} {key}: {len(clips)} clips, {record["textureCount"]} textures',flush=True)
    return record


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('archive',type=Path)
    parser.add_argument('--dex',type=int)
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    data = json.loads((ROOT/'UnityProject/Assets/_Project/Resources/Data/pokeapi_001-151.json').read_text('utf8'))
    with zipfile.ZipFile(args.archive) as z:
        records=[]
        for species in data['species']:
            if args.dex is not None and args.dex != species['id']: continue
            records.append(convert(z,species['id'],species['key']))
        if args.dex is None:
            assert len(records)==151
            (OUT/'import-report.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf8')
            for name in ('README.md','LICENSE'):
                (OUT/('SOURCE_'+name)).write_bytes(z.read('pokemon-master/'+name))
        print('COMPLETE',len(records),sum(len(r['clips']) for r in records),flush=True)

if __name__=='__main__': main()
