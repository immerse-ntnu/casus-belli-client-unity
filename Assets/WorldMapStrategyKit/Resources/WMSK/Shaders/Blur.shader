Shader "WMSK/Blur" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	CGINCLUDE
	#pragma fragmentoption ARB_precision_hint_fastest
	#include "UnityCG.cginc"
	
	const float blur_offset[3] = { 0.0, 1.3846153846, 3.2307692308 };
	const float blur_weight[3] = { 0.2270270270, 0.3162162162, 0.0702702703 };

	
    const float3 ones = float3(1.0, 1.0, 1.0);
                                              
	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	
	inline float getBlurOffset(int s) {
		if (s==1) return blur_offset[1];
		else if (s==2) return blur_offset[2];
		return blur_offset[0];
	}
	
	inline float getBlurWeight(int s) {
		if (s==1) return blur_weight[1];
		else if (s==2) return blur_weight[2];
		return blur_weight[0];	
	}
	
	fixed4 Blur(float2 uv, float2 inc) {

		fixed4 rgbM = tex2D (_MainTex, uv);
		float bloomDenom = blur_weight[0];
		float3 bloomSum = rgbM.rgb * bloomDenom;
		float2 nuv;
    	for (int s = 1; s < 3; ++s) {
    		float gaussian = getBlurWeight(s);
        	
        	nuv = uv - inc * getBlurOffset(s);
       		bloomSum += tex2D (_MainTex, nuv).rgb * gaussian;
       		bloomDenom += gaussian;
       		
        	nuv = uv + inc * getBlurOffset(s);
       		bloomSum += tex2D (_MainTex, nuv).rgb * gaussian;
       		bloomDenom += gaussian;
    	}
    	return fixed4(bloomSum / bloomDenom, rgbM.a);
	}

	// ---------------------------------------------------------------------------------------------------
	// Vertex shader
	// ---------------------------------------------------------------------------------------------------
	
	struct v2f
	{
		float4 pos : SV_POSITION; 
		float2 uv : TEXCOORD0;
	};

	v2f vert(appdata_img v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		#ifdef UNITY_HALF_TEXEL_OFFSET
        	v.texcoord.y += _MainTex_TexelSize.y;
        #endif
		#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				v.texcoord.y = 1.0 - v.texcoord.y;
		#endif
		o.uv = v.texcoord;
		return o; 
	}
	
	
	// ---------------------------------------------------------------------------------------------------
	// Fragment shaders
	// ---------------------------------------------------------------------------------------------------

	fixed4 fragBlurH (v2f_img i) : SV_Target {
		return Blur(i.uv, float2(_MainTex_TexelSize.x, 0));
	}

	fixed4 fragBlurV(v2f_img i) : SV_Target {
		return Blur(i.uv, float2(0, _MainTex_TexelSize.y));
	}
	
	ENDCG
	
Subshader {
 	ZTest Off Cull Off ZWrite Off Blend Off
 	Fog { Mode off }

	Pass { // 1: Blur horizontally
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment fragBlurH
		ENDCG
	}

	Pass { // 2: Blur vertically and blend with AO texture
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment fragBlurV
		ENDCG
	}
	
}

FallBack Off
}