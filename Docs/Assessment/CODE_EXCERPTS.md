# Current C# excerpt references

Examples for panel review; imported RCC is not claimed as group-authored physics. Learn what each line does and record actual review in CONTRIBUTIONS.csv.

## Two stage definitions

`Assets/KombiShift2/Scripts/Gameplay/KombiChallenge.cs`, line 11.

```csharp
        public static string Name(KombiStage stage) => stage == KombiStage.Morning ? "MORNING SHIFT" : "RUSH HOUR";
        public static int MinimumPassengers(KombiStage stage) => stage == KombiStage.Morning ? 1 : 6;
        public static float Seconds(KombiStage stage) => stage == KombiStage.Morning ? 600f : 420f;
        public static float RivalSpeed(KombiStage stage) => stage == KombiStage.Morning ? 6f : 9f;
        public static float RivalDelay(KombiStage stage) => stage == KombiStage.Morning ? 45f : 8f;
        public static int Best(KombiStage stage) => PlayerPrefs.GetInt("KombiShift2.Best." + stage, 0);
        public static void SaveBest(KombiStage stage, int earnings)
        {
            PlayerPrefs.SetInt("KombiShift2.Best." + stage, Mathf.Max(Best(stage), earnings));
            PlayerPrefs.Save();
        }
    }
}
```

## Boarding income and callback

`Assets/KombiShift2/Scripts/Gameplay/KombiShiftGameDirector.cs`, line 106.

```csharp
            yield return station.Board(vehicle.transform, cabin.Doorway, cabin.Seats, onboard.Count,
                person => { onboard.Add(person); passengers++; earnings += 2; OnFareEarned?.Invoke(2); },
                () => !finished && vehicle != null && vehicle.absoluteSpeed < 2f && Vector3.Distance(vehicle.transform.position, station.ServicePoint.position) < stopRadius + 2f);
            servicing = false;
            notice = station.HasWaiting ? "Stop fully to finish boarding." : "Passengers aboard. Continue to the next stop.";
        }

        private IEnumerator DropOff(PassengerStation station)
        {
            servicing = true;
            if (station.ArrivalSeats == null || station.ArrivalSeats.Length == 0)
            { Debug.LogError("Terminal has no arrival seats."); servicing = false; Finish(false); yield break; }
            for (int i = 0; i < onboard.Count; i++)
```

## Exclusive reservation

`Assets/KombiShift2/Scripts/Gameplay/PassengerStation.cs`, line 26.

```csharp

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
```

## Decision logic

`Assets/KombiShift2/Scripts/Traffic/RivalKombiAI.cs`, line 80.

```csharp

        private void ChooseTarget()
        {
            target = null; float best = float.MaxValue;
            foreach (var station in stations)
            {
                if (station == null || !station.HasWaiting || station.Reserved) continue;
                int index = NearestRouteIndex(station.ServicePoint.position);
                float distance = Mathf.Abs(index - routeIndex);
                if (distance >= best) continue;
                best = distance; target = station; destinationIndex = index;
            }
        }
```

## Buffered collision query

`Assets/KombiShift2/Scripts/Traffic/KombiTrafficVehicle.cs`, line 74.

```csharp

        private bool ObstacleAhead(Vector3 direction)
        {
            int count = Physics.SphereCastNonAlloc(transform.position + Vector3.up * .8f, .55f, direction, obstacleHits, 6f);
            var hits = count == obstacleHits.Length ? Physics.SphereCastAll(transform.position + Vector3.up * .8f, .55f, direction, 6f) : obstacleHits;
            if (hits != obstacleHits) count = hits.Length;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform)) continue;
                if (hit.rigidbody != null || hit.collider.GetComponentInParent<KombiTrafficVehicle>() != null) return true;
            }
            return false;
```

