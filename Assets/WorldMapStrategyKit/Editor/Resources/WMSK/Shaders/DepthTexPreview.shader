Shader "WMSK/Editor/DepthTexPreview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Sea Level", float) = 0
		_SeaColor ("Sea Color", Color) = (0,0,0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
            fixed4 _Color;
            fixed _SeaLevel;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D(_MainTex, i.uv);
				color = lerp(color, _Color, color.a < _SeaLevel);
				return color;
			}

			ENDCG
		}
	}
}
