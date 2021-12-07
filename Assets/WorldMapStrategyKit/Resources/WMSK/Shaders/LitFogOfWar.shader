Shader "WMSK/Lit Fog Of War" {
	Properties {
                _MainTex ("Base (RGB)", 2D) = "black" {}
                _NoiseTex ("Noise (RGB)", 2D) = "white" {}
         		_EmissionColor("Color", Color) = (1,1,1,1)
        }

   SubShader {
            Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
            LOD 200

         	Pass {
         	ZWrite Off
         	ZTest Always
         	Blend SrcAlpha OneMinusSrcAlpha

         	CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
            #pragma target 3.0

   			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            fixed3 _EmissionColor;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
                	fixed fogAlpha = tex2D (_MainTex, i.texcoord).a;
                    half vxy = (i.texcoord.x + i.texcoord.y);
					half wt = _Time[1] * 0.5;
					half2 waveDisp1 = half2(wt + cos(wt+i.texcoord.y * 32.0) * 0.125, 0) * 0.05;
					fixed4 fog1 = tex2D(_NoiseTex, (i.texcoord + waveDisp1) * 8);
                    wt*=1.1;
					half2 waveDisp2 = half2(wt + cos(wt+i.texcoord.y * 8.0) * 0.5, 0) * 0.05;
					fixed4 fog2 = tex2D(_NoiseTex, (i.texcoord + waveDisp2) * 2);
                    fixed4 fog = (fog1 + fog2) * 0.5;
                    fog.rgb *= _EmissionColor;
                    fog.a = fogAlpha;
                    return fog;
			}
			ENDCG 
        }
   }
}