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
    void Awake(){if(active!=null&&active!=this)Destroy(active.gameObject);active=this;}
    IEnumerator Start()
    {
        if(active!=this)yield break;
        Application.runInBackground=true;
        folder=Path.GetFullPath("Docs/Validation/TrafficCompactHud");Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"traffic-observation.txt"),"");
        Debug.Log("KOMBI_TRAFFIC_OBSERVATION_START");
        for(int second=0;second<91;second++)
        {
            foreach(var agent in FindObjectsByType<CityTrafficAgent>())
            {
                var id=agent;
                if(positions.TryGetValue(id,out var p)&&Vector2.Distance(new Vector2(agent.transform.position.x,agent.transform.position.z),new Vector2(p.x,p.z))>2f)moved.Add(id);
                else if(!positions.ContainsKey(id))positions[id]=agent.transform.position;
                if(agent.GetComponent<TrafficContactCapture>()==null)agent.gameObject.AddComponent<TrafficContactCapture>().owner=this;
            }
            var agents=FindObjectsByType<CityTrafficAgent>();peak=Mathf.Max(peak,agents.Length);
            if(second==10||second==30||second==60||second==90)
            {
                string details=string.Join("\n",agents.Select(a=>{
                    var b=a.Body;var wheels=a.GetComponentsInChildren<WheelCollider>();
                    var field=typeof(CityTrafficAgent).GetField("desiredSpeed",BindingFlags.Instance|BindingFlags.NonPublic);
                    var probe=typeof(CityTrafficAgent).GetMethod("Obstacle",BindingFlags.Instance|BindingFlags.NonPublic);
                    var args=new object[]{b.position+a.transform.forward*1.7f+Vector3.up*1.15f,a.transform.forward,12f,default(RaycastHit)};
                    bool blocked=(bool)probe.Invoke(a,args);var hit=(RaycastHit)args[3];
                    var junction=typeof(CityTrafficAgent).GetField("lastJunction",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(a) as CityIntersection;
                    string why=$"obstacle={(blocked?hit.collider.name:"none")} gap={hit.distance:0.0}, junction={(junction!=null?junction.name:"none")} signal={(junction!=null&&junction.MustStop(a.transform.forward))}";
                    return $"{a.name}: {why}, speed={b.linearVelocity.magnitude:0.00}, desired={field.GetValue(a)}, kinematic={b.isKinematic}, grounded={wheels.Count(w=>w.isGrounded)}/{wheels.Length}, torque={wheels.FirstOrDefault()?.motorTorque}, node={a.CurrentNode}, pos={b.position}";
                }));
                string text=$"t={second}, active={agents.Length}, moved={moved.Count}, contacts={contacts}\n{details}";
                File.AppendAllText(Path.Combine(folder,"traffic-observation.txt"),text+"\n");Debug.Log("KOMBI_TRAFFIC_OBSERVATION "+text);
                if(second==90)
                {
                    yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(folder,"compact-hud-traffic.png"),shot.EncodeToPNG());Destroy(shot);
                }
            }
            yield return new WaitForSeconds(1);
        }
        File.WriteAllText(Path.Combine(folder,"result.json"),$"{{\"peakActive\":{peak},\"movedVehicles\":{moved.Count},\"vehicleContacts\":{contacts}}}");
        Debug.Log($"KOMBI_TRAFFIC_OBSERVATION_DONE active={peak} moved={moved.Count} contacts={contacts}");
    }
    public void Contact(string vehicle,Collision collision)
    {
        if(collision.rigidbody!=null || collision.contacts.Any(c=>Mathf.Abs(c.normal.y)<.5f))
        {contacts++;Debug.LogWarning("KOMBI_TRAFFIC_CONTACT "+vehicle+" with "+collision.collider.name+" at "+collision.GetContact(0).point);}
    }
    void OnDestroy(){if(active==this)active=null;}
}
public sealed class TrafficContactCapture : MonoBehaviour
{
    public TrafficMotionCapture owner;
    void OnCollisionEnter(Collision collision){if(owner!=null)owner.Contact(name,collision);}
}
#endif
