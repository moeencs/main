// UIBlur.cginc - Include file for blur shader functions

#ifndef UI_BLUR_INCLUDED
#define UI_BLUR_INCLUDED

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float _Opacity;
float _Size;

struct VertexInput {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
};

struct v2f_blur {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 offset : TEXCOORD1;
    float4 color : COLOR;
};

v2f_blur VS_QuadProj(VertexInput v) {
    v2f_blur o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    
    float2 offset = _MainTex_TexelSize.xy * _Size;
    o.offset = float4(offset.x, 0, 0, 0);
    o.color = float4(1, 1, 1, 1);
    
    return o;
}

v2f_blur VS_QuadProjColor(VertexInput v) {
    v2f_blur o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    
    float2 offset = _MainTex_TexelSize.xy * _Size;
    o.offset = float4(0, offset.y, 0, 0);
    o.color = v.color;
    
    return o;
}

float4 blur_x(float2 uv, float4 offset, sampler2D tex) {
    float4 color = float4(0, 0, 0, 0);
    
    color += tex2D(tex, uv - offset.xy * 4.0) * 0.05;
    color += tex2D(tex, uv - offset.xy * 3.0) * 0.09;
    color += tex2D(tex, uv - offset.xy * 2.0) * 0.12;
    color += tex2D(tex, uv - offset.xy) * 0.15;
    color += tex2D(tex, uv) * 0.18;
    color += tex2D(tex, uv + offset.xy) * 0.15;
    color += tex2D(tex, uv + offset.xy * 2.0) * 0.12;
    color += tex2D(tex, uv + offset.xy * 3.0) * 0.09;
    color += tex2D(tex, uv + offset.xy * 4.0) * 0.05;
    
    return color;
}

float4 blur_y(float2 uv, float4 offset, float4 img_color, sampler2D tex) {
    float4 color = float4(0, 0, 0, 0);
    
    color += tex2D(tex, uv - offset.xy * 4.0) * 0.05;
    color += tex2D(tex, uv - offset.xy * 3.0) * 0.09;
    color += tex2D(tex, uv - offset.xy * 2.0) * 0.12;
    color += tex2D(tex, uv - offset.xy) * 0.15;
    color += tex2D(tex, uv) * 0.18;
    color += tex2D(tex, uv + offset.xy) * 0.15;
    color += tex2D(tex, uv + offset.xy * 2.0) * 0.12;
    color += tex2D(tex, uv + offset.xy * 3.0) * 0.09;
    color += tex2D(tex, uv + offset.xy * 4.0) * 0.05;
    
    color.rgb = lerp(float3(1, 1, 1), color.rgb, _Opacity);
    color.a = img_color.a * _Opacity;
    
    return color;
}

#endif // UI_BLUR_INCLUDED