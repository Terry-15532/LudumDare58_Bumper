Shader "Custom/SpriteFill"
{
    Properties
    {
        [HDR][MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _FillAmount("Fill Amount", Range(0,1)) = 1
        _FillClockwise("Fill Clockwise (0=CCW,1=CW)", Range(0,1)) = 0
        _FillOrigin("Fill Origin (Degrees)", Float) = 90
        _FillSoftness("Fill Softness", Range(0,1)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _FillAmount;
                float _FillClockwise;
                float _FillOrigin;
                float _FillSoftness;
            CBUFFER_END

            // Compute clockwise mask: returns value in [0,1]
            static inline float ComputeMaskCW(float rel, float fillAngle, float softDeg)
            {
                if (fillAngle <= 0.0001) return 0.0;
                if (fillAngle >= 359.9999) return 1.0;
                float start = 360.0 - fillAngle;
                float m = smoothstep(start, min(start + softDeg, 360.0), rel);
                if (start + softDeg > 360.0)
                {
                    float wrapEnd = start + softDeg - 360.0;
                    m = max(m, smoothstep(0.0, wrapEnd, rel));
                }
                return clamp(m, 0.0, 1.0);
            }

            // Compute counter-clockwise mask: returns value in [0,1]
            static inline float ComputeMaskCCW(float rel, float fillAngle, float softDeg)
            {
                if (fillAngle <= 0.0001) return 0.0;
                if (fillAngle >= 359.9999) return 1.0;
                float start = max(0.0, fillAngle - softDeg);
                float m = 1.0 - smoothstep(start, fillAngle, rel);
                return clamp(m, 0.0, 1.0);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample texture
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // Center UV around (0.5,0.5)
                float2 centered = IN.uv - float2(0.5, 0.5);

                // If outside the unit circle we still allow (for rectangular sprites)
                // compute angle in degrees [0,360)
                float ang = degrees(atan2(centered.y, centered.x));
                if (ang < 0.0) ang += 360.0;

                // Fill parameters
                float fill = saturate(_FillAmount);
                float fillAngle = saturate(fill) * 360.0;
                // Normalize origin to [0,360)
                float origin = fmod(_FillOrigin, 360.0);
                if (origin < 0.0) origin += 360.0;

                // Compute relative angle from origin measured CCW in degrees [0,360)
                float rel = ang - origin;
                rel = fmod(rel + 360.0, 360.0);

                // Softness in degrees (map _FillSoftness [0,1] to 0..30 degrees max)
                float softDeg = saturate(_FillSoftness) * 30.0;

                // Compute masks via helpers (functions always return a value)
                float maskCW = ComputeMaskCW(rel, fillAngle, softDeg);
                float maskCCW = ComputeMaskCCW(rel, fillAngle, softDeg);
                float useCW = step(0.5, _FillClockwise);
                float mask = lerp(maskCCW, maskCW, useCW);

                // Guarantee mask is in [0,1]
                mask = clamp(mask, 0.0, 1.0);

                // Combine texture alpha and mask
                half finalAlpha = tex.a * mask * _BaseColor.a * IN.color.a;
                half3 finalColor = tex.rgb * _BaseColor.rgb * IN.color.rgb;

                return half4(finalColor * finalAlpha, finalAlpha);
            }

            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
