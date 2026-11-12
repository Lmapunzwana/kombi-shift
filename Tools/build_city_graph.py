"""Bake navigation from Unity-exported city road triangles; requires numpy and Pillow.
Run after KombiCityInfrastructureAudit.Run. No external map input is used.
"""
from pathlib import Path
import json, math
from collections import deque, Counter
import numpy as np
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Docs/Validation/CityInfrastructure'
data=json.loads((OUT/'road-geometry.json').read_text())
def xyz(v):return [v['x'],v['y'],v['z']]
triangles=np.array([[xyz(t[k]) for k in ('a','b','c')] for t in data['triangles']],dtype=float)
route=np.array([xyz(p) for p in data['itinerary']])
step=2.;points=np.concatenate((triangles.reshape(-1,3),route));origin=np.floor(points[:,[0,2]].min(0)/step)*step-10
size=np.ceil((points[:,[0,2]].max(0)-origin)/step).astype(int)+6
shape=(int(size[1]),int(size[0]));mask=np.zeros(shape,bool);height=np.full(shape,-np.inf,dtype=np.float32)
for triangle in triangles:
 a,b,c=triangle;px=triangle[:,[0,2]];lo=np.maximum(np.floor((px.min(0)-origin)/step).astype(int),0);hi=np.minimum(np.ceil((px.max(0)-origin)/step).astype(int),size-1)
 xx,zz=np.meshgrid(np.arange(lo[0],hi[0]+1),np.arange(lo[1],hi[1]+1));wx=origin[0]+xx*step;wz=origin[1]+zz*step
 den=(b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2])
 if abs(den)<1e-6:continue
 u=((b[2]-c[2])*(wx-c[0])+(c[0]-b[0])*(wz-c[2]))/den;v=((c[2]-a[2])*(wx-c[0])+(a[0]-c[0])*(wz-c[2]))/den
 valid=(u>=-1e-5)&(v>=-1e-5)&(u+v<=1.00001);ys=u*a[1]+v*b[1]+(1-u-v)*c[1]
 region=height[lo[1]:hi[1]+1,lo[0]:hi[0]+1];np.maximum(region,np.where(valid,ys,-np.inf),out=region)
 mask[lo[1]:hi[1]+1,lo[0]:hi[0]+1]|=valid
# The already playable spline roads are part of the preserved city and join the mesh roads.
for a,b in zip(route[:-1],route[1:]):
 for t in np.linspace(0,1,max(2,int(np.linalg.norm((b-a)[[0,2]])/step)*2)):
  p=a+(b-a)*t;cx,cz=np.rint((p[[0,2]]-origin)/step).astype(int)
  for dz in range(-2,3):
   for dx in range(-2,3):
    x,z=cx+dx,cz+dz
    if 0<=x<size[0] and 0<=z<size[1] and np.linalg.norm(origin+np.array([x,z])*step-p[[0,2]])<=3.4:
     mask[z,x]=True;height[z,x]=p[1]-.025
# Estimate clearance to the road boundary without importing a scientific runtime dependency.
distance=np.zeros(shape,dtype=np.uint8);interior=mask.copy()
for n in range(1,32):
 distance[interior]=n
 eroded=interior & np.roll(interior,1,0)&np.roll(interior,-1,0)&np.roll(interior,1,1)&np.roll(interior,-1,1)
 if not eroded.any():break
 interior=eroded
skeleton=mask.copy();skeleton[[0,-1],:]=False;skeleton[:,[0,-1]]=False
for iteration in range(500):
 changed=False
 for phase in (0,1):
  p=[np.roll(skeleton,(dz,dx),(0,1)) for dz,dx in ((1,0),(1,-1),(0,-1),(-1,-1),(-1,0),(-1,1),(0,1),(1,1))]
  count=sum(x.astype(np.uint8) for x in p);transitions=sum((~p[i]&p[(i+1)%8]).astype(np.uint8) for i in range(8))
  if phase==0:condition=~(p[0]&p[2]&p[4]) & ~(p[2]&p[4]&p[6])
  else:condition=~(p[0]&p[2]&p[6]) & ~(p[0]&p[4]&p[6])
  remove=skeleton&(count>=2)&(count<=6)&(transitions==1)&condition
  if remove.any():skeleton[remove]=False;changed=True
 if not changed:break
cells={tuple(p) for p in np.argwhere(skeleton)}
def neighbors(p):
 z,x=p;result=[]
 for dz,dx in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)):
  q=(z+dz,x+dx)
  if q not in cells:continue
  if dz and dx and ((z+dz,x) in cells or (z,x+dx) in cells):continue
  if abs(float(height[p])-float(height[q]))<=1.5:result.append(q)
 return result
# Discard only tiny isolated fragments; record every remaining component honestly.
components=[];seen=set()
for p in sorted(cells):
 if p in seen:continue
 queue=[p];seen.add(p);component=[]
 while queue:
  q=queue.pop();component.append(q)
  for n in neighbors(q):
   if n not in seen:seen.add(n);queue.append(n)
 components.append(component)
components.sort(key=len,reverse=True);retained=[c for c in components if len(c)>=12];cells={p for c in retained for p in c}
# Retain sparse points along each chain, including every junction and endpoint.
adj={p:neighbors(p) for p in cells};anchors={p for p,v in adj.items() if len(v)!=2}
for component in retained:
 if not any(p in anchors for p in component):anchors.add(component[0])
segments=[];visited=set()
for start in sorted(anchors):
 for neighbor in adj[start]:
  if (start,neighbor) in visited:continue
  path=[start];previous,current=start,neighbor
  while True:
   visited.add((previous,current));visited.add((current,previous));path.append(current)
   if current in anchors:break
   ns=[p for p in adj[current] if p!=previous]
   if not ns:break
   previous,current=current,ns[0]
  segments.append(path)
selected=set(anchors);edges=[]
for path in segments:
 chain=[path[0]]+path[3:-1:3]+[path[-1]];selected.update(chain);edges.extend(zip(chain[:-1],chain[1:]))
ordered=sorted(selected);ids={p:i for i,p in enumerate(ordered)};links=[set() for p in ordered]
for a,b in edges:
 if a!=b:links[ids[a]].add(ids[b]);links[ids[b]].add(ids[a])
component_id={p:i for i,c in enumerate(retained) for p in c};nodes=[]
for p in ordered:
 z,x=p;position={'x':round(float(origin[0]+x*step),3),'y':round(float(height[p]+.025),3),'z':round(float(origin[1]+z*step),3)}
 width=max(5.,min(28.,float(distance[p])*step*1.6));degree=len(adj[p])
 nodes.append({'position':position,'links':sorted(links[ids[p]]),'component':component_id[p],'width':round(width,2),'speedLimit':30 if degree>2 else 50,'junction':degree>2,'allowPassing':width>=13 and degree==2})
annotations=json.loads((ROOT/'Tools/city_road_annotations.json').read_text())
for circle in annotations.get('roundabouts',[]):
 cx,cz,radius=circle['x'],circle['z'],circle['radius']
 for i,node in enumerate(nodes):
  p=node['position'];ax,az=p['x']-cx,p['z']-cz;ar=math.hypot(ax,az)
  node['roundabout']=abs(ar-radius)<9
  if not node['roundabout']:continue
  node['allowPassing']=False;node['speedLimit']=20
  allowed=[]
  for j in node['links']:
   q=nodes[j]['position'];bx,bz=q['x']-cx,q['z']-cz;br=math.hypot(bx,bz)
   if abs(br-radius)<9 and ax*bz-az*bx>1:continue
   allowed.append(j)
  node['links']=allowed
network={'nodes':nodes,'components':len(retained),'provenance':'Existing NYBlock02 road-material triangles plus the preserved authored spline itinerary; 2 m raster, thinned and sampled at approximately 6 m. Disconnected components retained and reported.'}
resource=ROOT/'Assets/Resources/CityRoadNetwork.json';resource.write_text(json.dumps(network,separators=(',',':')))
image=Image.new('RGB',(shape[1],shape[0]),'#172327');pixels=np.array(image);pixels[mask]=[75,85,86];pixels[skeleton]=[229,174,66];Image.fromarray(pixels).transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(OUT/'derived-road-network.png')
summary={'roadTriangles':len(triangles),'cityMeshRenderers':data['cityRenderers'],'rasterStepMetres':step,'skeletonIterations':iteration+1,'nodes':len(nodes),'directedLinks':sum(len(n['links']) for n in nodes),'retainedComponents':len(retained),'componentSkeletonSizes':[len(c) for c in retained],'discardedTinyFragments':len(components)-len(retained),'largestComponentNodeCount':sum(n['component']==0 for n in nodes)}
(OUT/'graph-bake.json').write_text(json.dumps(summary,indent=2));print(json.dumps(summary,indent=2))
