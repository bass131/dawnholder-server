// 스프라이트용 언릿 블러 셰이더 — 먼 배경 레이어를 부드럽게 흐려 대기원근(깊이감)을 만든다.
//   _BlurSize: 샘플 오프셋(텍셀 단위). 0이면 원본 그대로, 커질수록 더 흐림.
//   언릿이라 URP 2D 라이트와 무관(라이트 없는 정렬 레이어에서 검정으로 안 나옴).
//   부드러운 블러를 위해 텍스처 Filter Mode = Bilinear 권장(Point면 블러가 각져 보임).
Shader "Dawnholder/SpriteBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size (texels)", Range(0,8)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _BlurSize;

            v2f vert (appdata IN)
            {
                v2f OUT;
                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                // BlurSize=0이면 오프셋 0 → 9탭이 전부 같은 픽셀 → 원본과 동일.
                float2 t = _MainTex_TexelSize.xy * _BlurSize;

                // 3x3 가우시안 가중치(중앙 0.25 / 상하좌우 0.125 / 대각 0.0625, 합 1.0).
                fixed4 c  = tex2D(_MainTex, IN.uv)                          * 0.25;
                c += tex2D(_MainTex, IN.uv + float2( t.x, 0))              * 0.125;
                c += tex2D(_MainTex, IN.uv + float2(-t.x, 0))              * 0.125;
                c += tex2D(_MainTex, IN.uv + float2(0,  t.y))              * 0.125;
                c += tex2D(_MainTex, IN.uv + float2(0, -t.y))              * 0.125;
                c += tex2D(_MainTex, IN.uv + float2( t.x,  t.y))           * 0.0625;
                c += tex2D(_MainTex, IN.uv + float2(-t.x, -t.y))          * 0.0625;
                c += tex2D(_MainTex, IN.uv + float2( t.x, -t.y))           * 0.0625;
                c += tex2D(_MainTex, IN.uv + float2(-t.x,  t.y))           * 0.0625;

                return c * IN.color;
            }
            ENDCG
        }
    }
}
