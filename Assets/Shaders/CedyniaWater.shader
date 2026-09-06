Shader "Cedynia/Water"
{
    Properties
    {
        _Color ("Color", Color) = (0.12, 0.34, 0.40, 0.78)
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Speed", Float) = 0.06
        _Glossiness ("Smoothness", Range(0,1)) = 0.86
        _Metallic ("Metallic", Range(0,1)) = 0.12
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _Speed;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float t = _Time.y * _Speed;
            float2 uv1 = IN.uv_MainTex + float2(t, t * 0.35);
            float2 uv2 = IN.uv_MainTex * 1.65 + float2(-t * 0.55, t * 0.8);
            fixed4 c = (tex2D(_MainTex, uv1) + tex2D(_MainTex, uv2)) * 0.5 * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
