Shader "WMSK/Unlit Earth Scenic" {

	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}
		_BumpAmount ("Bump Amount", Range(0, 1)) = 0.5
		_CloudMap ("Cloud Map", 2D) = "black" {}
		_CloudSpeed ("Cloud Speed", Range(-1, 1)) = -0.04
		_CloudAlpha ("Cloud Alpha", Range(0, 1)) = 1
		_CloudShadowStrength ("Cloud Shadow Strength", Range(0, 1)) = 0.2
		_CloudElevation ("Cloud Elevation", Range(0.001, 0.1)) = 0.003
	}
	
	Subshader {
		Tags { "RenderType"="Opaque" }
    
Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag   
                #pragma target 3.0
                #pragma multi_compile __ WMSK_BUMPMAP_ENABLED
                #include "UnityCG.cginc"

              sampler2D _MainTex;
              sampler2D _NormalMap;
              sampler2D _CloudMap;
              float _BumpAmount;
              float _CloudSpeed;
              float _CloudAlpha;
              float _CloudShadowStrength;
              float _CloudElevation;
	          half3 _SunLightDirection;
                float4 _MainTex_ST;

                struct v2f {
                    float4 pos : SV_POSITION;
                    float2 uv: TEXCOORD0;
                    #if WMSK_BUMPMAP_ENABLED
                    half3 tspace0 : TEXCOORD1; // tangent.x, bitangent.x, normal.x
                    half3 tspace1 : TEXCOORD2; // tangent.y, bitangent.y, normal.y
                    half3 tspace2 : TEXCOORD3; // tangent.z, bitangent.z, normal.z        
                    #endif
                    float3 wpos: TEXCOORD4;            
                };
                
                v2f vert (float4 vertex : POSITION, float3 normal : NORMAL, float4 tangent : TANGENT, float2 uv : TEXCOORD0) {
                    v2f o;
                    o.pos = UnityObjectToClipPos(vertex);
                    // Push back
                    #if UNITY_REVERSED_Z
                        o.pos.z -= 0.0005;
                    #else
                        o.pos.z += 0.0005;
                    #endif
                    o.uv = TRANSFORM_TEX(uv, _MainTex);
                    #if WMSK_BUMPMAP_ENABLED
                half3 wNormal = UnityObjectToWorldNormal(normal);
                half3 wTangent = UnityObjectToWorldDir(tangent.xyz);
                // compute bitangent from cross product of normal and tangent
                half tangentSign = tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
                // output the tangent space matrix
                o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
                #endif
                    o.wpos = mul(unity_ObjectToWorld, vertex);  
                    return o;
                }

                half4 frag (v2f i) : SV_Target {
                  half4 earth = tex2D (_MainTex, i.uv);
                  half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.wpos));
                  
                  #if WMSK_BUMPMAP_ENABLED
                  half3 tnormal = UnpackNormal(tex2D(_NormalMap, i.uv));
                    // transform normal from tangent to world space
                    half3 worldNormal;
                    worldNormal.x = dot(i.tspace0, tnormal);
                    worldNormal.y = dot(i.tspace1, tnormal);
                    worldNormal.z = dot(i.tspace2, tnormal);
//                  	float d = 1.0 - 0.5 * saturate (dot(worldNormal, worldViewDir) + _BumpAmount - 1);
//                  	earth.rgb *= d;

                	half  LdotS = saturate(dot(_SunLightDirection, normalize(worldNormal)));
                	half wrappedDiffuse = LdotS * 0.5 + 0.5;
					earth.rgb = lerp(earth.rgb, earth.rgb * wrappedDiffuse, _BumpAmount);
                  #endif

    
                  fixed2 t = fixed2(_Time[0] * _CloudSpeed, 0);
                  fixed2 disp = -worldViewDir * _CloudElevation;
                    
                  half3 cloud = tex2D (_CloudMap, i.uv + t - disp);
                  half3 shadows = tex2D (_CloudMap, i.uv + t + fixed2(0.998,0) + disp) * _CloudShadowStrength;
                  #if WMSK_BUMPMAP_ENABLED
                  shadows *= saturate (dot(worldNormal, worldViewDir));
                  #endif
                  half3 color = earth.rgb + (cloud.rgb - clamp(shadows.rgb, shadows.rgb, 1-cloud.rgb)) * _CloudAlpha ;
                  return half4(color, 1.0);
                }
            
            ENDCG
        }
    }
}