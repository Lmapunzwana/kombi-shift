using System;
using System.Collections;
using System.Collections.Generic;
using KombiShift2.Vehicle;
using UnityEngine;

namespace KombiShift2.Gameplay
{
    // Queue/character movement stays in PassengerStation and StationPassenger. This coordinates
    // player service; the director still owns progression, manifest data and public events.
    public sealed class KombiPassengerService
    {
        readonly KombiShiftGameDirector director;
        readonly KombiPassengerCabin cabin;
        readonly List<StationPassenger> onboard;
        readonly List<PassengerTrip> trips;
        readonly Func<int, int, float> fare;
        readonly Action<PassengerTrip> changed, delivered;
        public bool Busy { get; private set; }
        public bool Alighting { get; private set; }

        public KombiPassengerService(KombiShiftGameDirector director, KombiPassengerCabin cabin,
            List<StationPassenger> onboard, List<PassengerTrip> trips, Func<int,int,float> fare,
            Action<PassengerTrip> changed, Action<PassengerTrip> delivered)
        {
            this.director = director; this.cabin = cabin; this.onboard = onboard;
            this.trips = trips; this.fare = fare; this.changed = changed; this.delivered = delivered;
        }

        public IEnumerator Board(PassengerStation station)
        {
            if (Busy || cabin == null || !CanBoard(station)) yield break;
            Busy = true;
            try
            {
                yield return station.Board(director.Vehicle.transform, cabin.Doorway, cabin.Seats, onboard.Count,
                    person =>
                    {
                        if (onboard.Contains(person)) return;
                        onboard.Add(person);
                        int origin = Array.IndexOf(director.Stations, station);
                        int destination = (trips.Count + 1) % 2 == 0 ? director.StopCount - 1 : Mathf.Min(origin + 1, director.StopCount - 1);
                        while (destination < director.StopCount - 1 && director.Stations[destination] == null) destination++;
                        var trip = new PassengerTrip { fare = fare(origin,destination), passenger = person,
                            boardingStation = origin, destination = destination, boardedAt = Time.time };
                        trips.Add(trip); changed(trip);
                    }, () => CanBoard(station, 2f));
            }
            finally { Busy = false; }
            director.Announce(station.HasWaiting ? "Hold Space and stop fully to finish boarding." : "Passengers aboard. Continue to the next stop.");
        }

        bool CanBoard(PassengerStation station, float tolerance = 0f) => station != null && station.isActiveAndEnabled && !director.Finished &&
            director.Vehicle != null && director.ParkingBrakeHeld && director.Vehicle.absoluteSpeed < 2f &&
            station.MatchesApproach(director.Vehicle.transform) &&
            Vector3.Distance(director.Vehicle.transform.position, station.ServicePoint.position) < director.StoppingRadius + tolerance;

        public IEnumerator Alight(PassengerStation station, bool terminal)
        {
            if (Busy || cabin == null) yield break;
            Busy = Alighting = true;
            bool interrupted=false;
            try
            {
                var leaving = new List<PassengerTrip>();
                foreach (var trip in trips)
                    if (trip.state != TripState.Completed && trip.passenger != null &&
                        (terminal || trip.missed || director.Stations[trip.destination] == station)) leaving.Add(trip);
                leaving.Sort((a,b) => (a.passenger.transform.position - cabin.Doorway.position).sqrMagnitude.CompareTo(
                    (b.passenger.transform.position - cabin.Doorway.position).sqrMagnitude));
                int departure = 0;
                foreach (var trip in leaving)
                {
                    if (director.Finished || director.Vehicle.absoluteSpeed >= 2f) break;
                    var arrival = station.ClaimArrival(trip.passenger, out bool seated);
                    trip.state = TripState.Alighting;
                    trip.passenger.LeaveAt(arrival, seated, cabin.Doorway, departure++ * .55f);
                }
                foreach (var trip in leaving)
                {
                    if (trip.state != TripState.Alighting) continue;
                    var person = trip.passenger;
                    while (!director.Finished && person != null && person.State != PassengerState.Delivered) yield return null;
                    // A destroyed actor is not evidence of a completed delivery and never earns a fare.
                    if (director.Finished) break;
                    if (person == null) { interrupted=true; continue; }
                    trip.state = TripState.Completed;
                    trip.Adjust(trip.missed ? 0f : 5f);
                    onboard.Remove(person);
                    delivered(trip);
                }
            }
            finally { Busy = Alighting = false; }
            director.Announce(interrupted ? "Passenger service interrupted. Restart the shift." : "Drop-off complete. Continue the shift. T repairs at stops.");
        }
    }
}
