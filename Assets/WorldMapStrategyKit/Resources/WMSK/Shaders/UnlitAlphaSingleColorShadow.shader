Shader "WMSK/Unlit Alpha Single Color Shadow" {
 
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1,0.5)
}
 
SubShader {
	Tags {
        "Queue"="Transparent+1"
        "RenderType"="Transparent"
    }
	Blend SrcAlpha OneMinusSrcAlpha
   	Offset -1, -1
   	
 	CGPROGRAM
      #pragma surface surf Lambert keepalpha addshadow
      
      struct Input {
          float4 color : SV_Target;
      };
      
      fixed4 _Color;
      
      void surf (Input IN, inout SurfaceOutput o) {
      	  o.Albedo = 0;
          o.Emission = _Color.rgb;
          o.Alpha = _Color.a;
      }
      ENDCG
 
}

}
