Shader "WMSK/Unlit Marker Line" {
 
Properties {
    _Color ("Color", Color) = (1,1,1,0.5)
    _MainTex("Texture (RGBA)", 2D) = "white" {}
}
 
SubShader {
         Tags {"Queue"="Geometry+301" "IgnoreProjector"="True" "RenderType"="Transparent"}
         Offset -1,-1
         ZWrite Off
         Blend SrcAlpha OneMinusSrcAlpha 
         ColorMask RGB
             
	Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				
		#include "UnityCG.cginc"

		fixed4 _Color;
		sampler2D _MainTex;
		float4 _MainTex_ST;

		struct AppData {
			float4 vertex : POSITION;
			float2 uv: TEXCOORD0;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.uv = TRANSFORM_TEX(v.uv, _MainTex);
		}
		
		fixed4 frag(AppData i) : SV_Target {
			fixed4 pix = tex2D(_MainTex, i.uv);
			return _Color * pix;					
		}
			
		ENDCG
    }
 }

}
 