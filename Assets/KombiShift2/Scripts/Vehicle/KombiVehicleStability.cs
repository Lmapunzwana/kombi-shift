using UnityEngine;
namespace KombiShift2.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(20000)]
    public sealed class KombiVehicleStability : MonoBehaviour
    {
        WheelCollider[] wheels;
        RCCP_CarController controller;
        Rigidbody body;Vector3 safePosition;Quaternion safeRotation;float groundY,unsafeTime;
        readonly RaycastHit[] hits=new RaycastHit[64];
        public int Recoveries { get; private set; }
        void Awake()
        {
            controller=GetComponent<RCCP_CarController>();body=GetComponent<Rigidbody>();wheels=GetComponentsInChildren<WheelCollider>();
            ConfigureDriving();
            body.constraints &= ~(RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ);
            body.angularDamping=2f;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            safePosition=transform.position;safeRotation=Quaternion.Euler(0,transform.eulerAngles.y,0);groundY=transform.position.y-.1f;
        }
        void ConfigureDriving()
        {
            foreach(var axle in GetComponentsInChildren<RCCP_Axle>())
            {
                axle.autoAlignWheelColliders=false;
                axle.antirollForce=0f;
            }
            var aero=GetComponentInChildren<RCCP_AeroDynamics>();
            if(aero!=null){aero.autoReset=false;aero.dynamicCOM=false;var centre=aero.transform.Find("COM");if(centre!=null)centre.localPosition=new Vector3(0,.24f,.04f);}
            var assist=GetComponentInChildren<RCCP_Stability>();
            if(assist!=null){assist.steeringHelper=false;assist.tractionHelper=false;}
            var damage=GetComponentInChildren<RCCP_Damage>();
            if(damage!=null){damage.wheelDamage=false;damage.wheelDetachment=false;}
            body.centerOfMass=new Vector3(0,.24f,.04f);
        }
        void FixedUpdate()
        {
            if(body.isKinematic)return;
            // Allow real suspension pitch/roll. Add bounded assistance only near a rollover.
            float tilt=Vector3.Angle(transform.up,Vector3.up);
            if(tilt>25f && tilt<80f)
                body.AddTorque(Vector3.Cross(transform.up,Vector3.up)*Mathf.Min(4f,(tilt-25f)*.08f),ForceMode.Acceleration);
            // Apply parking torque directly so automatic brake-to-reverse logic is not triggered.
            if(controller!=null&&controller.handbrakeInput_V>.1f)
                foreach(var wheel in wheels)wheel.brakeTorque=Mathf.Max(wheel.brakeTorque,5000f);
            int contacts=0;foreach(var wheel in wheels)if(wheel.isGrounded)contacts++;
            if(contacts>=2)body.AddForce(Vector3.down*6f,ForceMode.Acceleration);
            Vector3 velocity=body.linearVelocity;Vector3 flat=Vector3.ProjectOnPlane(velocity,Vector3.up);
            float limit=Vector3.Dot(flat,transform.forward)<-.5f?4.7f:30.6f;
            if(flat.magnitude>limit)body.linearVelocity=flat.normalized*limit+Vector3.up*velocity.y;
            Vector3 origin=new(body.position.x,Mathf.Max(body.position.y,groundY)+4f,body.position.z);
            int count=Physics.RaycastNonAlloc(origin,Vector3.down,hits,12f,~0,QueryTriggerInteraction.Ignore);
            float best=float.MaxValue;float candidate=groundY;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.transform.IsChildOf(transform)||hit.rigidbody!=null||hit.normal.y<.65f)continue;
                float difference=Mathf.Abs(hit.point.y-groundY);
                if(difference<best){best=difference;candidate=hit.point.y;}
            }
            if(best<4f)groundY=candidate;
            bool unsafePose=body.position.y<groundY-.45f||Vector3.Dot(transform.up,Vector3.up)<.35f;
            unsafeTime=unsafePose?unsafeTime+Time.fixedDeltaTime:0;
            if(unsafeTime>2f)
            {
                body.position=new Vector3(safePosition.x,Mathf.Max(safePosition.y,groundY+.14f),safePosition.z);
                body.rotation=safeRotation;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;unsafeTime=0;Recoveries++;
            }
            else if(!unsafePose&&body.position.y-groundY<1.2f)
            {safePosition=body.position;safeRotation=Quaternion.Euler(0,transform.eulerAngles.y,0);}
        }
    }
}
