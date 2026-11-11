using System;
using System.Collections.Generic;
using System.IO;
using KombiShift2.UI;
using KombiShift2.City;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class KombiCityInfrastructureAudit
{
    [Serializable] public class Geometry {public List<Triangle> triangles=new();public Vector3[] itinerary;public int cityRenderers;}
    [Serializable] public class Triangle {public Vector3 a,b,c;public string source;}
    public static void Run()
    {
        EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var log=new StringBuilder("CITY MESH AND MATERIAL INVENTORY\n");
        foreach(var mesh in Object.FindObjectsByType<MeshFilter>().OrderBy(x=>x.name))
        {
            var renderer=mesh.GetComponent<MeshRenderer>();if(renderer==null || mesh.sharedMesh==null)continue;
            var path=AssetDatabase.GetAssetPath(mesh.GetComponent<CityRoadSurface>()!=null?mesh.GetComponent<CityRoadSurface>().sourceMesh:mesh.sharedMesh);
            if(!path.Contains("Environment/City") && !mesh.name.ToLowerInvariant().Contains("road") && !mesh.name.StartsWith("bz"))continue;
            log.AppendLine(mesh.name+" | "+path+" | centre="+renderer.bounds.center+" size="+renderer.bounds.size+" | collider="+(mesh.GetComponent<Collider>()!=null)+" | materials="+string.Join(",",renderer.sharedMaterials.Select(x=>x==null?"null":x.name)));
        }
        Directory.CreateDirectory("Docs/Validation/CityInfrastructure");File.WriteAllText("Docs/Validation/CityInfrastructure/mesh-inventory.txt",log.ToString());
        var geometry=new Geometry();
        geometry.cityRenderers=Object.FindObjectsByType<MeshFilter>().Count(x=>AssetDatabase.GetAssetPath(x.sharedMesh).Contains("Environment/City")||x.GetComponent<CityRoadSurface>()!=null);
        geometry.itinerary=Object.FindFirstObjectByType<KombiNavigationHud>().RoutePoints.Select(x=>x.position).ToArray();
        foreach(var filter in Object.FindObjectsByType<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null || filter.sharedMesh==null)continue;
            var surface=filter.GetComponent<CityRoadSurface>();var mesh=surface!=null?surface.sourceMesh:filter.sharedMesh;var vertices=mesh.vertices;var materials=renderer.sharedMaterials;
            for(int slot=0;slot<Math.Min(materials.Length,mesh.subMeshCount);slot++)
            {
                if(materials[slot]==null || materials[slot].name!="road"&&surface==null)continue;
                var indices=mesh.GetTriangles(slot);
                for(int n=0;n<indices.Length;n+=3)
                {
                    Vector3 a=filter.transform.TransformPoint(vertices[indices[n]]),b=filter.transform.TransformPoint(vertices[indices[n+1]]),c=filter.transform.TransformPoint(vertices[indices[n+2]]);
                    if(Mathf.Abs(Vector3.Cross(b-a,c-a).normalized.y)<.7f)continue;
                    geometry.triangles.Add(new Triangle{a=a,b=b,c=c,source=filter.name});
                }
            }
        }
        File.WriteAllText("Docs/Validation/CityInfrastructure/road-geometry.json",JsonUtility.ToJson(geometry));
        Debug.Log("KOMBI_CITY_INVENTORY_SAVED triangles="+geometry.triangles.Count);
    }
}
