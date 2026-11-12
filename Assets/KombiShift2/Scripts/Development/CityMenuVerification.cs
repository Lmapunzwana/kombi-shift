#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.City;
using KombiShift2.Gameplay;
using KombiShift2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public sealed class CityMenuVerification : MonoBehaviour
{
    [Serializable] class Report{public List<string> passed=new(),failed=new();}
    static CityMenuVerification owner;Report report=new();bool done;float began;
    public static void Run(){CityTripSelection.Selected=null;EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiMainMenu.unity");EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;new GameObject("City market UI verification",typeof(CityMenuVerification));EditorApplication.isPlaying=true;EditorApplication.ExecuteMenuItem("Window/General/Game");}
    void Awake(){if(owner!=null&&owner!=this){Destroy(gameObject);return;}owner=this;}
    void Start(){if(owner!=this)return;DontDestroyOnLoad(gameObject);began=Time.realtimeSinceStartup;StartCoroutine(Scenario());}
    void Update(){if(!done&&Time.realtimeSinceStartup-began>100){Check(false,"Trip Market scenario timed out");Finish();}}
    void Check(bool value,string name){(value?report.passed:report.failed).Add(name);Debug.Log("KOMBI_CITY_MENU "+(value?"PASS ":"FAIL ")+name);}
    IEnumerator Scenario()
    {
        yield return null;yield return null;
        var menu=FindFirstObjectByType<KombiMainMenu>();menu.Activate(0);yield return null;
        var market=FindFirstObjectByType<KombiTripMarket>();Check(market!=null&&market.IsOpen,"Main menu opens the actual Trip Market");
        foreach(var trip in CityTripCatalog.Load().trips)Check(market.SelectTrip(trip.id),"Market validates and previews "+trip.destination);
        market.SelectTrip("borrowdale");KombiChallenge.Selected=KombiStage.RushHour;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/Validation/CityInfrastructure/trip-market.png"));yield return new WaitForSecondsRealtime(.5f);
        bool accepted=market.StartSelected();Check(accepted,"Start Trip accepts the selected reachable itinerary");if(!accepted){Finish();yield break;}
        float until=Time.realtimeSinceStartup+45;
        while(FindFirstObjectByType<KombiShiftGameDirector>()==null&&Time.realtimeSinceStartup<until)yield return null;
        yield return null;yield return null;
        var director=FindFirstObjectByType<KombiShiftGameDirector>();
        Check(director!=null&&CityTripSession.Active,"Trip Market start loads actual city gameplay");
        if(director!=null)
        {
            Check(director.Stations.Last().name.Contains("Borrowdale"),"Selected Borrowdale terminal is instantiated");
            Check(director.Stage==KombiStage.RushHour&&director.Stations[0].WaitingCount==8,"Market-selected Rush Hour initialises eight-person demand");
            Check(Vector3.Distance(director.Vehicle.transform.position,director.Stations[0].ServicePoint.position)<2,"Player starts at the selected reachable origin");
        }
        yield return new WaitForSecondsRealtime(3f);ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/Validation/CityInfrastructure/city-gameplay-ui.png"));yield return new WaitForSecondsRealtime(.5f);
        Finish();
    }
    void Finish(){if(done)return;done=true;File.WriteAllText("Docs/Validation/CityInfrastructure/trip-market-results.json",JsonUtility.ToJson(report,true));Debug.Log("KOMBI_CITY_MENU_RESULT "+(report.failed.Count==0?"PASS":"FAIL"));EditorApplication.Exit(report.failed.Count==0?0:1);}
    void OnDestroy(){if(owner==this)owner=null;}
}
#endif
