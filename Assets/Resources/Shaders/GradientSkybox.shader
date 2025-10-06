Shader "Custom/GradientSkybox"{
	Properties{
		_Color1 ("Color 1", Color) = (0.8, 0.8, 0.8, 1)
		_Color2 ("Color 2", Color) = (0.2, 0.2, 0.2, 1)
		_Hardness ("Hardness", Range(-1, 1)) = 1
		_Offset ("Offset", Range(-1, 1)) = 1
		_Width ("Width", Range(-1, 1)) = 1
		_Speed ("Animation Speed", Range(0, 5)) = 0.2
	}
	SubShader{
		Tags{
			"Queue" = "Background" "RenderType" = "Opaque"
		}
		Pass{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Assets/Resources/Shaders/Common/CommonShaderMethods.hlsl"

			struct appdata_t{
				float4 vertex : POSITION;
			};

			struct v2f{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _Color1;
			float4 _Color2;
			float _Hardness, _Offset, _Width, _Speed;

			v2f vert(appdata_t v){
				v2f o;
				o.position = TransformObjectToHClip(v.vertex);
				o.uv = -abs(v.vertex + _Offset + sin(_Time.y * _Speed) * 0.05);
				o.worldPos = TransformObjectToWorld(v.vertex);
				return o;
			}

			float3 frag(v2f i) : SV_Target{
				float animatedWidth = _Width + sin(_Time.y * _Speed * 0.7) * 0.05;
				float3 sun = _MainLightColor * pow(saturate(dot(_MainLightPosition, normalize(i.worldPos))), 10000) * 3 * _Color2 /
					Brightness(_Color2);
				float3 col = lerp(_Color2, _Color1, Posterize(saturate(i.uv.y + 0.5) + animatedWidth, 1, _Hardness));
				col = Overlay(col, sun, sun);
				return col;
			}
			ENDHLSL
		}
	}
	FallBack "Diffuse"
}