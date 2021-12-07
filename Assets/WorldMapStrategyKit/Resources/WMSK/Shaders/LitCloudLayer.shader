Shader "WMSK/Lit Cloud Layer" {
	Properties {
                _MainTex ("Emission (RGB)", 2D) = "white" {}
                _Specular ("Specular", Color) = (0, 0, 0, 0)
                _Smoothness ("Smoothness", Range(0,1)) = 0.0
         		[HideInInspector] _Brightness("Brightness", Float) = 1
        }
   SubShader {
                Tags { "Queue"="Transparent+2" "RenderType"="Transparent" }
           		ZWrite Off
           		Offset -1, -1
                LOD 200
         
                CGPROGRAM
                // Physically based Standard lighting model, and enable shadows on all light types
                #pragma surface surf StandardSpecular alpha vertex:vert
                #pragma target 3.0

                sampler2D _MainTex;

                struct Input {
                    float2 uv_MainTex;
                };
                half _Smoothness;
                fixed4 _Specular;
                half _Brightness;

				void vert (inout appdata_full v, out Input data) {
          			UNITY_INITIALIZE_OUTPUT(Input,data);
      			}

                void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
                    fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
                    o.Albedo = fixed3(0,0,0);
                    o.Smoothness = _Smoothness;
                    o.Specular = _Specular;
                    o.Emission = c.rgb * _Brightness;
                    o.Alpha = c.a * _Brightness;
                }
                ENDCG
        }
//        FallBack "Diffuse"
}