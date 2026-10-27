import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=r'C:/Users/andres/Documents/PokeMoba/tmp/animated-pokemon/001.glb')
for i in bpy.data.images: print('IMAGE',i.name,i.type,i.has_data,i.size[:],i.source, bool(i.packed_file))
for a in bpy.data.actions:
 print('ACTION',a.name,len(a.slots),[(s.identifier,s.target_id_type) for s in a.slots][:3])
