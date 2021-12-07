Shader "WMSK/Unlit Single Texture"{

Properties {
    _MainTex ("Texture", 2D) = ""
}
SubShader {
	    Tags {
        "Queue"="Geometry"
        "RenderType"="Opaque"
    }
    Offset 2,2
    CGPROGRAM
    #pragma surface surf SimpleLambert

    sampler2D _MainTex;
      
    struct Input {
        float2 uv_MainTex;
    };
                
                
 	half4 LightingSimpleLambert (SurfaceOutput s, half3 lightDir, half atten) {
        half4 c;
        c.rgb = s.Emission * atten;
        c.a = 1.0;
        return c;
    }
                          
    void surf (Input IN, inout SurfaceOutput o) {
        o.Albedo = 0;
        o.Emission = tex2D (_MainTex, IN.uv_MainTex).rgb * 0.5;
    }
    ENDCG
   }
Fallback "Diffuse"
}
