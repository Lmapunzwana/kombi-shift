using UnityEditor;
using UnityEngine;

public static class KombiPassengerPreview
{
    [MenuItem("Kombi Shift/Inspect Passenger Station")]
    public static void Open()
    {
        KombiProjectWorkbench.OpenShift();
        EditorApplication.delayCall += () =>
        {
            var stations=GameObject.Find("Passenger Stations");
            if(stations==null||stations.transform.childCount==0)return;
            var station=stations.transform.GetChild(0);
            Selection.activeGameObject=station.gameObject;
            Vector3 focus=station.position+Vector3.up*.9f;
            Vector3 eye=station.position+station.forward*8f+station.right*4f+Vector3.up*3.2f;
            var view=EditorWindow.GetWindow<SceneView>();
            view.LookAt(focus,Quaternion.LookRotation(focus-eye),6f,false,true);view.Focus();
            Debug.Log("KOMBI_PASSENGER_PREVIEW_READY: first station framed in Scene view.");
        };
    }
}
