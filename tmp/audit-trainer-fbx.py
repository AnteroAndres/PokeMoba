import struct, pathlib, json
p=pathlib.Path('UnityProject/Assets/_Project/Resources/Models/Trainers/ImportedFbx/tr0036_Rei/Rei.fbx')
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
for root in roots:
    if root[0]=='Objects':
        for n in root[2]:
            if n[0] in ('Material','Texture'):
                print(n[0],n[1][:3])