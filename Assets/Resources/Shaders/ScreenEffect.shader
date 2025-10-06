Shader "Custom/Emission"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _TextureSpeed ("Texture Speed", Vector) = (0,0,0,0)
        _Color ("Color", Color) = (1,1,1,1)
        _Emission ("Emission", Float) = 1
        _Stencil ("Stencil", Int) = 0
        _CRTIntensity ("CRT Intensity", Range(0,1)) = 0.5
        _GlitchIntensity ("Glitch Intensity", Range(0,1)) = 0.2
        _CRTSpeed ("CRT Speed", Range(0,10)) = 2.0
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.5
        _ScanlineFrequency ("Scanline Frequency", Range(100,2000)) = 800
    }



    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            //            "RenderType" = "Sprite"
        }
        LOD 100

        ZWrite on
        Cull off
        Blend One Zero

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _TextureSpeed;
            float _Emission;
            float4 _Color;
            float _CRTIntensity;
            float _GlitchIntensity;
            float _CRTSpeed;
            float _ScanlineIntensity;
            float _ScanlineFrequency;

            // Pseudo-random function
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.screenPos = o.vertex;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // CRT scanline effect using object's UV
                float2 time = float2(floor(_Time.x * 500) / 500, floor(_Time.y * 500) / 500);
                float2 crtUV = i.uv;
                float scanline = sin((crtUV.y + time.y * _CRTSpeed) * _ScanlineFrequency) * 0.5 + 0.5;
                float scanlineOverlay = lerp(1.0, scanline, _ScanlineIntensity);
                float vignette = smoothstep(0.0, 0.2, crtUV.x) * smoothstep(1.0, 0.8, crtUV.x) * smoothstep(0.0, 0.2, crtUV.y) * smoothstep(
                    1.0, 0.8, crtUV.y);

                // Glitch effect using object's UV
                float glitch = step(1.0 - _GlitchIntensity, rand(float2(time.y * _CRTSpeed, crtUV.y * 100.0)));
                float2 glitchOffset = float2(glitch * rand(float2(time.y, crtUV.y)) * 0.02 * _GlitchIntensity, 0);
                float2 uv = i.uv + glitchOffset + time.y * _TextureSpeed;

                // Color channel offset (chromatic aberration) using object's UV
                float channelOffset = sin(time.y * _CRTSpeed + crtUV.y * 50.0) * 0.0003;
                float4 col;
                col.r = tex2D(_MainTex, uv + float2(channelOffset, 0)).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - float2(channelOffset, 0)).b;
                col.a = tex2D(_MainTex, uv).a;

                // Apply scanline overlay and vignette
                col.rgb *= scanlineOverlay;
                col.rgb *= lerp(1.0, vignette, _CRTIntensity * 0.5);

                // Emission and color
                float4 color = _Color * (_Emission + 1);
                col *= color;
                col *= i.color;
                return col;
            }
            ENDHLSL
        }
    }
}