//Shader "Custom/SSRDenoise"
//{
//    Properties
//    {
//        _BlurSize ("Blur Size", Range(0.0, 5.0)) = 1
//        _AlphaThreshold("Alpha Variance Threshold", Range(0, 1)) = 0.3
//        _DepthThreshold("Depth Variance Threshold", Range(0, 1)) = 0.4
//    }
//
//    SubShader
//    {
//        Pass
//        {
//            
//            Name "DenoisePass"
//            Blend One Zero
//            ZWrite Off
//            Cull Off
//            ZTest Always
//
//            HLSLPROGRAM
//            #pragma vertex Vert
//            #pragma fragment frag
//
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
//
//
//            TEXTURE2D(_CameraDepthTexture);
//            SAMPLER(sampler_CameraDepthTexture);
//            float _BlurSize, _AlphaThreshold, _DepthThreshold;
//            // bool _FlipY;
//
//            static const float weights[2] = {0.6, 0.4};
//
//            float4 frag(Varyings input) : SV_Target
//            {
//                float2 texelSize = _BlitTexture_TexelSize.xy * _BlurSize;
//                float4 finalColor = 0;
//                float totalWeight = 0;
//
//                float4 texCol = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
//                float centerDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord);
//
//                float alphaVariation = 0;
//                float depthVariation = 0;
//                float centerAlpha = step(0.001, texCol.a);
//
//                for (int x = -1; x <= 1; x++){
//                    for (int y = -1; y <= 1; y++){
//                        float2 offset = float2(x, y) * texelSize;
//                        float2 sampleUV = input.texcoord + offset;
//                        sampleUV.y = clamp(sampleUV.y, 0.001, 0.999);
//                        sampleUV.x = clamp(sampleUV.x, 0.001, 0.999);
//
//                        float4 sampleColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV);
//
//                        depthVariation += abs(centerDepth - SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, sampleUV));
//                        sampleColor.rgb *= sampleColor.a;
//
//                        alphaVariation += abs(centerAlpha - step(0.001, sampleColor.a));
//
//                        float weight = weights[abs(x)] * weights[abs(y)] * (1 - 0.7 * (1 - step(0.001, sampleColor.a)));
//
//                        finalColor += sampleColor * weight;
//                        totalWeight += weight;
//                    }
//                }
//
//                if (alphaVariation / 9 < _AlphaThreshold || depthVariation / 9 > _DepthThreshold / 1000){
//                    return texCol;
//                }
//
//                finalColor /= max(totalWeight, 1e-3);
//
//                finalColor.rgb = finalColor.rgb / max(finalColor.a, 1e-3);
//
//                return finalColor;
//            }
//            ENDHLSL
//        }
//    }
//}

Shader "Custom/SSRDenoise"
{
    Properties
    {
        //		_MainTex ("Main Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0.0, 5.0)) = 1
        _AlphaThreshold("Alpha Variance Threshold", Range(0, 1)) = 0.3
        _DepthThreshold("Depth Variance Threshold", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Pass
        {
            Name "DenoisePass"
            Blend One Zero
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // struct appdata{
            // 	float4 vertex : POSITION;
            // 	float2 uv : TEXCOORD0;
            // };
            //
            // struct v2f{
            // 	float4 position : SV_POSITION;
            // 	float2 uv : TEXCOORD0;
            // };

            // TEXTURE2D(_MainTex);
            TEXTURE2D(_CameraDepthTexture);
            // SAMPLER(sampler_MainTex);
            SAMPLER(sampler_CameraDepthTexture);
            float _BlurSize, _AlphaThreshold, _DepthThreshold;
            bool _FlipY;
            // float4 _MainTex_TexelSize;

            static const float weights[2] = {0.6, 0.4};
            //
            // float2 flipY(float2 pos){
            // 	return _FlipY ? float2(pos.x, 1.0 - pos.y) : pos;
            // }

            // v2f vert(appdata input){
            // 	v2f output;
            // 	output.position = TransformObjectToHClip(input.vertex.xyz);
            // 	output.uv = flipY(input.uv);
            // 	return output;
            // }

            float4 frag(Varyings input) : SV_Target
            {
                float2 texelSize = _BlitTexture_TexelSize.xy * _BlurSize;
                float4 finalColor = 0;
                float totalWeight = 0;

                float4 texCol = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                // return float4(texCol.rgb,1);
                float centerDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord);

                float alphaVariation = 0;
                float depthVariation = 0;
                float centerAlpha = step(0.00001, texCol.a);

                for (int x = -1; x <= 1; x++){
                    for (int y = -1; y <= 1; y++){
                        float2 offset = float2(x, y) * texelSize;
                        float2 sampleUV = input.texcoord + offset;
                        sampleUV.y = clamp(sampleUV.y, 0.001, 0.999);
                        sampleUV.x = clamp(sampleUV.x, 0.001, 0.999);

                        float4 sampleColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, sampleUV);

                        depthVariation += abs(centerDepth - SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, sampleUV));
                        sampleColor.rgb *= sampleColor.a;

                        alphaVariation += abs(centerAlpha - step(0.0000001, sampleColor.a));

                        float weight = weights[abs(x)] * weights[abs(y)] * (1 - 0.7 * (1 - step(0.00001, sampleColor.a)));

                        finalColor += sampleColor * weight;
                        totalWeight += weight;
                    }
                }

                // if (alphaVariation > _AlphaThreshold){
                //     return 0;
                // }
                if ((alphaVariation / 9 < _AlphaThreshold || depthVariation / 9 > _DepthThreshold / 1000)){
                    return texCol;
                }

                finalColor /= max(totalWeight, 1e-5);

                // finalColor.a = step(0.1, finalColor.a);

                finalColor.rgb = finalColor.rgb / max(finalColor.a, 1e-5);

                finalColor.a = step(0.3, finalColor.a) * finalColor.a;

                return finalColor;
            }
            ENDHLSL
        }
    }
}