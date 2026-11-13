using UnityEngine;
namespace KombiShift.JaydenStarter
{
    [RequireComponent(typeof(Rigidbody),typeof(BoxCollider))]
    public class VehicleController : MonoBehaviour
    {
        public ShiftGame game;
        public float acceleration=13f,maximumSpeed=20f,steering=92f;
        public Rigidbody Body{get;private set;}
        public float Speed=>Body==null?0:new Vector2(Body.linearVelocity.x,Body.linearVelocity.z).magnitude;
        public float SignedSpeed=>Body==null?0:Vector3.Dot(Body.linearVelocity,transform.forward);
        public bool IsParked{get;private set;}=true;
        const RigidbodyConstraints DrivingConstraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
        public float Throttle{get;private set;} public float Steer{get;private set;}public bool Grounded{get;private set;}
        protected virtual void Awake()
        {
            Body=GetComponent<Rigidbody>();Body.mass=1400;Body.constraints=DrivingConstraints;
            Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;Body.centerOfMass=new Vector3(0,.2f,0);
            // Default high-friction box contacts can cancel tiny per-step acceleration. Use controlled tyre grip instead.
            var surface=new PhysicsMaterial("Arcade tyre contact"){dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum};
            GetComponent<BoxCollider>().material=surface;contactMaterial=surface;ParkNow();
        }
        bool stopRequested,requireNeutral;
        PhysicsMaterial contactMaterial; readonly RaycastHit[] groundHits=new RaycastHit[16];
        protected virtual void Update(){Throttle=ShiftInput.Throttle;Steer=ShiftInput.Turn;if(ShiftInput.Brake)RequestStop();}
        protected virtual void FixedUpdate()
        {
            if(game==null||!game.IsPlaying)return;
            // Start above the complete body and exclude our own body from the results.
            Grounded=false;
            int count=Physics.RaycastNonAlloc(Body.position+Vector3.up*2.5f,Vector3.down,groundHits,4f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(groundHits[i].rigidbody!=Body&&groundHits[i].normal.y>.5f){Grounded=true;break;}
            if(!Grounded)return;
            float command=ShiftInput.Throttle;var wear=game.Wear;float brakeStrength=wear==null?1:wear.BrakeStrength,grip=wear==null?1:wear.GripStrength;
            if(game.DriveBlocked){ParkNow();return;}
            if(IsParked&&Mathf.Abs(command)<.05f)requireNeutral=false;
            if(IsParked&&Mathf.Abs(command)>.05f&&!ShiftInput.Brake&&!requireNeutral){if(!game.RequestDeparture())return;IsParked=false;stopRequested=false;Body.constraints=DrivingConstraints;Body.WakeUp();}
            if(IsParked){Body.linearVelocity=new Vector3(0,Body.linearVelocity.y,0);Body.angularVelocity=Vector3.zero;return;}
            Vector3 v=transform.InverseTransformDirection(Body.linearVelocity);
            v.x=Mathf.MoveTowards(v.x,0,(stopRequested||ShiftInput.Brake?35:(game.Rainy?3:12)*grip)*Time.fixedDeltaTime);
            // Direct input only: releasing a key cannot fall back to a stale GUI-held key.
            bool stopping=ShiftInput.Brake||stopRequested||command*v.z<-.1f;
            v.z=Mathf.MoveTowards(v.z,0,(stopping?38*brakeStrength:command==0?7f:0)*Time.fixedDeltaTime);
            if((stopping||Mathf.Abs(command)<.05f)&&new Vector2(v.x,v.z).magnitude<.16f){ParkNow();return;}
            if(stopping&&wear!=null)wear.Brake(Mathf.Abs(v.z),Time.fixedDeltaTime);
            if(!stopping)v.z+=command*acceleration*Mathf.Lerp(.6f,1,game.Condition/100)*Time.fixedDeltaTime;
            v.z=Mathf.Clamp(v.z,-7,maximumSpeed);Body.linearVelocity=transform.TransformDirection(v);Body.angularVelocity=Vector3.zero;
            float speedFactor=Mathf.Clamp(v.z/5,-1,1);float angle=ShiftInput.Turn*steering*speedFactor*Time.fixedDeltaTime;
            Body.MoveRotation(Body.rotation*Quaternion.Euler(0,angle,0));
        }
        public void RequestStop(){stopRequested=true;requireNeutral=Mathf.Abs(ShiftInput.Throttle)>.05f;}
        public void ParkNow()
        {
            if(Body==null)return;IsParked=true;Body.constraints=DrivingConstraints|RigidbodyConstraints.FreezePositionX|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotationY;
            Body.linearVelocity=new Vector3(0,Body.linearVelocity.y,0);Body.angularVelocity=Vector3.zero;
        }
        protected virtual void OnApplicationFocus(bool focused){if(!focused)ShiftInput.Clear();}
        float nextCollision;
        protected virtual void OnCollisionEnter(Collision hit)
        {
            if(game==null||!game.IsPlaying||Time.time<nextCollision||hit.contactCount==0||hit.relativeVelocity.magnitude<3)return;
            if(hit.GetContact(0).normal.y>.6f)return;
            nextCollision=Time.time+1.5f;game.Damage(Mathf.Min(12,hit.relativeVelocity.magnitude*.65f));game.RegisterTrafficFine(hit);
        }
        protected virtual void OnDestroy(){if(contactMaterial!=null)Destroy(contactMaterial);}
    }
}
