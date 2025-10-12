Shader "Custom/SimpleSpriteScroll"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ScrollX ("Scroll Speed X", Float) = 0
        _ScrollY ("Scroll Speed Y", Float) = 0
        _OffsetX ("Manual Offset X", Float) = 0
        _OffsetY ("Manual Offset Y", Float) = 0
        [HDR] _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ScrollX, _ScrollY;
            float _OffsetX, _OffsetY;
            float4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                float2 scroll = float2(_ScrollX, _ScrollY) * time + float2(_OffsetX, _OffsetY);
                float2 uv = i.uv + scroll;
                float brigthtness = pow(i.uv.y + 0.1, 0.8);
                fixed4 col = tex2D(_MainTex, uv) * _Color * brigthtness;
                return col;
            }
            ENDCG
        }
    }
}

