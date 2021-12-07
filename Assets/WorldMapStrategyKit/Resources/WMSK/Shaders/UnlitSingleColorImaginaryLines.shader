Shader "WMSK/Unlit Single Color Imaginary Lines" {
 
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1)
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

		fixed4 _Color;

		struct appdata {
			float4 vertex : POSITION;
		};

		struct v2f {
			float4 pos : SV_POSITION;	
		};
		
		v2f vert(appdata v) {
			v2f o;							
			o.pos = UnityObjectToClipPos(v.vertex);
			#if UNITY_REVERSED_Z
				o.pos.z += 0.0001;
			#else
				o.pos.z -= 0.0001;
			#endif
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			return _Color;
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

		fixed4 _Color;

		struct appdata {
			float4 vertex : POSITION;
		};

		struct v2f {
			float4 pos : SV_POSITION;	
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
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			return _Color;
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

		fixed4 _Color;

		struct appdata {
			float4 vertex : POSITION;
		};

		struct v2f {
			float4 pos : SV_POSITION;	
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
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			return _Color;
		}
			
		ENDCG
    }

}
}
