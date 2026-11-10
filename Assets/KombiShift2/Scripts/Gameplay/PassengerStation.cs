using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KombiShift2.Gameplay
{
    public sealed class PassengerStation : MonoBehaviour
    {
        [SerializeField] private StationPassenger[] people;
        [SerializeField] private Transform servicePoint;
        [SerializeField] private Transform[] arrivalSeats;
        [SerializeField] private bool terminal;
        private Transform reservedBy;
        private bool groupBoarding;
        private readonly Dictionary<Transform, StationPassenger> arriving = new();
        private int standingArrivals;
        public bool MatchesApproach(Transform vehicle)
        {
            Vector3 forward=ServicePoint.forward; forward.y=0f;
            Vector3 heading=vehicle.forward; heading.y=0f;
            float lateral=Vector3.Dot(vehicle.position-ServicePoint.position,ServicePoint.right);
            return Vector3.Dot(heading.normalized,forward.normalized)>.5f && Mathf.Abs(lateral)<1.2f;
        }
        public Transform ClaimArrival(StationPassenger person, out bool seated)
        {
            foreach(var seat in arrivalSeats ?? Array.Empty<Transform>())
            {
                if(seat==null || seat.childCount>0)continue;
                if(arriving.TryGetValue(seat,out var owner) && owner!=null && owner!=person)continue;
                if(people!=null && people.Any(x=>x!=null && !x.Collected && x.WaitingSeat==seat))continue;
                arriving[seat]=person; seated=true; return seat;
            }
            // Overflow passengers stand on the pavement. Never seat a person at an unsupported marker.
            var exit=new GameObject("Standing arrival "+standingArrivals).transform;
            exit.SetParent(ServicePoint.parent!=null?ServicePoint.parent:transform,true);
            exit.position=ServicePoint.position-ServicePoint.right*5f+ServicePoint.forward*(standingArrivals++*.8f+3f);
            exit.rotation=ServicePoint.rotation;
            var hits=Physics.RaycastAll(exit.position+Vector3.up*1.5f,Vector3.down,4f,~0,QueryTriggerInteraction.Ignore);
            float floor=float.NegativeInfinity;
            foreach(var hit in hits)
                if(hit.rigidbody==null && hit.normal.y>.7f && !hit.transform.IsChildOf(exit.parent) && hit.point.y>floor)floor=hit.point.y;
            if(!float.IsNegativeInfinity(floor))exit.position=new Vector3(exit.position.x,floor+.025f,exit.position.z);
            seated=false;return exit;
        }
        public bool Terminal => terminal;
        public Transform ServicePoint => servicePoint != null ? servicePoint : transform;
        public int WaitingCount => people == null ? 0 : people.Count(x => x != null && !x.Collected);
        public bool Reserved => reservedBy != null;
        public bool HasWaiting => !terminal && WaitingCount > 0;
        public StationPassenger[] People => people;
        public Transform[] ArrivalSeats => arrivalSeats;
        public void Configure(StationPassenger[] passengers, Transform point, Transform[] seats, bool isTerminal)
        { people = passengers; servicePoint = point; arrivalSeats = seats; terminal = isTerminal; }

        public bool TryReserve(Transform owner)
        {
            if (owner == null || !HasWaiting || (reservedBy != null && reservedBy != owner)) return false;
            Vector3 delta = owner.position - ServicePoint.position; delta.y = 0f;
            if (delta.sqrMagnitude > 144f || !MatchesApproach(owner)) return false;
            reservedBy = owner; return true;
        }

        public IEnumerator Board(Transform owner, Transform door, Transform[] seats, int occupied, Action<StationPassenger> collected, Func<bool> mayContinue)
        {
            if (groupBoarding || !TryReserve(owner)) yield break;
            groupBoarding = true;
            int moving = 0;
            occupied = 0;
            try
            {
                foreach (var person in people)
                {
                    if (!mayContinue()) break;
                    if (person == null || person.Collected) continue;
                    while (occupied < seats.Length && seats[occupied].childCount > 0) occupied++;
                    if (occupied >= seats.Length) break;
                    Transform seat = seats[occupied++];
                    moving++;
                    StartCoroutine(BoardTogether(person,door,seat,collected,mayContinue,()=>moving--));
                }
                while (moving > 0) yield return null;
            }
            finally
            {
                groupBoarding = false;
                if (reservedBy == owner) reservedBy = null;
            }
        }

        private IEnumerator BoardTogether(StationPassenger person,Transform door,Transform seat,Action<StationPassenger> collected,Func<bool> mayContinue,Action completed)
        {
            try
            {
                yield return person.Board(door,seat,mayContinue);
                if (person.Boarded) collected?.Invoke(person);
            }
            finally { completed(); }
        }

        public void Release(Transform owner) { if (reservedBy == owner) reservedBy = null; }
    }
}
