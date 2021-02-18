using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class GLTFShaderProperties : UnityGLTF.GLTFSceneExporter.ShaderPropertyCollection
{
	public GLTFShaderProperties()
	{
		ShaderNames.Add("GLTFUtility / Standard(Specular)");
		ShaderNames.Add("GLTFUtility / Standard(Metallic)");

		AlbedoMapName = "_MainTex";
		AlbedoColorName = "_Color";

		//metallic B
		MetallicMapName = "_MetallicGlossMap";
		MetallicPowerName = "_Metallic";
		//MetallicProcessShader = "Hidden/MetalGlossChannelSwap";

		//Gloss G
		RoughnessMapName = "_MetallicGlossMap";
		RoughnessPowerName = "_Roughness";
		//RoughnessProcessShader = "Hidden/MetalGlossChannelSwap";

		NormalMapName = "_BumpMap";
		NormalMapPowerName = "_BumpScale";
		NormalProcessShader = "Hidden/NormalChannel";
	}

	//only use the metalic power if the metallic map isn't present
	public override bool TryGetMetallicPower(Material m, out float power)
	{
		Texture ignore = null;
		if (TryGetMetallicMap(m, out ignore))
		{
			power = 1;
			return false;
		}
		return base.TryGetMetallicPower(m, out power);
	}
}