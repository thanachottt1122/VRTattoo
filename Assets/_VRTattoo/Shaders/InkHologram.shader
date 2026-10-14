// Glowing see-through ring that shows how much ink is left (InkHologram). Drawn on a quad:
// the ring fills clockwise from the top up to _Fill, the empty part stays as a faint ghost.
// The glow is added on top of a faint dark backing disc so it stays readable over bright surfaces;
// very dark inks are given a grey glow by the script.
Shader "VRTattoo/InkHologram"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0, 1, 1, 1)
        _Fill ("Fill", Range(0, 1)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Inner ("Inner Radius", Range(0, 1)) = 0.68
        _Outer ("Outer Radius", Range(0, 1)) = 0.86
        _Backing ("Backing Darkness", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "InkHologram"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha // premultiplied: rgb = glow added, alpha = how much the backing darkens
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Fill;
                half _Alpha;
                half _Inner;
                half _Outer;
                half _Backing;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // 1 inside [a, b], with a pixel-wide soft edge.
            float Band(float x, float a, float b, float aa)
            {
                return saturate((x - a) / aa + 0.5) * saturate((b - x) / aa + 0.5);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 p = input.uv * 2.0 - 1.0;
                float r = length(p);
                float aa = max(fwidth(r), 1e-4);

                // 0 at the top, increasing clockwise.
                float angle = frac(atan2(p.x, p.y) / (2.0 * PI) + 1.0);
                float angleAA = max(fwidth(angle), 1e-4);
                if (angleAA > 0.5) angleAA = 1e-3; // seam at the top where angle wraps 1 -> 0
                float filled = saturate((_Fill - angle) / angleAA + 0.5) * step(0.001, _Fill);

                float ring = Band(r, _Inner, _Outer, aa);
                float innerLine = Band(r, _Inner - 0.07, _Inner - 0.05, aa);
                float outerLine = Band(r, _Outer + 0.05, _Outer + 0.065, aa);

                // Tick marks every 10% on the outer line.
                float tick = Band(frac(angle * 10.0 + 0.01), 0.0, 0.02, max(fwidth(angle * 10.0), 1e-4))
                           * Band(r, _Outer + 0.03, _Outer + 0.1, aa);

                // Soft glow around the filled part of the ring.
                float mid = (_Inner + _Outer) * 0.5;
                float halo = exp(-abs(r - mid) * 14.0) * 0.35 * filled * (1.0 - ring);

                // Bright leading edge where the ink level ends.
                float edge = exp(-abs(angle - _Fill) * 60.0) * ring * step(0.001, _Fill);

                // Hologram look: moving scan lines and a light sweep around the ring.
                float scan = 0.82 + 0.18 * sin(input.uv.y * 160.0 - _Time.y * 6.0);
                float sweep = 1.0 + 0.5 * exp(-frac(angle - _Time.y * 0.35) * 12.0) * ring * filled;

                float intensity = ring * lerp(0.12, 1.0, filled) * scan * sweep
                                + (innerLine + outerLine) * 0.35
                                + tick * 0.5
                                + halo
                                + edge * 0.8;

                float fade = _Alpha * _Color.a;
                float3 color = (_Color.rgb * intensity + edge * 0.4) * fade;
                float backing = Band(r, 0.0, _Outer + 0.1, aa) * (1.0 - 0.5 * r) * _Backing;
                return half4(color, backing * fade);
            }
            ENDHLSL
        }
    }
}
