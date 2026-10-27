import bpy,json
bpy.ops.import_scene.gltf(filepath=r'C:/Users/andres/Documents/PokeMoba/tmp/animated-pokemon/001.glb')
print('PROBE_OBJECTS',[(o.name,o.type,len(o.animation_data.nla_tracks) if o.animation_data else 0) for o in bpy.context.scene.objects])
print('PROBE_ACTIONS',[(a.name,a.frame_range[:],len(a.slots)) for a in bpy.data.actions])
for m in bpy.data.materials:
 print('PROBE_MAT',m.name,[(n.type,n.image.name if n.type=='TEX_IMAGE' and n.image else '') for n in m.node_tree.nodes])
