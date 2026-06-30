Shader "UI/ModernFrostedGlass"
{
    Properties
    {
        _Radius("Blur Radius", Range(1, 20)) = 8
        _TintColor("Tint Color (Liquid Glass)", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        
        Blend SrcAlpha OneMinusSrcAlpha 

        GrabPass
        {
            "_GrabTexture"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                float4 uvgrab : TEXCOORD0;
            };

            float _Radius;
            fixed4 _TintColor;
            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _TintColor; 
                
                // A CORREÇÃO DE ROTAÇÃO: 
                // A Unity lida automaticamente com a diferença entre PC, Android, Vulkan e iOS!
                o.uvgrab = ComputeGrabScreenPos(o.vertex);
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 sum = half4(0,0,0,0);
                int samples = 0;
                
                // A CORREÇÃO DE "SQUISH" (ESMAGAMENTO):
                // Fazemos a divisão da perspetiva (.xy / .w) UMA VEZ no início.
                float2 grabUV = i.uvgrab.xy / i.uvgrab.w;
                
                float step = max(1.0f, _Radius / 2.5f); 

                for(float x = -_Radius; x <= _Radius; x += step)
                {
                    for(float y = -_Radius; y <= _Radius; y += step)
                    {
                        // Lemos a textura já com as coordenadas corrigidas e limpas
                        float2 offset = float2(_GrabTexture_TexelSize.x * x, _GrabTexture_TexelSize.y * y);
                        sum += tex2D(_GrabTexture, grabUV + offset);
                        samples++;
                    }
                }

                half4 finalColor = sum / samples;
                
                // Retorna a cor com o Alpha do CanvasGroup para o Fade funcionar
                return half4(finalColor.rgb * i.color.rgb, i.color.a);
            }
            ENDCG
        }
    }
}