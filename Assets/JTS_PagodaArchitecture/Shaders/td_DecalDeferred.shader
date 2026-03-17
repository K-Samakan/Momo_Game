Shader "TanukiDigital/Decal Deferred"
{
	Properties 
	{
		_AlbedoMap ("(RGB)Albedo (A)Alpha", 2D) = "white" {}
		_MaskMap1 ("Mask1: (R)Metallic (G)Smoothness (B)Wetness (A)Occlusion", 2D) = "white" {}
		_MaskMap2 ("Mask2: Emission (R)/Height (A)", 2D) = "white" {}
		//_BumpMap ("Normal map", 2D) = "bump" {}
		_Norm ("Normal map", 2D) = "" {}
		_WaveMap ("Wave map", 2D) = "bump" {}

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
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Opaque" "ForceNoShadowCasting"="True"}
		LOD 300
		Offset -1, -1

		Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#pragma surface surf Standard finalgbuffer:DecalFinalGBuffer exclude_path:forward exclude_path:prepass noshadow noforwardadd keepalpha
		
		#pragma target 3.0

		sampler2D _AlbedoMap;
		sampler2D _MaskMap1;
		sampler2D _MaskMap2;
		sampler2D _Norm;
		sampler2D _WaveMap;
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


		inline float2 PxOffset( half h, half height, half3 viewDir ){
			h = h * height - height/2.0;
			float3 v = normalize(viewDir);
			v.z += 0.42;
			return h * (v.xy / v.z);
		}

		inline half3 RemapColorToRange (half3 f, half min, half max)
		{
			f = saturate (f);
			f = (f - min) / (max - min);
			return f;
		}


		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			//Calculate UV Offset
			float vDot = dot(IN.viewDir,half3(0,1,0));
			half hNorm = 1.0;
			float2 offset = PxOffset(1.0, _Height * hNorm, IN.viewDir) * lerp(0.05,0.1,vDot);
			IN.uv_AlbedoMap = IN.uv_AlbedoMap + offset;

			//Get Texture Maps
			fixed4 albedoMap = tex2D(_AlbedoMap, IN.uv_AlbedoMap);
			fixed4 maskMap1 = tex2D(_MaskMap1, IN.uv_AlbedoMap);
			fixed4 maskMap2 = tex2D(_MaskMap2, IN.uv_AlbedoMap);
			fixed3 normal = UnpackNormal(tex2D (_Norm, IN.uv_AlbedoMap));
			fixed3 wave = UnpackNormal(tex2D(_WaveMap, IN.uv_AlbedoMap * 4 + float2(-_Time.x, _Time.x* 0.2)));

				//Base Albedo
				o.Albedo = lerp(albedoMap.rgb, albedoMap.rgb * _DiffuseTint.rgb, _DiffuseTint.a);
				o.Alpha = albedoMap.a;

				//Adjust Normal via Wetness
				o.Normal = normal;
				//o.Normal = normal.rgb * 2 - 1;
				float diffWetMask = saturate(lerp(0.0, 2.0, maskMap1.b));
				o.Normal = lerp(o.Normal, half3(0,0,1), saturate(maskMap1.b * _DecalWetStrength * 1.2));

				//Adjust Wetness with Wave Norma
				o.Normal = lerp(o.Normal, wave, saturate(saturate(lerp(-3.5,1.0,maskMap1.b)) * _DecalWetStrength));

				//Base Metallic
				o.Metallic = maskMap1.r * _DecalMetalStrength * o.Alpha;

				//Base Occlusion
				o.Occlusion = (1.0-maskMap1.a) * o.Alpha;

				o.Smoothness = 1;
		}

		void DecalFinalGBuffer (Input IN, SurfaceOutputStandard o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
		{
			//fixed4 normalMap = tex2D (_Norm, IN.uv_AlbedoMap);
			fixed4 maskMap1 = tex2D (_MaskMap1, IN.uv_AlbedoMap);
			fixed4 maskMap2 = tex2D (_MaskMap2, IN.uv_AlbedoMap);

			//Base Diffuse Buffer
			float3 puddleColor = lerp(_ColorWetTint.rgb * 0.2, _ColorWetTint.rgb, saturate(maskMap1.b*2));
			float diffWetMask = saturate(lerp(0.0, 2.0, maskMap1.b));

			//Diffuse Alpha
			diffuse.a = _DecalAlbedoStrength * o.Alpha;
			diffuse.a = max(diffuse.a, diffWetMask * _ColorWetTint.a * saturate(lerp(0,2,_DecalWetStrength)));

			//Normal Buffer Visibility
			normal.a = _DecalNormalStrength * lerp(1.0, o.Alpha, maskMap2.b * _DecalAlphaBlend);
			normal.a = lerp(normal.a, 1.0, saturate(maskMap1.b * _DecalWetStrength * 1.6)) * maskMap2.g;

			//Emission
			emission.a = o.Alpha;

			//Blockout Specular in wet areas
			specSmoothness.a = _DecalWetStrength * maskMap1.b * o.Alpha;

			//Fudge specSmoothness to prevent render artifacts in metallic areas
			specSmoothness.a = lerp(specSmoothness.a, 1.0, o.Metallic);

		}

		ENDCG





		//BLEND SMOOTHNESS VALUES
		Blend One One
		ColorMask A

		CGPROGRAM
		#pragma surface surf Standard finalgbuffer:DecalFinalGBuffer exclude_path:forward exclude_path:prepass noshadow noforwardadd keepalpha
		#pragma target 3.0

		sampler2D _AlbedoMap;
		sampler2D _MaskMap1;
		float _DecalSmoothnessStrength;
		float _DecalWetStrength;


		struct Input 
		{
			float2 uv_AlbedoMap; 
			float3 viewDir;
		};
		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 albedoMap = tex2D (_AlbedoMap, IN.uv_AlbedoMap);
			o.Albedo = albedoMap.rgb;
			o.Alpha = albedoMap.a;
		}
		void DecalFinalGBuffer (Input IN, SurfaceOutputStandard o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
		{
			//Smoothness Buffer Overlay
			fixed4 maskMap1 = tex2D (_MaskMap1, IN.uv_AlbedoMap);
			specSmoothness.a = (_DecalSmoothnessStrength * maskMap1.g * 0.95) * o.Alpha;
			normal.a = o.Alpha;
		}
		ENDCG



		// BLEND WETNESS VALUES
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask A

		CGPROGRAM
		#pragma surface surf Standard finalgbuffer:DecalFinalGBuffer exclude_path:forward exclude_path:prepass noshadow noforwardadd keepalpha
		#pragma target 3.0

		sampler2D _AlbedoMap;
		sampler2D _MaskMap1;
		float _DecalWetStrength;

		struct Input 
		{
			float2 uv_AlbedoMap; 
			float3 viewDir;
		};
		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 albedoMap = tex2D (_AlbedoMap, IN.uv_AlbedoMap);
			o.Albedo = albedoMap.rgb;
			o.Alpha = albedoMap.a;
		}
		void DecalFinalGBuffer (Input IN, SurfaceOutputStandard o, inout half4 diffuse, inout half4 specSmoothness, inout half4 normal, inout half4 emission)
		{
			//Smoothness Buffer Overlay
			fixed4 maskMap1 = tex2D (_MaskMap1, IN.uv_AlbedoMap);
			specSmoothness.a = lerp(specSmoothness.a, 0.95, maskMap1.b * _DecalWetStrength) * o.Alpha;
			normal.a = o.Alpha;

		}
		ENDCG


	} 
	FallBack "Diffuse"
}
