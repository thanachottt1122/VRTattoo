// Fake light shaft under a lamp: an open cone (uv.y = 0 at the lamp, 1 at the far end) drawn additively.
// Fades along its length, towards the cone's silhouette (no hard edge) and when the camera gets close,
// so it never sits in front of the player's face. No volumetrics or post-processing.
Shader "VRTattoo/LightShaft"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.85, 0.65, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0.12
        _Falloff ("Length Falloff", Range(0.5, 6)) = 1.8
        _EdgePower ("Edge Softness", Range(0.5, 6)) = 2
        _NearFade ("Camera Fade Start/End (m)", Vector) = (0.4, 1.2, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "LightShaft"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Falloff;
                half _EdgePower;
                float4 _NearFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 toCamera = GetCameraPositionWS() - input.positionWS;
                float distance = length(toCamera);

                // Facing the camera = middle of the beam (bright); grazing = silhouette (zero).
                float facing = abs(dot(normalize(input.normalWS), toCamera / distance));
                float edge = pow(facing, _EdgePower);

                // Bright at the lamp, gone at the far end; also soft where it leaves the lamp.
                float along = pow(saturate(1.0 - input.uv.y), _Falloff) * saturate(input.uv.y * 12.0);

                float near = saturate((distance - _NearFade.x) / max(_NearFade.y - _NearFade.x, 1e-3));

                return half4(_Color.rgb * (_Intensity * edge * along * near), 0);
            }
            ENDHLSL
        }
    }
}
