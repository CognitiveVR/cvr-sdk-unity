Shader "Hidden/Cognitive/CommandDepth"
{
	CGINCLUDE

	#include "UnityCG.cginc"

	#pragma exclude_renderers d3d11_9x

	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
	//sampler2D_float _CameraDepthTexture;
	sampler2D_float _CameraDepthNormalsTexture;
	sampler2D_float _CameraMotionVectorsTexture;

	float4 _CameraDepthTexture_ST;
	float4 _CameraDepthNormalsTexture_ST;
	float4 _CameraMotionVectorsTexture_ST;

#if SOURCE_GBUFFER
	sampler2D _CameraGBufferTexture2;
	float4 _CameraGBufferTexture2_ST;
#endif

	// -----------------------------------------------------------------------------
	// Depth

	float _DepthScale;
	float4 _MainTex_ST;

	struct VaryingsDefault
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uvSPR : TEXCOORD1; // Single Pass Stereo UVs
	};

	struct AttributesDefault
	{
		float4 vertex : POSITION;
		float4 texcoord : TEXCOORD0;
	};

	VaryingsDefault VertDefault(AttributesDefault v)
	{
		VaryingsDefault o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.uvSPR = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);
		return o;
	}

	float4 FragDepth(VaryingsDefault i) : SV_Target
	{
		//float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoScreenSpaceUVAdjust(i.uv, _CameraDepthTexture_ST));
		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv);
		depth = Linear01Depth(depth);// *_DepthScale;
		//float3 d = depth.xxx;

		//float normaldepth = _ZBufferParams.y;

		//float projectedDepth = depth * _ProjectionParams.z;

		return float4(depth, depth, depth, 1);
	}
	ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		// (0) - Depth
		Pass
		{
			CGPROGRAM

#pragma vertex VertDefault
#pragma fragment FragDepth

			ENDCG
		}
	}
}
