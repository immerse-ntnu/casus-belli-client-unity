// Used by cities material

Shader "WMSK/Unlit Alpha Texture Color" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white"
    }

   	SubShader {
       Tags {
       	"Queue"="Geometry+302" // Draw over highlight
//       	"Queue"="Transparent" // Draw over highlight
       }

 	ZWrite Off
 	Blend SrcAlpha OneMinusSrcAlpha
    Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				
		#include "UnityCG.cginc"

		fixed4 _Color;
		sampler2D _MainTex;

		struct AppData {
			float4 vertex : POSITION;
			float2 texcoord: TEXCOORD0;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			#if UNITY_REVERSED_Z
				v.vertex.z += 0.0005;
			#else
				v.vertex.z -= 0.0005;
			#endif
		}
		
		fixed4 frag(AppData i) : SV_Target {
			return tex2D(_MainTex, i.texcoord) * _Color;					
		}
			
		ENDCG
    }
    }
    
}