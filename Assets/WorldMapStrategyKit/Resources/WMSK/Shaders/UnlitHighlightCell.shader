Shader "WMSK/Unlit Highlight Cell" {
Properties {
    _Color ("Color", Color) = (1,1,1,0.5)
    _MainTex ("Water Mask", 2D) = "white" {}
    _WaterLevel ("Water Level", Float) = 0.1
    _AlphaOnWater ("Alpha On Wagter", Float) = 0.2
}
 
SubShader {
  Tags {
        "Queue"="Geometry+5"
        "IgnoreProjector"="True"
        "RenderType"="Transparent"
    }
	Cull Off		
	ZWrite Off		
	Offset -5, -5
	Blend SrcAlpha OneMinusSrcAlpha
    Pass {
     	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag
        #pragma multi_compile _ USE_MASK
		#include "UnityCG.cginc"			

		sampler2D _MainTex;
		fixed4 _Color;
		fixed _WaterLevel, _AlphaOnWater;

		struct appdata {
			float4 vertex : POSITION;
            float2 uv     : TEXCOORD0;
            #if USE_MASK
                float2 uv2    : TEXCOORD1;
            #endif
		};

		struct v2f {
			float4 pos : SV_POSITION;	
			float2 uv  : TEXCOORD0;
            #if USE_MASK
                float2 uv2  : TEXCOORD1;
            #endif
		};
		
		v2f vert(appdata v) {
			v2f o;							
			o.pos = UnityObjectToClipPos(v.vertex);
			#if UNITY_REVERSED_Z
				o.pos.z += 0.001;
			#else
				o.pos.z -= 0.001;
			#endif
			o.uv = v.uv;
            #if USE_MASK
                o.uv2 = v.uv2;
            #endif
			return o;									
		}
		
		fixed4 frag(v2f i) : SV_Target {
			fixed4 color = _Color;
            #if USE_MASK
    			fixed4 mask = tex2D(_MainTex, i.uv2);
	    		if (mask.a <= _WaterLevel) {
		    		color.a *= _AlphaOnWater;
    			}
            #endif
			return color;
		}
			
		ENDCG
    }
}
}