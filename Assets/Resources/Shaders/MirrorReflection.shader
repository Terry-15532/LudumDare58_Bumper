Shader "Custom/MirrorReflection"
{
    Properties
    {
        _ReflectionTex ("Reflection Texture", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderQueue"="transparent"
            "RenderType"="transparent"
        }
        LOD 100

        Pass
        {

            Tags
            {
                "LightMode" = "UniversalForwardBase"
            }
            
            ZWrite On
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 refl = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, i.uv);
                return refl * _Color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}