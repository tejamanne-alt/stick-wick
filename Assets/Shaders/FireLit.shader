// The fortress at dawn in the walk-out (docs/story/walkout.md): its baked texture, never lit by
// Unity's lights, but caught by the blasts. Up to eight fires (global, set by BlastFx) add their
// warm light on the stone with a soft falloff; a global flash lifts everything at once; and the
// whole set fades into its burning bake (_FortBurn: windows full of fire, the smoke's dimness).
Shader "ProjectInk/FireLit"
{
    Properties
    {
        _MainTex ("Baked (dawn)", 2D) = "white" {}
        _BurnTex ("Baked (burning)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BurnTex;
            float4 _MainTex_ST;
            float _FortBurn;
            float4 _FortFlash;
            float4 _FireLights[8];     // xyz: where, w: reach (metres)
            float4 _FireColors[8];     // rgb: colour times strength
            float _FireCount;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 wpos : TEXCOORD1;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 a = tex2D(_MainTex, i.uv).rgb;
                fixed3 b = tex2D(_BurnTex, i.uv).rgb;
                fixed3 c = lerp(a, b, saturate(_FortBurn));
                float3 light = float3(0, 0, 0);
                for (int k = 0; k < 8; k++)
                {
                    if (k >= (int)_FireCount) break;
                    float3 d = _FireLights[k].xyz - i.wpos;
                    float r = max(_FireLights[k].w, 0.01);
                    float att = saturate(1.0 - dot(d, d) / (r * r));
                    light += _FireColors[k].rgb * att * att;
                }
                c = c * (1.0 + light) + light * 0.08 + _FortFlash.rgb * 0.35;
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
