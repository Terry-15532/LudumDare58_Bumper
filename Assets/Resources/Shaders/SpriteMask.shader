Shader "Custom/SpriteMask"{
    Properties{
        _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Stencil ("Stencil", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilOp ("StencilOp", Int) = 0
    }



    SubShader{
        Tags{
            "Queue" = "Transparent"
            "RenderType" = "Sprite"
        }
        LOD 100

        ZWrite off
        Cull off
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil{
            Ref [_Stencil]
            Comp [_StencilOp]
            Pass Replace
            fail Keep
        }

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"


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
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target{
                float a = tex2D(_MainTex, i.uv).a;
                float4 color = _Color * (_Emission + 1);
                return float4(color.rgb, a) * i.color;
            }
            ENDCG
        }
    }
}