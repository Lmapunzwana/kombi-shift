using UnityEngine;
using KombiShift2.Gameplay;

namespace KombiShift2.CameraSystem
{
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(Camera))]
    public sealed class KombiDriveCamera : MonoBehaviour
    {
        [SerializeField] private Transform vehicle;
        [SerializeField] private Vector3 chaseOffset = new(0f,3.2f,-7.2f);
        [SerializeField] private Vector3 cabOffset = new(-.262f,1.18f,1.193f);
        [SerializeField] private float positionDamping = 9f;
        private readonly RaycastHit[] hits = new RaycastHit[96];
        private bool cabView;
        private float orbitYaw, pitch = 19f, distance = 7.6f, cabYaw, cabPitch;
        private float lastHeading, groundY;
        private GUIStyle button, hint;
        private KombiShiftGameDirector director;
        private Renderer[] shelterParts;
        public bool CabinView => cabView;
        public Transform Vehicle => vehicle;
        public float OrbitYaw => orbitYaw;
        public float Distance => distance;
        public Vector3 FocusPoint => vehicle != null ? vehicle.position + Vector3.up*1.05f : transform.position;
        public void SetVehicle(Transform value) { vehicle=value;ResetView(); }
        void Awake() { shelterParts=System.Array.FindAll(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None), r=>r.name.StartsWith("Shelter ")); OwnGameView();director=Object.FindFirstObjectByType<KombiShiftGameDirector>();distance=PlayerPrefs.GetFloat("Kombi.CameraDistance",7.6f);ResetView(); }
        public void ResetView()
        {
            if(vehicle==null)return;
            cabView=false;orbitYaw=cabYaw=cabPitch=0f;pitch=19f;
            groundY=vehicle.position.y-.08f;lastHeading=vehicle.eulerAngles.y;
            UpdateCamera(true);
        }
        public void CycleView() { cabView=!cabView;cabYaw=cabPitch=0f;UpdateCamera(true); }
        public void Orbit(float yaw,float vertical)
        {
            if(cabView){cabYaw=Mathf.Clamp(cabYaw+yaw,-85f,85f);cabPitch=Mathf.Clamp(cabPitch+vertical,-35f,40f);}
            else{orbitYaw=Mathf.Repeat(orbitYaw+yaw+180f,360f)-180f;pitch=Mathf.Clamp(pitch+vertical,10f,58f);}
        }
        public void Zoom(float amount) { distance=Mathf.Clamp(distance+amount,4.5f,12f); }
        void LateUpdate()
        {
            if(vehicle==null)return;
            OwnGameView();
            if(Input.GetKeyDown(KeyCode.C))CycleView();
            float sensitivity=PlayerPrefs.GetFloat("Kombi.CameraSensitivity",1.6f);
            if(Input.GetMouseButton(1))Orbit(Input.GetAxis("Mouse X")*sensitivity*2f,-Input.GetAxis("Mouse Y")*sensitivity*1.6f);
            if(!cabView)Zoom(-Input.mouseScrollDelta.y*.7f);
            UpdateCamera(false);
        }
        public void OwnGameView()
        {
            var own=GetComponent<Camera>();own.enabled=true;own.depth=100;own.targetDisplay=0;gameObject.tag="MainCamera";
            foreach(var other in Camera.allCameras)
            {
                if(other==own||other.cameraType!=CameraType.Game||other.targetTexture!=null)continue;
                other.enabled=false;
                var listener=other.GetComponent<AudioListener>();if(listener!=null)listener.enabled=false;
            }
        }
        public void UpdateCamera(bool snap)
        {
            if(vehicle==null)return;
            Vector3 forward=Vector3.ProjectOnPlane(vehicle.forward,Vector3.up);
            if(forward.sqrMagnitude>.02f)lastHeading=Quaternion.LookRotation(forward,Vector3.up).eulerAngles.y;
            Quaternion heading=Quaternion.Euler(0,lastHeading,0);
            Vector3 focus=FocusPoint;
            GroundAt(vehicle.position);
            focus.y=Mathf.Max(focus.y,groundY+1f);
            Vector3 wanted;
            Quaternion rotation;
            if(cabView)
            {
                wanted=vehicle.position+heading*cabOffset;
                wanted.y=Mathf.Max(wanted.y,groundY+.8f);
                rotation=Quaternion.Euler(cabPitch,lastHeading+cabYaw,0);
            }
            else
            {
                Quaternion orbit=Quaternion.Euler(pitch,lastHeading+orbitYaw,0);
                wanted=focus+orbit*Vector3.back*distance;
                Vector3 delta=wanted-focus;
                int count=Physics.SphereCastNonAlloc(focus,.24f,delta.normalized,hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
                float allowed=delta.magnitude;
                for(int i=0;i<count;i++)
                {
                    if(hits[i].transform.IsChildOf(vehicle)||hits[i].transform.name.StartsWith("Shelter "))continue;
                    if(hits[i].distance>.4f)allowed=Mathf.Min(allowed,Mathf.Max(1.1f,hits[i].distance-.3f));
                }
                wanted=focus+delta.normalized*allowed;
                wanted.y=Mathf.Max(wanted.y,groundY+.75f);
                rotation=Quaternion.LookRotation(focus-wanted,Vector3.up);
            }
            bool invalid=transform.position.y<groundY+.4f||Vector3.Distance(transform.position,wanted)>18f;
            float t=snap||invalid?1f:1f-Mathf.Exp(-positionDamping*Time.unscaledDeltaTime);
            Vector3 position=Vector3.Lerp(transform.position,wanted,t);position.y=Mathf.Max(position.y,groundY+.6f);
            transform.SetPositionAndRotation(position,Quaternion.Slerp(transform.rotation,rotation,t));
            // Hide only shelter pieces crossing the view, including roofs without colliders.
            // This keeps a parked kombi visible without pulling the camera inside it.
            if(shelterParts!=null)
            {
                Vector3 sight=focus-transform.position;var ray=new Ray(transform.position,sight.normalized);
                foreach(var part in shelterParts)
                {
                    if(part==null)continue;
                    var bounds=part.bounds;bounds.Expand(.35f);
                    part.forceRenderingOff=!cabView&&bounds.IntersectRay(ray,out float along)&&along<sight.magnitude;
                }
            }
            var camera=GetComponent<Camera>();camera.fieldOfView=cabView?70f:61f;camera.nearClipPlane=cabView?.035f:.12f;
        }
        void OnDisable() { if(shelterParts!=null)foreach(var part in shelterParts)if(part!=null)part.forceRenderingOff=false; }
        void GroundAt(Vector3 point)
        {
            Vector3 origin=new(point.x,Mathf.Max(point.y,groundY)+4f,point.z);
            int count=Physics.RaycastNonAlloc(origin,Vector3.down,hits,14f,~0,QueryTriggerInteraction.Ignore);
            float best=float.MaxValue;float y=groundY;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.transform.IsChildOf(vehicle)||hit.normal.y<.65f||hit.rigidbody!=null)continue;
                float difference=Mathf.Abs(hit.point.y-groundY);
                if(difference<best){best=difference;y=hit.point.y;}
            }
            if(best<5f)groundY=y;
        }
        void OnGUI()
        {
            if(vehicle==null)return;
            if(director!=null&&(director.Paused||director.Finished))return;
            if(button==null){button=new GUIStyle(GUI.skin.button){fontSize=14,fontStyle=FontStyle.Bold};hint=new GUIStyle(GUI.skin.label){fontSize=13,alignment=TextAnchor.MiddleCenter};hint.normal.textColor=new Color(.85f,.9f,.92f);}
            Rect safe=Screen.safeArea;float scale=Mathf.Min(1f,safe.width/900f,safe.height/720f);var old=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(new Vector3(safe.x,Screen.height-safe.yMax),Quaternion.identity,Vector3.one*scale);
            float w=safe.width/scale,h=safe.height/scale;
            if(GUI.Button(new Rect(w-146,h-144,128,32),cabView?"CHASE VIEW  C":"DRIVER VIEW  C",button))CycleView();
            GUI.Label(new Rect(w*.5f-210,h-54,420,32),"RMB DRAG  Look around    WHEEL  Zoom    C  Camera",hint);
            GUI.matrix=old;
        }
    }
}
