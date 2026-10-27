import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r'C:/Users/andres/Documents/PokeMoba/UnityProject/Assets/_Project/Resources/Models/Pokemon/Animated/001_bulbasaur/Model.fbx')
print('FBX meshes',[(o.name,len(o.data.vertices),o.hide_render) for o in bpy.context.scene.objects if o.type=='MESH'])
print('FBX actions',[(a.name,len(a.slots),a.frame_range[:]) for a in bpy.data.actions])
