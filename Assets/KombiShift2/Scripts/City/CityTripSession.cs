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
        void Awake()
        {
            Active=false;SelectedDuration=0;
            var selected=CityTripSelection.Selected;if(selected==null)return;
            var graph=GetComponent<CityRoadNetwork>();graph.Load();
            var path=graph.FindTripPath(selected);
            if(path.Count<2 || stationPrefab==null){Debug.LogError("Selected city trip is not reachable; keeping the existing shift.");CityTripSelection.Selected=null;return;}
            var director=FindFirstObjectByType<KombiShiftGameDirector>();var player=director.Vehicle;
            foreach(var station in FindObjectsByType<PassengerStation>())
            {station.ServicePoint.parent.gameObject.SetActive(false);if(station.transform!=station.ServicePoint.parent)station.gameObject.SetActive(false);}
            var root=new GameObject("Selected trip — "+selected.destination).transform;root.SetParent(transform,false);
            var points=new Transform[path.Count];
            for(int i=0;i<points.Length;i++){points[i]=new GameObject("Route "+i).transform;points[i].SetParent(root,false);points[i].position=graph.Nodes[path[i]].position;}
            var stops=new List<Transform>();
            for(int i=0;i<selected.stopNodes.Length;i++)
            {
                int node=selected.stopNodes[i],index=path.IndexOf(node);if(index<0)continue;
                Vector3 forward=graph.Nodes[path[Mathf.Min(path.Count-1,index+1)]].position-graph.Nodes[path[Mathf.Max(0,index-1)]].position;forward.y=0;forward.Normalize();
                Vector3 left=-Vector3.Cross(Vector3.up,forward);var position=graph.Nodes[node].position;
                var go=Instantiate(stationPrefab,root);go.name=i==0?selected.origin+" Rank":i==selected.stopNodes.Length-1?selected.destination+" Terminal":selected.destination+" Stop "+i;
                go.transform.SetPositionAndRotation(Ground(position+left*(graph.Nodes[node].width*.5f+1.5f)),Quaternion.LookRotation(-left));
                var station=go.GetComponent<PassengerStation>();var service=station.ServicePoint;
                service.SetPositionAndRotation(Ground(graph.LanePoint(path,index)),Quaternion.LookRotation(forward));
                var people=station.People;
                bool terminal=i==selected.stopNodes.Length-1;
                if(!terminal&&KombiChallenge.Selected==KombiStage.RushHour)
                {
                    var demand=new List<StationPassenger>(people);
                    // Rush hour: every bench place is taken.
                    for(int n=people.Length;n<Mathf.Min(12,station.ArrivalSeats.Length);n++)
                    {
                        var seat=station.ArrivalSeats[n];var actor=Instantiate(people[n%people.Length].gameObject,seat,false).GetComponent<StationPassenger>();
                        actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;actor.SetWaitingSeat(seat);actor.PoseSeatedPreview();demand.Add(actor);
                    }
                    people=demand.ToArray();
                }
                if(terminal){foreach(var person in people)Destroy(person.gameObject);people=Array.Empty<StationPassenger>();}
                station.Configure(people,service,station.ArrivalSeats,terminal);
                foreach(var text in go.GetComponentsInChildren<TextMesh>())text.text=go.name.ToUpperInvariant();
                stops.Add(station.transform);
            }
            if(stops.Count<2)throw new InvalidOperationException("Selected trip needs at least two valid stops.");
            director.Configure(player,stops.ToArray(),4f);director.SetRoutePath(points);FindFirstObjectByType<KombiNavigationHud>().Configure(player,points,director);
            player.transform.SetPositionAndRotation(stops[0].GetComponent<PassengerStation>().ServicePoint.position+Vector3.up*.14f,stops[0].GetComponent<PassengerStation>().ServicePoint.rotation);
            var rigidbody=player.GetComponent<Rigidbody>();rigidbody.position=player.transform.position;rigidbody.rotation=player.transform.rotation;
            var rivals=FindObjectsByType<RivalKombiAI>();
            for(int i=0;i<rivals.Length;i++)
            {
                int index=Mathf.Min(path.Count-1,Mathf.Max(1,4+i*3));var point=graph.LanePoint(path,index);
                Vector3 forward=points[Mathf.Min(index+1,points.Length-1)].position-points[Mathf.Max(0,index-1)].position;forward.y=0;
                rivals[i].transform.SetPositionAndRotation(point+Vector3.up*.14f,Quaternion.LookRotation(forward));
                rivals[i].Configure(points,stops.Select(x=>x.GetComponent<PassengerStation>()).ToArray(),KombiChallenge.RivalDelay(KombiChallenge.Selected),KombiChallenge.Selected==KombiStage.RushHour?10.5f:7.5f);
            }
            CityTripSelection.Distance=graph.Length(path);
            SelectedDuration=Mathf.Max(KombiChallenge.Seconds(KombiChallenge.Selected),graph.Length(path)/(KombiChallenge.Selected==KombiStage.RushHour?7f:6f)+stops.Count*40f+150f);
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
        void OnDestroy(){Active=false;}
    }
}
