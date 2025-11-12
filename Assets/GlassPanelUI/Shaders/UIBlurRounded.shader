Shader "UI/BlurRoundedSimple" {
Properties {
    [HideInInspector] _MainTex("", 2D) = "white" {}
    _Opacity("Opacity", Range(0.0, 1.0)) = 0.5
    _Size("Blur Size", Range(1.0, 16.0)) = 4.0
    _CornerRadius("Corner Radius", Vector) = (20, 20, 20, 20)
    _BorderWidth("Border Width", Float) = 2.0
    _BorderColor("Border Color", Color) = (1, 1, 1, 0.5)
    _TintColor("Tint Color", Color) = (1, 1, 1, 0.1)
}

SubShader {
    Tags {
        "Queue" = "Transparent"
        "IgnoreProjector" = "True"
        "RenderType" = "Transparent"
        "PreviewType" = "Plane"
        "CanUseSpriteAtlas" = "True"
    }
    
    Cull Off
    Lighting Off
    ZWrite Off
    ZTest [unity_GUIZTestMode]
    Blend SrcAlpha OneMinusSrcAlpha

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UnityUI.cginc"

    sampler2D _MainTex;
    sampler2D _GrabTexture;
    float4 _MainTex_TexelSize;
    float _Opacity;
    float _Size;
    float4 _CornerRadius;
    float _BorderWidth;
    fixed4 _BorderColor;
    fixed4 _TintColor;
    float4 _ClipRect;

    struct appdata {
        float4 vertex : POSITION;
        float4 color : COLOR;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f {
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        float4 worldPosition : TEXCOORD1;
        float4 grabUV : TEXCOORD2;
    };

    // Rounded rectangle SDF
    float sdRoundBox(float2 p, float2 b, float4 r) {
        r.xy = (p.x > 0.0) ? r.xy : r.zw;
        r.x = (p.y > 0.0) ? r.x : r.y;
        float2 q = abs(p) - b + r.x;
        return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
    }

    v2f vert(appdata v) {
        v2f o;
        o.worldPosition = v.vertex;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.texcoord = v.texcoord;
        o.color = v.color;
        
        // Calculate grab texture UVs
        #if UNITY_UV_STARTS_AT_TOP
        float scale = -1.0;
        #else
        float scale = 1.0;
        #endif
        
        o.grabUV.xy = (float2(o.vertex.x, o.vertex.y * scale) + o.vertex.w) * 0.5;
        o.grabUV.zw = o.vertex.zw;
        
        return o;
    }

    fixed4 frag(v2f i) : SV_Target {
        // Sample blurred background
        float2 grabUV = i.grabUV.xy / i.grabUV.w;
        float4 blur = float4(0, 0, 0, 0);
        float2 offset = _MainTex_TexelSize.xy * _Size;
        
        // Simple box blur
        for (int x = -2; x <= 2; x++) {
            for (int y = -2; y <= 2; y++) {
                blur += tex2D(_GrabTexture, grabUV + float2(x, y) * offset);
            }
        }
        blur /= 25.0;
        
        // Apply tint and opacity
        blur.rgb = lerp(blur.rgb, _TintColor.rgb, _TintColor.a);
        
        // Get pixel position relative to rect center
        float2 uv = i.texcoord - 0.5;
        float2 size = float2(0.5, 0.5);
        
        // Calculate rounded rectangle mask
        float dist = sdRoundBox(uv, size, _CornerRadius * 0.01);
        float alpha = 1.0 - smoothstep(-0.001, 0.001, dist);
        
        // Calculate border
        float borderMask = 0.0;
        if (_BorderWidth > 0.0) {
            float innerDist = dist + _BorderWidth * 0.01;
            borderMask = smoothstep(-0.001, 0.001, -innerDist) - smoothstep(-0.001, 0.001, -dist);
        }
        
        // Blend
        fixed4 color = blur;
        color.rgb = lerp(color.rgb, _BorderColor.rgb, borderMask);
        color.a = alpha * _Opacity * i.color.a;
        
        // Apply clipping
        color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
        
        return color;
    }
    ENDCG

    GrabPass { "_GrabTexture" }
    
    Pass {
        Name "Default"
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        ENDCG
    }
}
}