 Shader "WMSK/Unlit Tile Overlay Alpha" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture 1", 2D) = "white"
        _MainTex1 ("Texture 2", 2D) = "white"
        _MainTex2 ("Texture 3", 2D) = "white"
        _MainTex3 ("Texture 4", 2D) = "white"
    }

   	SubShader {
   		
       Tags {
	       "Queue"="Geometry-1" 
//	       "Queue"="Transparent" 
       }
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

       Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag
        #pragma fragmentoption ARB_precision_hint_fastest
        #include "UnityCG.cginc"

		sampler2D _MainTex;
		sampler2D _MainTex1;
		sampler2D _MainTex2;
		sampler2D _MainTex3;
		fixed4 _Color;
		
		struct appdata {
			float4 vertex : POSITION;
			float2 texcoord: TEXCOORD0;
			fixed4 color: COLOR;
		};

		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv: TEXCOORD0;
			fixed4 color: COLOR;
		};
		
		v2f vert(appdata v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
			o.color = v.color;
			return o;
		}
		
		fixed4 frag(v2f i) : SV_Target {
			fixed4 p0 = tex2D(_MainTex, i.uv);
			fixed4 p1 = tex2D(_MainTex1, i.uv);
			fixed4 p2 = tex2D(_MainTex2, i.uv);
			fixed4 p3 = tex2D(_MainTex3, i.uv);
			fixed4 p = p0 * i.color.rrrr + p1 * i.color.gggg + p2 * i.color.bbbb + p3 * i.color.aaaa;
			p.a *= _Color.a;
			return p;
		}
			
		ENDCG
    }
  }  
}