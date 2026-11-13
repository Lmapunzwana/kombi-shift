using System.Collections.Generic;
using UnityEngine;

namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        readonly List<string> trafficFineHistory = new List<string>();
        float trafficFinesTotal;

        public float TrafficFinesTotal => trafficFinesTotal;
        public int TrafficFineCount => trafficFineHistory.Count;
        public string LatestTrafficFine => trafficFineHistory.Count == 0
            ? "No fines this shift"
            : trafficFineHistory[trafficFineHistory.Count - 1];

        public void ResetTrafficFines()
        {
            trafficFineHistory.Clear();
            trafficFinesTotal = 0f;
        }

        public void RegisterTrafficFine(Collision hit)
        {
            if (!IsPlaying || hit == null || hit.rigidbody == null || hit.rigidbody == driver.Body)
                return;

            float impactSpeed = hit.relativeVelocity.magnitude;
            float fine = impactSpeed >= 15f ? 20f
                       : impactSpeed >= 10f ? 10f
                       : impactSpeed >= 6f ? 5f : 2f;

            const string offence = "Collision with another vehicle";
            trafficFinesTotal += fine;
            wallet = Mathf.Max(0f, wallet - fine);

            string entry = string.Format("{0:0}s  {1}  -  ${2:F2}", elapsed, offence, fine);
            trafficFineHistory.Add(entry);
            Notify(string.Format("TRAFFIC FINE: {0}  |  ${1:F2} deducted from cash.", offence, fine));
        }
    }
}
