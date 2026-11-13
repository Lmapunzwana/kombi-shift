using System;
using System.Collections.Generic;
using UnityEngine;

namespace KombiShift2.City
{
    [Serializable] public sealed class CityIntersectionArm
    {
        public string id,control; public float bearing,width; public bool major,incoming,outgoing; public int phase;
        public Vector3 approachPoint;
        public Renderer lampHead,repeater;
        public Vector3 Direction=>new(Mathf.Sin(bearing*Mathf.Deg2Rad),0f,Mathf.Cos(bearing*Mathf.Deg2Rad));
    }
    public sealed class CityIntersection : MonoBehaviour
    {
        public static readonly List<CityIntersection> Active=new();
        public string junctionId,junctionType,control; public bool signalised,stopControlled,roundabout; public float radius=8f;
        public CityIntersectionArm[] arms=Array.Empty<CityIntersectionArm>();
        [HideInInspector] public Renderer[] eastWestLamps,northSouthLamps;
        readonly List<Reservation> reservations=new(); readonly Collider[] occupants=new Collider[24]; MaterialPropertyBlock lamp;
        public const float GreenSeconds=7f,AmberSeconds=2.5f,AllRedSeconds=1.5f;
        const float PlatoonGap=8f;
        struct Reservation { public Rigidbody vehicle; public Vector3 heading,exitDirection; public bool entered; }
        void Awake(){lamp=new MaterialPropertyBlock();}
        public int PhaseCount { get { int count=0;if(arms!=null)foreach(var arm in arms)if(arm!=null&&arm.incoming)count=Mathf.Max(count,arm.phase+1);return Mathf.Max(1,count); } }
        float CycleTime { get { int hash=StableHash(string.IsNullOrEmpty(junctionId)?name:junctionId);float duration=GreenSeconds+AmberSeconds+AllRedSeconds;return Mathf.Repeat(Time.time+Mathf.Abs(hash%1000)*.031f,duration*PhaseCount); } }
        public int Phase=>Mathf.FloorToInt(CycleTime/(GreenSeconds+AmberSeconds+AllRedSeconds));
        float PhaseTime=>Mathf.Repeat(CycleTime,GreenSeconds+AmberSeconds+AllRedSeconds);
        public bool Amber=>PhaseTime>=GreenSeconds&&PhaseTime<GreenSeconds+AmberSeconds;
        public bool AllRed=>PhaseTime>=GreenSeconds+AmberSeconds;
        public bool MustStop(Vector3 heading)=>signalised&&!SignalAllows(heading);
        public bool RequiresStop(Vector3 heading){int index=ArmIndex(heading);return index>=0&&arms[index].control=="Stop";}
        void OnEnable(){if(!Active.Contains(this))Active.Add(this);} void OnDisable(){Active.Remove(this);}
        void Update()
        {
            if(lamp==null)lamp=new MaterialPropertyBlock();
            if(arms!=null)foreach(var arm in arms)if(arm!=null){Color colour=ArmColour(arm);SetLamp(arm.lampHead,colour);SetLamp(arm.repeater,colour);}
            reservations.RemoveAll(r=>r.vehicle==null||!r.vehicle.gameObject.activeInHierarchy||Vector3.Distance(r.vehicle.position,transform.position)>radius+6f);
        }
        Color ArmColour(CityIntersectionArm arm)=>!signalised||arm.phase!=Phase||AllRed?Color.red:Amber?new Color(1,.55f,.05f):Color.green;
        void SetLamp(Renderer renderer,Color color){if(renderer==null)return;lamp.SetColor("_Color",color);lamp.SetColor("_EmissionColor",color*2f);renderer.SetPropertyBlock(lamp);}
        public int ArmIndex(Vector3 heading)
        {
            heading.y=0f;if(heading.sqrMagnitude<.01f||arms==null)return -1;heading.Normalize();int best=-1;float dot=-2f;
            for(int i=0;i<arms.Length;i++){var arm=arms[i];if(arm==null||!arm.incoming)continue;float candidate=Vector3.Dot(heading,-arm.Direction);if(candidate>dot){dot=candidate;best=i;}}
            return dot>.35f?best:-1;
        }
        public bool SignalAllows(Vector3 heading){if(!signalised)return true;int index=ArmIndex(heading);return index>=0&&arms[index].phase==Phase&&!Amber&&!AllRed;}
        public bool MayEnter(Rigidbody vehicle,Vector3 heading,Vector3 exit)
        {
            if(vehicle==null)return false;heading.y=0f;heading.Normalize();Vector3 exitDirection=exit-transform.position;exitDirection.y=0f;exitDirection.Normalize();
            for(int i=0;i<reservations.Count;i++){var r=reservations[i];if(r.vehicle!=vehicle)continue;if(r.entered||!signalised)return true;if(!SignalAllows(heading)){reservations.RemoveAt(i);return false;}if(Vector3.Distance(vehicle.position,transform.position)<=radius){r.entered=true;reservations[i]=r;}return true;}
            if(!SignalAllows(heading))return false;
            foreach(var r in reservations)if(!SameMovement(r,heading,exitDirection)||Vector3.Distance(r.vehicle.position,vehicle.position)<PlatoonGap)return false;
            int count=Physics.OverlapSphereNonAlloc(transform.position,radius,occupants,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++){var other=occupants[i].attachedRigidbody;if(other==null||other==vehicle)continue;if(Vector3.Distance(other.position,transform.position)<radius*.7f){var r=reservations.Find(x=>x.vehicle==other);if(r.vehicle==null||!SameMovement(r,heading,exitDirection)||Vector3.Distance(other.position,vehicle.position)<PlatoonGap)return false;}var delta=other.position-vehicle.position;if(Vector3.Dot(delta,vehicle.transform.right)>1f&&other.linearVelocity.magnitude>1f&&delta.magnitude<radius*1.7f)return false;}
            int outside=Physics.OverlapSphereNonAlloc(exit,1.8f,occupants,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<outside;i++){var other=occupants[i].attachedRigidbody;if(other==null||other==vehicle)continue;var r=reservations.Find(x=>x.vehicle==other);if(r.vehicle==null||!SameMovement(r,heading,exitDirection)||Vector3.Distance(other.position,vehicle.position)<PlatoonGap)return false;}
            reservations.Add(new Reservation{vehicle=vehicle,heading=heading,exitDirection=exitDirection,entered=Vector3.Distance(vehicle.position,transform.position)<=radius});return true;
        }
        static bool SameMovement(Reservation r,Vector3 heading,Vector3 exitDirection)=>Vector3.Dot(r.heading,heading)>.95f&&Vector3.Dot(r.exitDirection,exitDirection)>.85f;
        static int StableHash(string value){unchecked{int hash=23;foreach(char c in value)hash=hash*31+c;return hash;}}
        public static CityIntersection Near(Vector3 point){CityIntersection best=null;float distance=float.MaxValue;foreach(var junction in Active){if(junction==null)continue;float d=Vector3.Distance(point,junction.transform.position);if(d<junction.radius+10f&&d<distance){distance=d;best=junction;}}return best;}
    }
}
