using System.IO;
using UnityEditor;
using UnityEngine;
namespace KombiShift.JaydenStarter.Editor {
 public static class CatalogExporter {
  [MenuItem("Kombi Shift/Rebuild Reusable Art Catalog")]
  public static void Export(){const string dir="Assets/KombiShiftJayden/Catalog";Directory.CreateDirectory(dir);AssetDatabase.Refresh();if(AssetDatabase.LoadAssetAtPath<GameObject>(dir+"/Kombi.prefab")!=null)return;
   var root=new GameObject("Catalog export temporary");var game=root.AddComponent<ShiftGame>();var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/KombiShiftJayden/Generated/StarterSurface.mat");game.surfaceTemplate=source;
   var objects=game.CreateCatalogArt();int serial=0;
   void Persist(Object asset){if(asset==null||AssetDatabase.Contains(asset))return;string extension=asset is Mesh?".asset":asset is Material?".mat":".asset";AssetDatabase.CreateAsset(asset,dir+"/Art_"+(serial++)+extension);}
   foreach(var t in objects){foreach(var filter in t.GetComponentsInChildren<MeshFilter>())Persist(filter.sharedMesh);foreach(var renderer in t.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)Persist(material);
    if(t.name=="Passenger"){var a=t.gameObject.AddComponent<Animator>();a.runtimeAnimatorController=Resources.Load<RuntimeAnimatorController>("Animation/Passenger");}
    PrefabUtility.SaveAsPrefabAsset(t.gameObject,dir+"/"+t.name+".prefab");}
   game.PreserveCatalogAssets();Object.DestroyImmediate(root);AssetDatabase.SaveAssets();Debug.Log("KOMBI_CATALOG_EXPORTED");
  }
 }
}
