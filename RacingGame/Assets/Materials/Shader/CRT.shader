Shader "Hidden/CRT"
{
    Properties
    {
        _CurvatureAmount     ("Curvature Amount", Range(0, 0.5))     = 0.08
        _ScanlineIntensity   ("Scanline Intensity", Range(0, 1))     = 0.35
        _ScanlineCount       ("Scanline Count", Range(1, 1500))     = 540
        _ScanlineSpeed       ("Scanline Roll Speed", Range(-5, 5))   = 0
        _VignetteIntensity   ("Vignette Intensity", Range(0, 2))     = 0.6
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.02)) = 0.0025
        _Brightness          ("Brightness Boost", Range(0.5, 10))     = 1.1
        _MaskIntensity       ("RGB Mask Intensity", Range(0, 1))     = 0.25
        _MaskScale           ("RGB Mask Scale (px)", Range(1, 10))   = 3
        _NoiseAmount         ("Noise Amount", Range(0, 0.2))         = 0.03
        _FlickerAmount       ("Flicker Amount", Range(0, 0.2))       = 0.02
        _VignetteRoundness   ("Vignette Roundness", Range(0.5, 2))   = 1.2

        _CornerRadius        ("Corner Radius", Range(0, 0.3))        = 0.06
        _CornerSmoothness    ("Corner Smoothness", Range(0.0001, 0.05)) = 0.008

        _OutsideColor        ("Outside Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "CRTPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // _BlitTexture and sampler are declared by Blit.hlsl.
            // Full Screen Pass Renderer Feature binds the camera color to _BlitTexture.

            float _CurvatureAmount;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _VignetteIntensity;
            float _ChromaticAberration;
            float _Brightness;
            float _MaskIntensity;
            float _MaskScale;
            float _NoiseAmount;
            float _FlickerAmount;
            float _VignetteRoundness;

            float _CornerRadius;
            float _CornerSmoothness;

            half4 _OutsideColor;

            // Cheap hash-based noise, good enough for grain/flicker.
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Barrel-distort UVs around the center to fake a curved CRT tube.
            float2 CurveUV(float2 uv, float amount)
            {
                uv = uv * 2.0 - 1.0;              // -1..1
                float2 offset = uv.yx * uv.yx * amount;
                uv += uv * offset;
                uv = uv * 0.5 + 0.5;              // back to 0..1
                return uv;
            }

            float RoundedBoxSDF(float2 p, float2 half_size, float radius)
            {
                float2 q = abs(p) - half_size + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }
             
            float RoundedScreenMask(float2 uv, float radius, float smoothness)
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = (uv - 0.5) * float2(aspect, 1.0);
                float2 half_size = float2(0.5 * aspect, 0.5); // full extent — no subtraction here
                float dist = RoundedBoxSDF(p, half_size, radius);

                return 1.0 - smoothstep(0.0, max(smoothness, 1e-5), dist);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 curvedUV = CurveUV(uv, _CurvatureAmount);

                // Outside the curved screen bounds -> pure black bezel.
                if (curvedUV.x < 0.0 || curvedUV.x > 1.0 || curvedUV.y < 0.0 || curvedUV.y > 1.0)
                {
                    return half4(_OutsideColor);
                }

                float cornerMask = RoundedScreenMask(curvedUV, _CornerRadius, _CornerSmoothness);

                // Chromatic aberration: sample R/G/B at slightly different offsets,
                // pushed outward from screen center.
                float2 center = curvedUV - 0.5;
                float2 caDir = normalize(center + 1e-5) * _ChromaticAberration * length(center) * 2.0;

                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, curvedUV - caDir).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, curvedUV).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, curvedUV + caDir).b;
                half3 col = half3(r, g, b);

                // Scanlines, optionally rolling over time.
                float scanY = curvedUV.y * _ScanlineCount + _Time.y * _ScanlineSpeed;
                float scanline = 0.5 + 0.5 * cos(scanY * TWO_PI);
                col *= lerp(1.0, scanline, _ScanlineIntensity);

                // Fake aperture-grille RGB mask based on horizontal pixel position.
                float maskX = frac((curvedUV.x * _ScreenParams.x) / _MaskScale);
                half3 maskColor = half3(1, 1, 1);
                if (maskX < 0.333) maskColor = half3(1.2, 0.7, 0.7);
                else if (maskX < 0.666) maskColor = half3(0.7, 1.2, 0.7);
                else maskColor = half3(0.7, 0.7, 1.2);
                col *= lerp(1.0, maskColor, _MaskIntensity);

                // Vignette, darkening toward the rounded edges.
                float2 vc = center * float2(1.0, _VignetteRoundness);
                float vig = 1.0 - dot(vc, vc) * _VignetteIntensity;
                col *= saturate(vig);

                // Subtle grain + brightness flicker for that analog "alive" feel.
                float n = hash21(uv * _ScreenParams.xy + _Time.y * 60.0);
                col += (n - 0.5) * _NoiseAmount;

                float flicker = hash21(float2(_Time.y * 10.0, 0.0));
                col *= 1.0 + (flicker - 0.5) * _FlickerAmount;

                col *= _Brightness;
                col = lerp(_OutsideColor.rgb, col, cornerMask);

                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
}
