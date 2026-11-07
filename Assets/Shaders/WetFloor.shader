// The story's wet concrete: the scene reflected in the floor (rendered by PlanarReflection),
// faint on damp concrete and strong in the puddles, strongest where the camera looks along the
// floor (fresnel), shivering with the rain. Drawn over the baked floor, blended, never lit.
Shader "ProjectInk/WetFloor"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _ReflectionTex ("Reflection", 2D) = "black" {}
        _WetTex ("Wet mask (r: puddles)", 2D) = "white" {}
        _WetScale ("Wet mask tiling (per metre)", Float) = 0.08
        _Base ("Reflection on damp concrete", Range(0, 1)) = 0.14
        _Puddle ("Reflection in puddles", Range(0, 1)) = 0.62
        _Ripple ("Rain ripple", Float) = 0.0035
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ReflectionTex;
            sampler2D _WetTex;
            float4 _Color;
            float _WetScale, _Base, _Puddle, _Ripple;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 scr : TEXCOORD0;
                float3 wpos : TEXCOORD1;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.scr = ComputeNonStereoScreenPos(o.pos);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float wet = tex2D(_WetTex, i.wpos.xz * _WetScale).r;
                float t = _Time.y;
                // rain on the water: a fine shiver, more in the puddles
                float2 rip = float2(sin(i.wpos.x * 9.0 + t * 7.0) + sin(i.wpos.z * 11.0 - t * 5.0),
                                    cos(i.wpos.z * 8.0 + t * 6.0) + cos(i.wpos.x * 13.0 - t * 4.0)) * _Ripple * (0.35 + wet);
                float2 uv = i.scr.xy / i.scr.w + rip;
                fixed3 refl = tex2D(_ReflectionTex, uv).rgb * _Color.rgb;
                float3 view = normalize(_WorldSpaceCameraPos - i.wpos);
                float fres = pow(1.0 - saturate(view.y), 3.0);
                float a = lerp(_Base, _Puddle, wet) * lerp(0.5, 1.0, fres) * _Color.a;
                return fixed4(refl, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
