Shader "SURLHWA/UI/CountertopEdge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Emphasis ("Object emphasis", Range(0,1)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 local : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect;
            float _Emphasis;
            v2f vert(appdata v)
            {
                v2f o; o.local = v.vertex; o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; o.color = v.color * _Color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                // Sample one display pixel around the actual material. Low-alpha
                // glass interiors never become opaque highlight masks.
                float2 dx = ddx(i.uv), dy = ddy(i.uv);
                float a = smoothstep(.40, .75, source.a);
                float l = smoothstep(.40, .75, tex2D(_MainTex, i.uv - dx).a);
                float r = smoothstep(.40, .75, tex2D(_MainTex, i.uv + dx).a);
                float t = smoothstep(.40, .75, tex2D(_MainTex, i.uv + dy).a);
                float b = smoothstep(.40, .75, tex2D(_MainTex, i.uv - dy).a);
                float outer = saturate(max(max(l,r),max(t,b)) - a);
                float inner = a * (1 - min(min(l,r),min(t,b)));
                float glint = smoothstep(.42, .85, dot(source.rgb, float3(.2126,.7152,.0722))) * a;
                float3 warm = float3(1, .91, .73);
                float3 body = lerp(source.rgb, warm, _Emphasis * (.34 * inner + .18 * glint));
                float edgeAlpha = outer * _Emphasis * .64;
                float alpha = source.a + edgeAlpha * (1 - source.a);
                fixed4 c = fixed4((body * source.a + warm * edgeAlpha * (1-source.a)) / max(alpha, .0001), alpha) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - .001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
