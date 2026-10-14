// Ink and stencil drawn over a customer's forearm. CustomerTattooSkin renders it on a copy of the
// forearm triangles (same bones, so it sits exactly on the skin) whose UVs are skin coordinates:
// metres around × along the arm, scaled into the 0..1 canvas.
//   ink:     _InkTex (TattooCanvas, premultiplied: rgb = colour x alpha, so each stroke keeps its own colour)
//   stencil: st = 0.5 + (dot(uv - center, axisX), dot(uv - center, axisY)), visible where st is in [0,1]
Shader "VRTattoo/TattooSkinOverlay"
{
    Properties
    {
        _InkTex ("Ink (premultiplied)", 2D) = "black" {}
        _StencilTex ("Stencil (alpha)", 2D) = "black" {}
        _StencilColor ("Stencil Color", Color) = (0.4, 0.1, 0.8, 0.8)
        _Center ("Center UV", Vector) = (0.5, 0.5, 0, 0)
        _AxisX ("UV to stencil X", Vector) = (1, 0, 0, 0)
        _AxisY ("UV to stencil Y", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "TattooSkinOverlay"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_InkTex);
            SAMPLER(sampler_InkTex);
            TEXTURE2D(_StencilTex);
            SAMPLER(sampler_StencilTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _StencilColor;
                float4 _Center;
                float4 _AxisX;
                float4 _AxisY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Outside the canvas there is neither ink nor stencil.
                clip(min(i.uv, 1.0 - i.uv));

                float2 d = i.uv - _Center.xy;
                float2 st = 0.5 + float2(dot(d, _AxisX.xy), dot(d, _AxisY.xy));
                half inStencil = step(0.0, min(min(st.x, st.y), min(1.0 - st.x, 1.0 - st.y)));
                half stencilA = SAMPLE_TEXTURE2D(_StencilTex, sampler_StencilTex, saturate(st)).a * inStencil * _StencilColor.a;

                half4 ink = SAMPLE_TEXTURE2D(_InkTex, sampler_InkTex, i.uv);
                half inkA = ink.a;
                half3 inkRGB = ink.rgb / max(inkA, 0.001);

                half3 color = lerp(_StencilColor.rgb, inkRGB, inkA);
                half alpha = max(inkA, stencilA);
                clip(alpha - 0.004);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
