Shader "Custom/SimpleBlit"
{
    Properties
    {
        _Alpha ("Alpha", Range(0,1)) = 1
    }
    SubShader
    {

        Pass
        {

            Name "Blit"
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Alpha;

            float4 frag(Varyings i) : SV_Target
            {
                // return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, i.texcoord);
                float4 texCol = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearRepeat, i.texcoord);
                // return 0;
                return float4(texCol.rgb, saturate(texCol.a) * _Alpha);
            }
            ENDHLSL
        }
    }
}