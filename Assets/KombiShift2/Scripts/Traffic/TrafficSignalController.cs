using UnityEngine;

namespace KombiShift2.Traffic
{
    public sealed class TrafficSignalController : MonoBehaviour
    {
        public enum Signal { Red, Amber, Green }

        [SerializeField] private Renderer redLamp;
        [SerializeField] private Renderer amberLamp;
        [SerializeField] private Renderer greenLamp;
        [SerializeField, Min(1f)] private float greenSeconds = 12f;
        [SerializeField, Min(1f)] private float amberSeconds = 3f;
        [SerializeField, Min(1f)] private float redSeconds = 14f;
        [SerializeField] private bool startOnGreen;

        private Signal current;
        private float elapsed;
        public Signal Current => current;
        public bool MustStop => current != Signal.Green;

        private void Awake()
        {
            current = startOnGreen ? Signal.Green : Signal.Red;
            ApplyVisuals();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float duration = current switch
            {
                Signal.Green => greenSeconds,
                Signal.Amber => amberSeconds,
                _ => redSeconds
            };
            if (elapsed < duration) return;
            elapsed = 0f;
            current = current switch
            {
                Signal.Green => Signal.Amber,
                Signal.Amber => Signal.Red,
                _ => Signal.Green
            };
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            SetEmission(redLamp, current == Signal.Red, Color.red);
            SetEmission(amberLamp, current == Signal.Amber, new Color(1f, .45f, 0f));
            SetEmission(greenLamp, current == Signal.Green, Color.green);
        }

        private static void SetEmission(Renderer lamp, bool enabled, Color colour)
        {
            if (lamp == null) return;
            Material material = lamp.material;
            if (!material.HasProperty("_EmissionColor")) return;
            material.SetColor("_EmissionColor", enabled ? colour * 3f : Color.black);
            if (enabled) material.EnableKeyword("_EMISSION"); else material.DisableKeyword("_EMISSION");
        }
    }
}
