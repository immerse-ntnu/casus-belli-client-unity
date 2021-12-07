 
Shader "WMSK/Unlit Overlay" {
 
Properties
    {
       _MainTex ("Texture", 2D) = ""
    }
 
SubShader
    {
        Tags {
         "Queue" = "Transparent" 
         "RenderType"="Transparent"
        }
        Lighting Off
        ZWrite Off
//        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            SetTexture[_MainTex] { Combine texture, texture * primary}
        }
    }
}