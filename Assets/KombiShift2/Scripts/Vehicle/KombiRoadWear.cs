using KombiShift2.City;
using KombiShift2.Gameplay;
using UnityEngine;
namespace KombiShift2.Vehicle
{
    // Road geometry supplies the wheel displacement; this sampler supplies cumulative mechanical wear.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class KombiRoadWear : MonoBehaviour
    {
        WheelCollider[] wheels;JointSpring[] baseSprings;float nextSpringUpdate;float[] previousDepth,nextImpact;Rigidbody body;KombiVehicleCondition condition;KombiShiftGameDirector director;
        public int RecordedImpacts {get;private set;}
        void Awake(){wheels=GetComponentsInChildren<WheelCollider>();previousDepth=new float[wheels.Length];nextImpact=new float[wheels.Length];body=GetComponent<Rigidbody>();condition=GetComponent<KombiVehicleCondition>();}
        void Start(){wheels=GetComponentsInChildren<WheelCollider>();previousDepth=new float[wheels.Length];nextImpact=new float[wheels.Length];director=FindFirstObjectByType<KombiShiftGameDirector>();baseSprings=new JointSpring[wheels.Length];for(int i=0;i<wheels.Length;i++)baseSprings[i]=wheels[i].suspensionSpring;}
        void FixedUpdate()
        {
            if(baseSprings!=null&&condition!=null&&Time.time>=nextSpringUpdate)
            {
                nextSpringUpdate=Time.time+1f;float factor=Mathf.Lerp(.7f,1f,condition.SuspensionHealth/100f);
                for(int i=0;i<wheels.Length;i++){var spring=baseSprings[i];spring.spring*=factor;spring.damper*=Mathf.Lerp(.85f,1f,condition.SuspensionHealth/100f);wheels[i].suspensionSpring=spring;}
            }
            for(int i=0;i<wheels.Length;i++)
            {
                float depth=0;
                if(wheels[i].GetGroundHit(out var hit))
                {var road=hit.collider.GetComponent<CityRoadSurface>();if(road!=null)depth=road.DepthAt(hit.point);}
                float speed=body.linearVelocity.magnitude;
                if(depth>.035f && previousDepth[i]<=.035f && Time.time>=nextImpact[i] && speed>4.2f)
                {
                    nextImpact[i]=Time.time+1f;RecordedImpacts++;
                    float severity=Mathf.Clamp((speed-4.2f)*depth*.7f,0,5f);
                    if(condition!=null)condition.ApplyRoadWear(severity);
                    if(director!=null && director.Vehicle.gameObject==gameObject)
                        director.Quality.Apply("pothole",director.Passengers>0?"Passenger: Watch the road! Slow down for potholes.":"Pothole impact — suspension wear",-severity*2,-Mathf.CeilToInt(severity),3f);
                }
                previousDepth[i]=depth;
            }
        }
    }
}
