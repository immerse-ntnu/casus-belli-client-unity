Shader "WMSK/Unlit Country Frontiers Geom" {
 
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
    _Color ("Color", Color) = (0,1,0,1)
    _Thickness ("Thickness", Float) = 0.05
}
 
SubShader {
	LOD 300
    Tags {
        "Queue"="Geometry+300"
        "RenderType"="Opaque"
    	}
 	Blend SrcAlpha OneMinusSrcAlpha    	
    Pass {
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag	
		#include "UnityCG.cginc"

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
			#if UNITY_REVERSED_Z
			v.vertex.z+= 0.00001;
			#else
			v.vertex.z-=0.00001;
			#endif		
		}
		
		fixed4 frag(AppData i) : SV_Target {
			return _Color;					
		}
		ENDCG
		
    }
}

// LOD 100: WITH GEOMETRY SHADER

SubShader {
	LOD 100
    Tags {
        "Queue"="Geometry+300"
        "RenderType"="Opaque"
    	}
     Blend SrcAlpha OneMinusSrcAlpha
	 ZTest Less
	 Cull Off
     Pass {
	   	CGPROGRAM
		#pragma vertex vert	
		#pragma geometry geom
		#pragma fragment frag				
		#pragma target 4.0

		#include "UnityCG.cginc"
		
		fixed4 _Color;
		float _Thickness;

		struct v2g {
			float4 vertex : POSITION;
		};

		struct g2f {
			float4 pos    : SV_POSITION;
		};
		
		void vert(inout v2g v) {
			v.vertex = UnityObjectToClipPos(v.vertex);
		}

		[maxvertexcount(6)]
        void geom(line v2g p[2], inout TriangleStream<g2f> outputStream) {
           float4 p0 = p[0].vertex;
           float4 p1 = p[1].vertex;

           float4 ab = p1 - p0;
           float4 normal = float4(-ab.y, ab.x, 0, 0);
           normal.xy = normalize(normal.xy) * _Thickness;

           float4 tl = p0 - normal;
           float4 bl = p0 + normal;
           float4 tr = p1 - normal;
           float4 br = p1 + normal;
  		   float4 dd = float4(normalize(p1.xy-p0.xy) * _Thickness, 0, 0);

           g2f pIn;
           pIn.pos = p0 - dd;
           outputStream.Append(pIn);
           pIn.pos = bl;
           outputStream.Append(pIn);
           pIn.pos = tl;
           outputStream.Append(pIn);
           pIn.pos = br;
           outputStream.Append(pIn);
           pIn.pos = tr;
           outputStream.Append(pIn);
           pIn.pos = p1 + dd;
           outputStream.Append(pIn);
 		}
		
		fixed4 frag(g2f i) : SV_Target {
			return _Color;
		}
		ENDCG
    }
            
 }

}
