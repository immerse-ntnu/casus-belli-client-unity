Shader "WMSK/Unlit Single Color Order 1" {
 
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1)
}
 
SubShader {
        Tags {
        "Queue"="Geometry+1"
        "RenderType"="Opaque"
    	}
    Pass {
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
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			return _Color;
		}
			
		ENDCG
    }
}
 
}
