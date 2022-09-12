using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class URPShaderProperties : UnityGLTF.GLTFSceneExporter.ShaderPropertyCollection
{
	//KNOWN ISSUE - albedo alpha for smoothness isn't supported
	//KNOWN ISSUE - metallicMap.a *= _smoothness is higher than expected
	public URPShaderProperties()
	{
		ShaderNames.Add("Universal Render Pipeline/Lit");

		AlbedoMapName = "_BaseMap";
		AlbedoColorName = "_BaseColor";

		MetallicMapName = "_MetallicGlossMap";
		MetallicPowerName = "_Metallic"; //only used if no map

		RoughnessMapName = "_MetallicGlossMap"; //alpha channel (metallic or albedo)
		RoughnessPowerName = "_Smoothness";

		NormalMapName = "_BumpMap";
		NormalMapPowerName = "_BumpScale";
		NormalProcessShader = "Hidden/NormalChannel";
	}

	//invert smoothness for roughness
	//KNOWN ISSUE record 0 roughness if using albedo alpha
	public override bool TryGetRoughness(Material m, out float power)
	{
		if (m.HasProperty("_SmoothnessTextureChannel"))
		{
			float channel = m.GetFloat("_SmoothnessTextureChannel");
			if (Mathf.Approximately(channel, 1))
			{
				power = 1; //full roughness
				return true;
			}
		}

		bool hasRoughness = base.TryGetRoughness(m, out power);
		power = 1 - power;
		return hasRoughness;
	}
}