using System;
using UnityEngine;

namespace KombiShift2.City
{
    // Grow reusable buffers for dense traffic. At the cap use the complete query: a full
    // NonAlloc result does not guarantee the nearest/all relevant colliders were returned.
    internal static class CityPhysicsQueries
    {
        const int MaximumBuffer = 256;
        public static int SphereCast(Vector3 origin,float radius,Vector3 direction,float distance,
            ref RaycastHit[] buffer,out RaycastHit[] results,out bool saturated)
        {
            saturated=false;
            while(true)
            {
                int count=Physics.SphereCastNonAlloc(origin,radius,direction,buffer,distance,~0,QueryTriggerInteraction.Ignore);
                if(count<buffer.Length){results=buffer;return count;}
                saturated=true;
                if(buffer.Length>=MaximumBuffer)
                {results=Physics.SphereCastAll(origin,radius,direction,distance,~0,QueryTriggerInteraction.Ignore);return results.Length;}
                Array.Resize(ref buffer,Mathf.Min(MaximumBuffer,buffer.Length*2));
            }
        }
        public static int BoxCast(Vector3 origin,Vector3 halfExtents,Vector3 direction,Quaternion rotation,float distance,
            ref RaycastHit[] buffer,out RaycastHit[] results)
        {
            while(true)
            {
                int count=Physics.BoxCastNonAlloc(origin,halfExtents,direction,buffer,rotation,distance,~0,QueryTriggerInteraction.Ignore);
                if(count<buffer.Length){results=buffer;return count;}
                if(buffer.Length>=MaximumBuffer)
                {results=Physics.BoxCastAll(origin,halfExtents,direction,rotation,distance,~0,QueryTriggerInteraction.Ignore);return results.Length;}
                Array.Resize(ref buffer,Mathf.Min(MaximumBuffer,buffer.Length*2));
            }
        }
        public static int OverlapSphere(Vector3 origin,float radius,ref Collider[] buffer,out Collider[] results,out bool saturated)
        {
            saturated=false;
            while(true)
            {
                int count=Physics.OverlapSphereNonAlloc(origin,radius,buffer,~0,QueryTriggerInteraction.Ignore);
                if(count<buffer.Length){results=buffer;return count;}
                saturated=true;
                if(buffer.Length>=MaximumBuffer)
                {results=Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore);return results.Length;}
                Array.Resize(ref buffer,Mathf.Min(MaximumBuffer,buffer.Length*2));
            }
        }
    }
}
