Shader "TanukiDigital/Standard_Packed_PBR" {
	Properties {
		_Color ("Tint Color", Color) = (1,1,1,1)
		_RimColor ("Rim Color", Color) = (0,0,0,0)

		_MainTex ("(RGB) Albedo ,(A) Smoothness", 2D) = "(RGBA: 0.5,0.5,0.5,0.5)" {}
		_MaskTex ("(RGBA) Mask Channels", 2D) = "(RGBA: 0.0,1.0,1.0,1.0)"{}
		_NTex ("(RGB) Normal, (A) Height", 2D) = "(RGBA: 0.5,0.5,1.0,0.0)"{}

		_NormalFac ("Normal Strength", Range(0,1)) = 0.0
		_SmoothnessFac ("Smoothness Factor", Range(0,1)) = 0.0
		_MetallicFac ("Metallic Factor", Range(0,1)) = 0.0
		_OcclusionFac ("Occlusion Factor", Range(0,1)) = 0.0
		_HeightFac ("Height Factor", Range(0,1)) = 0.0
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _NTex;
		sampler2D _MaskTex;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
		};

		half _SmoothnessFac;
		half _NormalFac;
		half _MetallicFac;
		half _OcclusionFac;
		half _HeightFac;

		fixed4 _Color;
		fixed4 _RimColor;

		//Calculate Offset UVS (DISABLED!)
		//inline float2 PxOffset( half h, half height, half3 viewDir ){
		//	h = h * height - height/2.0;
		//	float3 v = normalize(viewDir);
		//	v.z += 0.42;
		//	return h * (v.xy / v.z);
		//}

		void surf (Input IN, inout SurfaceOutputStandard o) {

			//Calculate UVs
			float2 UVs = IN.uv_MainTex;
			float2 offset = float2(0.0,0.0);
			float vDot = dot(IN.viewDir,half3(0,0,1));

			//Use Height Distortion (DISABLED!)
			//if (_Height > 0.0){
			//	fixed4 hNorm = tex2D (_NTex, UVs);
			//	offset = PxOffset(1.0,_Height * hNorm.a,IN.viewDir) * lerp(0.05,0.1,vDot);
			//	UVs = ((IN.uv_MainTex + offset));
			//}

			//Load Maps
			fixed4 texCol = tex2D (_MainTex, UVs);
			fixed4 texNorm = tex2D (_NTex, UVs);
			fixed4 texMask = tex2D (_MaskTex, UVs);

			//Color Output
			o.Albedo = lerp(texCol.rgb, texCol.rgb * _Color.rgb, texMask.b * _Color.a);

			//Normal Output
			o.Normal = lerp(half3(0,0,1), UnpackNormal(texNorm), _NormalFac);

			//Rim Color
			o.Albedo = lerp(o.Albedo, _RimColor.rgb, lerp(1.0,0.0,vDot) * _RimColor.a );

			//Property Output
			o.Smoothness = texCol.a * _SmoothnessFac;
			o.Metallic = texMask.r * _MetallicFac;
			o.Occlusion = lerp(1.0, texMask.g, _OcclusionFac);
			o.Alpha = 1.0;

		}

		ENDCG
	}
	FallBack "Diffuse"
}
