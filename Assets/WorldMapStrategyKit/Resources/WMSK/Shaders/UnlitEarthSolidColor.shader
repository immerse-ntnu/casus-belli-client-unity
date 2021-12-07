Shader "WMSK/Unlit Earth Solid Color" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }
   SubShader {
       Tags { "Queue"="Geometry" "RenderType"="Opaque" }
       Pass {
       	CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag	
		#include "UnityCG.cginc"

          fixed4 _Color;

				struct v2f {
					float4 pos : SV_POSITION;
				};

				v2f vert( appdata_base v ) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					// Push back
					#if UNITY_REVERSED_Z
						o.pos.z -= 0.0005;
					#else
						o.pos.z += 0.0005;
					#endif
					return o;
				}

				fixed4 frag (v2f i) : SV_Target {
					return _Color;
				}

		ENDCG
        }
    } 
    FallBack Off
}