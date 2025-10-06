Shader "Custom/Boundary"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Emission ("Emission", Float) = 1
        _Speed ("Speed", Vector) = (0,0,0,0)
        _Scale("Scale", Vector) = (0,0,0,0)
        _FresnelPower("Fresnel Power", Float) = 3
        _FresnelIntensity("Fresnel Intensity", Float) = 1
        [HideInInspector]
        _Stencil ("Stencil", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        LOD 100

        ZWrite off
        Cull off
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref [_Stencil]
            Comp LEqual
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Resources/Shaders/Common/CommonShaderMethods.hlsl"


            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Emission;
            float4 _Color;
            float2 _Scale, _Speed;
            float _FresnelPower, _FresnelIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.worldPos = TransformObjectToWorld(v.vertex);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float a = tex2D(_MainTex, i.uv).a;

                float3 objPos = TransformObjectToWorld(float3(0, 0, 0));

                float glit = clamp(GradientNoise(i.uv * _Scale + objPos.x + objPos.z + _Time * _Speed / -15, 1.5), 0.1, 1);

                float fresnel = pow(1.0 - saturate(dot(normalize(i.worldPos - _WorldSpaceCameraPos), normalize(i.normal))), _FresnelPower);
                float3 color = (_Color) * (_Emission + 1) * _Color;
                return float4(color.rgb, a * saturate(pow(1 - i.uv.y, 5) * glit * saturate(1 - fresnel * _FresnelIntensity))) * i.color;
            }
            ENDHLSL
        }
    }
}