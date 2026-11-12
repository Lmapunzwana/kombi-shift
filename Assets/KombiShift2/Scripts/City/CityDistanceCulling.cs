using UnityEngine;
namespace KombiShift2.City
{
    public sealed class CityDistanceCulling : MonoBehaviour
    {
        public Renderer[] cityRenderers;
        public bool optimize=true;
        public float distance=650f;
        Vector3[] positions;float[] radii;bool[] original;float next;
        void Start()
        {
            positions=new Vector3[cityRenderers.Length];radii=new float[positions.Length];original=new bool[positions.Length];
            for(int i=0;i<positions.Length;i++)if(cityRenderers[i]!=null){positions[i]=cityRenderers[i].bounds.center;radii[i]=cityRenderers[i].bounds.extents.magnitude;original[i]=cityRenderers[i].enabled;}
        }
        void Update()
        {
            if(positions==null||Camera.main==null||Time.unscaledTime<next)return;next=Time.unscaledTime+.5f;var camera=Camera.main.transform.position;
            for(int i=0;i<positions.Length;i++)if(cityRenderers[i]!=null&&original[i])
            {float range=distance+Mathf.Min(300,radii[i]);bool visible=!optimize||(positions[i]-camera).sqrMagnitude<range*range;if(cityRenderers[i].enabled!=visible)cityRenderers[i].enabled=visible;}
        }
        void OnDestroy(){if(original!=null)for(int i=0;i<original.Length;i++)if(cityRenderers[i]!=null)cityRenderers[i].enabled=original[i];}
    }
}
