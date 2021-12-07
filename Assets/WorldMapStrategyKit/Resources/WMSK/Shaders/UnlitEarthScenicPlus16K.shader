Shader "WMSK/Unlit Earth Scenic Plus 16K" {

	Properties {
		_TexTL ("Tex TL", 2D) = "white" {}
		_TexTR ("Tex TR", 2D) = "white" {}
		_TexBL ("Tex BL", 2D) = "white" {}
		_TexBR ("Tex BR", 2D) = "white" {}
		_TerrestrialMap ("Terrestrial Map (RGBA)", 2D) = "black" {}
		_Water ("Water (RGB)", 2D) = "black" {}
		_Noise ("Noise (RGB)", 2D) = "black" {}
		_Distance ("Plane Distance", Float) = 1
		_WaterColor ("Water Color (RGB)", Color) = (0,0.41,0.58,1)
		_WaterLevel ("Water Level", Vector) = (0.1, 0.1, 30) // x=water level, y=foam threshold, z=foam intensity
		_CloudMap ("Cloud Map", 2D) = "black" {}
		_CloudShadowStrength ("Cloud Shadow Strength", Range(0, 1)) = 0.2			
		_CloudMapOffset ("Cloud Map Offset", Vector) = (0,0,0)			
	}
	
	Subshader {
		Tags { "RenderType"="Opaque" }
		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag	
				#pragma target 3.0

				#include "UnityCG.cginc"

				sampler2D _TexTL;
				sampler2D _TexTR;
				sampler2D _TexBL;
				sampler2D _TexBR;
				
				sampler2D _TerrestrialMap;
				sampler2D _Water;
				sampler2D _Noise;
				sampler2D _CloudMap;
				float2 _CloudMapOffset;
				
				float _CloudSpeed;
				float _CloudShadowStrength;
				float _CloudElevation;
								
				float _Distance;
				fixed4 _WaterColor;
				float3 _WaterLevel;
				
				struct v2f {
					float4 pos : SV_POSITION;
					float2 uv: TEXCOORD0;
				};
				
				fixed4 getWater(half2 uv, half4 terra) {
					const fixed4 zeros = fixed4(0,0,0,0);
					fixed4 water0, water1, water2;
					fixed4 waterBG = _WaterColor;
					half vxy = (uv.x + uv.y);
					half wt = _Time[1] * 0.5;
					half f2 = 0;
					half f1 = 0;
					half f0 = saturate( (0.2 - _Distance) * 6);
					if (f0>0) {
						half st = _SinTime[3];
						half2 waveDisp0 = st * 0.0005 * (terra.gb - 0.5);
						half4 foam0 = tex2Dlod(_Noise, float4((uv + waveDisp0) * 1024, 0, 0));
						foam0 += saturate(st)*0.2;
						water0 = 0.25 + foam0 * saturate(terra.a - _WaterLevel.x + _WaterLevel.y) * _WaterLevel.z;
						f1 = saturate( (0.2 - _Distance) * 5);
						half2 waveDisp1 = half2(wt, cos(wt+vxy * 1024.0)) * 0.0002;
						water1 = tex2Dlod(_Water, float4((uv + waveDisp1) * 512, 0, 0));
						f2 = saturate( (0.3 - _Distance) * 2.0);
						if (f2>0) {
							wt*=1.1;
							half2 waveDisp2 = half2(wt, cos(wt+vxy * 128.0)) * 0.0005;
							water2 = tex2Dlod(_Water, float4((uv + waveDisp2) * 64, 0, 0));
						} else {
							water2 = zeros;
						}
					} else {
						water0 = zeros;
						water1 = zeros;
						water2 = zeros;
						waterBG += (tex2Dlod(_Noise, float4(uv, 0, 0)) - 0.5)*0.1;
					}
					
					fixed4 water = (water0 * f0 + water1 * f1 + water2 * f2 + waterBG) / (f0+f1+f2+1.0);
					return water;
				}
				
				v2f vert( appdata_base v ) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					// Push back
					#if UNITY_REVERSED_Z
						o.pos.z -= 0.0005;
					#else
						o.pos.z += 0.0005;
					#endif
					o.uv = v.texcoord;
					return o;
				}

				fixed4 frag (v2f i) : SV_Target {
					half4 terra = tex2D(_TerrestrialMap, i.uv);
					half elevation = terra.r; 
					fixed4 color;
					if (elevation<_WaterLevel.x) {
						color = getWater(i.uv, terra);
					} else {
						// compute Earth pixel color
						if (i.uv.x<0.5) {
							if (i.uv.y>0.5) {
								color = tex2Dlod(_TexTL, float4(i.uv.x * 2.0, (i.uv.y - 0.5) * 2.0, 0, 0));
							} else {
								color = tex2Dlod(_TexBL, float4(i.uv.x * 2.0, i.uv.y * 2.0, 0, 0));
							}
						} else {
							if (i.uv.y>0.5) {
								color = tex2Dlod(_TexTR, float4((i.uv.x - 0.5) * 2.0f, (i.uv.y - 0.5) * 2.0, 0, 0));
							} else {
								color = tex2Dlod(_TexBR, float4((i.uv.x - 0.5) * 2.0f, i.uv.y * 2.0, 0, 0));
							}
						}
						color.a = 0;
					}

					if (_CloudShadowStrength>0) {
						fixed4 shadowColor = tex2Dlod (_CloudMap, float4(i.uv + _CloudMapOffset, 0, 0));
						fixed4 shadows = shadowColor * shadowColor.a * _CloudShadowStrength;
						color *= saturate(1.0f - shadows);
					}
					return color;
				}
			
			ENDCG
		}
	}
}