Shader "Custom/OilyWrestler"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _OilColor ("Oil Color", Color) = (1, 0.9, 0.7, 0.3)
        _OilIntensity ("Oil Intensity", Range(0, 2)) = 0.8
        _ShineSpeed ("Shine Speed", Range(0, 5)) = 1.5
        _ShineFrequency ("Shine Frequency", Range(1, 10)) = 3
        _OilNoise ("Oil Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Range(0.1, 5)) = 1
        _Glossiness ("Glossiness", Range(0, 1)) = 0.8
        _SpecularPower ("Specular Power", Range(1, 100)) = 20
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
                float2 screenPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _OilNoise;
            float4 _OilNoise_ST;
            
            fixed4 _OilColor;
            float _OilIntensity;
            float _ShineSpeed;
            float _ShineFrequency;
            float _NoiseScale;
            float _Glossiness;
            float _SpecularPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.screenPos = ComputeScreenPos(o.pos).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Ana texture'ı sample et
                fixed4 mainTex = tex2D(_MainTex, i.uv) * i.color;
                
                // Alpha test - transparan pikselleri atla
                if (mainTex.a < 0.1)
                    discard;

                // Noise texture için UV koordinatları
                float2 noiseUV = i.uv * _NoiseScale + _Time.y * 0.1;
                fixed4 noise = tex2D(_OilNoise, noiseUV);
                
                // Yağ efekti için parlaklık hesaplama
                float time = _Time.y * _ShineSpeed;
                
                // Çoklu sine dalgaları ile kompleks parlaklık deseni
                float shine1 = sin(i.uv.x * _ShineFrequency + time) * 0.5 + 0.5;
                float shine2 = sin(i.uv.y * _ShineFrequency * 0.7 + time * 1.3) * 0.5 + 0.5;
                float shine3 = sin((i.uv.x + i.uv.y) * _ShineFrequency * 0.5 + time * 0.8) * 0.5 + 0.5;
                
                // Noise ile parlaklığı varyasyon ekle
                float combinedShine = (shine1 + shine2 + shine3) / 3.0;
                combinedShine = lerp(combinedShine, noise.r, 0.3);
                
                // Ekran pozisyonuna dayalı highlight
                float2 screenCenter = float2(0.5, 0.5);
                float distFromCenter = distance(i.screenPos, screenCenter);
                float screenHighlight = 1.0 - saturate(distFromCenter * 2);
                
                // Specular highlight (basit)
                float3 lightDir = normalize(float3(1, 1, -1));
                float3 normal = float3(0, 0, 1); // 2D sprite için sabit normal
                float3 viewDir = normalize(float3(0, 0, -1));
                float3 reflectDir = reflect(-lightDir, normal);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), _SpecularPower);
                
                // Yağ rengini hesapla
                float oilFactor = combinedShine * _OilIntensity;
                oilFactor += spec * _Glossiness * 0.5;
                oilFactor += screenHighlight * 0.2;
                
                // Final renk karışımı
                fixed4 finalColor = mainTex;
                
                // Yağ rengini blend et
                finalColor.rgb = lerp(finalColor.rgb, 
                                    finalColor.rgb + _OilColor.rgb * oilFactor, 
                                    _OilColor.a);
                
                // Parlaklık ekle
                finalColor.rgb += oilFactor * 0.2;
                
                // Saturasyon artır (yağlı görünüm için)
                float gray = dot(finalColor.rgb, float3(0.299, 0.587, 0.114));
                finalColor.rgb = lerp(float3(gray, gray, gray), finalColor.rgb, 1.2);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}