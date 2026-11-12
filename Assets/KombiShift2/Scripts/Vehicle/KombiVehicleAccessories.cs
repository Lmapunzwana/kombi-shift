using KombiShift2.Weather;
using UnityEngine;

namespace KombiShift2.Vehicle
{
    public sealed class KombiVehicleAccessories : MonoBehaviour
    {
        [SerializeField] private Light[] headlights;
        [SerializeField] private Transform leftWiper;
        [SerializeField] private Transform rightWiper;
        [SerializeField] private KombiWeatherController weather;
        private bool lightsOn;
        private bool manualWipers;
        private float wiperPhase;

        public void Configure(Light[] lights, Transform left, Transform right, KombiWeatherController weatherController)
        {
            headlights = lights;
            leftWiper = left;
            rightWiper = right;
            weather = weatherController;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                lightsOn = !lightsOn;
                foreach (Light light in headlights) if (light != null) light.enabled = lightsOn;
            }
            if (Input.GetKeyDown(KeyCode.V)) manualWipers = !manualWipers;
            bool active = manualWipers;
            if (leftWiper == null || rightWiper == null) return;
            if (active) wiperPhase += Time.deltaTime * 1.15f;
            else wiperPhase = Mathf.MoveTowards(wiperPhase, Mathf.Floor(wiperPhase), Time.deltaTime * 2.5f);
            float cycle = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(wiperPhase, 1f));
            float sweep = Mathf.Lerp(-52f, 24f, cycle);
            leftWiper.localRotation = Quaternion.Euler(-11f, 0f, sweep);
            rightWiper.localRotation = Quaternion.Euler(-11f, 0f, sweep);
        }
    }
}
