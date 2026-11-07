using UnityEngine;
namespace KombiShift.JaydenStarter {
 public sealed partial class ShiftGame {
  public Transform[] CreateCatalogArt(){world=transform;worldFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");worldLabelMaterial=new Material(Resources.Load<Shader>(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null&&UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRender")?"HDRPWorldLabel":"WorldLabel"));materials.Add(worldLabelMaterial);
   var bus=Bus("Kombi",new Color(.83f,.83f,.79f),world);var skin=Mat(C("87563A"));var person=Person("Passenger",Vector3.zero,C("746A5D"),skin);
   var road=Group("Road Straight 10m",world,Vector3.zero);Box("Asphalt",Vector3.zero,new Vector3(12,.1f,10),Mat(C("3B3D3B")),road,true);Box("Centre dash",new Vector3(0,.06f,0),new Vector3(.15f,.015f,4),Mat(C("E8E1CB")),road);
   var stop=Group("Rank shelter",world,Vector3.zero);var metal=Mat(C("4E5B5E"));Box("Roof",new Vector3(0,2.3f,0),new Vector3(3.5f,.2f,5),metal,stop);for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Post",new Vector3(x*1.4f,1.1f,z*2),new Vector3(.12f,2.2f,.12f),metal,stop);Box("Bench",new Vector3(0,.5f,0),new Vector3(2,.15f,.6f),Mat(C("80573C")),stop);
   var fuel=Group("Fuel pump",world,Vector3.zero);Box("Pedestal",new Vector3(0,.6f,0),new Vector3(.8f,1.2f,.6f),Mat(C("D8D5C9")),fuel,true);Box("Display",new Vector3(0,1.4f,0),new Vector3(1,.6f,.7f),metal,fuel);Box("Meter",new Vector3(0,1.4f,-.36f),new Vector3(.7f,.3f,.03f),Mat(C("28343B")),fuel);
   return new[]{bus,person,road,stop,fuel};
  }
  public void PreserveCatalogAssets(){materials.Clear();generatedMeshes.Clear();generatedTextures.Clear();clips.Clear();}
 }
}
