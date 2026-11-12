using System;
using System.Collections.Generic;
using System.Linq;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using UnityEngine;
namespace KombiShift2.City
{
    [DefaultExecutionOrder(-500)]
    public sealed class CityTripSession : MonoBehaviour
    {
        public GameObject stationPrefab;
        public static bool Active {get;private set;}
        public static float SelectedDuration {get;private set;}
        public static CityTripPlan Plan {get;private set;}
        public static KombiShiftConfiguration Configuration {get;private set;}
        public static float FareForTrip(int origin,int destination,KombiStage stage) => Plan.Fare(origin,destination,stage);
        void Awake()
        {
            Active=false;SelectedDuration=0;Plan=null;Configuration=new KombiShiftConfiguration(KombiChallenge.Selected);
            var selected=CityTripSelection.Selected;if(selected==null)return;
            selected=JsonUtility.FromJson<CityTripDefinition>(JsonUtility.ToJson(selected));
            selected.stopNodes ??= new[]{selected.originNode,selected.destinationNode};
            CityTripSelection.Selected=selected;
            var graph=GetComponent<CityRoadNetwork>();
            var director=FindFirstObjectByType<KombiShiftGameDirector>();
            var navigation=FindFirstObjectByType<KombiNavigationHud>();
            if(graph==null||director==null||director.Vehicle==null||navigation==null)
            {Debug.LogError("Selected city trip requires a network, player/director and navigation HUD; keeping the existing shift.");CityTripSelection.Selected=null;return;}
            graph.Load();
            var plan=graph.PlanTrip(selected);
            var template=stationPrefab!=null?stationPrefab.GetComponent<PassengerStation>():null;
            if(plan==null || template==null || template.People==null || template.People.Length==0 || template.ArrivalSeats==null || template.ArrivalSeats.Length<Configuration.CityQueueSize){Debug.LogError("Selected city trip is unreachable, has invalid fares, or has an incomplete station template; keeping the existing shift.");CityTripSelection.Selected=null;return;}
            var player=director.Vehicle;
            var path=plan.Path;
            Plan=plan;
            foreach(var station in FindObjectsByType<PassengerStation>())
            {var parent=station.ServicePoint.parent;if(parent!=null)parent.gameObject.SetActive(false);if(station.transform!=parent)station.gameObject.SetActive(false);}
            var root=new GameObject("Selected trip — "+selected.destination).transform;root.SetParent(transform,false);
            var points=new Transform[path.Count];
            for(int i=0;i<points.Length;i++){points[i]=new GameObject("Route "+i).transform;points[i].SetParent(root,false);points[i].position=graph.Nodes[path[i]].position;}
            var stops=new List<Transform>();
            for(int i=0;i<selected.stopNodes.Length;i++)
            {
                int node=selected.stopNodes[i],index=plan.PathIndexForStop(i);
                Vector3 forward=graph.Nodes[path[Mathf.Min(path.Count-1,index+1)]].position-graph.Nodes[path[Mathf.Max(0,index-1)]].position;forward.y=0;forward.Normalize();
                Vector3 left=-Vector3.Cross(Vector3.up,forward);var position=graph.Nodes[node].position;
                var go=Instantiate(stationPrefab,root);go.name=i==0?selected.origin+" Rank":i==selected.stopNodes.Length-1?selected.destination+" Terminal":selected.destination+" Stop "+i;
                go.transform.SetPositionAndRotation(Ground(position+left*(graph.Nodes[node].width*.5f+1.5f)),Quaternion.LookRotation(-left));
                var station=go.GetComponent<PassengerStation>();var service=station.ServicePoint;
                service.SetPositionAndRotation(Ground(graph.LanePoint(path,index)),Quaternion.LookRotation(forward));
                var people=station.People;
                bool terminal=i==selected.stopNodes.Length-1;
                if(!terminal)
                {
                    int count=Configuration.CityQueueSize;
                    var demand=new List<StationPassenger>(count);
                    for(int n=0;n<count;n++)
                    {
                        var seat=station.ArrivalSeats[n];
                        var actor=n<people.Length?people[n]:Instantiate(people[n%people.Length].gameObject,seat,false).GetComponent<StationPassenger>();
                        actor.transform.SetParent(seat,false);actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;
                        actor.SetWaitingSeat(seat);actor.PoseSeatedPreview();demand.Add(actor);
                    }
                    for(int n=count;n<people.Length;n++)Destroy(people[n].gameObject);
                    people=demand.ToArray();
                }
                if(terminal){foreach(var person in people)Destroy(person.gameObject);people=Array.Empty<StationPassenger>();}
                station.Configure(people,service,station.ArrivalSeats,terminal);
                foreach(var text in go.GetComponentsInChildren<TextMesh>())text.text=go.name.ToUpperInvariant();
                stops.Add(station.transform);
            }
            if(stops.Count<2)throw new InvalidOperationException("Selected trip needs at least two valid stops.");
            director.Configure(player,stops.ToArray(),4f);navigation.Configure(player,points,director);
            player.transform.SetPositionAndRotation(stops[0].GetComponent<PassengerStation>().ServicePoint.position+Vector3.up*.14f,stops[0].GetComponent<PassengerStation>().ServicePoint.rotation);
            var rigidbody=player.GetComponent<Rigidbody>();rigidbody.position=player.transform.position;rigidbody.rotation=player.transform.rotation;
            var rivals=FindObjectsByType<RivalKombiAI>();
            for(int i=0;i<rivals.Length;i++)
            {
                int index=Mathf.Min(path.Count-1,Mathf.Max(1,4+i*3));var point=graph.LanePoint(path,index);
                Vector3 forward=points[Mathf.Min(index+1,points.Length-1)].position-points[Mathf.Max(0,index-1)].position;forward.y=0;
                rivals[i].transform.SetPositionAndRotation(point+Vector3.up*.14f,Quaternion.LookRotation(forward));
                rivals[i].Configure(points,stops.Select(x=>x.GetComponent<PassengerStation>()).ToArray(),Configuration.RivalDelay,Configuration.RivalSpeed);
            }
            CityTripSelection.Distance=plan.Distance;
            SelectedDuration=Configuration.TripDuration(plan.Distance,stops.Count);
            Active=true;Physics.SyncTransforms();
        }
        static Vector3 Ground(Vector3 p)
        {
            var hits=Physics.RaycastAll(p+Vector3.up*1.5f,Vector3.down,5f,~0,QueryTriggerInteraction.Ignore);float y=float.NegativeInfinity;
            foreach(var hit in hits)
                if(hit.rigidbody==null&&hit.normal.y>.7f&&Mathf.Abs(hit.point.y-p.y)<1.5f&&hit.collider.GetComponentInParent<PassengerStation>()==null&&hit.collider.GetComponentInParent<CityIntersection>()==null)
                    y=Mathf.Max(y,hit.point.y+.025f);
            if(!float.IsNegativeInfinity(y))p.y=y;return p;
        }
        void OnDestroy(){Active=false;SelectedDuration=0;Plan=null;Configuration=null;}
    }
}
