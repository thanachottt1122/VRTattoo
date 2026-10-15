// Shared UI panel look (NeonPanel): see-through black rounded rectangle with a thin glowing
// neon edge, like the TATTOO sign. Drawn on one quad; the quad is _Size metres and the panel
// itself is _Pad smaller on every side so the glow has room to fade out.
// Values are set per renderer by the NeonPanel script (MaterialPropertyBlock).
Shader "VRTattoo/NeonPanel"
{
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.02, 0.01, 0.03, 0.78)
        [HDR] _EdgeColor ("Edge Color", Color) = (1.25, 0.28, 0.95, 1)
        _Size ("Quad Size (m)", Vector) = (0.4, 0.3, 0, 0)
        _Pad ("Glow Padding (m)", Float) = 0.012
        _Radius ("Corner Radius (m)", Float) = 0.025
        _EdgeWidth ("Edge Width (m)", Float) = 0.0025
        _GlowWidth ("Glow Width (m)", Float) = 0.005
        _Glow ("Glow Strength", Range(0, 2)) = 0.35
        _Hover ("Hover", Range(0, 1)) = 0
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "NeonPanel"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha // premultiplied: the fill darkens, the edge glow adds light
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half4 _EdgeColor;
                float4 _Size;
                float _Pad;
                float _Radius;
                float _EdgeWidth;
                float _GlowWidth;
                half _Glow;
                half _Hover;
                half _Alpha;
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

            // Signed distance to a rounded rectangle (negative inside), in metres.
            float RoundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 p = (input.uv - 0.5) * _Size.xy;
                float2 halfSize = max(_Size.xy * 0.5 - _Pad, 0.001);
                float d = RoundedBox(p, halfSize, min(_Radius, min(halfSize.x, halfSize.y)));
                float aa = max(fwidth(d), 1e-5);

                float inside = saturate(0.5 - d / aa);
                float edge = saturate(0.5 - (abs(d) - _EdgeWidth * 0.5) / aa);
                // Glow fades out around the panel; inside only a thin rim so the fill stays black.
                float w = max(_GlowWidth, 1e-4);
                float glow = (d > 0.0 ? exp(-d / w) : exp(d / (w * 0.3)) * 0.5) * (1.0 - edge);

                float hover = 1.0 + _Hover * 0.9;
                float3 light = _EdgeColor.rgb * (edge + glow * _Glow) * hover;
                float3 fill = _FillColor.rgb * _FillColor.a * inside + _EdgeColor.rgb * 0.06 * _Hover * inside;

                float fade = _Alpha;
                return half4((fill + light) * fade, _FillColor.a * inside * fade);
            }
            ENDHLSL
        }
    }
}
