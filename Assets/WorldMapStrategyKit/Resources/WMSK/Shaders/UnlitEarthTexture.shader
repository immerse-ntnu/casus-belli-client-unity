Shader "WMSK/Unlit Earth Texture" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white"
		_NormalMap ("Normal Map", 2D) = "bump" {}
		_BumpAmount ("Bump Amount", Range(0, 1)) = 0.5
		_SunLightDirection("Sun Light Direction", Vector) = (0,0,1)
    }
   SubShader {
       Tags { "Queue"="Geometry" "RenderType"="Opaque" }
       Pass {
       	CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag	
		#pragma multi_compile __ WMSK_BUMPMAP_ENABLED
		#include "UnityCG.cginc"

          half4 _Color;
          sampler2D _MainTex;
          float4 _MainTex_ST;
          sampler2D _NormalMap;
          half _BumpAmount;
          half3 _SunLightDirection;

				struct v2f {
					float4 pos : SV_POSITION;
					float2 uv  : TEXCOORD0;
					#if WMSK_BUMPMAP_ENABLED
					half3 wNormal : TEXCOORD1;
					half3 tspace0 : TEXCOORD2; // tangent.x, bitangent.x, normal.x
                	half3 tspace1 : TEXCOORD3; // tangent.y, bitangent.y, normal.y
                	half3 tspace2 : TEXCOORD4; // tangent.z, bitangent.z, normal.z
                	#endif
				};

				v2f vert(float4 vertex : POSITION, half3 normal : NORMAL, half4 tangent : TANGENT, float2 uv : TEXCOORD0) {
					v2f o;
					o.pos = UnityObjectToClipPos(vertex);
					// Push back
					#if UNITY_REVERSED_Z
						o.pos.z -= 0.0005;
					#else
						o.pos.z += 0.0005;
					#endif
					o.uv = TRANSFORM_TEX(uv, _MainTex);
					// normal stuff
					#if WMSK_BUMPMAP_ENABLED
					half3 wNormal = UnityObjectToWorldNormal(normal);
					o.wNormal = wNormal;
	                half3 wTangent = UnityObjectToWorldDir(tangent.xyz);
        	        half tangentSign = tangent.w * unity_WorldTransformParams.w;
            	    half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
                	// output the tangent space matrix
	                o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
    	            o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
        	        o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
        	        #endif

					return o;
				}

				half4 frag (v2f i) : SV_Target {
					half4 color = tex2D(_MainTex, i.uv) * _Color;

					// transform normal from tangent to world space
					#if WMSK_BUMPMAP_ENABLED
					half3 tnormal = UnpackNormal(tex2D(_NormalMap, i.uv)); 
                	half3 worldNormal;
                	worldNormal.x = dot(i.tspace0, tnormal);
                	worldNormal.y = dot(i.tspace1, tnormal);
                	worldNormal.z = dot(i.tspace2, tnormal);
                	half  LdotS = saturate(dot(_SunLightDirection, normalize(worldNormal)));
                	half wrappedDiffuse = LdotS * 0.5 + 0.5;
                	color = lerp(color, color * wrappedDiffuse, _BumpAmount);
                	#endif
                	return color;

				}

		ENDCG
        }
    } 
    FallBack Off
}