using UnityEngine;

namespace KombiShift2.Vehicle
{
    public sealed class KombiPassengerCabin : MonoBehaviour
    {
        [SerializeField] private Transform doorway;
        [SerializeField] private Transform[] seats;
        public Transform Doorway => doorway;
        public Transform[] Seats => seats;
        public void Configure(Transform entry, Transform[] passengerSeats) { doorway = entry; seats = passengerSeats; }
    }
}
