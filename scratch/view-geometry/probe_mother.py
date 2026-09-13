import bpy, json, sys
from mathutils import Vector
sc=bpy.context.scene
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
print('RIG',rig.name,tuple(rig.matrix_world.translation),tuple(rig.rotation_euler))
man=json.load(open('/Users/kashiwa505/Documents/app/Ten/scratch/visual/parent/unity-reference-r56-approved/r56-animation-map.json'))
body=bpy.data.objects['Mother_Body']
groups={'head':['mixamorig:Head','mixamorig:HeadTop_End'],'lhand':[],'rhand':[]}
vg={g.index:g.name for g in body.vertex_groups}
def part(name):
    if name=='head': return lambda n: 'Head' in n or 'Neck' in n
    if name=='lhand': return lambda n: n.startswith('mixamorig:LeftHand')
    return lambda n: n.startswith('mixamorig:RightHand')
sel={}
for k in ['head','lhand','rhand']:
    f=part(k); idx=[v.index for v in body.data.vertices if any(f(vg[g.group]) and g.weight>0.5 for g in v.groups)]
    sel[k]=idx
out={}
dg=bpy.context.evaluated_depsgraph_get()
for c in man['clips']:
    rig.animation_data_create(); rig.animation_data.action=bpy.data.actions[c['rig_action']]
    res=[]
    n=c['frames'][1]
    for f in range(1,n+1,3):
        sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get()
        ev=body.evaluated_get(dg); me=ev.to_mesh(); mw=body.matrix_world
        fr={}
        for k,idx in sel.items():
            ps=[mw@me.vertices[i].co for i in idx]
            # to Unity (x, z, y)
            mn=[min(p[0] for p in ps),min(p[2] for p in ps),min(p[1] for p in ps)]
            mx=[max(p[0] for p in ps),max(p[2] for p in ps),max(p[1] for p in ps)]
            fr[k]=[mn,mx]
        ev.to_mesh_clear(); res.append(fr)
    out[c['clip']]=res
    print('CLIP',c['clip'],json.dumps(res[len(res)//2]))
json.dump(out,open('/Users/kashiwa505/Documents/app/Ten/scratch/view-geometry/mother_bounds.json','w'))
