using System.Collections.Generic;
using UnityEngine;
namespace KombiShift2.City
{
    public sealed class CityIntersection : MonoBehaviour
    {
        public static readonly List<CityIntersection> Active=new();
        public bool signalised;
        public bool stopControlled;
        public bool roundabout;
        public float radius=8f;
        public Renderer[] eastWestLamps,northSouthLamps;
        readonly List<Reservation> reservations=new();
        readonly Collider[] occupants=new Collider[24];MaterialPropertyBlock lamp;
        const float PlatoonGap=8f;
        struct Reservation
        {
            public Rigidbody vehicle;
            public Vector3 heading,exitDirection;
            public bool entered;
        }
        void Awake(){lamp=new MaterialPropertyBlock();}
        public int Phase=>Mathf.FloorToInt(Time.time/14f)%2;
        public bool Amber=>Time.time%14f>10f&&Time.time%14f<=12f;
        public bool AllRed=>Time.time%14f>12f;
        public bool MustStop(Vector3 heading)=>signalised&&(AllRed||Amber||(Mathf.Abs(heading.x)>Mathf.Abs(heading.z)?0:1)!=Phase);
        void OnEnable(){Active.Add(this);}
        void OnDisable(){Active.Remove(this);}
        void Update()
        {
            SetLamps(eastWestLamps,Phase==0);SetLamps(northSouthLamps,Phase==1);
            reservations.RemoveAll(r=>r.vehicle==null||!r.vehicle.gameObject.activeInHierarchy||Vector3.Distance(r.vehicle.position,transform.position)>radius+6f);
        }
        void SetLamps(Renderer[] lamps,bool green)
        {
            Color color=green&&!AllRed?(Amber?new Color(1,.55f,.05f):Color.green):Color.red;
            lamp.SetColor("_Color",color);lamp.SetColor("_EmissionColor",color*2);
            if(lamps!=null)foreach(var renderer in lamps)if(renderer!=null)renderer.SetPropertyBlock(lamp);
        }
        public bool MayEnter(Rigidbody vehicle,Vector3 heading,Vector3 exit)
        {
            if(vehicle==null)return false;
            heading.y=0f;heading.Normalize();
            Vector3 exitDirection=exit-transform.position;exitDirection.y=0f;exitDirection.Normalize();
            for(int i=0;i<reservations.Count;i++)
            {
                var reservation=reservations[i];if(reservation.vehicle!=vehicle)continue;
                if(reservation.entered||!signalised)return true;
                if(!SignalAllows(heading)){reservations.RemoveAt(i);return false;}
                if(Vector3.Distance(vehicle.position,transform.position)<=radius){reservation.entered=true;reservations[i]=reservation;}
                return true;
            }
            if(!SignalAllows(heading))return false;
            foreach(var reservation in reservations)
            {
                if(!SameMovement(reservation,heading,exitDirection)||Vector3.Distance(reservation.vehicle.position,vehicle.position)<PlatoonGap)return false;
            }
            int count=Physics.OverlapSphereNonAlloc(transform.position,radius,occupants,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var other=occupants[i].attachedRigidbody;if(other==null||other==vehicle)continue;
                if(Vector3.Distance(other.position,transform.position)<radius*.7f)
                {
                    var reservation=reservations.Find(r=>r.vehicle==other);
                    if(reservation.vehicle==null||!SameMovement(reservation,heading,exitDirection)||Vector3.Distance(other.position,vehicle.position)<PlatoonGap)return false;
                }
                // Left-hand intersections yield to traffic approaching from the right.
                var delta=other.position-vehicle.position;
                if(Vector3.Dot(delta,vehicle.transform.right)>1f && other.linearVelocity.magnitude>1f && delta.magnitude<radius*1.7f)return false;
            }
            // Never reserve a junction whose immediate exit is blocked.
            int outside=Physics.OverlapSphereNonAlloc(exit,1.8f,occupants,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<outside;i++)
            {
                var other=occupants[i].attachedRigidbody;if(other==null||other==vehicle)continue;
                var reservation=reservations.Find(r=>r.vehicle==other);
                if(reservation.vehicle==null||!SameMovement(reservation,heading,exitDirection)||Vector3.Distance(other.position,vehicle.position)<PlatoonGap)return false;
            }
            reservations.Add(new Reservation{vehicle=vehicle,heading=heading,exitDirection=exitDirection,entered=Vector3.Distance(vehicle.position,transform.position)<=radius});return true;
        }
        bool SignalAllows(Vector3 heading)
        {
            if(!signalised)return true;
            int axis=Mathf.Abs(heading.x)>Mathf.Abs(heading.z)?0:1;
            return axis==Phase&&!Amber&&!AllRed;
        }
        static bool SameMovement(Reservation reservation,Vector3 heading,Vector3 exitDirection)
            =>Vector3.Dot(reservation.heading,heading)>.95f&&Vector3.Dot(reservation.exitDirection,exitDirection)>.85f;
        public static CityIntersection Near(Vector3 point)
        {CityIntersection best=null;float distance=float.MaxValue;foreach(var junction in Active){if(junction==null)continue;float d=Vector3.Distance(point,junction.transform.position);if(d<junction.radius+10f&&d<distance){distance=d;best=junction;}}return best;}
    }
}
