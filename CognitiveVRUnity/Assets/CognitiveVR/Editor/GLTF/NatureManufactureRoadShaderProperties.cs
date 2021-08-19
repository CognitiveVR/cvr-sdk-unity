using GLTF.Schema;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF;

class NatureManufactureRoadShaderProperties : UnityGLTF.GLTFSceneExporter.ShaderPropertyCollection
{
	public NatureManufactureRoadShaderProperties()
	{
		ShaderNames.Add("NatureManufacture Shaders/Standard Metalic Road Material Parallax ArrayTrial");
		ShaderNames.Add("NatureManufacture Shaders/Standard Metalic Cross Road Material Parallax ArrayTrial");
		
		ShaderNames.Add("NatureManufacture Shaders/Decal Metalic Road Material Parallax ArrayTrial");
		//decal should be alpha mapped

		AlbedoMapName = "_DetailAlbedoMap";
		AlbedoColorName = "_MainRoadColor";

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

    public override void FillProperties(GLTFSceneExporter exporter, GLTFMaterial material, Material materialAsset)
    {
        base.FillProperties(exporter, material, materialAsset);
		if (materialAsset.shader.name.Contains("Decal"))
        {
			material.AlphaMode = AlphaMode.BLEND;
        }
    }

	public override bool TryGetAlbedoMap(Material m, out Texture texture)
    {
		try
		{
			int index = Mathf.RoundToInt(m.GetFloat("MainRoadIndex"));
			var texture2DArray = m.GetTexture("_ArrayMainRoadAlbedo_T") as Texture2DArray;
			Color32[] pixels = texture2DArray.GetPixels32(index, 0);
			Texture2D outTexture = new Texture2D(texture2DArray.width, texture2DArray.height);
			outTexture.SetPixels32(pixels);
			outTexture.Apply();
			outTexture.name = texture2DArray.name + "_" + index;
			texture = outTexture;
			return true;
		}
		catch (System.Exception e)
        {
			Debug.LogException(e);
			texture = new Texture2D(256,256);			
			return true;
        }
    }
}