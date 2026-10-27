import bpy,json
from pathlib import Path
root=Path(r'C:/Users/andres/Documents/PokeMoba/UnityProject/Assets/_Project/Resources/Models/Pokemon/Animated')
for key in ('001_bulbasaur','025_pikachu','143_snorlax'):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(root/key/'Model.fbx'))
 record=json.loads((root/key/'import.json').read_text('utf8'))
 clip=next(c for c in record['clips'] if c['name']=='walk')
 scene=bpy.context.scene
 def coordinates(frame):
  scene.frame_set(frame);deps=bpy.context.evaluated_depsgraph_get();points=[]
  for obj in scene.objects:
   if obj.type!='MESH': continue
   evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh()
   points.extend(evaluated.matrix_world@v.co for v in mesh.vertices)
   evaluated.to_mesh_clear()
  return points
 first=coordinates(clip['firstFrame']+1)
 second=coordinates(round(clip['firstFrame']+(clip['lastFrame']-clip['firstFrame'])*.33)+1)
 delta=max((a-b).length for a,b in zip(first,second))
 assert delta>.001,(key,delta)
 print('ANIMATION_SAMPLE_PASS',key,'max_vertex_displacement',round(delta,5),flush=True)
