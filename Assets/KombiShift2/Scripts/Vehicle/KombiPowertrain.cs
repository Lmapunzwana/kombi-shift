using UnityEngine;

namespace KombiShift2.Vehicle
{
    // Apply after RCC player registration, which can otherwise restore its sport preset.
    public sealed class KombiPowertrain : MonoBehaviour
    {
        public const float GovernorStartKph = 95f;
        public const float GovernorEndKph = 105f;
        public void Configure()
        {
            var engine = GetComponentInChildren<RCCP_Engine>();
            engine.minEngineRPM = 750f;
            engine.maxEngineRPM = 5000f;
            engine.peakRPM = 2800f;
            engine.maximumTorqueAsNM = 260f;
            // This value sets RCC gearing, not the road governor. Taller gearing keeps cruising revs down.
            engine.maximumSpeed = 160f;
            var gearbox = GetComponentInChildren<RCCP_Gearbox>();
            gearbox.shiftUpRPM = 3200f;
            gearbox.shiftDownRPM = 1500f;
            gearbox.shiftThreshold = .65f;
            for(int i=0;i<gearbox.gearRPMs.Length&&i<gearbox.gearRatios.Length;i++)
                gearbox.gearRPMs[i] = engine.maxEngineRPM / gearbox.gearRatios[i];
            gearbox.shiftingTime = .28f;
            gearbox.automaticTransmission = true;
        }
        public static float ThrottleFactor(float speed, bool reversing)
        {
            float start = reversing ? 12f : GovernorStartKph;
            float end = reversing ? 16f : GovernorEndKph;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, speed));
        }
    }
}
