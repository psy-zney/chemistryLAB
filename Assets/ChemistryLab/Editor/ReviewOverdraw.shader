Shader "Hidden/ChemistryLab/ReviewOverdraw"
{
    Properties { _MainTex("Mask",2D)="white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass { ZWrite On ColorMask 0 }
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            ZWrite Off ZTest LEqual Cull Back Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o; }
            float4 frag(v2f i):SV_Target { clip(tex2D(_MainTex,i.uv).a-.025);return float4(1,0,0,1); }
            ENDCG
        }
    }
    Fallback Off
}
