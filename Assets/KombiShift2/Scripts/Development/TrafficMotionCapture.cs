#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KombiShift2.City;
using UnityEngine;

public sealed class TrafficMotionCapture : MonoBehaviour
{
    static TrafficMotionCapture active;
    readonly Dictionary<CityTrafficAgent,Vector3> positions=new();
    readonly HashSet<CityTrafficAgent> moved=new();
    int peak,contacts;
    string folder;
    void Awake(){if(active!=null&&active!=this){Destroy(gameObject);return;}active=this;}
    IEnumerator Start()
    {
        if(active!=this)yield break;
        Application.runInBackground=true;
        folder=Path.GetFullPath("Docs/Validation/TrafficCompactHud");Directory.CreateDirectory(folder);
        Debug.Log("KOMBI_TRAFFIC_OBSERVATION_START");
        for(int second=0;second<46;second++)
        {
            foreach(var agent in FindObjectsByType<CityTrafficAgent>())
            {
                var id=agent;
                if(positions.TryGetValue(id,out var p)&&(agent.transform.position-p).sqrMagnitude>4)moved.Add(id);
                else if(!positions.ContainsKey(id))positions[id]=agent.transform.position;
                if(agent.GetComponent<TrafficContactCapture>()==null)agent.gameObject.AddComponent<TrafficContactCapture>().owner=this;
            }
            var agents=FindObjectsByType<CityTrafficAgent>();peak=Mathf.Max(peak,agents.Length);
            if(second==10||second==30||second==45)
            {
                string details=string.Join("\n",agents.Take(8).Select(a=>{
                    var b=a.Body;var wheels=a.GetComponentsInChildren<WheelCollider>();
                    var field=typeof(CityTrafficAgent).GetField("desiredSpeed",BindingFlags.Instance|BindingFlags.NonPublic);
                    return $"{a.name}: speed={b.linearVelocity.magnitude:0.00}, desired={field.GetValue(a)}, kinematic={b.isKinematic}, grounded={wheels.Count(w=>w.isGrounded)}/{wheels.Length}, torque={wheels.FirstOrDefault()?.motorTorque}, node={a.CurrentNode}, pos={b.position}";
                }));
                string text=$"t={second}, active={agents.Length}, moved={moved.Count}, contacts={contacts}\n{details}";
                File.AppendAllText(Path.Combine(folder,"traffic-observation.txt"),text+"\n");Debug.Log("KOMBI_TRAFFIC_OBSERVATION "+text);
                yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder,"compact-hud-traffic.png"),shot.EncodeToPNG());Destroy(shot);
            }
            yield return new WaitForSeconds(1);
        }
        File.WriteAllText(Path.Combine(folder,"result.json"),$"{{\"peakActive\":{peak},\"movedVehicles\":{moved.Count},\"vehicleContacts\":{contacts}}}");
        Debug.Log($"KOMBI_TRAFFIC_OBSERVATION_DONE active={peak} moved={moved.Count} contacts={contacts}");
    }
    public void Contact(Collision collision)
    {
        if(collision.rigidbody!=null&&collision.rigidbody.GetComponent<CityTrafficAgent>()!=null)contacts++;
    }
    void OnDestroy(){if(active==this)active=null;}
}
public sealed class TrafficContactCapture : MonoBehaviour
{
    public TrafficMotionCapture owner;
    void OnCollisionEnter(Collision collision){if(owner!=null)owner.Contact(collision);}
}
#endif
