Shader "UI/LiquidGlass"
{
    Properties
    {
        [Header(Shape Options)]
        [Toggle] _FillRect("Fill Entire Rect (Fix Corners)", Float) = 1
        _Power("Squircle Power", Range(1.0, 10.0)) = 3.0
        _AspectRatio("Aspect Ratio (Width/Height)", Float) = 1.0

        [Header(Glass Optics)]
        _Refraction("Edge Refraction", Range(0.0, 0.2)) = 0.05
        _ChromaticAberration("Chromatic Aberration", Range(0.0, 0.05)) = 0.015
        _BlurRadius("Blur Radius", Range(0.0, 5.0)) = 2.5

        [Header(Surface Texture)]
        _Tint("Glass Tint", Color) = (1, 1, 1, 0.05)
        _Noise("Frosted Noise", Range(0.0, 0.5)) = 0.08
        _Shine("Diagonal Shine", Range(0.0, 1.0)) = 0.25
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }

        Cull Off 
        Lighting Off 
        ZWrite Off 
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        GrabPass { "_GlassGrab" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
            };

            sampler2D _GlassGrab;
            float4 _GlassGrab_TexelSize;

            float _FillRect;
            float _Power;
            float _AspectRatio;
            float _Refraction;
            float _ChromaticAberration;
            float _BlurRadius;
            fixed4 _Tint;
            float _Noise;
            float _Shine;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.texcoord = v.texcoord;
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            // Ruído para a textura fosca
            float rand(float2 co){
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Blur Gaussiano 9-tap otimizado
            half3 sampleBlur(float2 uv) {
                half3 color = 0;
                float2 texel = _GlassGrab_TexelSize.xy * _BlurRadius;
                
                color += tex2D(_GlassGrab, uv).rgb * 0.227027;
                color += tex2D(_GlassGrab, uv + float2(texel.x, 0.0)).rgb * 0.15;
                color += tex2D(_GlassGrab, uv + float2(-texel.x, 0.0)).rgb * 0.15;
                color += tex2D(_GlassGrab, uv + float2(0.0, texel.y)).rgb * 0.15;
                color += tex2D(_GlassGrab, uv + float2(0.0, -texel.y)).rgb * 0.15;
                color += tex2D(_GlassGrab, uv + float2(texel.x, texel.y)).rgb * 0.043;
                color += tex2D(_GlassGrab, uv + float2(-texel.x, texel.y)).rgb * 0.043;
                color += tex2D(_GlassGrab, uv + float2(texel.x, -texel.y)).rgb * 0.043;
                color += tex2D(_GlassGrab, uv + float2(-texel.x, -texel.y)).rgb * 0.043;
                
                return color;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float2 p = (i.texcoord - center) * 2.0;
                
                // Correção de Aspect Ratio para o Squircle não esticar
                float2 p_sdf = p;
                if (_FillRect == 0.0) p_sdf.x *= _AspectRatio;

                // MATEMÁTICA DO SQUIRCLE (sdSuperellipse)
                float2 p_abs = abs(p_sdf);
                float n = _Power;
                float numerator = pow(p_abs.x, n) + pow(p_abs.y, n) - 1.0;
                float den_x = pow(max(p_abs.x, 0.0001), 2.0 * n - 2.0);
                float den_y = pow(max(p_abs.y, 0.0001), 2.0 * n - 2.0);
                float d = numerator / (n * sqrt(den_x + den_y) + 0.00001);

                // CORTA OS CANTOS APENAS SE A CHECKBOX ESTIVER DESLIGADA
                if (_FillRect == 0.0 && d > 0.0) discard;

                // CÁLCULO DAS BORDAS (Para fazer a refração curvar)
                float distToEdge = 0.0;
                if (_FillRect == 1.0) {
                    float2 box = abs(p);
                    distToEdge = 1.0 - max(box.x, box.y); // Borda da UI normal
                } else {
                    distToEdge = -d; // Borda do squircle
                }

                // Curva da Lente
                float curve = smoothstep(0.0, 0.3, distToEdge);
                
                // REFRAÇÃO
                float2 refractionVec = p * (1.0 - curve) * _Refraction;
                float2 grabUV = i.grabPos.xy / i.grabPos.w;
                float2 refractedUV = grabUV + refractionVec;

                // ABERRAÇÃO CROMÁTICA & BLUR (O SEGREDO DO VIDRO)
                half3 glassColor = 0;
                float2 caOffset = normalize(p) * _ChromaticAberration * (1.0 - curve);
                
                glassColor.r = sampleBlur(refractedUV + caOffset).r;
                glassColor.g = sampleBlur(refractedUV).g;
                glassColor.b = sampleBlur(refractedUV - caOffset).b;

                // TEXTURA FOSCA (NOISE)
                float noiseVal = (rand(i.texcoord * 100.0) - 0.5) * 2.0;
                glassColor += noiseVal * _Noise;

                // BRILHO ESPECULAR (SHINE)
                float shine = smoothstep(0.4, 0.6, sin(p.x * 4.0 + p.y * 4.0));
                glassColor += shine * _Shine * (1.0 - curve);

                // TINT
                glassColor = lerp(glassColor, _Tint.rgb, _Tint.a);

                // Aplica o Alpha do CanvasGroup para Fade In/Out
                return half4(glassColor * i.color.rgb, i.color.a);
            }
            ENDCG
        }
    }
}