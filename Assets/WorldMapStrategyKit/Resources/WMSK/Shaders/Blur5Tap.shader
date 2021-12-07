Shader "WMSK/Blur5Tap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		ZTest Always ZWrite Off Cull Off

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
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				// sample the texture
				half4 c0 = tex2D(_MainTex, i.uv);
				half4 c1 = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0));
				half4 c2 = tex2D(_MainTex, i.uv - float2(_MainTex_TexelSize.x, 0));
				half4 c3 = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y));
				half4 c4 = tex2D(_MainTex, i.uv - float2(0, _MainTex_TexelSize.y));
				return (c0+c1+c2+c3+c4) * 0.2;
			}
			ENDCG
		}
	}
}
