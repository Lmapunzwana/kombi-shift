"""Compare the full authored scene against the pre-infrastructure city baseline."""
import re,json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];scene='Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity'
baseline='7a6a733';old=subprocess.check_output(['git','show',f'{baseline}:{scene}'],cwd=root,text=True);new=(root/scene).read_text()
def chunks(text):return {m.group(2):(m.group(1),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
a,b=chunks(old),chunks(new)
meta=(root/'Assets/KombiShift2/Art/Environment/City/Pack02/NYBlock02/NYBlock02.prefab.meta').read_text();guid=re.search(r'^guid: (\w+)',meta,re.M).group(1)
original={i:v for i,v in a.items() if v[0]=='1001' and f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}' in v[1]}
missing=[i for i in original if i not in b or guid not in b[i][1]]
changes=subprocess.check_output(['git','diff','--name-only',baseline,'--','Assets/KombiShift2/Art/Environment/City'],cwd=root,text=True).splitlines()
def modifications(text):
 entries={}
 for target,path,value,reference in re.findall(r'- target: (.*?)\n\s+propertyPath: (.*?)\n\s+value: (.*?)\n\s+objectReference: (.*?)(?=\n)',text):entries[(target,path.strip("\"'"))]=(value,reference)
 return entries
changed_paths=[]
for i,v in original.items():
 if i not in b:continue
 before,after=modifications(v[1]),modifications(b[i][1])
 for key,value in after.items():
  if before.get(key)!=value:changed_paths.append(key[1])
allowed={'m_Mesh','m_Materials.Array.data[0]','m_Enabled'}
unexpected=sorted(set(changed_paths)-allowed)
report={'baselineCommit':baseline,'originalCityPrefabInstances':len(original),'missingCityPrefabInstances':missing,'modifiedImportedCityAssets':changes,'changedCityOverridePaths':sorted(set(changed_paths)),'unexpectedChangedCityOverridePaths':unexpected,'scope':'Preserves the original imported city prefab instances, source assets and transforms. Road mesh/material and foliage-collider overrides are intentional. Runtime inventory separately verifies all 12611 city mesh renderers.'}
(root/'Docs/Validation/CityInfrastructure/city-preservation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
if not original or missing or changes or unexpected:raise SystemExit('City preservation audit requires review')
