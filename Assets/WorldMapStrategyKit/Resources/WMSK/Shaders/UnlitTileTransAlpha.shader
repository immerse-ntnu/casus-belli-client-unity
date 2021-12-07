Shader "WMSK/Unlit Tile Overlay Trans Alpha" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _Alpha ("Alpha 1", Float) = 0
        _Alpha1 ("Alpha 2", Float) = 0
        _Alpha2 ("Alpha 3", Float) = 0
        _Alpha3 ("Alpha 4", Float) = 0
        _MainTex ("Texture 1", 2D) = "white"
        _MainTex1 ("Texture 2", 2D) = "white"
        _MainTex2 ("Texture 3", 2D) = "white"
        _MainTex3 ("Texture 4", 2D) = "white"
        _ParentTex ("Parent Texture", 2D) = "white"
        _ParentCoords ("Parent Tex Coords 1", Vector) = (0,0,1,1)
        _ParentCoords1 ("Parent Tex Coords 2", Vector) = (0,0,1,1)
        _ParentCoords2 ("Parent Tex Coords 3", Vector) = (0,0,1,1)
        _ParentCoords3 ("Parent Tex Coords 4", Vector) = (0,0,1,1)
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
		sampler2D _ParentTex;
		fixed4 _Color;
		fixed _Alpha;
		fixed _Alpha1;
		fixed _Alpha2;
		fixed _Alpha3;
		float4 _ParentCoords;
		float4 _ParentCoords1;
		float4 _ParentCoords2;
		float4 _ParentCoords3;
		
		struct appdata {
			float4 vertex  : POSITION;
			float2 texcoord: TEXCOORD0;
			fixed4 color   : COLOR;
		};

		struct v2f {
			float4 pos     : SV_POSITION;
			float2 uv      : TEXCOORD0;
			fixed4 color   : COLOR;
			float2 uvParent: TEXCOORD1;
			fixed  t       : TEXCOORD2;
		};
		
		v2f vert(appdata v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.color = v.color;
			o.t = dot(fixed4(_Alpha, _Alpha1, _Alpha2, _Alpha3), v.color);
			o.uv = v.texcoord;
			float4 parentCoords = _ParentCoords * v.color.rrrr + _ParentCoords1 * v.color.gggg + _ParentCoords2 * v.color.bbbb + _ParentCoords3 * v.color.aaaa;
			o.uvParent = float2(lerp(parentCoords.x, parentCoords.z, o.uv.x), lerp(parentCoords.y, parentCoords.w, o.uv.y));
			return o;
		}
		
		fixed4 frag(v2f i) : SV_Target {
			fixed4 p0 = tex2D(_MainTex, i.uv);
			fixed4 p1 = tex2D(_MainTex1, i.uv);
			fixed4 p2 = tex2D(_MainTex2, i.uv);
			fixed4 p3 = tex2D(_MainTex3, i.uv);
			fixed4 p = p0 * i.color.rrrr + p1 * i.color.gggg + p2 * i.color.bbbb + p3 * i.color.aaaa;
			fixed4 ph = tex2D(_ParentTex, i.uvParent);
			fixed4 c = lerp(ph, p, i.t);
			c.a *= _Color.a;
			return c;
		}
			
		ENDCG
    }
  }  
}