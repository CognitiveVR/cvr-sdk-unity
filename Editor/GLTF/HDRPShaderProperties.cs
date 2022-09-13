using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class HDRPShaderProperties : UnityGLTF.GLTFSceneExporter.ShaderPropertyCollection
{
	public HDRPShaderProperties()
	{
		ShaderNames.Add("HDRP/Lit");

		AlbedoMapName = "_MainTex";
		AlbedoColorName = "_BaseColor";

		//metallic R
		MetallicMapName = "_MaskMap";
		MetallicPowerName = "_Metallic";

		//smoothness A (can be remap 0-1 in material. not a single exportable value. could pass these values into a shader to modify exported texture?)
		RoughnessMapName = "_MaskMap";
		RoughnessPowerName = "_Smoothness";

		NormalMapName = "_NormalMap";
		NormalMapPowerName = "_NormalScale";
		NormalProcessShader = "Hidden/NormalChannel";
	}
}