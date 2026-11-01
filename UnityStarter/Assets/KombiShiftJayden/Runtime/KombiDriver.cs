using UnityEngine;
namespace KombiShift.JaydenStarter
{
    [RequireComponent(typeof(Rigidbody),typeof(BoxCollider))]
    public sealed class KombiDriver : MonoBehaviour
    {
        public ShiftGame game;
        public float acceleration=13f,maximumSpeed=20f,steering=92f;
        public Rigidbody Body{get;private set;}
        public float Speed=>Body==null?0:new Vector2(Body.linearVelocity.x,Body.linearVelocity.z).magnitude;
        public float SignedSpeed=>Body==null?0:Vector3.Dot(Body.linearVelocity,transform.forward);
        public float Throttle{get;private set;} public float Steer{get;private set;}public bool Grounded{get;private set;}
        void Awake()
        {
            Body=GetComponent<Rigidbody>();Body.mass=1400;Body.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;Body.centerOfMass=new Vector3(0,.2f,0);
            // Default high-friction box contacts can cancel tiny per-step acceleration. Use controlled tyre grip instead.
            var surface=new PhysicsMaterial("Arcade tyre contact"){dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum};
            GetComponent<BoxCollider>().material=surface;contactMaterial=surface;
        }
        PhysicsMaterial contactMaterial; readonly RaycastHit[] groundHits=new RaycastHit[16];
        void Update(){Throttle=ShiftInput.Throttle;Steer=ShiftInput.Turn;}
        void FixedUpdate()
        {
            if(game==null||!game.IsPlaying)return;
            // Start above the complete body and exclude our own body from the results.
            Grounded=false;
            int count=Physics.RaycastNonAlloc(Body.position+Vector3.up*2.5f,Vector3.down,groundHits,4f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(groundHits[i].rigidbody!=Body&&groundHits[i].normal.y>.5f){Grounded=true;break;}
            if(!Grounded)return;
            Vector3 v=transform.InverseTransformDirection(Body.linearVelocity);
            v.x=Mathf.MoveTowards(v.x,0,(game.Rainy?3:12)*Time.fixedDeltaTime);
            float command=ShiftInput.Throttle; // Same provider used by physical keyboard, onscreen controls and regression tests.
            bool stopping=ShiftInput.Brake || command*v.z<-.1f;
            v.z=Mathf.MoveTowards(v.z,0,(stopping?25:command==0?2.2f:0)*Time.fixedDeltaTime);
            if(!ShiftInput.Brake)v.z+=command*acceleration*Mathf.Lerp(.6f,1,game.Condition/100)*Time.fixedDeltaTime;
            v.z=Mathf.Clamp(v.z,-7,maximumSpeed);Body.linearVelocity=transform.TransformDirection(v);
            float speedFactor=Mathf.Clamp(v.z/5,-1,1);float angle=ShiftInput.Turn*steering*speedFactor*Time.fixedDeltaTime;
            Body.MoveRotation(Body.rotation*Quaternion.Euler(0,angle,0));
        }
        void OnApplicationFocus(bool focused){if(!focused)ShiftInput.Clear();}
        float nextCollision;
        void OnCollisionEnter(Collision hit)
        {
            if(game==null||!game.IsPlaying||Time.time<nextCollision||hit.contactCount==0||hit.relativeVelocity.magnitude<3)return;
            if(hit.GetContact(0).normal.y>.6f)return;
            nextCollision=Time.time+1;game.Damage(hit.relativeVelocity.magnitude*1.2f);
        }
        void OnDestroy(){if(contactMaterial!=null)Destroy(contactMaterial);}
    }
}
