using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KombiShift2.Weather
{
    public enum KombiWeather
    {
        Clear,
        Cloudy,
        LightRain,
        HeavyRain,
        Thunderstorm,
        MorningFog,
        Dusty
    }

    /// <summary>
    /// Asset-independent weather state and vehicle-surface integration. Imported
    /// particle and audio prefabs can be assigned later without changing gameplay code.
    /// </summary>
    public sealed class KombiWeatherController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private KombiWeather initialWeather = KombiWeather.Clear;
        [SerializeField, Min(0.1f)] private float transitionDuration = 8f;
        [SerializeField] private bool enableDebugWeatherKeys = true;

        [Header("Optional imported effects")]
        [SerializeField] private ParticleSystem rain;
        [SerializeField] private ParticleSystem fog;
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private AudioSource rainAudio;
        [SerializeField] private AudioSource windAudio;
        [SerializeField] private AudioSource thunderAudio;
        [SerializeField] private Light lightningFlash;

        [Header("Vehicle")]
        [SerializeField] private GameObject playerVehicle;

        private readonly Dictionary<Component, float> wheelGrip = new();
        private Light sun;
        private KombiWeather currentWeather;
        private WeatherValues current;
        private WeatherValues target;
        private float transitionProgress = 1f;
        private float nextLightningTime;

        public KombiWeather CurrentWeather => currentWeather;
        public float Wetness { get; private set; }

        private readonly struct WeatherValues
        {
            public readonly Color fogColor;
            public readonly float fogStart;
            public readonly float fogEnd;
            public readonly float sunIntensity;
            public readonly float ambientIntensity;
            public readonly float rainRate;
            public readonly float fogRate;
            public readonly float dustRate;
            public readonly float wetness;
            public readonly float grip;
            public readonly float wind;

            public WeatherValues(Color fogColor, float fogStart, float fogEnd, float sunIntensity,
                float ambientIntensity, float rainRate, float fogRate, float dustRate,
                float wetness, float grip, float wind)
            {
                this.fogColor = fogColor;
                this.fogStart = fogStart;
                this.fogEnd = fogEnd;
                this.sunIntensity = sunIntensity;
                this.ambientIntensity = ambientIntensity;
                this.rainRate = rainRate;
                this.fogRate = fogRate;
                this.dustRate = dustRate;
                this.wetness = wetness;
                this.grip = grip;
                this.wind = wind;
            }
        }

        public void SetPlayerVehicle(GameObject value)
        {
            playerVehicle = value;
            CacheWheelGrip();
        }

        private void Awake()
        {
            foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional && (sun == null || light.intensity > sun.intensity))
                    sun = light;
            }
            if (lightningFlash != null) lightningFlash.enabled = false;
            CacheWheelGrip();
            currentWeather = initialWeather;
            current = ValuesFor(initialWeather);
            target = current;
            Apply(current);
        }

        private void Update()
        {
            if (enableDebugWeatherKeys && Input.GetKeyDown(KeyCode.F5))
            {
                int count = Enum.GetValues(typeof(KombiWeather)).Length;
                SetWeather((KombiWeather)(((int)currentWeather + 1) % count));
            }

            if (transitionProgress < 1f)
            {
                transitionProgress = Mathf.Min(1f, transitionProgress + Time.deltaTime / transitionDuration);
                current = Lerp(current, target, Mathf.SmoothStep(0f, 1f, transitionProgress));
                Apply(current);
            }

            UpdateLightning();
        }

        public void SetWeather(KombiWeather weather)
        {
            currentWeather = weather;
            target = ValuesFor(weather);
            transitionProgress = 0f;
            nextLightningTime = Time.time + UnityEngine.Random.Range(4f, 10f);
            Debug.Log($"KOMBI_WEATHER Transitioning to {weather}.");
        }

        private void Apply(WeatherValues values)
        {
            RenderSettings.fog = values.fogEnd < 1500f;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = values.fogColor;
            RenderSettings.fogStartDistance = values.fogStart;
            RenderSettings.fogEndDistance = values.fogEnd;
            RenderSettings.ambientIntensity = values.ambientIntensity;
            if (sun != null) sun.intensity = values.sunIntensity;
            Wetness = values.wetness;

            SetParticleRate(rain, values.rainRate);
            SetParticleRate(fog, values.fogRate);
            SetParticleRate(dust, values.dustRate);
            SetAudio(rainAudio, Mathf.InverseLerp(0f, 1800f, values.rainRate));
            SetAudio(windAudio, values.wind);
            ApplyWheelGrip(values.grip);
        }

        private static void SetParticleRate(ParticleSystem system, float rate)
        {
            if (system == null) return;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
            if (rate > 0.01f && !system.isPlaying) system.Play();
            else if (rate <= 0.01f && system.isPlaying) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private static void SetAudio(AudioSource source, float volume)
        {
            if (source == null) return;
            source.volume = Mathf.Clamp01(volume);
            if (source.volume > 0.01f && !source.isPlaying) source.Play();
            else if (source.volume <= 0.01f && source.isPlaying) source.Stop();
        }

        private void UpdateLightning()
        {
            if (currentWeather != KombiWeather.Thunderstorm || Time.time < nextLightningTime) return;
            nextLightningTime = Time.time + UnityEngine.Random.Range(5f, 15f);
            if (lightningFlash != null)
            {
                lightningFlash.enabled = true;
                lightningFlash.intensity = UnityEngine.Random.Range(2.5f, 5f);
                Invoke(nameof(EndLightningFlash), 0.12f);
            }
            if (thunderAudio != null)
            {
                thunderAudio.pitch = UnityEngine.Random.Range(0.9f, 1.08f);
                thunderAudio.PlayDelayed(UnityEngine.Random.Range(0.4f, 1.8f));
            }
        }

        private void EndLightningFlash()
        {
            if (lightningFlash != null) lightningFlash.enabled = false;
        }

        private void CacheWheelGrip()
        {
            wheelGrip.Clear();
            if (playerVehicle == null) return;
            foreach (Component component in playerVehicle.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetType().Name != "RCCP_WheelCollider") continue;
                MemberInfo member = FindGripMember(component.GetType());
                if (TryReadFloat(component, member, out float value)) wheelGrip[component] = value;
            }
        }

        private void ApplyWheelGrip(float multiplier)
        {
            foreach (KeyValuePair<Component, float> entry in wheelGrip)
            {
                if (entry.Key == null) continue;
                WriteFloat(entry.Key, FindGripMember(entry.Key.GetType()), entry.Value * multiplier);
            }
        }

        private static MemberInfo FindGripMember(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return type.GetField("grip", flags) as MemberInfo ?? type.GetProperty("grip", flags);
        }

        private static bool TryReadFloat(object targetObject, MemberInfo member, out float value)
        {
            value = 1f;
            if (member is FieldInfo field && field.FieldType == typeof(float))
            {
                value = (float)field.GetValue(targetObject);
                return true;
            }
            if (member is PropertyInfo property && property.PropertyType == typeof(float) && property.CanRead)
            {
                value = (float)property.GetValue(targetObject);
                return true;
            }
            return false;
        }

        private static void WriteFloat(object targetObject, MemberInfo member, float value)
        {
            if (member is FieldInfo field && field.FieldType == typeof(float)) field.SetValue(targetObject, value);
            else if (member is PropertyInfo property && property.PropertyType == typeof(float) && property.CanWrite) property.SetValue(targetObject, value);
        }

        private static WeatherValues Lerp(WeatherValues from, WeatherValues to, float t) => new(
            Color.Lerp(from.fogColor, to.fogColor, t),
            Mathf.Lerp(from.fogStart, to.fogStart, t), Mathf.Lerp(from.fogEnd, to.fogEnd, t),
            Mathf.Lerp(from.sunIntensity, to.sunIntensity, t), Mathf.Lerp(from.ambientIntensity, to.ambientIntensity, t),
            Mathf.Lerp(from.rainRate, to.rainRate, t), Mathf.Lerp(from.fogRate, to.fogRate, t),
            Mathf.Lerp(from.dustRate, to.dustRate, t), Mathf.Lerp(from.wetness, to.wetness, t),
            Mathf.Lerp(from.grip, to.grip, t), Mathf.Lerp(from.wind, to.wind, t));

        private static WeatherValues ValuesFor(KombiWeather weather) => weather switch
        {
            KombiWeather.Cloudy => new(new Color(.55f, .59f, .63f), 180f, 700f, .72f, .82f, 0f, 2f, 0f, 0f, 1f, .25f),
            KombiWeather.LightRain => new(new Color(.48f, .53f, .57f), 90f, 480f, .55f, .68f, 650f, 5f, 0f, .55f, .88f, .4f),
            KombiWeather.HeavyRain => new(new Color(.40f, .45f, .50f), 45f, 280f, .38f, .55f, 1300f, 10f, 0f, .9f, .76f, .65f),
            KombiWeather.Thunderstorm => new(new Color(.30f, .34f, .39f), 25f, 190f, .22f, .42f, 1800f, 18f, 0f, 1f, .68f, 1f),
            KombiWeather.MorningFog => new(new Color(.68f, .71f, .72f), 8f, 120f, .48f, .72f, 0f, 24f, 0f, .15f, .94f, .1f),
            KombiWeather.Dusty => new(new Color(.69f, .57f, .42f), 30f, 240f, .85f, .74f, 0f, 2f, 38f, 0f, .84f, .8f),
            _ => new(new Color(.66f, .73f, .78f), 280f, 900f, 1.25f, 1.15f, 0f, 0f, 0f, 0f, 1f, .08f)
        };
    }
}
