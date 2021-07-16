using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class NatureManufactureFloorsShaderProperties : UnityGLTF.GLTFSceneExporter.ShaderPropertyCollection
{
	public NatureManufactureFloorsShaderProperties()
	{
		ShaderNames.Add("NatureManufacture Shaders/Standard Metalic UV Walls and Floors");

		AlbedoMapName = "_AlbedoRGB";
		AlbedoColorName = "_AlbedoColor";

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