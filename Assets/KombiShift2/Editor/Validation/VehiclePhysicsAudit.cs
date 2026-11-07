using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class KombiVehiclePhysicsAudit
{
 public static void Run()
 {
  EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
  var car=Object.FindFirstObjectByType<RCCP_CarController>();var body=car.GetComponent<Rigidbody>();
  string text=$"Vehicle {car.name}: position={car.transform.position}, rotation={car.transform.eulerAngles}, scale={car.transform.lossyScale}, constraints={body.constraints}, mass={body.mass}, centre={body.centerOfMass}\n";
  foreach(var wheel in car.GetComponentsInChildren<WheelCollider>(true))text+=$"Wheel {wheel.name} pos={wheel.transform.position} scale={wheel.transform.lossyScale} radius={wheel.radius} centre={wheel.center} suspension={wheel.suspensionDistance} enabled={wheel.enabled}\n";
  foreach(var hit in Physics.RaycastAll(car.transform.position+Vector3.up*3f,Vector3.down,30f).OrderBy(x=>x.distance))
   if(!hit.transform.IsChildOf(car.transform))text+=$"Ground hit {hit.collider.name}: point={hit.point} normal={hit.normal}\n";
  foreach(var mesh in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
   if(mesh.name.Contains("Road")||mesh.name.Contains("road"))text+=$"Road collider {mesh.name}: enabled={mesh.enabled}, mesh={(mesh.sharedMesh!=null?mesh.sharedMesh.name:"MISSING")}, bounds={mesh.bounds}\n";
  Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/vehicle-physics-audit.txt",text);Debug.Log(text);
 }
}
