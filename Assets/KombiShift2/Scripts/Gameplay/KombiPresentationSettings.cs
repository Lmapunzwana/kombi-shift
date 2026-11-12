using UnityEngine;
using UnityEngine.Rendering;

namespace KombiShift2.Gameplay
{
    [DefaultExecutionOrder(-10000)]
    public sealed class KombiPresentationSettings : MonoBehaviour
    {
        private void Awake() => Apply();
        public static void Apply()
        {
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)) light.shadows=LightShadows.None;
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            { renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false; }
        }
    }
}
