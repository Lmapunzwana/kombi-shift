using UnityEngine;

namespace KombiShift2.UI
{
    // Native uGUI geometry keeps the reference dial crisp at any HUD scale.
    public sealed class KombiSpeedometerDial : UnityEngine.UI.MaskableGraphic
    {
        float speed;
        static readonly Color Orange=new Color(.97f,.31f,.16f),DarkOrange=new Color(.32f,.075f,.035f);
        public void SetSpeed(float value)
        {value=Mathf.Clamp(value,0,240);if(Mathf.Abs(speed-value)<.05f)return;speed=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var center=rectTransform.rect.center;
            const int sectors=14,steps=9;
            for(int sector=0;sector<sectors;sector++)
            {
                float start=225f-sector*270f/sectors-.35f;
                float end=225f-(sector+1)*270f/sectors+.35f;
                for(int i=0;i<steps;i++)
                {
                    float a=Mathf.Lerp(start,end,i/(float)steps),b=Mathf.Lerp(start,end,(i+1f)/steps);
                    Band(vh,center,75,105,a,b,DarkOrange,Orange);
                    Band(vh,center,105,109,a,b,new Color(1f,.46f,.26f),new Color(.65f,.19f,.10f));
                }
            }
            float endAngle=225f-speed/240f*270f;
            // White/yellow speed trace and an exact radial indicator, matching the reference.
            int lit=Mathf.Max(1,Mathf.CeilToInt(speed/240f*120f));
            for(int i=0;i<lit;i++)
            {
                float a=Mathf.Lerp(225,endAngle,i/(float)lit),b=Mathf.Lerp(225,endAngle,(i+1f)/lit);
                Band(vh,center,106,109,a,b,new Color(1f,.95f,.69f),Color.white);
                Band(vh,center,76,104,a,b,new Color(1f,.75f,.22f,.28f),new Color(1f,.87f,.4f,.65f));
            }
            Band(vh,center,69,111,endAngle-.65f,endAngle+.65f,Color.white,Color.white);
        }
        static Vector2 Point(Vector2 center,float radius,float degrees)
        {float a=degrees*Mathf.Deg2Rad;return center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}
        static void Band(UnityEngine.UI.VertexHelper vh,Vector2 c,float inner,float outer,float a,float b,Color innerColor,Color outerColor)
        {
            int index=vh.currentVertCount;
            vh.AddVert(Point(c,inner,a),innerColor,Vector2.zero);vh.AddVert(Point(c,outer,a),outerColor,Vector2.zero);
            vh.AddVert(Point(c,outer,b),outerColor,Vector2.zero);vh.AddVert(Point(c,inner,b),innerColor,Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
        }
        static void Disc(UnityEngine.UI.VertexHelper vh,Vector2 c,float radius,Color middle,Color edge)
        {
            for(int i=0;i<96;i++)
            {
                int index=vh.currentVertCount;vh.AddVert(c,middle,Vector2.zero);
                vh.AddVert(Point(c,radius,i*360f/96),edge,Vector2.zero);vh.AddVert(Point(c,radius,(i+1)*360f/96),edge,Vector2.zero);
                vh.AddTriangle(index,index+1,index+2);
            }
        }
    }
}
