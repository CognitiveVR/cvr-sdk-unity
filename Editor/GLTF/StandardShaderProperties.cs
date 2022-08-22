using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class StandardShaderProperties : UnityGLTF.GLTFSceneExporter.ShaderPropertyCollection
{
	//KNOWN ISSUE - albedo alpha for smoothness isn't supported
	public StandardShaderProperties()
	{
		ShaderNames.Add("Standard");

		AlbedoMapName = "_MainTex";
		AlbedoColorName = "_Color";

		MetallicMapName = "_MetallicGlossMap";
		MetallicPowerName = "_Metallic";
		MetallicProcessShader = "Hidden/UnityStandardToORM"; //must be set the same as RoughnessProcessShader because of caching

		RoughnessMapName = "_MetallicGlossMap";
		RoughnessPowerName = "_GlossMapScale"; //_GlossMapScale if _MetallicGlossMap is set. _Glossiness if not set
		RoughnessProcessShader = "Hidden/UnityStandardToORM"; // UNITY metal r, gloss a  ->   GLTF metal  b, roughness g

		NormalMapName = "_BumpMap";
		NormalMapPowerName = "_BumpScale";
		NormalProcessShader = "Hidden/NormalChannel"; // UNITY rgba  ->   GLTF ag11
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

	//invert smoothness for roughness. use glossmap or glossiness
	//KNOWN ISSUE record 0 roughness if using albedo alpha
	public override bool TryGetRoughness(Material m, out float power)
	{
		//TODO why is gltf smoothness much stronger than unity? possible roughness channel baked wrong
		if (m.HasProperty("_SmoothnessTextureChannel"))
		{
			float channel = m.GetFloat("_SmoothnessTextureChannel");
			if (Mathf.Approximately(channel, 1)) //albedo alpha channel for shininess
			{
				power = 1; //full roughness
				return true;
			}
		}

		if (m.HasProperty(MetallicMapName) && m.GetTexture(MetallicMapName) != null) //_GlossMapScale
		{
			//if using map, set roughness as 1
			power = 1;
			return true;
		}
		else //_Glossiness
		{
			power = 0;
			if (m.HasProperty("_Glossiness"))
			{
				power = 1 - m.GetFloat("_Glossiness");
				return true;
			}
			return false;
		}
	}
}