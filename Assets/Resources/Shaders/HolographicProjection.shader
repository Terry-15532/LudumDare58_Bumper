Shader "Custom/HolographicProjection"
{
    Properties
    {
        [Header(Holographic Settings)]
        [Space(10)]
        [HDR] _HoloColor ("Holographic Color", Color) = (0, 2, 4, 1)
        _Alpha ("Base Alpha", Range(0, 1)) = 0.5

        [Header(Dither Dissolve Settings)]
        [Space(10)]
        _DitherScale ("Dither Scale", Range(0, 1)) = 50
        _DitherNoiseScale ("Noise Scale", Range(0.1, 50)) = 10
        _DitherSmoothness ("Dissolve Smoothness", Range(0, 0.5)) = 0.1
        _DissolveEdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
        [HDR] _DissolveEdgeColor ("Edge Color", Color) = (0, 4, 8, 1)

        [Header(Scanline Effect)]
        [Space(10)]
        _ScanlineSpeed ("Scanline Speed", Float) = 1
        _ScanlineFrequency ("Scanline Frequency", Float) = 50
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.3

        [Header(Fresnel Effect)]
        [Space(10)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 1

        [Header(Flicker Effect)]
        [Space(10)]
        _FlickerSpeed ("Flicker Speed", Float) = 5
        _FlickerIntensity ("Flicker Intensity", Range(0, 1)) = 0.1

        [Header(Other Settings)]
        [Space(10)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Culling Mode", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
//            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100

//        Pass
//        {
//            Name "DepthOnly"
//            Tags
//            {
//                "LightMode" = "DepthOnly"
//            }
//            ZWrite On
//            ColorMask 0
//            Cull Off
//            HLSLPROGRAM
//            #pragma shader_feature _DEPTH_TEXTURE
//            #pragma vertex DepthOnlyVertex
//            #pragma fragment DepthOnlyFragment
//
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
//            #pragma target 2.0
//            ENDHLSL
//        }
//
//        Pass
//        {
//            Name "DepthNormalsPass"
//            Tags
//            {
//                "LightMode" = "DepthNormals"
//            }
//
//            ZWrite On
//            ColorMask 0
//            Cull Off
//
//            HLSLPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//
//            struct Attributes
//            {
//                float4 positionOS : POSITION;
//                float3 normalOS : NORMAL;
//            };
//
//            struct Varyings
//            {
//                float4 positionHCS : SV_POSITION;
//                float3 normalWS : TEXCOORD0;
//            };
//
//            Varyings vert(Attributes input)
//            {
//                Varyings output;
//                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
//                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
//                return output;
//            }
//
//            float4 frag(Varyings input) : SV_TARGET
//            {
//                float3 normalWS = normalize(input.normalWS);
//                return float4(normalWS * 0.5 + 0.5, 1.0); // Encode normal to [0,1]
//            }
//            ENDHLSL
//        }

        Pass
        {
            Name "Holographic"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Stencil
            {
                Ref 114
                Comp NotEqual
                Fail Keep
                Pass Replace
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Common/CommonShaderMethods.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _HoloColor;
                float _DitherScale;
                float _DitherNoiseScale;
                float _DitherSmoothness;
                float _DissolveEdgeWidth;
                float4 _DissolveEdgeColor;
                float _Alpha;
                float _ScanlineSpeed;
                float _ScanlineFrequency;
                float _ScanlineIntensity;
                float _FresnelPower;
                float _FresnelIntensity;
                float _FlickerSpeed;
                float _FlickerIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
            };

            // 改进的8x8 Bayer矩阵，提供更平滑的dither效果
            float BayerDither8x8(float2 screenPos)
            {
                float2 ditherCoord = frac(screenPos / 8.0) * 8.0;
                int x = int(ditherCoord.x);
                int y = int(ditherCoord.y);

                // 8x8 Bayer矩阵提供64个不同的阈值
                const float bayerMatrix[8][8] = {
                    {0.0 / 64.0, 32.0 / 64.0, 8.0 / 64.0, 40.0 / 64.0, 2.0 / 64.0, 34.0 / 64.0, 10.0 / 64.0, 42.0 / 64.0},
                    {48.0 / 64.0, 16.0 / 64.0, 56.0 / 64.0, 24.0 / 64.0, 50.0 / 64.0, 18.0 / 64.0, 58.0 / 64.0, 26.0 / 64.0},
                    {12.0 / 64.0, 44.0 / 64.0, 4.0 / 64.0, 36.0 / 64.0, 14.0 / 64.0, 46.0 / 64.0, 6.0 / 64.0, 38.0 / 64.0},
                    {60.0 / 64.0, 28.0 / 64.0, 52.0 / 64.0, 20.0 / 64.0, 62.0 / 64.0, 30.0 / 64.0, 54.0 / 64.0, 22.0 / 64.0},
                    {3.0 / 64.0, 35.0 / 64.0, 11.0 / 64.0, 43.0 / 64.0, 1.0 / 64.0, 33.0 / 64.0, 9.0 / 64.0, 41.0 / 64.0},
                    {51.0 / 64.0, 19.0 / 64.0, 59.0 / 64.0, 27.0 / 64.0, 49.0 / 64.0, 17.0 / 64.0, 57.0 / 64.0, 25.0 / 64.0},
                    {15.0 / 64.0, 47.0 / 64.0, 7.0 / 64.0, 39.0 / 64.0, 13.0 / 64.0, 45.0 / 64.0, 5.0 / 64.0, 37.0 / 64.0},
                    {63.0 / 64.0, 31.0 / 64.0, 55.0 / 64.0, 23.0 / 64.0, 61.0 / 64.0, 29.0 / 64.0, 53.0 / 64.0, 21.0 / 64.0}
                };

                return bayerMatrix[y][x];
            }

            // 原神/鸣潮风格的溶解dither效果
            float DitherDissolve(float2 screenPos, float2 worldPos, float dissolveAmount)
            {
                // 使用噪声纹理添加有机感
                float noise = unity_gradientNoise(worldPos * _DitherNoiseScale);
                noise = noise * 0.5 + 0.5; // 转换到0-1范围

                // 8x8 Bayer矩阵
                float bayer = BayerDither8x8(screenPos * _DitherScale);

                // 混合噪声和Bayer矩阵，创建更自然的溶解效果
                float ditherPattern = lerp(bayer, noise, 0.3);

                // 创建溶解阈值，加上平滑度
                float dissolveEdge = dissolveAmount + (ditherPattern - 0.5) * _DitherSmoothness;

                return dissolveEdge;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // 屏幕空间坐标
                float2 screenPos = input.screenPos.xy / input.screenPos.w * _ScreenParams.xy;

                // Fresnel effect
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                fresnel *= _FresnelIntensity;

                // Scanline effect
                float4 objPos = TransformObjectToHClip(float3(0,0,0));
                float scanline = sin((screenPos.y * objPos.w + _Time.y * _ScanlineSpeed) * _ScanlineFrequency);
                scanline = scanline * 0.5 + 0.5;
                scanline = lerp(1.0, scanline, _ScanlineIntensity);

                // Flicker effect
                float flicker = sin(_Time.y * _FlickerSpeed) * 0.5 + 0.5;
                flicker = lerp(1.0, flicker, _FlickerIntensity);

                // 计算最终的透明度
                float alpha = _Alpha;
                alpha *= (1.0 + fresnel * 0.5) * flicker;

                // 原神风格的dither溶解效果
                float dissolve = DitherDissolve(screenPos, input.positionWS.xy, alpha);

                // 溶解边缘发光效果
                float edgeFactor = smoothstep(alpha - _DissolveEdgeWidth, alpha, dissolve) *
                (1.0 - smoothstep(alpha, alpha + _DissolveEdgeWidth, dissolve));

                // 如果溶解值小于alpha，丢弃像素
                clip(dissolve - alpha);

                // Combine effects
                float3 holoColor = _HoloColor.rgb;
                holoColor *= (1.0 + fresnel);

                // 添加溶解边缘发光
                holoColor += _DissolveEdgeColor.rgb * edgeFactor;

                return float4(holoColor, alpha * scanline);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}