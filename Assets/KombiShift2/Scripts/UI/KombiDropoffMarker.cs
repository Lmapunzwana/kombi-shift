using System.Linq;
using KombiShift2.Gameplay;
using UnityEngine;
namespace KombiShift2.UI
{
    public sealed class KombiDropoffMarker : MonoBehaviour
    {
        KombiShiftGameDirector director;LineRenderer ring;Material material;PassengerStation target;float next,completedUntil;string state;
        void Start()
        {
            director=GetComponent<KombiShiftGameDirector>();
            var go=new GameObject("Requested passenger stopping ring");go.transform.SetParent(transform,false);
            ring=go.AddComponent<LineRenderer>();ring.useWorldSpace=true;ring.loop=true;ring.positionCount=64;ring.widthMultiplier=.12f;
            material=new Material(Shader.Find("Sprites/Default"));ring.sharedMaterial=material;ring.enabled=false;
            director.TripChanged+=Changed;
        }
        void Changed(PassengerTrip trip){if(trip.state==TripState.Completed){completedUntil=Time.time+2f;state="COMPLETED";}}
        void Update()
        {
            if(director==null||director.Paused)return;
            if(Time.time>=next)
            {
                next=Time.time+.2f;
                var trip=director.Trips.Where(t=>t.requested&&t.state!=TripState.Completed).OrderBy(t=>(director.Stations[t.destination].ServicePoint.position-director.Vehicle.transform.position).sqrMagnitude).FirstOrDefault();
                if(trip!=null)
                {
                    target=director.Stations[trip.destination];float distance=Vector3.Distance(target.ServicePoint.position,director.Vehicle.transform.position);
                    bool aligned=target.MatchesApproach(director.Vehicle.transform);
                    state=trip.missed?"MISSED — STOP AT A SAFE RECOVERY STATION":distance<director.StoppingRadius&&aligned?(director.Vehicle.absoluteSpeed<2f?"READY":"STOPPING"):"APPROACHING";
                    ring.enabled=distance<300f;
                }
                else if(Time.time>completedUntil){target=null;ring.enabled=false;}
            }
            if(target==null||!ring.enabled)return;
            Color color=state=="READY"||state=="COMPLETED"?new Color(.2f,1f,.5f):state.StartsWith("MISSED")?new Color(1,.25f,.18f):new Color(1,.75f,.2f);
            color.a=.55f+Mathf.Sin(Time.time*3f)*.18f;ring.startColor=ring.endColor=color;
            for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;ring.SetPosition(i,target.ServicePoint.position+new Vector3(Mathf.Cos(angle)*3.5f,.07f,Mathf.Sin(angle)*3.5f));}
        }
        void OnGUI()
        {
            if(target==null||ring==null||!ring.enabled||Camera.main==null)return;
            var p=Camera.main.WorldToScreenPoint(target.ServicePoint.position+Vector3.up*1.2f);
            if(p.z<0)return;float distance=Vector3.Distance(target.ServicePoint.position,director.Vehicle.transform.position);
            GUI.Box(new Rect(Mathf.Clamp(p.x-170,5,Screen.width-345),Mathf.Clamp(Screen.height-p.y-35,110,Screen.height-95),340,60),target.name+"  /  "+Mathf.RoundToInt(distance)+" m\n"+state);
        }
        void OnDestroy(){if(director!=null)director.TripChanged-=Changed;if(material!=null)Destroy(material);}
    }
}
