Shader "TanukiDigital/Decal Forward"
{
	Properties 
	{
		_AlbedoMap ("(RGB)Albedo (A)Alpha", 2D) = "white" {}
		_MaskMap1 ("Mask1: (R)Metallic (G)Smoothness (B)Wetness (A)Occlusion", 2D) = "white" {}
		_MaskMap2 ("Mask2: Emission (R)/Height (A)", 2D) = "white" {}
		//_BumpMap ("Normal map", 2D) = "bump" {}
		_Norm ("Normal map", 2D) = "" {}

		_DecalAlbedoStrength ("Albedo Strength", Range(0,1)) = 0.75
		_DecalNormalStrength ("Normal Strength", Range(0,1)) = 0.0
		_DecalAlphaBlend ("Normal Alpha Blend", Range(0,1)) = 1.0
		_DecalSmoothnessStrength ("Smoothness Strength", Range(0,1)) = 0.0
		_DecalMetalStrength ("Metal Strength", Range(0,1)) = 0.0
		_DecalWetStrength ("Wetness Strength", Range(0,1)) = 0.0
		_DecalEmissionStrength ("(Disabled) Emission Strength", Range(0,16)) = 0.0

		_Height ("Offset Height", Range (0.005, 0.08)) = 0.02
		_DiffuseTint ("Diffuse Tint", Color) = (0.5,0.5,0.5,1)
		_EmissionTint ("(disabled) Emission color", Color) = (0,0,0,0)
		_ColorWetTint ("Wet Tint color", Color) = (0,0,0,0)
	}
	SubShader 
	{


		//BLEND DEFERRED BUFFER VALUES
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "ForceNoShadowCasting"="True"}
		LOD 300
		Offset -1, -1

		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#pragma surface surf Standard exclude_path:prepass noshadow noforwardadd keepalpha
		
		#pragma target 3.0

		sampler2D _AlbedoMap;
		sampler2D _MaskMap1;
		sampler2D _MaskMap2;
		sampler2D _Norm;
		float _DecalAlbedoStrength;
		float _DecalNormalStrength;
		float _DecalAlphaBlend;
		float _DecalSmoothnessStrength;
		float _DecalMetalStrength;
		float _DecalWetStrength;
		float _DecalEmissionStrength;
		float4 _DiffuseTint;
		float4 _ColorWetTint;
		float4 _EmissionTint;
		float _Height;
		float _ParallaxHeight;

		struct Input 
		{
			float2 uv_AlbedoMap;
			float3 viewDir;
		};


		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			//Calculate UV Offset
			float vDot = dot(IN.viewDir,half3(0,1,0));
			half hNorm = 1.0;
			IN.uv_AlbedoMap = IN.uv_AlbedoMap;

			//Get Texture Maps
			fixed4 albedoMap = tex2D(_AlbedoMap, IN.uv_AlbedoMap);
			fixed4 maskMap1 = tex2D(_MaskMap1, IN.uv_AlbedoMap);
			fixed4 maskMap2 = tex2D(_MaskMap2, IN.uv_AlbedoMap);
			fixed3 normal = UnpackNormal(tex2D (_Norm, IN.uv_AlbedoMap));

				//Base Albedo
				o.Albedo = lerp(albedoMap.rgb, albedoMap.rgb * _DiffuseTint.rgb, _DiffuseTint.a);
				//o.Alpha = max(saturate(albedoMap.a - maskMap2.g), maskMap2.g);
				o.Alpha =albedoMap.a;

				//Adjust Normal via Wetness
				o.Normal = normal;

				//Base Metallic
				o.Metallic = maskMap1.r * _DecalMetalStrength * o.Alpha;

				//Base Occlusion
				o.Occlusion = (1.0-maskMap1.a) * o.Alpha;

				o.Smoothness = maskMap1.g * _DecalSmoothnessStrength;
		}

		ENDCG


	} 
	FallBack "Diffuse"
}
