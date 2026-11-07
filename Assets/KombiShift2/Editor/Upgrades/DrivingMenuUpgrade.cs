using System.IO;
using System.Linq;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using KombiShift2.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class KombiDrivingMenuUpgrade
{
 const string Materials="Assets/KombiShift2/Generated/MenuStage";
 public static void Build()
 {
  foreach(string path in Directory.GetFiles("Assets/KombiShift2/Art/Characters","*.fbx"))
  {
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   if(importer.importCameras||importer.importLights){importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();}
  }
  EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
  var car=Object.FindFirstObjectByType<RCCP_CarController>();
  if(car.GetComponent<KombiVehicleStability>()==null)car.gameObject.AddComponent<KombiVehicleStability>();
  var body=car.GetComponent<Rigidbody>();body.centerOfMass=new Vector3(0,.24f,.04f);body.constraints|=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
  int paint=0;
  foreach(var collider in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
   if(collider.name.StartsWith("Centre Line")){collider.enabled=false;EditorUtility.SetDirty(collider);paint++;}
  var follow=Object.FindFirstObjectByType<KombiShift2.CameraSystem.KombiDriveCamera>();
  foreach(var extra in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
  {
   if(extra==follow.GetComponent<Camera>()||extra.targetTexture!=null)continue;
   var listener=extra.GetComponent<AudioListener>();if(listener!=null)Object.DestroyImmediate(listener);
   Object.DestroyImmediate(extra);
  }
  follow.ResetView();
  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  var display=Object.Instantiate(car.gameObject);display.name="Hiace Showcase";
  foreach(var script in display.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
  foreach(var collider in display.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
  foreach(var source in display.GetComponentsInChildren<AudioSource>(true))Object.DestroyImmediate(source);
  foreach(var light in display.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(light);
  foreach(var rigid in display.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(rigid);
  foreach(var particle in display.GetComponentsInChildren<ParticleSystem>(true))particle.gameObject.SetActive(false);
  foreach(var child in display.transform.Cast<Transform>().ToArray())
   if(child.name.StartsWith("Model_Skyline")||child.name.StartsWith("RCCP_")||child.name=="Passenger Cabin")Object.DestroyImmediate(child.gameObject);
  display.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(display,scene);
  EditorSceneManager.SetActiveScene(scene);
  EditorSceneManager.CloseScene(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(KombiProjectWorkbench.Shift),true);
  Directory.CreateDirectory(Materials);AssetDatabase.Refresh();
  var renderers=display.GetComponentsInChildren<MeshRenderer>().Where(x=>x.enabled).ToArray();
  Bounds bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
  display.transform.position+=Vector3.up*(-bounds.min.y+.025f);
  var floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/KombiShift2/Art/Roads/Materials/WornAsphalt.mat");
  var surface=AssetDatabase.LoadAssetAtPath<Material>(Materials+"/Menu asphalt.mat");
  if(surface==null){surface=new Material(floor);AssetDatabase.CreateAsset(surface,Materials+"/Menu asphalt.mat");}
  surface.color=new Color(.16f,.19f,.21f);surface.mainTextureScale=Vector2.one*14f;
  Cube("Rank surface",new Vector3(0,-.12f,0),new Vector3(70,.2f,70),surface);
  var wall=Mat("Concrete",new Color(.10f,.15f,.17f));var steel=Mat("Shutter steel",new Color(.16f,.21f,.22f));var white=Mat("Paint",new Color(.68f,.68f,.57f));var yellow=Mat("Rank yellow",new Color(.65f,.43f,.15f));
  Cube("Rank workshop wall",new Vector3(0,2.6f,-8),new Vector3(22,5.2f,.35f),wall);
  for(int i=0;i<19;i++)Cube("Shutter slat",new Vector3(1.7f,.15f+i*.205f,-7.78f),new Vector3(6.8f,.18f,.08f),steel);
  Cube("Bay left edge",new Vector3(-2.2f,.01f,0),new Vector3(.085f,.02f,7),white);
  Cube("Bay right edge",new Vector3(2.2f,.01f,0),new Vector3(.085f,.02f,7),white);
  Cube("Bay rear line",new Vector3(0,.011f,-3.5f),new Vector3(4.5f,.02f,.1f),yellow);
  for(int i=0;i<6;i++)Cube("Rank safety marking",new Vector3(-5f+i*.4f,.012f,3.2f),new Vector3(.22f,.02f,1.2f),yellow);
  var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";
  var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.045f,.058f);camera.nearClipPlane=.1f;camera.farClipPlane=90;camera.fieldOfView=43;
  Vector3 focus=display.transform.position+Vector3.up*.85f;camera.transform.position=focus+Quaternion.Euler(9,146,0)*Vector3.back*7.3f;camera.transform.LookAt(focus+Vector3.right*1.3f);
  Light("Warm key",new Vector3(35,-25,0),new Color(1,.9f,.76f),1.3f);
  Light("Soft fill",new Vector3(20,130,0),new Color(.60f,.76f,.88f),.7f);
  Light("Back fill",new Vector3(65,220,0),new Color(.85f,.90f,.95f),.45f);
  RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.38f,.42f);RenderSettings.fog=false;
  new GameObject("Kombi Main Menu",typeof(KombiMainMenu));
  var showcase=new GameObject("Live Hiace Presentation",typeof(KombiMenuShowcase)).GetComponent<KombiMenuShowcase>();showcase.Configure(display.transform,camera);
  new GameObject("Presentation Settings",typeof(KombiPresentationSettings));KombiPresentationSettings.Apply();
  EditorSceneManager.SaveScene(scene,KombiProjectWorkbench.Menu);AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/driving-menu-upgrade.txt",$"Live Hiace menu rebuilt. Painted road colliders disabled: {paint}. Vehicle upright arcade stabilization and ground-aware camera applied.\n");
  Debug.Log("KOMBI_DRIVING_MENU_BUILT");
 }
 static Material Mat(string name,Color color){string path=$"{Materials}/{name}.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;mat.SetFloat("_Glossiness",.18f);return mat;}
 static void Cube(string name,Vector3 position,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());}
 static void Light(string name,Vector3 rotation,Color color,float intensity){var go=new GameObject(name,typeof(Light));go.transform.rotation=Quaternion.Euler(rotation);var light=go.GetComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;light.shadows=LightShadows.None;}
 static void Sign(string name,string message,Vector3 pos,float size){var go=new GameObject(name,typeof(TextMesh));go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,180,0);var text=go.GetComponent<TextMesh>();text.text=message;text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.font=Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");go.GetComponent<Renderer>().sharedMaterial=text.font.material;text.color=new Color(.67f,.71f,.65f);}
}
