Shader "Kombi Shift/City Asphalt World"
{
 Properties {
  _MainTex("Worn asphalt",2D)="white"{}
  _BumpMap("Normal",2D)="bump"{}
  _RoughnessMap("Roughness",2D)="white"{}
  _OcclusionMap("Occlusion",2D)="white"{}
  _Color("Tint",Color)=(.7,.7,.7,1)
  _MetresPerTile("Metres per tile",Float)=4
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  #pragma multi_compile_instancing
  sampler2D _MainTex,_BumpMap,_RoughnessMap,_OcclusionMap;
  fixed4 _Color;float _MetresPerTile;
  struct Input {float3 worldPos;};
  void surf(Input IN,inout SurfaceOutputStandard o) {
   float2 uv=IN.worldPos.xz/max(1,_MetresPerTile);
   fixed3 a=tex2D(_MainTex,uv).rgb;
   float grey=dot(a,float3(.2126,.7152,.0722));
   a=lerp(grey.xxx,a,.08)*.7;
   float patch=step(.94,frac(sin(dot(floor(IN.worldPos.xz/3),float2(12.9898,78.233)))*43758.5453));
   o.Albedo=a*_Color.rgb*lerp(1,.72,patch);
   o.Normal=UnpackNormal(tex2D(_BumpMap,uv));
   o.Metallic=0;o.Smoothness=1-tex2D(_RoughnessMap,uv).r;
   o.Occlusion=tex2D(_OcclusionMap,uv).r;
  }
  ENDCG
 }
 FallBack "Standard"
}
