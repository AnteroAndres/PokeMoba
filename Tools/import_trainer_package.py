"""Blender conversion of the locally extracted trainers.7z package.
Uses the reviewed Pokemon Switch V3 importer in tmp/trainers-package/converter.
No add-on is installed globally. Original Roshi/Ash assets are excluded.
"""
import bpy,sys,types,importlib,json,hashlib,math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'tmp/trainers-package/deps'))
pkg=types.ModuleType('trinity_import');pkg.__path__=[str(root/'tmp/trainers-package/converter')];sys.modules['trinity_import']=pkg
import requests
requests.get=lambda *a,**k: (_ for _ in ()).throw(OSError('Offline conversion; use bundled shader'))
converter=importlib.import_module('trinity_import.PokemonSwitch')
source=root/'tmp/trainers-package/source'
dest=root/'UnityProject/Assets/_Project/Resources/Models/Trainers/Package'
only=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else None
records=[]
for directory in sorted(source.iterdir()):
 if not directory.is_dir() or only and directory.name!=only:continue
 output=dest/directory.name
 metadata=output/'import.json'
 if metadata.exists():records.append(json.loads(metadata.read_text('utf8')));print('SKIP',directory.name,flush=True);continue
 bpy.ops.wm.read_factory_settings(use_empty=True)
 model=next(directory.glob('*.trmdl'))
 converter.from_trmdlsv(str(directory),model.name,False,False,True,False,False,False)
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 assert meshes,directory
 bpy.ops.object.select_all(action='DESELECT')
 for mesh in meshes:mesh.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0]
 bpy.ops.object.join()
 mesh=bpy.context.object;mesh.name='Body'
 output.mkdir(parents=True,exist_ok=True);(output/'tex').mkdir(exist_ok=True)
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=1
 bindings=[];targets=[]
 used=set(p.material_index for p in mesh.data.polygons)
 for i,slot in enumerate(mesh.material_slots):
  if i not in used:continue
  mat=slot.material
  image=bpy.data.images.new(mat.name+'_baked',width=512,height=512,alpha=True)
  image.file_format='PNG';image.filepath_raw=str(output/'tex'/(mat.name+'.png'))
  node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.nodes.active=node
  targets.append((mat,image))
 bpy.ops.object.bake(type='DIFFUSE',pass_filter={'COLOR'},use_clear=True,margin=4)
 for mat,image in targets:
  image.save()
  bindings.append({'material':mat.name,'texture':'tex/'+Path(image.filepath_raw).name})
  # The palette shader is baked to an ordinary albedo map for Unity and FBX.
  mat.node_tree.nodes.clear();out=mat.node_tree.nodes.new('ShaderNodeOutputMaterial');bsdf=mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled');tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
  mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);mat.node_tree.links.new(bsdf.outputs[0],out.inputs['Surface']);bsdf.inputs['Roughness'].default_value=.8
 bpy.ops.export_scene.fbx(filepath=str(output/'Model.fbx'),object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
 record={'key':'Package/'+directory.name,'source':model.relative_to(source).as_posix(),'sha256':hashlib.sha256(model.read_bytes()).hexdigest(),'bindings':bindings,'vertices':len(mesh.data.vertices),'bones':sum(len(a.bones) for a in bpy.data.armatures),'bakedAlbedo':True}
 metadata.write_text(json.dumps(record,indent=2)+'\n',encoding='utf8');records.append(record)
 print('CONVERTED',directory.name,len(bindings),'materials',record['vertices'],'vertices',flush=True)
if only is None:
 assert len(records)==33
 (dest/'import-report.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf8')
print('COMPLETE',len(records),flush=True)
