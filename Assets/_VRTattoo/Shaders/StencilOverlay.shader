// Faint transfer-paper stencil drawn over the skin as a second material on the arm renderer.
// It never touches the ink RenderTexture. StencilOverlay.cs sets the placement:
//   st = 0.5 + (dot(uv - center, axisX), dot(uv - center, axisY)), visible where st is in [0,1].
Shader "VRTattoo/StencilOverlay"
{
    Properties
    {
        _StencilTex ("Stencil (alpha)", 2D) = "black" {}
        _Color ("Color", Color) = (0.4, 0.1, 0.8, 0.8)
        _Center ("Center UV", Vector) = (0.5, 0.5, 0, 0)
        _AxisX ("UV to stencil X", Vector) = (1, 0, 0, 0)
        _AxisY ("UV to stencil Y", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "StencilOverlay"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_StencilTex);
            SAMPLER(sampler_StencilTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
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
                float2 d = i.uv - _Center.xy;
                float2 st = 0.5 + float2(dot(d, _AxisX.xy), dot(d, _AxisY.xy));
                clip(min(st, 1.0 - st));
                half a = SAMPLE_TEXTURE2D(_StencilTex, sampler_StencilTex, st).a;
                return half4(_Color.rgb, _Color.a * a);
            }
            ENDHLSL
        }
    }
}
