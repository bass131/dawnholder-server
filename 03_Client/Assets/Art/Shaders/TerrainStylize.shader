Shader "Dawnholder/TerrainStylize"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0,2)) = 1
        _Contrast ("Contrast", Range(0,2)) = 1
        _Brightness ("Brightness", Range(0,2)) = 1

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness (texels)", Range(0,4)) = 0
        _OutlineThreshold ("Outline Alpha Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float4 _Color;
                float  _Saturation;
                float  _Contrast;
                float  _Brightness;
                float4 _OutlineColor;
                float  _OutlineThickness;
                float  _OutlineThreshold;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color * _Color;

                half3 c = tex.rgb;
                c *= _Brightness;
                c = (c - 0.5) * _Contrast + 0.5;
                half luma = dot(c, half3(0.299, 0.587, 0.114));
                c = lerp(luma.xxx, c, _Saturation);
                c = saturate(c);

                if (_OutlineThickness > 0.0 && tex.a < _OutlineThreshold)
                {
                    float2 o = _MainTex_TexelSize.xy * _OutlineThickness;
                    half n = 0;
                    n = max(n, SampleAlpha(IN.uv + float2( o.x, 0)));
                    n = max(n, SampleAlpha(IN.uv + float2(-o.x, 0)));
                    n = max(n, SampleAlpha(IN.uv + float2(0,  o.y)));
                    n = max(n, SampleAlpha(IN.uv + float2(0, -o.y)));
                    n = max(n, SampleAlpha(IN.uv + float2( o.x,  o.y)));
                    n = max(n, SampleAlpha(IN.uv + float2(-o.x,  o.y)));
                    n = max(n, SampleAlpha(IN.uv + float2( o.x, -o.y)));
                    n = max(n, SampleAlpha(IN.uv + float2(-o.x, -o.y)));

                    if (n >= _OutlineThreshold)
                        return half4(_OutlineColor.rgb, _OutlineColor.a);
                }

                return half4(c, tex.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
