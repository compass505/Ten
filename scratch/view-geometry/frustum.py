import json, math, itertools
B=json.load(open('/Users/kashiwa505/Documents/app/Ten/scratch/view-geometry/mother_bounds.json'))
cam=(0,0.28,0)
def boxes(clip):
    fr=B[clip]; hd=[[min(f['head'][0][i] for f in fr) for i in range(3)],[max(f['head'][1][i] for f in fr) for i in range(3)]]
    rh=[[min(f['rhand'][0][i] for f in fr) for i in range(3)],[max(f['rhand'][1][i] for f in fr) for i in range(3)]]
    return hd, rh
win=[[2.15,0.85,-1.15],[2.23,2.15,0.55]]
def visible(box,yaw,pitch,hfov,vfov):
    y=math.radians(yaw);p=math.radians(pitch)
    d=(-math.sin(y)*math.cos(p), math.cos(y)*math.cos(p), math.sin(p))
    foot=(0,0,1)
    r=(d[1]*foot[2]-d[2]*foot[1], d[2]*foot[0]-d[0]*foot[2], d[0]*foot[1]-d[1]*foot[0]); n=math.sqrt(sum(c*c for c in r)); r=tuple(c/n for c in r)
    u=(r[1]*d[2]-r[2]*d[1], r[2]*d[0]-r[0]*d[2], r[0]*d[1]-r[1]*d[0])
    th=math.tan(math.radians(hfov/2)); tv=math.tan(math.radians(vfov/2))
    # sample box corners + grid; conservative-ish: any sample point inside
    for fx in [i/6 for i in range(7)]:
     for fy in [i/6 for i in range(7)]:
      for fz in [i/6 for i in range(7)]:
        P=[box[0][k]+(box[1][k]-box[0][k])*f for k,f in zip(range(3),(fx,fy,fz))]
        v=[P[k]-cam[k] for k in range(3)]
        z=sum(v[k]*d[k] for k in range(3))
        if z<=0.02: continue
        x=sum(v[k]*r[k] for k in range(3)); yy=sum(v[k]*u[k] for k in range(3))
        if abs(x)<=z*th and abs(yy)<=z*tv: return True
    return False
aspect=1080/2400
for label,hfov in [('horiz30 (RoomRig)',30.0),('vert70 (Codex)',2*math.degrees(math.atan(math.tan(math.radians(35))*aspect)))]:
    vfov=2*math.degrees(math.atan(math.tan(math.radians(hfov/2))/aspect))
    hd,rh=boxes('C0')
    both=[]; wset=[]; fset=[]; hset=[]
    for yaw10 in range(-550,551,5):
        yaw=yaw10/10
        w=visible(win,yaw,0,hfov,vfov); f=visible(hd,yaw,0,hfov,vfov); h=visible(rh,yaw,0,hfov,vfov)
        if w: wset.append(yaw)
        if f: fset.append(yaw)
        if h: hset.append(yaw)
        if w+f+h>=2: both.append((yaw,w,f,h))
    rng=lambda s:(min(s),max(s)) if s else None
    print(label,'hfov=%.1f vfov=%.1f'%(hfov,vfov),'window',rng(wset),'face',rng(fset),'hand',rng(hset),'multi',rng([b[0] for b in both]),len(both))
