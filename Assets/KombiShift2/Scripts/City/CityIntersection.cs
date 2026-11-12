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
        Collider[] occupants=new Collider[24];MaterialPropertyBlock lamp;
        public int SaturatedQueries {get;private set;}
        const float PlatoonGap=8f;
        const float CellSize=64f;
        static readonly Dictionary<Vector2Int,List<CityIntersection>> cells=new();
        static float maximumRadius=8f;
        Vector2Int indexedCell;float indexedRadius;Vector3 indexedPosition;
        static Vector2Int Cell(Vector3 p)=>new(Mathf.FloorToInt(p.x/CellSize),Mathf.FloorToInt(p.z/CellSize));
        void Register()
        {
            indexedPosition=transform.position;indexedCell=Cell(indexedPosition);indexedRadius=radius;
            if(!cells.TryGetValue(indexedCell,out var entries))cells[indexedCell]=entries=new List<CityIntersection>();
            if(!entries.Contains(this))entries.Add(this);
            maximumRadius=Mathf.Max(maximumRadius,radius);
        }
        void Unregister()
        {
            if(cells.TryGetValue(indexedCell,out var entries)){entries.Remove(this);if(entries.Count==0)cells.Remove(indexedCell);}
        }
        public void Release(Rigidbody vehicle)
        {
            for(int i=reservations.Count-1;i>=0;i--)if(reservations[i].vehicle==vehicle)reservations.RemoveAt(i);
        }
        public static void ReleaseAll(Rigidbody vehicle)
        {if(vehicle!=null)foreach(var junction in Active)if(junction!=null)junction.Release(vehicle);}
        void PruneReservations()
        {
            for(int i=reservations.Count-1;i>=0;i--)
            {
                var item=reservations[i];
                if(item.vehicle==null || !item.vehicle.gameObject.activeInHierarchy ||
                    (item.vehicle.position-transform.position).sqrMagnitude>(radius+24f)*(radius+24f) ||
                    (!item.entered && Time.time-item.lastSeen>3f)) reservations.RemoveAt(i);
            }
        }
        struct Reservation
        {
            public Rigidbody vehicle;
            public Vector3 heading,exitDirection;
            public bool entered;
            public float lastSeen;
        }
        void Awake(){lamp=new MaterialPropertyBlock();}
        public int Phase=>Mathf.FloorToInt(Time.time/14f)%2;
        public bool Amber=>Time.time%14f>10f&&Time.time%14f<=12f;
        public bool AllRed=>Time.time%14f>12f;
        public bool MustStop(Vector3 heading)=>signalised&&(AllRed||Amber||(Mathf.Abs(heading.x)>Mathf.Abs(heading.z)?0:1)!=Phase);
        void OnEnable(){if(!Active.Contains(this))Active.Add(this);Register();}
        void OnDisable(){Unregister();Active.Remove(this);reservations.Clear();}
        void Update()
        {
            if(indexedPosition!=transform.position || indexedRadius!=radius){Unregister();Register();}
            SetLamps(eastWestLamps,Phase==0);SetLamps(northSouthLamps,Phase==1);
            PruneReservations();
        }
        void SetLamps(Renderer[] lamps,bool green)
        {
            // Nonserialized resources must also survive an Editor script reload.
            lamp??=new MaterialPropertyBlock();
            Color color=green&&!AllRed?(Amber?new Color(1,.55f,.05f):Color.green):Color.red;
            lamp.SetColor("_Color",color);lamp.SetColor("_EmissionColor",color*2);
            if(lamps!=null)foreach(var renderer in lamps)if(renderer!=null)renderer.SetPropertyBlock(lamp);
        }
        public bool MayEnter(Rigidbody vehicle,Vector3 heading,Vector3 exit)
        {
            if(vehicle==null)return false;
            PruneReservations();
            heading.y=0f;heading.Normalize();
            Vector3 exitDirection=exit-transform.position;exitDirection.y=0f;exitDirection.Normalize();
            for(int i=0;i<reservations.Count;i++)
            {
                var reservation=reservations[i];if(reservation.vehicle!=vehicle)continue;
                reservation.lastSeen=Time.time;
                reservations[i]=reservation;
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
            int count=CityPhysicsQueries.OverlapSphere(transform.position,radius,ref occupants,out var nearby,out bool saturated);
            if(saturated)SaturatedQueries++;
            for(int i=0;i<count;i++)
            {
                var other=nearby[i].attachedRigidbody;if(other==null||other==vehicle)continue;
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
            int outside=CityPhysicsQueries.OverlapSphere(exit,1.8f,ref occupants,out var atExit,out saturated);
            if(saturated)SaturatedQueries++;
            for(int i=0;i<outside;i++)
            {
                var other=atExit[i].attachedRigidbody;if(other==null||other==vehicle)continue;
                var reservation=reservations.Find(r=>r.vehicle==other);
                if(reservation.vehicle==null||!SameMovement(reservation,heading,exitDirection)||Vector3.Distance(other.position,vehicle.position)<PlatoonGap)return false;
            }
            reservations.Add(new Reservation{vehicle=vehicle,heading=heading,exitDirection=exitDirection,lastSeen=Time.time,entered=Vector3.Distance(vehicle.position,transform.position)<=radius});return true;
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
        {
            CityIntersection best=null;float distance=float.MaxValue;var cell=Cell(point);
            int range=Mathf.CeilToInt((maximumRadius+10f)/CellSize);
            for(int x=-range;x<=range;x++)for(int z=-range;z<=range;z++)
            {
                if(!cells.TryGetValue(cell+new Vector2Int(x,z),out var entries))continue;
                foreach(var junction in entries)
                {
                    if(junction==null)continue;
                    float d=(point-junction.transform.position).sqrMagnitude;
                    float radius=junction.radius+10f;
                    if(d<radius*radius&&d<distance){distance=d;best=junction;}
                }
            }
            return best;
        }
        public static bool Ahead(Vector3 point,Vector3 heading,float distance)
        {
            var cell=Cell(point);int range=Mathf.CeilToInt(distance/CellSize);
            for(int x=-range;x<=range;x++)for(int z=-range;z<=range;z++)
            {
                if(!cells.TryGetValue(cell+new Vector2Int(x,z),out var entries))continue;
                foreach(var junction in entries)if(junction!=null)
                {var delta=junction.transform.position-point;if(delta.sqrMagnitude<distance*distance&&Vector3.Dot(heading,delta)>0)return true;}
            }
            return false;
        }
    }
}
