"""Audit trainer animation references without mistaking state graphs for playable clips."""
import argparse,collections,json,re,subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
def main():
 parser=argparse.ArgumentParser()
 parser.add_argument('--archive',type=Path,default=Path(r'C:/Users/andres/Downloads/trainers.7z'))
 parser.add_argument('--source',type=Path,default=ROOT/'tmp/trainers-package/source')
 parser.add_argument('--output',type=Path,default=ROOT/'UnityProject/Assets/_Project/Resources/Models/Trainers/Package/animation-audit.json')
 args=parser.parse_args()
 names=subprocess.check_output(['tar','-tf',str(args.archive)],text=True).splitlines()
 playable=[n for n in names if Path(n).suffix.lower() in ('.tranm','.gfbanm','.fbx','.glb','.gltf','.anim')]
 names_lower={n.replace('\\','/').lower() for n in names}
 by_filename=collections.defaultdict(list)
 for n in names:by_filename[Path(n).name.lower()].append(n)
 entries=[]
 for folder in sorted(p for p in args.source.iterdir() if p.is_dir()):
  references=set()
  for table in folder.glob('*.tracr'):
   references.update(s.decode('ascii') for s in re.findall(rb'[A-Za-z0-9_./\\-]+\.(?:tranm|gfbanm)',table.read_bytes()))
  available=[];missing=[]
  for ref in sorted(references):
   (available if Path(ref).name.lower() in by_filename else missing).append(ref)
  entries.append({'model':folder.name,'resourceTables':[p.name for p in sorted(folder.glob('*.tracr'))],
                  'availableAnimationReferences':available,'missingAnimationReferences':missing})
 report={'archive':args.archive.name,'models':len(entries),'playableFilesInArchive':playable,
         'referencedAnimationCount':sum(len(e['missingAnimationReferences'])+len(e['availableAnimationReferences']) for e in entries),
         'availableReferenceCount':sum(len(e['availableAnimationReferences']) for e in entries),
         'currentFallback':'Procedural limb locomotion; no original clips are claimed. Roshi/Ash excluded.',
         'entries':entries}
 args.output.parent.mkdir(parents=True,exist_ok=True)
 args.output.write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
 print(f"AUDIT: {len(entries)} trainers; {len(playable)} playable animation files; {report['referencedAnimationCount']} referenced clips; {report['availableReferenceCount']} references available.")
 for prefix in ('tr0036_00_','tr0002_00_'):
  entry=next(e for e in entries if e['model'].startswith(prefix))
  print(entry['model'], 'missing samples:',entry['missingAnimationReferences'][:3])
if __name__=='__main__':main()
