"""Run with Blender --background --python Tools/import_ch_characters.py.
Requires reviewed archives extracted by tar into tmp/characters-ch/source.
Only selected characters are converted; existing Ash/Roshi files are never written.
"""
import bpy,json,shutil,hashlib,math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
selection=json.loads((root/'Tools/ch_character_selection.json').read_text())
dest=root/'UnityProject/Assets/_Project/Resources/Models/Trainers/Package'
for entry in selection:
 folder=entry['folder'];source=root/'tmp/characters-ch/source'/folder/'Models'
 out=dest/folder;out.mkdir(parents=True,exist_ok=True);(out/'tex').mkdir(exist_ok=True)
 if (out/'import.json').exists():continue
 bpy.ops.wm.read_factory_settings(use_empty=True)
 fbx=source/(folder+'.fbx')
 if not fbx.exists():fbx=next(source.glob('*.fbx'))
 bpy.ops.import_scene.fbx(filepath=str(fbx),use_anim=False)
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 assert meshes,folder
 textures={p.name.lower():p for p in source.rglob('*.png')}
 body=next((p for name,p in textures.items() if name==folder+'_co.png'),None)
 if body is None:body=next((p for name,p in textures.items() if name.endswith('_co.png') and 'face' not in name),None)
 face=next((p for name,p in textures.items() if 'face' in name and name.endswith('_co.png')),body)
 bindings=[]
 for mat in bpy.data.materials:
  name=mat.name.lower();texture=None
  if mat.use_nodes:
   for node in mat.node_tree.nodes:
    if node.type=='TEX_IMAGE' and node.image:
     basename=Path(node.image.filepath.replace('\\','/')).name.lower()
     if basename.endswith('_co.png') and basename in textures:texture=textures[basename];break
  if texture is None:texture=face if any(t in name for t in ['face','eye','mouth','brow']) else body
  assert texture is not None,(folder,mat.name)
  target=out/'tex'/texture.name
  if not target.exists():shutil.copyfile(texture,target)
  mat.use_nodes=True;mat.node_tree.nodes.clear()
  output=mat.node_tree.nodes.new('ShaderNodeOutputMaterial');bsdf=mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
  tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(target),check_existing=True)
  mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);mat.node_tree.links.new(bsdf.outputs[0],output.inputs[0]);bsdf.inputs['Roughness'].default_value=.8
  bindings.append({'material':mat.name,'texture':'tex/'+target.name})
 bpy.ops.export_scene.fbx(filepath=str(out/'Model.fbx'),object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
 bones=[b.name for a in bpy.data.armatures for b in a.bones]
 assert bones,(folder,'missing skeleton')
 (out/'import.json').write_text(json.dumps({'key':'Package/'+folder,'name':entry['name'],'sourceArchive':'Characters ch.zip/'+folder+'.7z','sha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'bindings':bindings,'bones':len(bones),'boneNames':bones,'vertices':sum(len(m.data.vertices) for m in meshes)},indent=2))
 print('CONVERTED',folder,'bones',len(bones),'materials',len(bindings),flush=True)
print('COMPLETE',len(selection),flush=True)
