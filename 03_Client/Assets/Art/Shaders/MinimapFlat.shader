Shader "Dawnholder/MinimapFlat"
{
    // 미니맵 전용 평면 단색 셰이더 — 스프라이트/타일맵 텍스처의 알파(모양)만 쓰고 RGB는 _FlatColor로 채움.
    // 어두운 텍스처/조명에 무관하게 walkable 지형을 또렷한 단색 실루엣으로 보여줌.
    // MinimapTerrainTint가 미니맵 카메라 렌더 직전에만 per-camera로 머티리얼 스왑해 사용.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FlatColor ("Flat Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _FlatColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                return half4(_FlatColor.rgb, a * _FlatColor.a);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
