using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using KombiShift.JaydenStarter;
using KombiShift.JaydenStarter.Editor;
public static class HDRPSetup {
 const string dir="Assets/KombiShiftHDRP";
 public static void BuildDesktop(){Configure();StarterBuild.Linux();}
 public static void Configure(){Directory.CreateDirectory(dir);AssetDatabase.Refresh();PlayerSettings.colorSpace=ColorSpace.Linear;
  var pipeline=AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(dir+"/KombiHDRP.asset");if(pipeline==null){pipeline=ScriptableObject.CreateInstance<HDRenderPipelineAsset>();AssetDatabase.CreateAsset(pipeline,dir+"/KombiHDRP.asset");}
  var settings=pipeline.currentPlatformRenderPipelineSettings;settings.supportedLitShaderMode=RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly;settings.supportSSR=false;settings.supportSSRTransparent=false;settings.supportSSAO=false;settings.supportSSGI=false;settings.supportVolumetrics=false;settings.supportVolumetricClouds=false;settings.supportLightLayers=false;settings.supportShadowMask=false;settings.supportSubsurfaceScattering=false;settings.supportWater=false;settings.supportDecals=false;settings.supportMotionVectors=false;pipeline.currentPlatformRenderPipelineSettings=settings;EditorUtility.SetDirty(pipeline);
  GraphicsSettings.defaultRenderPipeline=pipeline;for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=pipeline;}QualitySettings.SetQualityLevel(0,false);
  typeof(HDRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings").GetMethod("Ensure",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{true});
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64,new[]{GraphicsDeviceType.Vulkan});PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
  foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/KombiShiftJayden"})){var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(material.shader.name=="Standard"){Color c=material.color;material.shader=Shader.Find("HDRP/Lit");material.SetColor("_BaseColor",c);EditorUtility.SetDirty(material);}else if(material.shader.name=="KombiShift/WorldLabel"){material.shader=Shader.Find("KombiShift/HDRPWorldLabel");EditorUtility.SetDirty(material);}}
  var scene=EditorSceneManager.OpenScene("Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity");var game=Object.FindFirstObjectByType<ShiftGame>();var light=game.GetComponent<HDRPSceneLighting>();if(light==null)light=game.gameObject.AddComponent<HDRPSceneLighting>();light.day=Profile("Day",false);light.rain=Profile("Rain",true);CorrectProfile(light.day,false);CorrectProfile(light.rain,true);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();var workshopProfile=light.day;CompleteProject.Prepare();ConfigureWorkshop(workshopProfile);AssetDatabase.SaveAssets();Debug.Log("KOMBI_HDRP_CONFIGURED");
 }
 static void CorrectProfile(VolumeProfile p,bool wet){if(p.TryGet<Exposure>(out var e))e.fixedExposure.Override(wet?13:14.5f);if(p.TryGet<GradientSky>(out var sky)){sky.skyIntensityMode.Override(SkyIntensityMode.Multiplier);sky.multiplier.Override(wet?8192:16384);}EditorUtility.SetDirty(p);foreach(var c in p.components)EditorUtility.SetDirty(c);}
 static void ConfigureWorkshop(VolumeProfile day){var path="Assets/KombiShiftJayden/Generated/ArtWorkshop.unity";if(!File.Exists(path))return;var scene=EditorSceneManager.OpenScene(path);var root=GameObject.Find("Workshop HDRP sky and exposure");if(root==null)root=new GameObject("Workshop HDRP sky and exposure");var volume=root.GetComponent<Volume>();if(volume==null)volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=day;foreach(var sun in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){if(sun.GetComponent<HDAdditionalLightData>()==null)sun.gameObject.AddComponent<HDAdditionalLightData>();sun.lightUnit=LightUnit.Lux;sun.intensity=60000;sun.shadows=LightShadows.None;}var cam=Camera.main;if(cam!=null&&cam.GetComponent<HDAdditionalCameraData>()==null)cam.gameObject.AddComponent<HDAdditionalCameraData>();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorSceneManager.OpenScene("Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity");}
 static VolumeProfile Profile(string name,bool wet){var p=AssetDatabase.LoadAssetAtPath<VolumeProfile>(dir+"/"+name+".asset");if(p!=null)return p;p=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(p,dir+"/"+name+".asset");
  var visual=p.Add<VisualEnvironment>(true);visual.skyType.Override((int)SkyType.Gradient);var sky=p.Add<GradientSky>(true);sky.top.Override(wet?new Color(.23f,.28f,.3f):new Color(.32f,.43f,.5f));sky.middle.Override(wet?new Color(.45f,.48f,.48f):new Color(.65f,.7f,.69f));sky.bottom.Override(new Color(.32f,.31f,.28f));
  var exposure=p.Add<Exposure>(true);exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(10);var tone=p.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
  var fog=p.Add<Fog>(true);fog.enabled.Override(wet);fog.meanFreePath.Override(wet?220:600);fog.enableVolumetricFog.Override(false);
  foreach(var c in p.components)AssetDatabase.AddObjectToAsset(c,p);EditorUtility.SetDirty(p);return p;
 }
}
