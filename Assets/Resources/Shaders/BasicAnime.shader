Shader "Custom/BasicAnime"
{
    Properties
    {
        [Header(Basic)]
        [Space(10)]
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [Space(10)]
        [HDR] _Color ("Base Color", Color) = (1,1,1)
        _Emission ("Emission", Range(0,10)) = 1

        [Header(Lighting)]
        [Space(10)]
        _LightEffect ("Light Influence On Surface", Range(0,1)) = 1
        _LightEffectOnOutline ("Light Influence On Outline", Range(0,1)) = 1

        [Header(Normal Map)]
        [Space(10)]
        [Toggle(_USE_NORMAL_MAP)] _UseNormalMap("Use Normal Map", Integer) = 0
        [Space(10)]
        _NormalMap ("Normal Map", 2D) = "white" {}
        [Toggle] _ObjectSpaceNormalMap("Object Space Normal Map", Integer) = 0
        [Toggle(_PER_VERTEX_NORMAL)] _PerVertexNormal("Sample Per Vertex", Integer) = 0
        [Toggle(_UNPACK_NORMAL)] _UnpackNormal("Unpack Normal", Integer) = 0
        _NormalMapStrength("Normal Map Strength", Float) = 1
        [Toggle] _NormalDebug("Normal Debug", Integer) = 0
        [Space(10)]
        _NormalOffset ("Normal Offset", Vector) = (0,0,0)
        _RotateNormal ("Rotate Normal", Vector) = (0,0,0)

        [Header(Diffuse)]
        [Space(10)]
        [Toggle(_SMOOTH_NORMAL_DIFFUSE)] _SmoothNormalDiffuse("Use Smoothed Normal", Integer) = 0
        [HDR] _DiffuseColor ("Diffuse Color", Color) = (1,1,1)
        _DiffuseOffset ("Diffuse Offset", Range(0, 10)) = 0.5
        _DiffuseSteps ("Diffuse Steps", Integer) = 3
        _DiffuseHardness ("Diffuse Hardness", Range(0,1)) = 1
        _DiffuseStrength("Diffuse Strength", Range(0,10)) = 1
        _DiffuseRange("Diffuse Range", Range(0, 10)) = 1

        [Header(Outline)]
        [Space(10)]
        [Toggle(_SHOW_OUTLINE_INNER)] _ShowInnerOutline ("Inner Outline", Integer) = 1
        [Toggle(_SHOW_OUTLINE_OUTER)] _ShowOuterOutline ("Outer Outline", Integer) = 1
        [Toggle(_USE_SMOOTHED_NORMAL)] _UseSmoothedNormal("Use Smoothed Normal", Integer) = 1
        [HDR] _InnerOutlineColor ("Inner Outline Color", Color) = (1,1,1,1)
        [HDR] _OuterOutlineColor ("Outer Outline Color", Color) = (1,1,1,1)
        _InnerWidth ("Inner Outline Width", Range(0,10)) = 1
        _OuterWidth ("Outer Outline Width", Range(0,10)) = 1
        _InnerZOffset ("Inner Z Offset", Float) = 10
        _OuterZOffset ("Outer Z Offset", Float) = 10
        [Space(10)]
        _StencilVal ("Stencil Value", Integer) = 5
        [Enum(UnityEngine.Rendering.CompareFunction)] _InnerStencilComp ("Inner Stencil Comp", Float) = 3
        [Enum(UnityEngine.Rendering.CompareFunction)] _OuterStencilComp ("Outer Stencil Comp", Float) = 6

        [Header(Casting Shadow)]
        [Space(10)]
        [Toggle(_CAST_SHADOW)] _CastShadow ("Cast Sahdow", Integer) = 0
        [Toggle(_OVERRIDE_BIAS)] _OverrideBias ("Override Shadow Bias", Integer) = 0
        _DepthBias ("Depth Bias", Range(-1, 1)) = 0
        _NormalBias ("Normal Bias", Range(-1, 1)) = 0

        [Header(Recieving Shadow)]
        [Space(10)]
        [Toggle(_RCV_SHADOW)] _RcvShadow ("Recieve Sahdow", Integer) = 0
        [Space(10)]
        [HDR] _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.3)
        _ShadowSteps ("Shadow Steps", Integer) = 3
        _ShadowHardness ("Shaow Hardness", Range(0,1)) = 1
        _ShadowOffset ("Shadow Offset", Float) = 0
        [Space(5)]
        [Toggle(_SMOOTH_NORMAL_SHADOW)] _SmoothNormalShadow("Use Smoothed Normal", Integer) = 0
        [Space(5)]
        _ShadowNormalThreshold ("Normal Threshold", Range(0,1)) = 0.2
        _ShadowNormalTransition ("Normal Transition", Range(0.0001,1)) = 0.2

        [Header(Specular)]
        [Space(10)]
        [Toggle(_SHOW_SPECULAR)] _ShowSpecular("Show Specular", Integer) = 0
        [Toggle(_SMOOTH_NORMAL_SPECULAR)] _SmoothNormalSpecular("Use Smoothed Normal", Integer) = 0
        [HDR] _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        [PowerSlider(1.001)]_SpecularRange ("Specular Range", Range(0.001, 0.5)) = 1
        _SpecularHardness ("Specular Hardness", Range(0,1)) = 0.1
        _SpecularThreshold ("Specular Threshold", Float) = 0.5
        _SpecularOffset ("Specular Offset", Float) = 0

        [Header(Spot)]
        [Space(10)]
        [Toggle(_SHOW_SPOTS)] _ShowSpot ("Show Spot", Integer) = 1
        [HDR] _SpotColor ("Spot Color", Color) = (1,1,1)
        _SpotRadius ("Spot Radius", Float) = 1
        _SpotDensity ("Spot Density", Float) = 1
        _SpotOffset ("Spot Offset", Float) = 0
        _UVScale("Scale", Vector) = (1,1,0,0)
        _SpotSpeed("Speed", Vector) = (0,0,0,0)

        [Header(Fresnel)]
        [Space(10)]
        [Toggle(_SHOW_FRESNEL)] _ShowFresnel ("Show Fresnel", Integer) = 0
        [HDR] _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
        _FresnelRange ("Fresnel Range", Float) = 0.5
        _FresnelStrength ("Fresnel Srtength", Float) = 0
        _FadeWithShadow ("Fade With Shadow", Range(0,1)) = 1
        _FadeWithDiffuse ("Fade With Diffuse", Range(0,1)) = 0

        [Header(Other Settings)]
        [Space(10)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Culling Mode", Float) = 3

        [Space(10)]
        [Toggle(_G_BUFFER)]_GBuffer("Using Deffered Rendering", Float) = 0
        [Toggle(_DEPTH_TEXTURE)]_DepthTexture("Write to Depth Texture", Float) = 0

    }

    SubShader
    {

        Tags
        {
            "RenderType"="Opaque"
        }

        LOD 100


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

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            ZWrite On
            ColorMask 0
            Cull Front
            HLSLPROGRAM
            #pragma shader_feature _DEPTH_TEXTURE
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            #pragma target 2.0
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsPass"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag(Varyings input) : SV_TARGET
            {
                float3 normalWS = normalize(input.normalWS);
                return float4(normalWS * 0.5 + 0.5, 1.0); // Encode normal to [0,1]
            }
            ENDHLSL
        }

        Pass
        {
            Name "Base"

            Tags
            {
                "LightMode" = "UniversalGBuffer"
                "LightMode" = "UniversalForwardBase"
                "Queue" = "Geometry"
            }

            ZTest [_ZTest]
            ZWrite On

            Cull [_CullMode]

            Stencil
            {
                Ref [_StencilVal]
                Comp Always
                Pass Replace
            }


            HLSLPROGRAM
            #pragma fragment frag
            #pragma vertex vert

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Resources/Shaders/Common/CommonShaderMethods.hlsl"

            #pragma multi_compile  _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ADDITIONAL_LIGHT_SHADOWS
            // #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #define ADDITIONAL_LIGHT_CALCULATE_SHADOWS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _SHADOWS_SOFT
            #pragma shader_feature _SHOW_SPOTS
            #pragma shader_feature _RCV_SHADOW
            #pragma shader_feature _SHOW_FRESNEL
            #pragma shader_feature _SHOW_SPECULAR
            #pragma shader_feature _USE_NORMAL_MAP
            #pragma shader_feature _UNPACK_NORMAL
            #pragma shader_feature _PER_VERTEX_NORMAL
            #pragma shader_feature _SMOOTH_NORMAL_DIFFUSE
            #pragma shader_feature _SMOOTH_NORMAL_SPECULAR
            #pragma shader_feature _SMOOTH_NORMAL_SHADOW
            #pragma shader_feature _G_BUFFER _DEPTH_TEXTURE _


            float3 _Color;
            float3 _DiffuseColor;
            float3 _RotateNormal, _NormalOffset;
            float3 _FresnelColor;
            float4 _SpotColor;
            float4 _ShadowColor;
            float4 _SpecularColor;
            float2 _UVScale, _SpotSpeed;
            float _NormalMapStrength;
            float _SpotRadius, _SpotDensity, _SpotOffset;
            float _DiffuseOffset, _DiffuseSteps, _DiffuseHardness, _DiffuseStrength, _DiffuseRange;
            float _FresnelStrength, _SpotFresnelStrength, _FresnelRange, _FadeWithShadow, _FadeWithDiffuse;
            float _ShadowSteps, _ShadowOffset, _ShadowNormalThreshold, _ShadowNormalTransition, _ShadowHardness;
            float _SpecularRange, _SpecularOffset, _SpecularThreshold, _SpecularHardness;
            float _Emission, _LightEffect;

            bool _NormalDebug;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            #if _USE_NORMAL_MAP
			bool _ObjectSpaceNormalMap;
			sampler2D _NormalMap;
			float4 _NormalMap_ST;
            #endif

            struct appdata
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
                float2 uv6 : TEXCOORD6;
                float2 uv7 : TEXCOORD7;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 smoothedNormal : TEXCOORD2;
                //float4 shadowCoord : TEXCOORD3;
                #if _USE_NORMAL_MAP
                #ifndef _PER_VERTEX_NORMAL
				float2 normaluv : TEXCOORD4;
                #endif
                #endif
                float3 normal : TEXCOORD5;
            };

            struct GBufferOut
            {
                half4 albedo : SV_Target0;
                half4 specular : SV_Target1;
                half3 normal : SV_Target2;
                half3 emission : SV_Target3;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                VertexPositionInputs positions = GetVertexPositionInputs(v.vertex.xyz);

                // o.shadowCoord = TransformWorldToShadowCoord(o.worldPos);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.tangent = v.tangent;
                #if _USE_NORMAL_MAP
                #if _PER_VERTEX_NORMAL
				float2 normalUv = TRANSFORM_TEX(v.texcoord, _NormalMap);

                #if _UNPACK_NORMAL
				float3 normalMapCol = tex2Dlod(_NormalMap, float4(normalUv, 0, 0)) * 2 - 1;
                #else
				float3 normalMapCol = tex2Dlod(_NormalMap, float4(normalUv, 0, 0));
                #endif

				if (!_ObjectSpaceNormalMap){
					normalMapCol = TangentToObjectNormal(normalize(normalMapCol), v.normal, v.tangent.xyz);
				}

				o.normal = lerp(v.normal, normalMapCol, _NormalMapStrength);

				o.normal = Rotate(o.normal, _RotateNormal) + _NormalOffset;
                #else
				o.normal = v.normal;
				o.normaluv = TRANSFORM_TEX(v.texcoord, _NormalMap);
				o.smoothedNormal = ConvertNormal(float3(v.uv6.xy, v.uv7.x), v.tangent.xyz, Rotate(o.normal, _RotateNormal) + _NormalOffset);
                #endif
                #else
                o.normal = v.normal;
                o.normal = Rotate(o.normal, _RotateNormal) + _NormalOffset;
                o.smoothedNormal = ConvertNormal(float3(v.uv6.xy, v.uv7.x), v.tangent.xyz, o.normal);
                #endif
                return o;
            }

            #ifdef _G_BUFFER
			GBufferOut frag(v2f i){
            #else
            half3 frag(v2f i) : SV_Target
            {
                #endif

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float4 texColor = tex2D(_MainTex, i.uv);

                #if _USE_NORMAL_MAP
                #ifndef _PER_VERTEX_NORMAL
                #if _UNPACK_NORMAL
				float3 normalMapCol = tex2D(_NormalMap, i.normaluv) * 2 - 1;
                #else
				float3 normalMapCol = tex2D(_NormalMap, i.normaluv);
                #endif

				if (!_ObjectSpaceNormalMap){
					normalMapCol = TangentToObjectNormal(normalMapCol, i.normal, i.tangent);
				}

				i.normal = lerp(i.normal, normalMapCol, _NormalMapStrength);

				i.normal = Rotate(i.normal, _RotateNormal);

				i.normal += _NormalOffset;

                #endif
                #endif

                i.normal = TransformObjectToWorldNormal(i.normal);
                i.smoothedNormal = TransformObjectToWorldNormal(i.smoothedNormal);


                #if _SMOOTH_NORMAL_DIFFUSE
                float diffuse = saturate(dot(lightDir, i.smoothedNormal));
                #else
                float diffuse = saturate(dot(lightDir, i.normal));
                #endif

                float finalDiffuse;

                float shadow = 0;
                float baseDiffuse = saturate((dot(lightDir, i.normal) + 1) / 2);
                #if _RCV_SHADOW
                #if _SMOOTH_NORMAL_SHADOW
				baseDiffuse = saturate((dot(lightDir, i.smoothedNormal) + 1) / 2);
                #endif
                float4 shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                //return MainLightRealtimeShadow(shadowCoord);
                shadow = Posterize(saturate(1 - MainLightRealtimeShadow(shadowCoord) + _ShadowOffset), _ShadowSteps, _ShadowHardness);
                shadow *= saturate((baseDiffuse - _ShadowNormalThreshold) / _ShadowNormalTransition);
                diffuse *= 1 - shadow * (_ShadowColor.a);
                #endif
                finalDiffuse = saturate(pow(Posterize(pow(diffuse + _DiffuseOffset / 10, 1 / _DiffuseRange), _DiffuseSteps, _DiffuseHardness),
                                            _DiffuseStrength));


                //Calculate Basic Color
                float3 col = _Color;

                col = Overlay(col, texColor.xyz * col, texColor.a);

                //Apply Diffuse
                col = Overlay(col, _DiffuseColor * texColor * finalDiffuse, 1 - finalDiffuse);

                #if _SHOW_SPOTS
                float2 worldPos = (i.uv + _Time.x * _SpotSpeed) / _UVScale;
                float spotDensity = _SpotDensity * 100;
                float3 spot = length(floor((worldPos + 1 / spotDensity / 2) * spotDensity) / spotDensity - worldPos) > _SpotRadius / spotDensity *
                saturate((0.5 - diffuse + _SpotOffset));
                spot = !spot;

                col = Overlay(col, spot * texColor * _Emission * _SpotColor, spot > 0);
                #endif

                #if _SHOW_FRESNEL
				float fresnel = _FresnelStrength * saturate(pow(1 - dot(normalize(viewDir), normalize(i.normal)), abs(1 / _FresnelRange)));
				col = Overlay(col, _FresnelColor, fresnel * lerp(1, baseDiffuse, _FadeWithDiffuse) * lerp(1, 1 - shadow, _FadeWithShadow));
                #endif

                #if _SHOW_SPECULAR
				float4 specular = _SpecularColor;
				_SpecularHardness = (1 - _SpecularHardness) / 2;
				_SpecularRange = 1 / _SpecularRange;
                #if _SMOOTH_NORMAL_SPECULAR
                specular.a *= smoothstep(_SpecularThreshold - _SpecularHardness, _SpecularThreshold + _SpecularHardness, pow(1 - (1 + dot(reflect(viewDir, i.smoothedNormal), lightDir)) / 2 + _SpecularOffset, _SpecularRange));
                #else
				specular.a *= smoothstep(_SpecularThreshold - _SpecularHardness, _SpecularThreshold + _SpecularHardness,
										pow(1 - (1 + dot(reflect(viewDir, i.normal), lightDir)) / 2 + _SpecularOffset, _SpecularRange));
                #endif
				col = Overlay(col, specular.rgba, specular.a * (1 - shadow));
                #endif

                if (_LightEffect > 0){
                    Light l = GetMainLight();
                    float3 ambient = GetAmbientColor();
                    float3 lightCol = l.color;
                    for (int idx = 0; idx < GetAdditionalLightsCount(); idx++){
                        Light light = GetAdditionalLight(idx, i.worldPos);
                        float3 ldir = normalize(light.direction);
                        float NdotL = saturate(abs(dot(TransformObjectToWorldNormal(i.normal), ldir)));
                        lightCol += light.color * NdotL * light.distanceAttenuation * light.shadowAttenuation;
                    }
                    float3 color = lightCol + ambient;
                    color = lerp(color, Brightness(color), saturate(0.6 - baseDiffuse * (1.5 - shadow)));

                    col *= lerp(float3(1, 1, 1), color, _LightEffect);
                }

                #ifdef _G_BUFFER
				GBufferOut o;
				o.albedo = 0;
				o.normal = i.normal;
				o.specular = 0;
				o.emission = col;
				return o;
                #else
                return col;
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "Inner Outline"

            Tags
            {
                "LightMode" = "InnerOutline"
                "RenderMode" = "Transparent"
            }

            Blend SrcAlpha OneMinusSrcAlpha

            ZTest Less
            ZWrite On

            Stencil
            {
                Ref [_StencilVal]
                Comp [_InnerStencilComp]
            }

            Offset [_InnerZOffset], 1

            Cull Front

            HLSLPROGRAM
            #pragma shader_feature _SHOW_OUTLINE_INNER
            #pragma shader_feature _USE_SMOOTHED_NORMAL


            #if _SHOW_OUTLINE_INNER
			#pragma vertex vert
			#pragma fragment frag

			#include "Assets/Resources/Shaders/Common/CommonShaderMethods.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _InnerOutlineColor;
			float _InnerWidth, _Emission, _LightEffectOnOutline;

			struct appdata{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float2 uv6 : TEXCOORD6;
				float2 uv7 : TEXCOORD7;
			};

			struct v2f{
				float3 normal : TEXCOORD1;
				float4 position : POSITION;
			};


			v2f vert(appdata v){
				_InnerWidth /= 150;
				v2f o;
				o.position = TransformObjectToHClip(v.vertex);
				float dist = o.position.w;
				half3 scaleFactor = float3(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21),
											length(unity_ObjectToWorld._m02_m12_m22));

				float fov = 2 * atan(-1 / UNITY_MATRIX_P._m11);

				o.position = v.vertex;
            #if _USE_SMOOTHED_NORMAL
				float4 decoded = float4(v.uv6, v.uv7);
				float3 smoothedNormal = ConvertNormal(decoded.xyz, v.tangent.xyz, v.normal);
				o.position.xyz += smoothedNormal * _InnerWidth * pow(dist, 0.3) / scaleFactor * decoded.w * pow(fov, 0.7);

            #else
				o.position.xyz += normalize(v.normal) * _InnerWidth * pow(dist, 0.3) / scaleFactor * pow(fov, 0.7) / 2;
            #endif
				o.position = TransformObjectToHClip(o.position);

				return o;
			}

			float4 frag(v2f i) : SV_Target{
				if (_LightEffectOnOutline > 0){
					_InnerOutlineColor *= float4(lerp(float3(1, 1, 1), GetMainLight().color, _LightEffectOnOutline), 1);
				}
				return _InnerOutlineColor;
			}

            #else
            void vert()
            {
            }

            void frag()
            {
                discard;
            }
            #endif
            ENDHLSL
        }

        Pass
        {
            Name "Outer Outline"

            ZTest Less
            ZWrite On

            Offset [_OuterZOffset], 1


            Stencil
            {
                Ref [_StencilVal]
                Comp [_OuterStencilComp]
            }

            Tags
            {
                "LightMode" = "OuterOutline"
            }

            Cull Front

            HLSLPROGRAM
            #pragma shader_feature _SHOW_OUTLINE_OUTER
            #pragma shader_feature _USE_SMOOTHED_NORMAL

            #pragma vertex vert
            #pragma fragment frag

            #if _SHOW_OUTLINE_OUTER

			#include "Assets/Resources/Shaders/Common/CommonShaderMethods.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _OuterOutlineColor, _InnerOutlineColor;
			float _OuterWidth, _InnerWidth, _Emission, _LightEffectOnOutline;


			struct appdata{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float2 uv6 : TEXCOORD6;
				float2 uv7 : TEXCOORD7;
			};

			struct v2f{
				float4 position : POSITION;
			};

			v2f vert(appdata v){
				_OuterWidth /= 150;
				v2f o;
				float4 clipPos = TransformObjectToHClip(v.vertex);
				float dist = clipPos.w;
				half3 scaleFactor = float3(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21),
											length(unity_ObjectToWorld._m02_m12_m22));

				float fov = 2 * atan(-1 / UNITY_MATRIX_P._m11);

				o.position = v.vertex;

            #if _USE_SMOOTHED_NORMAL
				float4 decoded = float4(v.uv6, v.uv7);
				float3 smoothedNormal = ConvertNormal(decoded.xyz, v.tangent.xyz, v.normal);
				o.position.xyz += smoothedNormal * _OuterWidth * pow(dist, 0.3) / scaleFactor * decoded.w * pow(fov, 0.7);
            #else
				o.position.xyz += normalize(v.normal) * _OuterWidth * pow(dist, 0.3) / scaleFactor * pow(fov, 0.7) / 2;
            #endif

				o.position = TransformObjectToHClip(o.position);

				return o;
			}

			float3 frag(v2f i) : SV_Target{
				if (_LightEffectOnOutline > 0){
					_OuterOutlineColor *= float4(lerp(float3(1, 1, 1), GetMainLight().color, _LightEffectOnOutline), 1);
				}
				return _OuterOutlineColor;
			}
            #else
            void vert()
            {
            }

            void frag()
            {
            }
            #endif
            ENDHLSL
        }




    }
    Fallback "Universal Render Pipeline/Lit"
}