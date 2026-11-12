using System;
using UnityEngine;
using KombiShift2.Gameplay;
namespace KombiShift2.City
{
    [Serializable] public sealed class CityTripDefinition
    {
        public string id,origin,destination,description;
        public int originNode,destinationNode,difficulty=1;
        // Retained for old bake tools/catalogues. Actual demand comes from stop count and stage queues.
        public int expectedPassengers=12;
        public bool ValidFares => Finite(baseFare)&&Finite(perKilometre)&&Finite(peakMultiplier)&&Finite(minFare)&&Finite(maxFare)&&baseFare>=0&&perKilometre>=0&&peakMultiplier>0&&minFare>=0&&maxFare>=minFare;
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public float baseFare=1f,perKilometre=.3f,peakMultiplier=1.25f,minFare=1f,maxFare=5f;
        public int[] stopNodes;
        public int Demand(KombiStage stage)=>new KombiShiftConfiguration(stage).TripDemand(stopNodes?.Length??2);
        public float Fare(float metres,KombiStage stage)=>Mathf.Round(Mathf.Clamp((baseFare+Mathf.Max(0,metres)/1000f*perKilometre)*(stage==KombiStage.RushHour?peakMultiplier:1f),minFare,maxFare)*100f)/100f;
    }
    [Serializable] public sealed class CityTripCatalog
    {
        public CityTripDefinition[] trips;
        static CityTripCatalog cached;static string cachedText;
        public static CityTripCatalog Load()
        {
            var text=Resources.Load<TextAsset>("CityTrips");string content=text!=null?text.text:null;
            if(cached==null||cachedText!=content)
            {
                cachedText=content;
                try { cached=content!=null?JsonUtility.FromJson<CityTripCatalog>(content):null; }
                catch(ArgumentException error) { cached=null;Debug.LogError("Invalid city trip catalogue: "+error.Message); }
                cached??=new CityTripCatalog();
                cached.trips=Array.FindAll(cached.trips??Array.Empty<CityTripDefinition>(),trip=>trip!=null);
            }
            return cached;
        }
    }
    public static class CityTripSelection
    {
        public static CityTripDefinition Selected;
        public static float Distance;
        public static int DeliveryTarget=>Selected==null?0:new KombiShiftConfiguration(KombiChallenge.Selected).TripTarget(Selected.Demand(KombiChallenge.Selected));
    }
}
