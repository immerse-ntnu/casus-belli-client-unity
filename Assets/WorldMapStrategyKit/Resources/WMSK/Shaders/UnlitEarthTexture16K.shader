Shader "WMSK/Unlit Earth 16K" {

	Properties {
		_TexTL ("Tex TL", 2D) = "white" {}
		_TexTR ("Tex TR", 2D) = "white" {}
		_TexBL ("Tex BL", 2D) = "white" {}
		_TexBR ("Tex BR", 2D) = "white" {}
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

				struct v2f {
					float4 pos : SV_POSITION;
					float2 uv: TEXCOORD0;
				};

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
					fixed4 color;
					float mip = -8;
					// compute Earth pixel color
					if (i.uv.x<0.5) {
						if (i.uv.y>0.5) {
							color = tex2Dlod(_TexTL, float4(i.uv.x * 2.0, (i.uv.y - 0.5) * 2.0, 0, mip));
						} else {
							color = tex2Dlod(_TexBL, float4(i.uv.x * 2.0, i.uv.y * 2.0, 0, mip));
						}
					} else {
						if (i.uv.y>0.5) {
							color = tex2Dlod(_TexTR, float4((i.uv.x - 0.5) * 2.0f, (i.uv.y - 0.5) * 2.0, 0, mip));
						} else {
							color = tex2Dlod(_TexBR, float4((i.uv.x - 0.5) * 2.0f, i.uv.y * 2.0, 0, mip));
						}
					}
					return color;
				}
			
			ENDCG
		}
	}
}