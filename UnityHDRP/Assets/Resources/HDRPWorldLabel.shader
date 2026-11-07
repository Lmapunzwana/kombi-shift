Shader "KombiShift/HDRPWorldLabel" {
 Properties { _MainTex("Font atlas",2D)="white"{} }
 SubShader {
 Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Name "ForwardOnly" Tags { "LightMode"="ForwardOnly" }
 Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off ZTest LEqual
 HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
 #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
 #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 Varyings vert(Attributes i){Varyings o;o.positionCS=TransformWorldToHClip(TransformObjectToWorld(i.positionOS));o.uv=i.uv;o.color=i.color;return o;}
 float4 frag(Varyings i):SV_Target {float4 c=i.color;c.a*=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;clip(c.a-.01);return c;}
 ENDHLSL
 }
 }
}
