using UnityEngine;

namespace KombiShift2.Vehicle
{
    public sealed class KombiPassengerCabin : MonoBehaviour
    {
        public const int SeatCount = 12;
        public const float PassengerScale = .75f;
        // Three seats across, four rows. Scale the stylized avatars to the usable interior and clear the rear bodywork.
        public static Vector3 SeatLocalPosition(int index) => new Vector3((index % 3 - 1) * .38f, .35f, .55f - index / 3 * .6f);
        [SerializeField] private Transform doorway;
        [SerializeField] private Transform[] seats;
        public Transform Doorway => doorway;
        public Transform[] Seats => seats;
        public void Configure(Transform entry, Transform[] passengerSeats) { doorway = entry; seats = passengerSeats; }
    }
}
