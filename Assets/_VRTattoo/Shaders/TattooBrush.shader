// Soft round brush stamped into the tattoo RenderTexture by TattooCanvas (GL immediate mode).
Shader "VRTattoo/TattooBrush"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Hardness ("Hardness", Range(0.01, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, Zero One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Hardness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float d = length(i.uv - 0.5) * 2.0;
                float alpha = 1.0 - smoothstep(_Hardness, 1.0, d);
                return float4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }
}
