Shader "ChemistryLab/ReadableGlass"
{
    Properties
    {
        _Color ("Glass tint", Color) = (0.85,0.94,0.96,0.08)
        _EdgeOpacity ("Edge opacity", Range(0,1)) = 0.42
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        ZWrite Off
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            fixed4 _Color;
            float _EdgeOpacity;
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal);
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float edge=pow(1-saturate(dot(n,view)),3);
                float3 reflected=reflect(-view,n);
                float3 reflection=DecodeHDR(UNITY_SAMPLE_TEXCUBE(unity_SpecCube0,reflected),unity_SpecCube0_HDR);
                float3 light=normalize(_WorldSpaceLightPos0.xyz);
                float spec=pow(saturate(dot(n,normalize(light+view))),96);
                float3 colour=_Color.rgb*(.65+.35*saturate(dot(n,light)))
                    + reflection*.28 + _LightColor0.rgb*spec*.65;
                return fixed4(colour,saturate(_Color.a+edge*_EdgeOpacity+spec*.15));
            }
            ENDCG
        }
    }
    Fallback Off
}
