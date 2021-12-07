Shader "WMSK/Unlit Single Color Hex Grid" {
 
Properties {
    _Color ("Color", Color) = (1,1,1)
    _MainTex ("Water Mask", 2D) = "white" {}
    _WaterLevel ("Water Level", Float) = 0.1
    _AlphaOnWater ("Alpha On Wagter", Float) = 0.2
}
 
SubShader {
    Tags {
        "Queue"="Geometry+260"
        "RenderType"="Opaque"
    }
	Pass {
	Blend SrcAlpha OneMinusSrcAlpha
	ZWrite Off
 	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag	
		#include "UnityCG.cginc"			

		sampler2D _MainTex;
		fixed4 _Color;
		fixed _WaterLevel, _AlphaOnWater;

		struct appdata {
			float4 vertex : POSITION;
		};

		struct v2f {
			float4 pos : SV_POSITION;	
			float2 uv  : TEXCOORD0;
		};
		
		v2f vert(appdata v) {
			v2f o;							
			o.pos = UnityObjectToClipPos(v.vertex);
			#if UNITY_REVERSED_Z
				o.pos.z += 0.001;
			#else
				o.pos.z -= 0.001;
			#endif
			o.uv = v.vertex.xy + 0.5.xx;
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			fixed4 mask = tex2D(_MainTex, i.uv);
			fixed4 color = _Color;
			if (mask.a <= _WaterLevel) {
				color.a *= _AlphaOnWater;
			}
			return color;
		}
			
		ENDCG
    }

    Pass {
	Blend SrcAlpha OneMinusSrcAlpha
	ZWrite Off
 	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag	
		#include "UnityCG.cginc"			

		sampler2D _MainTex;
		fixed4 _Color;
		fixed _WaterLevel, _AlphaOnWater;

		struct appdata {
			float4 vertex : POSITION;
			float2 uv  : TEXCOORD0;

		};

		struct v2f {
			float4 pos : SV_POSITION;	
			float2 uv  : TEXCOORD0;
		};
		
		v2f vert(appdata v) {
			v2f o;							
			o.pos = UnityObjectToClipPos(v.vertex);
			#if UNITY_REVERSED_Z
				o.pos.z += 0.0001;
			#else
				o.pos.z -= 0.0001;
			#endif
			o.pos.x += 2 * (o.pos.w/_ScreenParams.x);
			o.uv = v.uv;
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			fixed4 mask = tex2D(_MainTex, i.uv);
			fixed4 color = _Color * 0.95;
			if (mask.a <= _WaterLevel) {
				color.a *= _AlphaOnWater;
			}
			return color;
		}
			
		ENDCG
    }

    Pass {
	Blend SrcAlpha OneMinusSrcAlpha
	ZWrite Off
 	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag	
		#include "UnityCG.cginc"			

		sampler2D _MainTex;
		fixed4 _Color;
		fixed _WaterLevel, _AlphaOnWater;

		struct appdata {
			float4 vertex : POSITION;
			float2 uv  : TEXCOORD0;
		};

		struct v2f {
			float4 pos : SV_POSITION;	
			float2 uv  : TEXCOORD0;
		};
		
		v2f vert(appdata v) {
			v2f o;							
			o.pos = UnityObjectToClipPos(v.vertex);
			#if UNITY_REVERSED_Z
				o.pos.z += 0.0001;
			#else
				o.pos.z -= 0.0001;
			#endif
			o.pos.y += 2 * (o.pos.w/_ScreenParams.y);
			o.uv = v.uv;
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			fixed4 mask = tex2D(_MainTex, i.uv);
			fixed4 color = _Color * 0.95;
			if (mask.a <= _WaterLevel) {
				color.a *= _AlphaOnWater;
			}
			return color;
		}
			
		ENDCG
    }

}
}
