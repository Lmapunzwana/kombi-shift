"""Apply scene-audited lane exclusions and retain a strongly connected playable network.
Run after KombiCityNetworkSceneAudit.Run, then regenerate CityTrips.json.
"""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1];resource=root/'Assets/Resources/CityRoadNetwork.json';data=json.loads(resource.read_text());nodes=data['nodes'];audit=json.loads((root/'Docs/Validation/CityInfrastructure/scene-road-audit.json').read_text());exclusions_file=root/'Tools/city_lane_exclusions.json'
def key(a,b):
 p,q=nodes[a]['position'],nodes[b]['position'];return ','.join(str(round(x,2)) for x in (p['x'],p['z'],q['x'],q['z']))
exclusions=json.loads(exclusions_file.read_text()) if exclusions_file.exists() else {}
for issue in audit.get('laneIssues',[]):
 if issue['blocked'] or issue['unsupported']:exclusions[key(issue['from'],issue['to'])]=issue['collider'] or 'No verified drivable road surface'
removed=0
for i,node in enumerate(nodes):
 before=len(node['links']);node['links']=[j for j in node['links'] if key(i,j) not in exclusions];removed+=before-len(node['links'])
# Kosaraju SCC: a playable area must allow a legal route back, not just an outward path.
seen=set();order=[]
for start in range(len(nodes)):
 if start in seen:continue
 stack=[(start,False)]
 while stack:
  i,expanded=stack.pop()
  if expanded:order.append(i);continue
  if i in seen:continue
  seen.add(i);stack.append((i,True));stack.extend((n,False) for n in nodes[i]['links'] if n not in seen)
reverse=[[] for n in nodes]
for i,node in enumerate(nodes):
 for j in node['links']:reverse[j].append(i)
seen=set();components=[]
for start in reversed(order):
 if start in seen:continue
 component=[];stack=[start];seen.add(start)
 while stack:
  i=stack.pop();component.append(i)
  for j in reverse[i]:
   if j not in seen:seen.add(j);stack.append(j)
 components.append(component)
components.sort(key=len,reverse=True)
for component,indices in enumerate(components):
 for i in indices:nodes[i]['component']=component
for node in nodes:node['links']=[j for j in node['links'] if nodes[j]['component']==node['component']]
data['components']=len(components);data['provenance']='Existing NYBlock02 road triangles and preserved itinerary; physical road-only support and vehicle-sized clearance exclusions; largest strongly connected component is playable. Small inaccessible components remain documented.'
resource.write_text(json.dumps(data,separators=(',',':')));exclusions_file.write_text(json.dumps(exclusions,indent=2))
summary={'nodes':len(nodes),'playableNodes':len(components[0]),'stronglyConnectedComponents':len(components),'componentSizes':[len(c) for c in components],'newlyRemovedDirectedLinks':removed,'recordedLaneExclusions':len(exclusions),'retainedDirectedLinks':sum(len(n['links']) for n in nodes)}
(root/'Docs/Validation/CityInfrastructure/validated-graph.json').write_text(json.dumps(summary,indent=2));print(json.dumps(summary,indent=2))
