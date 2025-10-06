Shader "Custom/DitherEffectShader"
{
    Properties
    {
        _StepThreshold ("Step Threshold", Float) = 0.2
        _DitherScale ("Dither Scale", Float) = 0.5
        _ColorA ("Color A", Color) = (0.2,0.2,0.2,1)
        _ColorB ("Color B", Color) = (0.7,0.7,0.7,1)


    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _DitherScale, _StepThreshold;
            half4 _ColorA, _ColorB;

            float Dither(float value, float2 uv)
            {
                // Define a 4x4 Bayer matrix
                const float4x4 bayerMatrix = {
                    0.0 / 16.0, 8.0 / 16.0, 2.0 / 16.0, 10.0 / 16.0,
                    12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0, 6.0 / 16.0,
                    3.0 / 16.0, 11.0 / 16.0, 1.0 / 16.0, 9.0 / 16.0,
                    15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0, 5.0 / 16.0
                };

                // Scale UV coordinates to the size of the Bayer matrix
                float2 scaledUV = frac(uv * 4.0);
                int2 index = int2(scaledUV * 4.0);

                // Retrieve the threshold from the Bayer matrix
                float threshold = bayerMatrix[index.y][index.x];

                // Apply dithering
                return value - threshold;
            }


            half4 frag(Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.texcoord);
                half brightness = Luminance(col);
                float dither = Dither(brightness, i.texcoord / float2(_ScreenSize.y / _ScreenSize.x, 1) / _DitherScale);
                return lerp(_ColorA, _ColorB, smoothstep(_StepThreshold-0.1, _StepThreshold+0.1, dither));
                // return col;
            }
            ENDHLSL
        }
    }
}