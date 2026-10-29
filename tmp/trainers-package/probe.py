import bpy,sys,types,importlib
from pathlib import Path
root=Path(r'C:/Users/andres/Documents/PokeMoba')
sys.path.insert(0,str(root/'tmp/trainers-package/deps'))
package=types.ModuleType('trinity_import');package.__path__=[str(root/'tmp/trainers-package/converter')];sys.modules['trinity_import']=package
import requests
requests.get=lambda *args,**kwargs: (_ for _ in ()).throw(OSError('Offline converter'))
m=importlib.import_module('trinity_import.PokemonSwitch')
bpy.ops.wm.read_factory_settings(use_empty=True)
f=root/'tmp/trainers-package/source/tr0002_00_doctor_b'
m.from_trmdlsv(str(f),next(f.glob('*.trmdl')).name,False,False,True,False,False,False)
print('PROBE_OBJECTS',[(o.name,o.type) for o in bpy.context.scene.objects][:20])
for mat in bpy.data.materials:
 print('PROBE_MAT',mat.name,[(n.name,n.image.name if n.type=='TEX_IMAGE' and n.image else '') for n in mat.node_tree.nodes if n.type in ('TEX_IMAGE','GROUP')])
for a in bpy.data.armatures:print('BONES', [b.name for b in a.bones][:35])
