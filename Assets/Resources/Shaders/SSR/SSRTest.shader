Shader "Custom/SSRTest"{

	Properties{
		[Toggle]_Noise("Water Noise", Integer) = 0
		_NoiseScale("Noise Scale", Float) = 0.1
		_NoiseSpeed("Noise Speed", Float) = 5
		_NoiseStrength("Noise Strength", Float) = 0.03
		_JitterStrength("Jitter Strength", Float) = 1
		_SpecularNoiseStrength("Specular Noise Strength", Float) = 0.03
		_SpecularStrength("Specular Strength", Range(0,5)) = 1
		_ReflectionStrength("Reflection Strength", Range(0,1)) = 0.2
		_MaxSteps("Trace Steps", Integer) = 20
		_StepLength("Step Length", Float) = 0.1
		_DepthTolerance("Depth Tolerance", Range(0,1)) = 0.05
		_BinaryTolerance("Binary Search Tolerance", Range(0,1)) = 0.005
	}
	SubShader{
		Tags{
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
		}
		LOD 200

		Pass{
			Name "SSRBase"
			Blend SrcAlpha OneMinusSrcAlpha

			tags{
				"LightMode"="SSRBase"
			}

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Assets/Resources/Shaders/Common/CommonShaderMethods.hlsl"

			#pragma vertex vert
			#pragma fragment frag


			TEXTURE2D(_CameraOpaqueTexture);
			TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthTexture);
			SAMPLER(sampler_CameraOpaqueTexture);


			float2 _CameraOpaqueTexture_TexelSize;

			float _StepLength, _SpecularStrength, _NoiseStrength, _SpecularNoiseStrength,
				_ReflectionStrength, _NoiseScale, _DepthTolerance, _NoiseSpeed, _BinaryTolerance,
				_JitterStrength;

			uint _MaxSteps, _BinarySteps;

			bool _Noise, _FlipY;

			struct appdata{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 vertPos : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float3 worldNormal : NORMAL;
			};

			v2f vert(appdata v){
				v2f o;
				o.vertPos = v.vertex.xyz;
				o.position = TransformObjectToHClip(v.vertex);
				o.worldPos = TransformObjectToWorld(v.vertex);
				o.worldNormal = TransformObjectToWorldNormal(v.normal);
				o.uv = v.texcoord;
				return o;
			}

			float2 ClipToScreenPos(float4 pos){
				return (float2(1, -1) * pos.xy / pos.w + 1) / 2;
			}

			static float dither[16] = {
				0.0, 0.5, 0.13, 0.625,
				0.75, 0.25, 0.875, 0.375,
				0.187, 0.687, 0.0625, 0.562,
				0.937, 0.437, 0.817, 0.312
			};

			float2 flipY(float2 pos){
				if (_FlipY){
					pos.y = 1 - pos.y;
				}
				return pos;
			}

			float4 frag(v2f i) : SV_Target{
				float screenDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture,
														flipY(ClipToScreenPos(TransformObjectToHClip(i.vertPos)))).r;
				if (i.position.z <= screenDepth + 0.0001){
					discard;
				}

				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

				float3 noise = 0;
				if (_Noise){
					float noiseScale = 1 / _NoiseScale;
					float t = _Time.x * _NoiseSpeed;
					noise = float3(GradientNoise(i.uv + t, noiseScale) - 0.5,
									GradientNoise(i.uv + 0.1 + t, noiseScale) - 0.5,
									GradientNoise(i.uv + 0.2 + t * 0.5, noiseScale) - 0.5);
				}

				float3 reflectedViewDir = -normalize(reflect(viewDir, i.worldNormal + noise * _NoiseStrength));


				float4 clipPosA = TransformObjectToHClip(i.vertPos);

				float2 screenPos = ClipToScreenPos(clipPosA);


				float4 clipPosB = TransformWorldToHClip(i.worldPos + reflectedViewDir);

				_BinaryTolerance /= pow(dot(viewDir, i.worldNormal) + 1, 2) * 10;
				_DepthTolerance /= pow(dot(viewDir, i.worldNormal) + 1, 2);


				float rayDepth = clipPosA.z / clipPosA.w;
				float depthDelta = (clipPosB.z / clipPosB.w - rayDepth);

				float2 screenDelta = ClipToScreenPos(clipPosB) - screenPos;
				float lengthBefore = length(screenDelta);

				screenDelta = normalize(screenDelta);

				float lengthRatio = length(screenDelta) / lengthBefore;

				depthDelta *= lengthRatio * _StepLength;
				screenDelta *= _StepLength;

				float4 col = 0;
				float3 reflectionColor = float3(0, 0, 0);

				bool found = false;

				float steps = 0;

				[loop]
				for (uint a = 0; a < _MaxSteps; a++){
					float n = (dither[(screenPos.x * _ScreenSize.x + screenPos.y + _SinTime.x) * 10000 % 16] - 0.5) * _JitterStrength;
					screenPos += screenDelta * (1 + n);
					rayDepth += depthDelta * (1 + n);
					steps += 1 + n;

					float d = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, flipY(screenPos)).r;


					const float diff = d - rayDepth;

					if (d > 0 && diff > 0 && diff < _DepthTolerance && all(abs(screenPos - 0.5) < 0.5)){
						found = true;
						break;
					}
				}

				if (found){
					// screenDelta *= 1;
					// depthDelta *= 1;
					float2 startPos = screenPos - screenDelta;
					float2 endPos = screenPos + screenDelta;
					float startDepth = rayDepth - depthDelta;
					float endDepth = rayDepth + depthDelta;
					float minDiff = 100;

					for (uint j = 0; j < 10; j++){
						screenPos = (startPos + endPos) / 2;
						rayDepth = (startDepth + endDepth) / 2;
						float d = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, flipY(screenPos)).r;
						float diff = d - rayDepth;
						if (d > 0 && diff < minDiff && diff > 0){
							endPos = screenPos;
							endDepth = rayDepth;
							minDiff = diff;
						}
						else{
							startPos = screenPos;
							startDepth = rayDepth;
						}
					}
					if (minDiff < _BinaryTolerance){
						reflectionColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, flipY(screenPos)).rgb;
					}
					else{
						found = false;
					}
				}
				if (found){
					col = float4(reflectionColor, _ReflectionStrength);
					//col.a *= pow(clipPosA.z/clipPosA.w * 8, 0.2);
				}
				return col;
			}
			ENDHLSL
		}
	}

	//FallBack "Universal Render Pipeline/Lit"


}

//Shader "Custom/SSRTest" {
//    Properties {
//        [Toggle]_Noise("Water Noise", Integer) = 0
//        _NoiseScale("Noise Scale", Float) = 0.1
//        _NoiseSpeed("Noise Speed", Float) = 5
//        _NoiseStrength("Noise Strength", Float) = 0.03
//        _JitterStrength("Jitter Strength", Float) = 1
//        _SpecularNoiseStrength("Specular Noise Strength", Float) = 0.03
//        _SpecularStrength("Specular Strength", Range(0,5)) = 1
//        _ReflectionStrength("Reflection Strength", Range(0,1)) = 0.2
//        _MaxSteps("Trace Steps", Integer) = 20
//        _StepLength("Step Length", Float) = 0.1
//        _DepthTolerance("Depth Tolerance", Range(0,1)) = 0.05
//        _BinaryTolerance("Binary Search Tolerance", Range(0,1)) = 0.005
//    }
//    SubShader {
//        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
//        LOD 200
//
//        Pass {
//            Name "SSRBase"
//            Blend SrcAlpha OneMinusSrcAlpha
//            Tags { "LightMode"="SSRBase" }
//
//            HLSLPROGRAM
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            #include "Assets/Shaders/Common/CommonShaderMethods.hlsl"
//
//            #pragma vertex vert
//            #pragma fragment frag
//
//            TEXTURE2D(_CameraOpaqueTexture);
//            TEXTURE2D(_CameraDepthTexture);
//            SAMPLER(sampler_CameraDepthTexture);
//            SAMPLER(sampler_CameraOpaqueTexture);
//
//            float2 _CameraOpaqueTexture_TexelSize;
//
//            float _StepLength, _SpecularStrength, _NoiseStrength, _SpecularNoiseStrength;
//            float _ReflectionStrength, _NoiseScale, _DepthTolerance, _NoiseSpeed, _BinaryTolerance, _JitterStrength;
//            uint _MaxSteps, _BinarySteps;
//            bool _Noise, _FlipY;
//
//            struct appdata {
//                float4 vertex : POSITION;
//                float3 normal : NORMAL;
//                float4 texcoord : TEXCOORD0;
//            };
//
//            struct v2f {
//                float4 position : SV_POSITION;
//                float2 uv : TEXCOORD0;
//                float3 vertPos : TEXCOORD1;
//                float3 worldPos : TEXCOORD2;
//                float3 worldNormal : NORMAL;
//            };
//
//            v2f vert(appdata v) {
//                v2f o;
//                o.vertPos = v.vertex.xyz;
//                o.position = TransformObjectToHClip(v.vertex);
//                o.worldPos = TransformObjectToWorld(v.vertex);
//                o.worldNormal = TransformObjectToWorldNormal(v.normal);
//                o.uv = v.texcoord;
//                return o;
//            }
//
//            float2 ClipToScreenPos(float4 pos) {
//                return (float2(1, -1) * pos.xy / pos.w + 1) * 0.5;
//            }
//
//            float2 flipY(float2 pos) {
//                return _FlipY ? float2(pos.x, 1 - pos.y) : pos;
//            }
//
//            float cheapNoise(float2 p) {
//                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
//            }
//
//            float4 frag(v2f i) : SV_Target {
//                float4 clipPosA = TransformObjectToHClip(i.vertPos);
//                float2 screenPos = ClipToScreenPos(clipPosA);
//                screenPos = flipY(screenPos);
//
//                float screenDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenPos).r;
//                if (i.position.z <= screenDepth - 0.0003) return 0;
//
//                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
//
//                float3 noise = 0;
//                if (_Noise) {
//                    float t = _Time.x * _NoiseSpeed;
//                    float scale = 1 / _NoiseScale;
//                    noise = float3(
//                        cheapNoise(i.uv + t) - 0.5,
//                        cheapNoise(i.uv + 0.1 + t) - 0.5,
//                        cheapNoise(i.uv + 0.2 + t * 0.5) - 0.5);
//                }
//
//                float3 reflectedDir = -normalize(reflect(viewDir, i.worldNormal + noise * _NoiseStrength));
//                float4 clipPosB = TransformWorldToHClip(i.worldPos + reflectedDir);
//
//                float angleFactor = pow(dot(viewDir, i.worldNormal) + 1, 2);
//                float depthTolerance = _DepthTolerance / angleFactor;
//                float binaryTolerance = _BinaryTolerance / (angleFactor * 10);
//
//                float rayDepth = clipPosA.z / clipPosA.w;
//                float depthDelta = (clipPosB.z / clipPosB.w - rayDepth);
//
//                float2 screenDelta = ClipToScreenPos(clipPosB) - screenPos;
//                float lengthBefore = length(screenDelta);
//                screenDelta = normalize(screenDelta);
//                float lengthRatio = length(screenDelta) / max(lengthBefore, 1e-5);
//
//                depthDelta *= lengthRatio * _StepLength;
//                screenDelta *= _StepLength;
//
//                float4 col = 0;
//                float3 reflectionColor = 0;
//                float steps = 0;
//                bool found = false;
//
//                [loop]
//                for (uint a = 0; a < _MaxSteps; a++) {
//                    float jitter = (cheapNoise(screenPos * _ScreenSize.xy + _Time.y) - 0.5) * _JitterStrength;
//                    float stepSize = 1 + jitter;
//                    screenPos += screenDelta * stepSize;
//                    rayDepth += depthDelta * stepSize;
//                    steps += stepSize;
//
//                    float d = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenPos).r;
//                    float diff = d - rayDepth;
//
//                    if (d > 0 && diff > 0 && diff < depthTolerance && all(abs(screenPos - 0.5) < 0.5)) {
//                        found = true;
//                        break;
//                    }
//                }
//
//                if (found) {
//                    float2 startPos = screenPos - screenDelta;
//                    float2 endPos = screenPos + screenDelta;
//                    float startDepth = rayDepth - depthDelta;
//                    float endDepth = rayDepth + depthDelta;
//                    float minDiff = 100;
//
//                    for (uint j = 0; j < 10; j++) {
//                        screenPos = lerp(startPos, endPos, 0.5);
//                        rayDepth = lerp(startDepth, endDepth, 0.5);
//                        float d = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenPos).r;
//                        float diff = d - rayDepth;
//                        if (d > 0 && diff < minDiff && diff > 0) {
//                            endPos = screenPos;
//                            endDepth = rayDepth;
//                            minDiff = diff;
//                        } else {
//                            startPos = screenPos;
//                            startDepth = rayDepth;
//                        }
//                    }
//                    if (minDiff < binaryTolerance) {
//                        reflectionColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenPos).rgb;
//                    } else {
//                        found = false;
//                    }
//                }
//
//                if (found) {
//                    col = float4(reflectionColor, _ReflectionStrength);
//                    col.a *= pow(clipPosA.z / clipPosA.w * 8, 0.2);
//                }
//                return col;
//            }
//            ENDHLSL
//        }
//    }
//} 
