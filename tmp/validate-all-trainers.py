import pathlib, struct, re
def read_fbx(p):
    b=p.read_bytes(); version=struct.unpack_from('<I',b,23)[0]; large=version>=7500
    
    def node(pos):
        end,count,plen=struct.unpack_from('<QQQ' if large else '<III', b,pos); pos+=24 if large else 12
        n=b[pos];pos+=1;name=b[pos:pos+n].decode();pos+=n
        if not end:return None,len(b)
        props=[]
        for i in range(count):
            t=chr(b[pos]);pos+=1
            if t in 'YCFDIL':
                fmt={'Y':'h','C':'?','F':'f','D':'d','I':'i','L':'q'}[t];v=struct.unpack_from('<'+fmt,b,pos)[0];pos+=struct.calcsize(fmt)
            elif t in 'SR':
                n=struct.unpack_from('<I',b,pos)[0];pos+=4;v=b[pos:pos+n];pos+=n
                if t=='S':v=v.decode(errors='replace')
            elif t in 'fdlibc':
                n,encoding,size=struct.unpack_from('<III',b,pos);pos+=12+size;v=None
            else:raise ValueError(t)
            props.append(v)
        children=[]
        while pos<end-(25 if large else 13):
            child,pos=node(pos)
            if child:children.append(child)
        return (name,props,children),end
    pos=27;roots=[]
    while pos<len(b)-(25 if large else 13):
        n,pos=node(pos)
        if n: roots.append(n)
    
    return roots

root=pathlib.Path('UnityProject/Assets/_Project/Resources/Models/Trainers/ImportedFbx')
failures=[]; count=0; materials=0; lods=0
for file in sorted(root.glob('*/*.fbx')):
    trees=read_fbx(file)
    objects=next(n[2] for n in trees if n[0]=='Objects')
    connections=next(n[2] for n in trees if n[0]=='Connections')
    byid={n[1][0]:n for n in objects if n[1]}
    texture_files={}
    for n in objects:
        if n[0]=='Texture':
            paths=[c[1][0] for c in n[2] if c[0] in ('FileName','RelativeFilename')]
            if paths: texture_files[n[1][0]]=paths[-1].replace('\\','/').split('/')[-1]
    mats={n[1][0]:n[1][1].split('\x00')[0].lower() for n in objects if n[0]=='Material'}
    # Binary FBX names contain literal NUL separators.
    mats={k:v.split(chr(0))[0] for k,v in mats.items()}
    available={p.name.lower() for p in file.parent.glob('*.png')}
    for link in connections:
        v=link[1]
        if len(v)<4 or v[2] not in mats or v[3]!='DiffuseColor':continue
        texture=texture_files.get(v[1])
        if not texture:continue
        materials+=1
        if texture.lower() not in available:failures.append(f'{file.parent.name}: missing {texture}')
        name=mats[v[2]]
        token=next((token for part,token in [('face','face'),('eye','eye'),('hair','hairstyle'),('shoe','shoes'),('baggage','baggage'),('cap','cap'),('obj01','obj01'),('obj02','obj02'),('obj','obj'),('obi','obi'),('ring','ring'),('body','body')] if part in name),None)
        if token and ('_'+token+'_alb') not in texture.lower():failures.append(f'{file.parent.name}: {name} expects {texture}, name rule {token}')
        if not token:failures.append(f'{file.parent.name}: unresolved material {name}')
    lower=[n for n in objects if n[0]=='Model' and len(n[1])>1 and re.search(r'_lod[1-9]\d*',str(n[1][1]),re.I)]
    lods+=len(lower); count+=1
print(f'Audited {count} FBX files, {materials} diffuse material connections, {lods} lower-LOD objects.')
for failure in failures: print('FAIL:',failure)
print('PASS' if not failures else f'{len(failures)} failures')
raise SystemExit(bool(failures))