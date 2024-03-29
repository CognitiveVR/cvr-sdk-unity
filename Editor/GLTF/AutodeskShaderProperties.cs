﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.UnityGLTF
{
	class AutodeskShaderProperties : GLTFSceneExporter.ShaderPropertyCollection
	{
		public AutodeskShaderProperties()
		{
			ShaderNames.Add("Autodesk Interactive");

			AlbedoMapName = "_MainTex";
			AlbedoColorName = "_Color";

			MetallicMapName = "_MetallicGlossMap";
			MetallicPowerName = "_Metallic";

			RoughnessMapName = "_SpecGlossMap";
			RoughnessPowerName = "_GlossMapScale";

			NormalMapName = "_BumpMap";
			NormalMapPowerName = "_BumpScale";
			NormalProcessShader = "Hidden/NormalChannel"; // UNITY rgba  ->   GLTF ag11
		}
	}
}