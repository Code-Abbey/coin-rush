// Coin Rush - hand-written HLSL skybox for URP.
// Three-colour vertical gradient + soft sun disc that follows the scene's main light
// + optional procedural twinkling stars (used by the Frozen Night world).
// Shader Graph can't target skyboxes in URP, so this one is written by hand.
Shader "CoinRush/GradientSkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.25, 0.5, 0.9, 1)
        _HorizonColor ("Horizon Color", Color) = (0.75, 0.88, 1, 1)
        _BottomColor ("Bottom Color", Color) = (0.35, 0.4, 0.45, 1)
        _Exponent ("Gradient Exponent", Range(0.1, 4)) = 1.2
        [Header(Sun)]
        _SunColor ("Sun Color", Color) = (1, 0.95, 0.8, 1)
        _SunSize ("Sun Size", Range(0.001, 0.2)) = 0.04
        _SunGlow ("Sun Glow", Range(0, 2)) = 0.6
        [Header(Stars)]
        _StarDensity ("Star Density", Range(0, 1)) = 0
        _StarBrightness ("Star Brightness", Range(0, 4)) = 1.5
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half _Exponent;
                half4 _SunColor;
                half _SunSize;
                half _SunGlow;
                half _StarDensity;
                half _StarBrightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDirWS  : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // A skybox mesh is centred on the camera, so object position == view direction.
                output.viewDirWS = TransformObjectToWorldDir(input.positionOS.xyz);
                return output;
            }

            // Cheap hash used to scatter stars on a grid of directions.
            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.viewDirWS);
                float h = dir.y;

                // Sky gradient: horizon -> top above, horizon -> bottom below.
                half3 sky = h >= 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), _Exponent))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-h * 3.0), 0.6));

                // Sun disc + glow, positioned by the main directional light.
                float3 sunDir = normalize(_MainLightPosition.xyz);
                float sunDot = saturate(dot(dir, sunDir));
                half disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.6, sunDot);
                half glow = pow(sunDot, 64.0) * _SunGlow;
                sky += _SunColor.rgb * (disc + glow);

                // Stars: only above the horizon, twinkling over time.
                if (_StarDensity > 0.001 && h > 0)
                {
                    float3 cell = floor(dir * 180.0);
                    float rnd = Hash13(cell);
                    float star = step(1.0 - _StarDensity * 0.02, rnd);
                    float twinkle = 0.6 + 0.4 * sin(_Time.y * (2.0 + rnd * 6.0) + rnd * 40.0);
                    sky += star * twinkle * _StarBrightness * saturate(h * 4.0);
                }

                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
