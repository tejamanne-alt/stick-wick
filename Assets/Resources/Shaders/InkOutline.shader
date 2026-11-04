// A flat-colour outline for the fighters: the inflated "hull" of the body is drawn with front
// faces culled, so only a thin rim shows around the silhouette. Black fighters stay readable
// against dark windows, and every fighter pops off the busy black-and-white building.
Shader "ProjectInk/InkOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.92, 0.92, 0.92, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Front
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
    Fallback Off
}
