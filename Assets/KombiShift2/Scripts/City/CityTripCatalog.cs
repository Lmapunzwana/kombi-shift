using System;
using UnityEngine;
using KombiShift2.Gameplay;
namespace KombiShift2.City
{
    [Serializable] public sealed class CityTripDefinition
    {
        public string id,origin,destination,description;
        public int originNode,destinationNode,expectedPassengers=12,difficulty=1;
        public float baseFare=1f,perKilometre=.3f,peakMultiplier=1.25f,minFare=1f,maxFare=5f;
        public int[] stopNodes;
        public int Demand(KombiStage stage)=>Mathf.RoundToInt(expectedPassengers*(stage==KombiStage.RushHour?4f/3f:1f));
        public float Fare(float metres,KombiStage stage)=>Mathf.Round(Mathf.Clamp((baseFare+Mathf.Max(0,metres)/1000f*perKilometre)*(stage==KombiStage.RushHour?peakMultiplier:1f),minFare,maxFare)*100f)/100f;
    }
    [Serializable] public sealed class CityTripCatalog
    {
        public CityTripDefinition[] trips;
        static CityTripCatalog cached;static string cachedText;
        public static CityTripCatalog Load()
        {var text=Resources.Load<TextAsset>("CityTrips");string content=text!=null?text.text:null;if(cached==null||cachedText!=content){cachedText=content;cached=content!=null?JsonUtility.FromJson<CityTripCatalog>(content):new CityTripCatalog{trips=Array.Empty<CityTripDefinition>()};}return cached;}
    }
    public static class CityTripSelection
    {
        public static CityTripDefinition Selected;
        public static float Distance;
        public static int DeliveryTarget=>Selected==null?0:Mathf.Min(Selected.expectedPassengers,KombiChallenge.Selected==KombiStage.Morning?4:6);
    }
}
