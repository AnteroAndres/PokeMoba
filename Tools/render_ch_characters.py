"""Blender thumbnail rendering for the selected CH catalog (does not modify models)."""
import bpy,json,math
from mathutils import Vector
from pathlib import Path
root=Path(__file__).resolve().parents[1]
selection=json.loads((root/'Tools/ch_character_selection.json').read_text())
for entry in selection:
 folder=entry['folder'];out=root/'UnityProject/Assets/_Project/Resources/Models/Trainers/Icons/Package'/(folder+'.png')
 out.parent.mkdir(parents=True,exist_ok=True)
 if out.exists():continue
 bpy.ops.wm.read_factory_settings(use_empty=True)
 source=root/'UnityProject/Assets/_Project/Resources/Models/Trainers/Package'/folder/'Model.fbx'
 bpy.ops.import_scene.fbx(filepath=str(source),use_anim=False)
 for obj in bpy.data.objects:
  if obj.type=='MESH':
   for mat in obj.data.materials:
    if mat and mat.use_nodes:
     for node in mat.node_tree.nodes:
      if node.type=='TEX_IMAGE':mat.node_tree.nodes.active=node
 scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH'
 scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE'
 scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
 scene.render.resolution_x=192;scene.render.resolution_y=256;scene.render.resolution_percentage=100
 scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
 points=[obj.matrix_world@Vector(corner) for obj in scene.objects if obj.type=='MESH' for corner in obj.bound_box]
 low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
 center=(low+high)*.5;height=high.z-low.z
 data=bpy.data.cameras.new('Preview');camera=bpy.data.objects.new('Preview',data);scene.collection.objects.link(camera)
 camera.location=center+Vector((height*.18,-height*2,height*.10))
 camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=max(height*1.15,(high.x-low.x)*1.5)
 scene.camera=camera;scene.render.filepath=str(out)
 bpy.ops.render.render(write_still=True)
 print('RENDERED',folder,flush=True)
