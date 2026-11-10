// Combat 3.0's ink (docs/design/combat_v3.md §6.6, option C): flat black like Unlit/Color, but in a
// fight the limbs on the far side of the body, the ones behind the hips as the camera sees them, turn
// a near-black charcoal, so you always see which hand or foot is working, whichever way he faces.
// _InkCenter (per fighter, w = 1 when set) is his hips; _InkDepthShade (global) fades the effect to
// nothing in cutscenes, close-ups and silhouettes.
Shader "ProjectInk/InkDepth"
{
    Properties
    {
        _Color ("Ink", Color) = (0.02, 0.02, 0.02, 1)
        _FarColor ("Far limbs", Color) = (0.1, 0.1, 0.103, 1)
        _InkCenter ("Centre (set by the fighter)", Vector) = (0, 0, 0, 0)
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

            fixed4 _Color;
            fixed4 _FarColor;
            float4 _InkCenter;
            float _InkDepthShade;

            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 fwd = normalize(_InkCenter.xyz - _WorldSpaceCameraPos);
                float d = dot(i.wpos - _InkCenter.xyz, fwd);              // metres behind the hips
                float k = saturate((d - 0.035) / 0.05) * saturate(_InkDepthShade) * _InkCenter.w;
                fixed3 far = max(_Color.rgb, _FarColor.rgb);
                return fixed4(lerp(_Color.rgb, far, k), 1);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
