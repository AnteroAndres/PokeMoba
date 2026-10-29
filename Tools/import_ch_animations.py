"""Offline LMD conversion. Requires Romulion's reviewed reader in tmp/characters-ch/converter.
Reader source: https://github.com/Romulion/LMD_Animation_Loader
Only converts the CH selection; never opens Ash or Roshi assets.
"""
import bpy,sys,json,struct,hashlib,importlib.util,math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
reader_path=root/'tmp/characters-ch/converter/io_import_pmm_animation.py'
spec=importlib.util.spec_from_file_location('lmd_reader',reader_path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
methods={name:value for name,value in vars(module.PokeMastAnimImport).items() if callable(value) and not name.startswith('__')}
Reader=type('Reader',(),methods)
selection=json.loads((root/'Tools/ch_character_selection.json').read_text())
only=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else None
for item in selection:
 folder=item['folder']
 if only and folder!=only:continue
 directory=root/'UnityProject/Assets/_Project/Resources/Models/Trainers/Package'/folder
 output=directory/'Animations';output.mkdir(exist_ok=True)
 files=list((root/'tmp/characters-ch/source'/folder/'Motions').glob('*.lmd'))
 choices={
  'idle':['_110_idle'], 'walk':['_220_walk_1','_walk'], 'run':['_run'],
  'command':['_110_direct'], 'battle':['_110_start_battle'], 'greeting':['_221_greeting_1'], 'throw':['_110_join']
 }
 records=[]
 for role,patterns in choices.items():
  found=None
  for pattern in patterns:
   found=next((p for p in files if pattern in p.stem and not any(x in p.stem for x in ['_uv','_cam'])),None)
   if found:break
  if found is None:continue
  record_path=output/(role+'.json')
  if record_path.exists():records.append(json.loads(record_path.read_text()));continue
  bpy.ops.wm.read_factory_settings(use_empty=True)
  bpy.ops.import_scene.fbx(filepath=str(directory/'Model.fbx'),use_anim=False)
  armatures=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];assert len(armatures)==1,(folder,len(armatures))
  arm=armatures[0];bpy.context.view_layer.objects.active=arm;arm.select_set(True)
  reader=Reader();reader.fps=30
  with found.open('rb') as stream:
   kind=struct.unpack('<I',stream.read(4))[0];stream.seek(100 if kind==20 else 72)
   duration=struct.unpack('<f',stream.read(4))[0];reader.maxFrames=round(duration*reader.fps)
   raw=reader.ReadAnimation(stream,kind)
   reader.maxFrames=round(duration*reader.fps)
  matches=set(raw)&set(arm.pose.bones.keys());assert len(matches)>10,(folder,role,len(matches))
  reader.ApplyAnimation(raw,reader.fps,found.name)
  action=arm.animation_data.action;action.name=role
  scene=bpy.context.scene;scene.frame_set(0)
  # Locomotion stays on the gameplay root; remove horizontal motion of the armature object.
  points=[]
  for frame in [0,reader.maxFrames//3,reader.maxFrames*2//3]:
   scene.frame_set(frame);bpy.context.view_layer.update()
   points.append([tuple((arm.matrix_world@b.matrix).translation) for b in arm.pose.bones])
  delta=max(math.dist(a,b) for a,b in zip(points[0],points[1]))
  print('CHECK',role,'duration',duration,'frames',reader.maxFrames,'action',action.name,'slots',len(action.slots),'delta',delta,flush=True)
  if delta<=.00001:raise ValueError((folder,role,'static clip',list(raw)[:8]))
  scene.frame_set(0)
  bpy.ops.export_scene.fbx(filepath=str(output/(role+'.fbx')),object_types={'ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_step=1,bake_anim_simplify_factor=0)
  record={'name':role,'source':found.name,'sha256':hashlib.sha256(found.read_bytes()).hexdigest(),'fps':30,'frames':reader.maxFrames,'duration':duration,'matchedBones':len(matches),'movementDelta':delta}
  record_path.write_text(json.dumps(record,indent=2));records.append(record)
  print('ANIMATED',folder,role,reader.maxFrames,'frames',len(matches),'bones',flush=True)
 (directory/'animations.json').write_text(json.dumps({'clips':records},indent=2))
print('COMPLETE',flush=True)
