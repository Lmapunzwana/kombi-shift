using System;
using System.Linq;
using KombiShift2.City;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace KombiShift2.Development
{
    // Explicit native-build smoke flag only; normal menu/gameplay startup is unchanged.
    public sealed class CityBuildSmokeBoot : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kombi-smoke")>=0)
                new GameObject("Native city smoke entry",typeof(CityBuildSmokeBoot));
        }
        void Start()
        {CityTripSelection.Selected=CityTripCatalog.Load().trips.FirstOrDefault(t=>t.id=="mbare");KombiShift2.Gameplay.KombiShiftGameDirector.LoadRequestedAt=Time.realtimeSinceStartup;SceneManager.LoadScene("KombiShift_FirstShift");}
    }
}
