Shader "WMSK/Unlit Province Borders Order 2 Thick" {
 
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1)
}
SubShader {
	LOD 300
        Tags {
        "Queue"="Geometry+260"
        "RenderType"="Opaque"
    }
    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite Off // avoids z-fighting issues with country frontiers
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

SubShader {
	LOD 200
        Tags {
        "Queue"="Geometry+260"
        "RenderType"="Opaque"
    }
    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite Off // avoids z-fighting issues with country frontiers
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
			o.pos.y += 2.0 * (o.pos.w/_ScreenParams.x);								
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			return fixed4(_Color.rgb, _Color.a * 0.5);
		}
			
		ENDCG
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
			o.pos.y -= 2.0 * (o.pos.w/_ScreenParams.x);								
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			return fixed4(_Color.rgb, _Color.a * 0.5);
		}
			
		ENDCG
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
			o.pos.x += 2.0 * (o.pos.w/_ScreenParams.x);
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			return fixed4(_Color.rgb, _Color.a * 0.5);
		}
			
		ENDCG
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
			o.pos.x -= 2.0 * (o.pos.w/_ScreenParams.x);
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			return fixed4(_Color.rgb, _Color.a * 0.5);
		}
			
		ENDCG
    }
}
 
}
