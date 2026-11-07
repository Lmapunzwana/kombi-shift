using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using KombiShift.JaydenStarter;
public sealed class HDRPSceneLighting:MonoBehaviour {
 public VolumeProfile day,rain;Volume volume;ShiftGame game;
 void Awake(){game=GetComponent<ShiftGame>();game.WeatherChanged+=Apply;volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;volume.sharedProfile=day;}
 void Start()=>Apply(game.Rainy);
 void Apply(bool wet){volume.sharedProfile=wet?rain:day;foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None)){if(light.type!=LightType.Directional)continue;if(light.GetComponent<HDAdditionalLightData>()==null)light.gameObject.AddComponent<HDAdditionalLightData>();light.lightUnit=UnityEngine.Rendering.LightUnit.Lux;light.intensity=wet?18000:60000;light.shadows=LightShadows.None;}foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(camera.GetComponent<HDAdditionalCameraData>()==null)camera.gameObject.AddComponent<HDAdditionalCameraData>();}
 void OnDestroy(){if(game!=null)game.WeatherChanged-=Apply;}
}
