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
        Rigidbody owner;float leasedAt;readonly Collider[] occupants=new Collider[24];MaterialPropertyBlock lamp;
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
            if(owner!=null && (Vector3.Distance(owner.position,transform.position)>radius+6f || !owner.gameObject.activeInHierarchy))owner=null;
        }
        void SetLamps(Renderer[] lamps,bool green)
        {
            Color color=green&&!AllRed?(Amber?new Color(1,.55f,.05f):Color.green):Color.red;
            lamp.SetColor("_Color",color);lamp.SetColor("_EmissionColor",color*2);
            if(lamps!=null)foreach(var renderer in lamps)if(renderer!=null)renderer.SetPropertyBlock(lamp);
        }
        public bool MayEnter(Rigidbody vehicle,Vector3 heading,Vector3 exit)
        {
            if(owner==vehicle)return true;
            if(signalised)
            {int axis=Mathf.Abs(heading.x)>Mathf.Abs(heading.z)?0:1;if(axis!=Phase||Amber||AllRed)return false;}
            int count=Physics.OverlapSphereNonAlloc(transform.position,radius,occupants,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var other=occupants[i].attachedRigidbody;if(other==null||other==vehicle)continue;
                if(Vector3.Distance(other.position,transform.position)<radius*.7f)return false;
                // Left-hand intersections yield to traffic approaching from the right.
                var delta=other.position-vehicle.position;
                if(Vector3.Dot(delta,vehicle.transform.right)>1f && other.linearVelocity.magnitude>1f && delta.magnitude<radius*1.7f)return false;
            }
            if(owner!=null)return false;
            // Never reserve a junction whose immediate exit is blocked.
            int outside=Physics.OverlapSphereNonAlloc(exit,1.8f,occupants,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<outside;i++){var other=occupants[i].attachedRigidbody;if(other!=null&&other!=vehicle)return false;}
            owner=vehicle;leasedAt=Time.time;return true;
        }
        public static CityIntersection Near(Vector3 point)
        {CityIntersection best=null;float distance=float.MaxValue;foreach(var junction in Active){if(junction==null)continue;float d=Vector3.Distance(point,junction.transform.position);if(d<junction.radius+10f&&d<distance){distance=d;best=junction;}}return best;}
    }
}
