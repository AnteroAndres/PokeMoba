import bpy
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r'C:/Users/andres/Documents/PokeMoba/UnityProject/Assets/_Project/Resources/Models/Pokemon/Animated/001_bulbasaur/Model.fbx')
scene=bpy.context.scene; scene.frame_set(20)
meshes=[o for o in scene.objects if o.type=='MESH']
print('VERIFY_MESHES',[(o.name,len(o.data.vertices)) for o in meshes])
deps=bpy.context.evaluated_depsgraph_get()
points=[o.evaluated_get(deps).matrix_world @ Vector(c) for o in meshes for c in o.evaluated_get(deps).bound_box]
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=(lo+hi)/2;size=max(hi-lo)
print('VERIFY_BOUNDS',lo[:],hi[:])
bpy.ops.object.camera_add(location=center+Vector((1.4,-2.2,1.1))*size)
camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=size*1.65;scene.camera=camera
bpy.ops.object.light_add(type='AREA',location=center+Vector((1,-1,2))*size);bpy.context.object.data.energy=800; bpy.context.object.data.shape='DISK';bpy.context.object.data.size=size*2
bpy.ops.object.light_add(type='AREA',location=center+Vector((-1,1,1))*size);bpy.context.object.data.energy=500;bpy.context.object.data.size=size*2
scene.world=bpy.data.worlds.new('PreviewWorld');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.18,.21,.25,1)
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=8
scene.render.resolution_x=640;scene.render.resolution_y=480;scene.render.resolution_percentage=100
scene.render.filepath=r'C:/Users/andres/Documents/PokeMoba/tmp/animated-pokemon/bulbasaur-preview.png'
bpy.ops.render.render(write_still=True)
