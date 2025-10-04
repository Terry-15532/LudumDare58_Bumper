Shader "Custom/GlowWithVectorDisplacementAndNoise"{
	Properties{
		_MainTex("Main Texture", 2D) = "white" {}
		_MainTex_ST("Main Tex Scale/Offset", Vector) = (1, 1, 0, 0)
		_GlowColor("Glow Color", Color) = (1,1,1,1)
		_GlowIntensity("Glow Intensity", Range(0, 10)) = 1.0
		_DisplacementSpeed("Displacement Speed", Vector) = (0.1, 0.1, 0, 0)
		_NoiseScale("Noise Scale", Range(0.01, 10.0)) = 1.0
		_NoiseSpeed("Noise Speed", Vector) = (0.05, 0.05, 0, 0)
		_NoiseIntensity("Noise Intensity", Range(0, 1)) = 1.0
	}
	SubShader{
		Tags{
			"RenderType"="Opaque"
		}
		LOD 200

		Pass{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Assets/Resources/Shaders/Common/CommonShaderMethods.hlsl"

			// Properties
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _GlowColor;
			float _GlowIntensity;
			float4 _DisplacementSpeed;
			float _NoiseScale;
			float4 _NoiseSpeed;
			float _NoiseIntensity;

			struct Attributes{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct Varyings{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
			};

			Varyings vert(Attributes input){
				Varyings output;
				output.position = TransformObjectToHClip(input.vertex);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);

				// Calculate world position and normal
				output.worldPos = TransformObjectToWorld(input.vertex);
				output.worldNormal = TransformObjectToWorldNormal(input.normal);
				return output;
			}

			half4 frag(Varyings input) : SV_Target{
				// Time-based UV displacement with separate X and Y speeds
				float2 uv = input.uv;
				uv += _Time.y * _DisplacementSpeed;

				// Sample the main texture
				half4 glow = _GlowColor * _GlowIntensity;
				half4 color = tex2D(_MainTex, uv) * glow;

				// Time-based UV displacement for noise
				float2 noiseUV = input.uv;
				noiseUV += _Time.y * _NoiseSpeed;

				// Glow effect with gradient noise
				float noise = GradientNoise(noiseUV, _NoiseScale);
				noise = pow(noise, 2);

				// Apply noise intensity
				half4 finalColor = lerp(color, color * noise, _NoiseIntensity);
				// finalColor.rgb = glow * noise;
				return finalColor;
			}
			ENDHLSL
		}
	}
	FallBack "Diffuse"
}