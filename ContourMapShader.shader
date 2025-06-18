Shader "Custom/ContourMap"
{
    Properties
    {
        _ContourSpacing ("Contour Spacing", Float) = 2.0
        _LineWidth ("Line Width", Float) = 0.1
        _BaseColor ("Base Color", Color) = (1,1,1,0.2)
        _LineColor ("Line Color", Color) = (1,1,1,1)
		_BackgroundColor ("Background Color", Vector) = (0,0,0,1)
        _HeightMin ("Height Min", Float) = 0
        _HeightMax ("Height Max", Float) = 100
        _MaxOpacity ("Maximum Opacity", Range(0, 1)) = 0.8
		_OpacityMultiplier ("Opacity Multiplier", Range(0.1, 5.0)) = 1.0
        _GradientSmoothing ("Gradient Smoothing", Range(0.1, 2.0)) = 1.0
		_ContourOffset ("Contour Offset", Range(-1.0, 1.0)) = 0
		_HeightOffset ("Height Offset", Range(-10.0, 10.0)) = 0
    }
    
    SubShader
    {
	    Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float worldY : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };
            
            float _ContourSpacing;
            float _LineWidth;
            float4 _BaseColor;
            float4 _LineColor;
			float4 _BackgroundColor;
            float _HeightMin;
            float _HeightMax;
            float _MaxOpacity;
			float _OpacityMultiplier;
            float _GradientSmoothing;
			float _ContourOffset;
			float _HeightOffset;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldY = o.worldPos.y + _HeightOffset;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
				float heightGradient = length(fwidth(i.worldY));
				heightGradient = max(heightGradient * _GradientSmoothing, 0.001);
			
				float contourLevel = _ContourOffset + round(i.worldY / _ContourSpacing) * _ContourSpacing;
                float distToLine = abs(i.worldY - contourLevel);
			
				float screenDist = distToLine / heightGradient;
                float lineStrength = 1.0 - smoothstep(0, _LineWidth, screenDist);
                
                float regionHeight = _ContourOffset + floor(i.worldY / _ContourSpacing) * _ContourSpacing;
                float regionHeightNorm = saturate((regionHeight - _HeightMin) / (_HeightMax - _HeightMin));
                
                float4 regionColor = _BaseColor;
                regionColor.rgb = regionColor.rgb * regionHeightNorm;
                float regionOpacity = _BaseColor.a * min(min(regionHeightNorm, 1.0) * _OpacityMultiplier, _MaxOpacity);
				
				float4 contourColor = lerp(regionColor, _LineColor, lineStrength);
				float contourAlpha = max(regionOpacity, lineStrength * _LineColor.a);
				
                float4 finalColor;
                finalColor.rgb = contourColor.rgb * contourAlpha + _BackgroundColor.rgb * (1.0 - contourAlpha);
                finalColor.a = 1.0;
				
                return finalColor;
            }
            ENDCG
        }
    }
}
