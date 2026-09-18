Shader "Unlit/SH_CameraEffect"
{
    Properties
    {
        _BlurAmount ("Blur Amount", Range(0,1)) = 0
        _ChromaAmount ("Chromatic Aberration", Range(0,0.05)) = 0
        _VignetteAmount ("Vignette", Range(0,1)) = 0
        _PixelAmount("Pixel Amount", Range(0, 1)) = 0
        _PixelCount("Pixel Count", Range(1, 852)) = 128
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Name "SpeedDistortion"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurAmount;
            float _ChromaAmount;
            float _VignetteAmount;
            float _PixelAmount;
            float _PixelCount;

            float3 SampleBlurred(float2 uv, float2 dir, float dist)
            {
                float3 col = float3(0, 0, 0);
                const int SAMPLES = 8;
                [unroll]
                for (int s = 0; s < SAMPLES; s++)
                {
                    float scale = 1.0 - _BlurAmount * dist * (float(s) / SAMPLES) * 0.15;
                    float2 sample_uv = float2(0.5, 0.5) + (uv - float2(0.5, 0.5)) * scale;
                    col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sample_uv).rgb;
                }
                return col / SAMPLES;
            }

            float2 Pixelate(float2 uv)
            {
                float2 texel_size = _BlitTexture_TexelSize.zw;
                float aspect = texel_size.x / texel_size.y;

                float2 grid_size = float2(_PixelCount, _PixelCount / aspect);
                float2 pixUV = floor(uv * grid_size) / grid_size + (0.5 / grid_size);

                return lerp(uv, pixUV, _PixelAmount);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = Pixelate(input.texcoord);
                float2 center = float2(0.5, 0.5);
                float2 dir = uv - center;
                float dist = length(dir);
                float2 dirN = dist > 0.0001 ? dir / dist : float2(0, 0);

                // Radial blur, sampled per-channel with chromatic offset
                float2 chromaOffset = dirN * _ChromaAmount * dist;

                float3 colR = SampleBlurred(uv + chromaOffset, dirN, dist);
                float3 colG = SampleBlurred(uv, dirN, dist);
                float3 colB = SampleBlurred(uv - chromaOffset, dirN, dist);

                float3 col = float3(colR.r, colG.g, colB.b);

                // Vignette
                float vig = smoothstep(0.8, 0.2, dist * (1.0 + _VignetteAmount));
                col *= lerp(1.0, vig, _VignetteAmount);

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
