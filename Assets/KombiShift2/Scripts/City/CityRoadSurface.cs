using System;
using UnityEngine;
namespace KombiShift2.City
{
    public enum RoadCondition { Good, Moderate, Severe }
    [Serializable] public struct CityPothole
    {
        public Vector3 centre, forward;
        public float radius, depth;
        public float Depression(Vector3 position)
        {
            var delta=position-centre;delta.y=0;
            float r=delta.magnitude/Mathf.Max(.1f,radius);
            return r>=1?0:depth*(1-Mathf.SmoothStep(0,1,r));
        }
    }
    public sealed class CityRoadSurface : MonoBehaviour
    {
        public Mesh sourceMesh;
        public RoadCondition condition;
        public CityPothole[] potholes=Array.Empty<CityPothole>();
        public float DepthAt(Vector3 position){float value=0;foreach(var hole in potholes)value=Mathf.Max(value,hole.Depression(position));return value;}
    }
}
