using System;
using System.Collections;
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
            if (!HasWaiting || (reservedBy != null && reservedBy != owner)) return false;
            Vector3 delta = owner.position - ServicePoint.position; delta.y = 0f;
            if (delta.sqrMagnitude > 144f) return false;
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
