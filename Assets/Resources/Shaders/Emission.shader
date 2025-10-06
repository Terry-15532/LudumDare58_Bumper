Shader "Custom/Emission"{
    Properties{
        _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Emission ("Emission", Float) = 1
        [HideInInspector]
        _Stencil ("Stencil", Int) = 0
    }



    SubShader{
        Tags{
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }
        LOD 100

        ZWrite On
        Cull off
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil{
            Ref [_Stencil]
            Comp LEqual
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            HLSLPROGRAM
            #pragma shader_feature _CAST_SHADOW
            #pragma shader_feature _OVERRIDE_BIAS

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            #pragma vertex CustomShadowVertex
            #pragma fragment ShadowPassFragment

            float _DepthBias, _NormalBias;
            bool _OverrideBias;

            Varyings CustomShadowVertex(Attributes v)
            {
                #if _CAST_SHADOW
                #if _OVERRIDE_BIAS
                    _ShadowBias.xy = float2(_DepthBias / -10, _NormalBias / -10);
                #endif
				return ShadowPassVertex(v);
                #else
                Varyings varyings = ShadowPassVertex(v);
                varyings.positionCS = float4(-1, -1, -1, -100);
                return varyings;
                #endif
            }
            ENDHLSL
        }

        Pass{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Emission;
            float4 _Color;

            v2f vert(appdata v){
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target{
                float a = tex2D(_MainTex, i.uv).a;
                float4 color = _Color * (_Emission + 1);
                return float4(color.rgb, a) * i.color;
            }
            ENDHLSL
        }
    }
}