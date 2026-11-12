"""Author fictional route names against reachable nodes of the baked existing city."""
import json,heapq,math
from pathlib import Path
root=Path(__file__).resolve().parents[1];data=json.loads((root/'Assets/Resources/CityRoadNetwork.json').read_text());nodes=data['nodes']
def pos(i):p=nodes[i]['position'];return (p['x'],p['y'],p['z'])
def distance(a,b):return math.dist(pos(a),pos(b))
def path(a,b):
 q=[(0,a)];cost={a:0};previous={}
 while q:
  _,i=heapq.heappop(q)
  if i==b:
   result=[b]
   while result[-1] in previous:result.append(previous[result[-1]])
   return result[::-1]
  for n in nodes[i]['links']:
   new=cost[i]+distance(i,n)
   if new<cost.get(n,math.inf):cost[n]=new;previous[n]=i;heapq.heappush(q,(new+distance(n,b),n))
 return []
geometry=json.loads((root/'Docs/Validation/CityInfrastructure/road-geometry.json').read_text());start=geometry['itinerary'][5]
junctions=[i for i,n in enumerate(nodes) if n['component']==0 and n['junction']]
def safe_stop(i):
 n=nodes[i]
 if n['component']!=0 or len(n['links'])!=2 or n['width']<6 or n.get('roundabout'):return False
 a,b=(pos(j) for j in n['links']);p=pos(i);u=[a[k]-p[k] for k in range(3)];v=[b[k]-p[k] for k in range(3)]
 if sum(x*y for x,y in zip(u,v))/max(.001,math.sqrt(sum(x*x for x in u)*sum(x*x for x in v)))>-.9:return False
 return not any(distance(i,j)<25 for j in junctions)
valid=[i for i in range(len(nodes)) if safe_stop(i)]
origin=min(valid,key=lambda i:math.dist(pos(i),(start['x'],start['y'],start['z'])))
xs=[pos(i)[0] for i in valid];zs=[pos(i)[2] for i in valid];cx=(min(xs)+max(xs))/2;cz=(min(zs)+max(zs))/2;rx=(max(xs)-min(xs))*.36;rz=(max(zs)-min(zs))*.36
names=['Mbare','Avondale','Borrowdale','Highfield','Glen View','Budiriro','Kuwadzana','Hatfield','Chitungwiza'];trips=[];used=set()
for n,name in enumerate(names):
 angle=n*2*math.pi/len(names);target=(cx+rx*math.cos(angle),cz+rz*math.sin(angle))
 destination=min((i for i in valid if i not in used and distance(origin,i)>450),key=lambda i:(pos(i)[0]-target[0])**2+(pos(i)[2]-target[1])**2);used.add(destination);route=path(origin,destination)
 stops=[origin];last=origin;travelled=0
 for a,b in zip(route[:-1],route[1:]):
  travelled+=distance(a,b)
  if travelled>=260 and b in valid and distance(b,destination)>180:
   stops.append(b);last=b;travelled=0
 stops.append(destination)
 trips.append({'id':name.lower(),'origin':'CBD','destination':name,'description':'Fictional Harare route on the existing imported city; no geographic equivalence is claimed.','originNode':origin,'destinationNode':destination,'stopNodes':stops,'expectedPassengers':min(30,(len(stops)-1)*6),'difficulty':min(5,1+len(stops)//2),'baseFare':1.,'perKilometre':.3,'minFare':1.,'maxFare':5.,'peakMultiplier':1.25})
(root/'Assets/Resources/CityTrips.json').write_text(json.dumps({'trips':trips},indent=2));print([(t['destination'],len(t['stopNodes'])) for t in trips])
