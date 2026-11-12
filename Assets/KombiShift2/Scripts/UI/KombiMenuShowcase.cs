using UnityEngine;
namespace KombiShift2.UI
{
    public sealed class KombiMenuShowcase : MonoBehaviour
    {
        [SerializeField] Transform vehicle;
        [SerializeField] Camera showcaseCamera;
        float time;
        public void Configure(Transform model,Camera camera){vehicle=model;showcaseCamera=camera;}
        void LateUpdate()
        {
            if(vehicle==null||showcaseCamera==null)return;
            time+=Time.unscaledDeltaTime;
            float mouse=(Input.mousePosition.x/Mathf.Max(1f,Screen.width)-.5f);
            float yaw=146f+Mathf.Sin(time*.16f)*5f+mouse*2.5f;
            Vector3 focus=vehicle.position+Vector3.up*.85f;
            Vector3 position=focus+Quaternion.Euler(9f,yaw,0)*Vector3.back*7.3f;
            showcaseCamera.transform.position=Vector3.Lerp(showcaseCamera.transform.position,position,1f-Mathf.Exp(-Time.unscaledDeltaTime*2f));
            showcaseCamera.transform.LookAt(focus+Vector3.right*1.3f);
            showcaseCamera.fieldOfView=43f;
        }
    }
}
