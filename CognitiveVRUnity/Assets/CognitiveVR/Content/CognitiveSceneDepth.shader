Shader "Hidden/CognitiveVRSceneDepth" {
	Properties {}

	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off
				
		CGPROGRAM
		#pragma vertex vert_img
		#pragma fragment fragThin
		#include "UnityCG.cginc"

		sampler2D_float _CameraDepthTexture;

		half4 fragThin(v2f_img i) : SV_Target
		{
			float d = Linear01Depth(tex2D(_CameraDepthTexture, i.uv));
			half4 depth = half4(d,d,d,d);

			return depth;
		}
		ENDCG

		}
	}
Fallback off

}
