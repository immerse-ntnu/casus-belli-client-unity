Shader "WMSK/Unlit Single Color Cursor" {
 
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1)
    _Orientation ("Orientation", Float) = 0 // 0 = horizontal, 1 = vertical
}
 
SubShader {
    Color [_Color]
        Tags {
        "Queue"="Transparent"
        "RenderType"="Transparent"
    }
    ZWrite Off
    Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				
		
		#include "UnityCG.cginc"
		
		fixed4 _Color;
		float _Orientation;

		struct AppData {
			float4 vertex : POSITION;
			float4 scrPos : TEXCOORD1;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.scrPos = ComputeScreenPos(v.vertex);
		}
		
		fixed4 frag(AppData i) : SV_Target {
			float2 wcoord = (i.scrPos.xy/i.scrPos.w);
			wcoord.x *= _ScreenParams.x;
			wcoord.y *= _ScreenParams.y;
			float wc = _Orientation==0 ? wcoord.x: wcoord.y;
			if ( fmod((int)(wc/4),2) )
				discard;
			return _Color;					
		}
			
		ENDCG
    }

}
 
}
