Shader "Kombi Shift/Road Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 1
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1
        _RoughnessMap ("Roughness", 2D) = "white" {}
        _RoughnessStrength ("Roughness Strength", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;
        sampler2D _RoughnessMap;
        fixed4 _Color;
        half _NormalStrength;
        half _OcclusionStrength;
        half _RoughnessStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 albedo = tex2D(_MainTex, input.uv_MainTex) * _Color;
            fixed3 normal = UnpackNormal(tex2D(_BumpMap, input.uv_MainTex));
            normal.xy *= _NormalStrength;

            half occlusion = tex2D(_OcclusionMap, input.uv_MainTex).r;
            half roughness = tex2D(_RoughnessMap, input.uv_MainTex).r;

            output.Albedo = albedo.rgb;
            output.Normal = normalize(normal);
            output.Metallic = 0;
            output.Smoothness = saturate(1 - roughness * _RoughnessStrength);
            output.Occlusion = lerp(1, occlusion, _OcclusionStrength);
            output.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Standard"
}
