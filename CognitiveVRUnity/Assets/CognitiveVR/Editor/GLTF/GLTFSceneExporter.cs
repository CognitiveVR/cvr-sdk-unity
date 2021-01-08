using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Extensions;
using CameraType = GLTF.Schema.CameraType;
using WrapMode = GLTF.Schema.WrapMode;

namespace UnityGLTF
{
	public class GLTFSceneExporter
	{
		//shaders may have custom implementations for values (such as using roughness for roughness) unlike unity standard shader
		//CONSIDER how to deal with separate metallic/roughness maps?
		//CONSIDER should this just render out required channels and hold them in memory until they get combined/saved?
		//TODO support for emission and occlusion maps/values
		abstract class ShaderPropertyCollection
		{
			public string AlbedoMapName;
			public string AlbedoColorName;

			public string MetallicMapName;
			public string MetallicPowerName;
			//the shader to apply when exporting texture. ignored if empty
			public string MetallicProcessShader;

			public string RoughnessMapName;
			public string RoughnessPowerName;
			//the shader to apply when exporting texture. ignored if empty
			public string RoughnessProcessShader;

			public string NormalMapName;
			public string NormalMapPowerName;
			//the shader to apply when exporting texture. ignored if empty
			public string NormalProcessShader;

			//public string EmissionMapName;
			//public string EmissionColorName;

			//public string OcclusionMapName;
			//public string OcclusionPowerName;

			public virtual void FillProperties(GLTFSceneExporter exporter, GLTFMaterial material, Material materialAsset)
			{
				material.DoubleSided = materialAsset.HasProperty("_Cull") && materialAsset.GetInt("_Cull") == (float)CullMode.Off;

				if (materialAsset.HasProperty("_Cutoff"))
				{
					material.AlphaCutoff = materialAsset.GetFloat("_Cutoff");
				}

				switch (materialAsset.GetTag("RenderType", false, ""))
				{
					case "TransparentCutout":
						material.AlphaMode = AlphaMode.MASK;
						break;
					case "Transparent":
						material.AlphaMode = AlphaMode.BLEND;
						break;
					default:
						material.AlphaMode = AlphaMode.OPAQUE;
						break;
				}

				var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };

				Texture tempTexture;
				Color tempColor;
				float tempFactor;

				if (TryGetAlbedoMap(materialAsset, out tempTexture))
				{
					pbr.BaseColorTexture = exporter.ExportTextureInfo(tempTexture, TextureMapType.Main, false, string.Empty);
					exporter.ExportTextureTransform(pbr.BaseColorTexture, materialAsset, AlbedoMapName);
				}
				if (TryGetAlbedoColor(materialAsset, out tempColor))
				{
					pbr.BaseColorFactor = tempColor.ToNumericsColorRaw();
				}

				if (TryGetMetallicMap(materialAsset, out tempTexture))
				{
					pbr.MetallicRoughnessTexture = exporter.ExportTextureInfo(tempTexture, TextureMapType.MetallicGloss, true, MetallicProcessShader);
					exporter.ExportTextureTransform(pbr.MetallicRoughnessTexture, materialAsset, MetallicMapName);
				}
				if (TryGetMetallicPower(materialAsset, out tempFactor))
				{
					pbr.MetallicFactor = tempFactor;
				}

				if (TryGetRoughnessMap(materialAsset, out tempTexture))
				{
					pbr.MetallicRoughnessTexture = exporter.ExportTextureInfo(tempTexture, TextureMapType.MetallicGloss, true, RoughnessProcessShader);
					exporter.ExportTextureTransform(pbr.MetallicRoughnessTexture, materialAsset, RoughnessMapName);
				}
				if (TryGetRoughness(materialAsset, out tempFactor))
				{
					pbr.RoughnessFactor = tempFactor;
				}

				if (TryGetNormalMap(materialAsset, out tempTexture))
				{
					material.NormalTexture = exporter.ExportNormalTextureInfo(tempTexture, TextureMapType.Bump, materialAsset, NormalProcessShader);
					exporter.ExportTextureTransform(material.NormalTexture, materialAsset, NormalMapName);
				}

				material.PbrMetallicRoughness = pbr;
			}

			public virtual bool TryGetAlbedoColor(Material m, out Color color)
			{
				color = Color.white;
				if (m.HasProperty(AlbedoColorName))
				{
					color = m.GetColor(AlbedoColorName);
					return true;
				}
				return false;
			}

			public virtual bool TryGetAlbedoMap(Material m, out Texture texture)
			{
				texture = null;
				if (m.HasProperty(AlbedoMapName))
				{
					texture = m.GetTexture(AlbedoMapName);
					if (texture == null)
					{
						return false;
					}
					return true;
				}
				return false;
			}

			public virtual bool TryGetMetallicPower(Material m, out float power)
			{
				power = 0;
				if (m.HasProperty(MetallicPowerName))
				{
					power = m.GetFloat(MetallicPowerName);
					return true;
				}
				return false;
			}

			public virtual bool TryGetMetallicMap(Material m, out Texture texture)
			{
				texture = null;
				if (m.HasProperty(MetallicMapName))
				{
					texture = m.GetTexture(MetallicMapName);
					if (texture == null)
					{
						return false;
					}
					return true;
				}
				return false;
			}

			public virtual bool TryGetRoughness(Material m, out float power)
			{
				power = 0;
				if (m.HasProperty(RoughnessPowerName))
				{
					power = m.GetFloat(RoughnessPowerName);
					return true;
				}
				return false;
			}

			public virtual bool TryGetRoughnessMap(Material m, out Texture texture)
			{
				texture = null;
				if (m.HasProperty(RoughnessMapName))
				{
					texture = m.GetTexture(RoughnessMapName);
					if (texture == null)
					{
						return false;
					}
					return true;
				}
				return false;
			}

			public virtual bool TryGetNormalMap(Material m, out Texture texture)
			{
				texture = null;
				if (m.HasProperty(NormalMapName))
				{
					texture = m.GetTexture(NormalMapName);
					if (texture == null)
					{
						return false;
					}
					return true;
				}
				return false;
			}

			public virtual bool TryGetNormalPower(Material m, out float power)
			{
				power = 0;
				if (m.HasProperty(NormalMapPowerName))
				{
					power = m.GetFloat(NormalMapPowerName);
					return true;
				}
				return false;
			}
		}

		class UnityStandard : ShaderPropertyCollection
		{
			//KNOWN ISSUE - albedo alpha for smoothness isn't supported
			public UnityStandard()
			{
				AlbedoMapName = "_MainTex";
				AlbedoColorName = "_Color";

				MetallicMapName = "_MetallicGlossMap";
				MetallicPowerName = "_Metallic";
				//MetallicProcessShader = "Hidden/MetalGlossChannelSwap";

				RoughnessMapName = "_MetallicGlossMap";
				RoughnessPowerName = "_GlossMapScale"; //_GlossMapScale if _MetallicGlossMap is set. _Glossiness if not set
				RoughnessProcessShader = "Hidden/MetalGlossChannelSwap"; // UNITY metal r, gloss a  ->   GLTF metal  b, roughness g

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
					bool hasRoughness = base.TryGetRoughness(m, out power);
					power = 1 - power;
					return hasRoughness;
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

		class GLTFStandard : ShaderPropertyCollection
		{
			public GLTFStandard()
			{
				AlbedoMapName = "_MainTex";
				AlbedoColorName = "_Color";

				//metallic B
				MetallicMapName = "_MetallicGlossMap";
				MetallicPowerName = "_Metallic";
				//MetallicProcessShader = "Hidden/MetalGlossChannelSwap";

				//Gloss G
				RoughnessMapName = "_MetallicGlossMap";
				RoughnessPowerName = "_Roughness";
				RoughnessProcessShader = "Hidden/MetalGlossChannelSwap";

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

		class UnityURP : ShaderPropertyCollection
		{
			//KNOWN ISSUE - albedo alpha for smoothness isn't supported
			//KNOWN ISSUE - metallicMap.a *= _smoothness is higher than expected
			public UnityURP()
			{
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

		class UnityHDRP : ShaderPropertyCollection
		{
			public UnityHDRP()
			{
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

		internal enum TextureMapType
		{
			Main,
			Bump,
			SpecGloss,
			Emission,
			MetallicGloss,
			Light,
			Occlusion
		}

		internal struct ImageInfo
		{
			public Texture2D texture;
			public TextureMapType textureMapType;

			public bool Linear;
			public string ShaderOverrideName;
		}

		static Dictionary<string, ShaderPropertyCollection> MaterialExportPropertyCollection = new Dictionary<string, ShaderPropertyCollection>() {
			//sample shaders from https://github.com/Siccity/GLTFUtility
			{ "GLTFUtility/Standard (Metallic)", new GLTFStandard() },
			{ "GLTFUtility/Standard (Specular)", new GLTFStandard() },
			//unity universal render pipeline
			{ "Universal Render Pipeline/Lit", new UnityURP() },
			//unity hd render pipeline
			{ "HDRP/Lit", new UnityHDRP() },
			//built-in rendering
			{ "Standard", new UnityStandard() }
			};

		public delegate string RetrieveTexturePathDelegate(Texture texture);

		private enum IMAGETYPE
		{
			RGB,
			RGBA,
			R,
			G,
			B,
			A,
			G_INVERT
		}

		private Transform[] _rootTransforms;
		private GLTFRoot _root;
		private BufferId _bufferId;
		private GLTFBuffer _buffer;
		private BinaryWriter _bufferWriter;
		private List<ImageInfo> _imageInfos;
		private List<Texture> _textures;
		private List<Material> _materials;

		private RetrieveTexturePathDelegate _retrieveTexturePathDelegate;

		private const uint MagicGLTF = 0x46546C67;
		private const uint Version = 2;
		private const uint MagicJson = 0x4E4F534A;
		private const uint MagicBin = 0x004E4942;
		private const int GLTFHeaderSize = 12;
		private const int SectionHeaderSize = 8;

		protected struct PrimKey
		{
			public Mesh Mesh;
			public Material Material;
		}
		private readonly Dictionary<PrimKey, MeshId> _primOwner = new Dictionary<PrimKey, MeshId>();
		private readonly Dictionary<Mesh, MeshPrimitive[]> _meshToPrims = new Dictionary<Mesh, MeshPrimitive[]>();

		// Settings
		public static bool ExportNames = true; //MUST BE TRUE
		public static bool RequireExtensions = false; //PROBABLY FALSE

		public CognitiveVR.DynamicObject Dynamic;

		/// <summary>
		/// Create a GLTFExporter that exports out a transform. if dynamic is NOT set, skip any dynamic objects in nodes. if dynamic IS set, skip any dynamics that are not equal to this object
		/// </summary>
		/// <param name="rootTransforms">Root transform of object to export</param>
		public GLTFSceneExporter(Transform[] rootTransforms, RetrieveTexturePathDelegate retrieveTexturePathDelegate, CognitiveVR.DynamicObject dynamic = null)
		{
			Dynamic = dynamic;
			_retrieveTexturePathDelegate = retrieveTexturePathDelegate;

			_rootTransforms = rootTransforms;
			_root = new GLTFRoot
			{
				Accessors = new List<Accessor>(),
				Asset = new Asset
				{
					Version = "2.0"
				},
				Buffers = new List<GLTFBuffer>(),
				BufferViews = new List<BufferView>(),
				Cameras = new List<GLTFCamera>(),
				Images = new List<GLTFImage>(),
				Materials = new List<GLTFMaterial>(),
				Meshes = new List<GLTFMesh>(),
				Nodes = new List<Node>(),
				Samplers = new List<Sampler>(),
				Scenes = new List<GLTFScene>(),
				Textures = new List<GLTFTexture>()
			};

			if (_root.ExtensionsUsed == null)
				_root.ExtensionsUsed = new List<string>(new[] { "KHR_lights_punctual" });

			_imageInfos = new List<ImageInfo>();
			_materials = new List<Material>();
			_textures = new List<Texture>();

			_buffer = new GLTFBuffer();
			_bufferId = new BufferId
			{
				Id = _root.Buffers.Count,
				Root = _root
			};
			_root.Buffers.Add(_buffer);
		}

		List<CognitiveVR.BakeableMesh> OverrideBakeables = new List<CognitiveVR.BakeableMesh>();
		public void SetNonStandardOverrides(List<CognitiveVR.BakeableMesh> meshes)
		{
			OverrideBakeables = meshes;
		}

		/// <summary>
		/// Gets the root object of the exported GLTF
		/// </summary>
		/// <returns>Root parsed GLTF Json</returns>
		public GLTFRoot GetRoot()
		{
			return _root;
		}

		/// <summary>
		/// Writes a binary GLB file with filename at path.
		/// </summary>
		/// <param name="path">File path for saving the binary file</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLB(string path, string fileName)
		{
			Stream binStream = new MemoryStream();
			Stream jsonStream = new MemoryStream();

			_bufferWriter = new BinaryWriter(binStream);

			TextWriter jsonWriter = new StreamWriter(jsonStream, Encoding.ASCII);

			_root.Scene = ExportScene(fileName, _rootTransforms);

			_buffer.ByteLength = (uint)_bufferWriter.BaseStream.Length;

			_root.Serialize(jsonWriter);

			_bufferWriter.Flush();
			jsonWriter.Flush();

			// align to 4-byte boundary to comply with spec.
			AlignToBoundary(jsonStream);
			AlignToBoundary(binStream, 0x00);

			int glbLength = (int)(GLTFHeaderSize + SectionHeaderSize +
				jsonStream.Length + SectionHeaderSize + binStream.Length);

			string fullPath = Path.Combine(path, Path.ChangeExtension(fileName, "glb"));


			using (FileStream glbFile = new FileStream(fullPath, FileMode.Create))
			{

				BinaryWriter writer = new BinaryWriter(glbFile);

				// write header
				writer.Write(MagicGLTF);
				writer.Write(Version);
				writer.Write(glbLength);

				// write JSON chunk header.
				writer.Write((int)jsonStream.Length);
				writer.Write(MagicJson);

				jsonStream.Position = 0;
				CopyStream(jsonStream, writer);

				writer.Write((int)binStream.Length);
				writer.Write(MagicBin);

				binStream.Position = 0;
				CopyStream(binStream, writer);

				writer.Flush();
			}

			ExportImages(path);
		}

		/// <summary>
		/// Convenience function to copy from a stream to a binary writer, for
		/// compatibility with pre-.NET 4.0.
		/// Note: Does not set position/seek in either stream. After executing,
		/// the input buffer's position should be the end of the stream.
		/// </summary>
		/// <param name="input">Stream to copy from</param>
		/// <param name="output">Stream to copy to.</param>
		private static void CopyStream(Stream input, BinaryWriter output)
		{
			byte[] buffer = new byte[8 * 1024];
			int length;
			while ((length = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				output.Write(buffer, 0, length);
			}
		}

		/// <summary>
		/// Pads a stream with additional bytes.
		/// </summary>
		/// <param name="stream">The stream to be modified.</param>
		/// <param name="pad">The padding byte to append. Defaults to ASCII
		/// space (' ').</param>
		/// <param name="boundary">The boundary to align with, in bytes.
		/// </param>
		private static void AlignToBoundary(Stream stream, byte pad = (byte)' ', uint boundary = 4)
		{
			uint currentLength = (uint)stream.Length;
			uint newLength = CalculateAlignment(currentLength, boundary);
			for (int i = 0; i < newLength - currentLength; i++)
			{
				stream.WriteByte(pad);
			}
		}

		/// <summary>
		/// Calculates the number of bytes of padding required to align the
		/// size of a buffer with some multiple of byteAllignment.
		/// </summary>
		/// <param name="currentSize">The current size of the buffer.</param>
		/// <param name="byteAlignment">The number of bytes to align with.</param>
		/// <returns></returns>
		public static uint CalculateAlignment(uint currentSize, uint byteAlignment)
		{
			return (currentSize + byteAlignment - 1) / byteAlignment * byteAlignment;
		}


		/// <summary>
		/// Specifies the path and filename for the GLTF Json and binary
		/// </summary>
		/// <param name="path">File path for saving the GLTF and binary files</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLTFandBin(string path, string fileName)
		{
			var binFile = File.Create(Path.Combine(path, fileName + ".bin"));
			_bufferWriter = new BinaryWriter(binFile);

			try
			{

				_root.Scene = ExportScene(fileName, _rootTransforms);

				_buffer.Uri = fileName + ".bin";
				_buffer.ByteLength = (uint)_bufferWriter.BaseStream.Length;

				var gltfFile = File.CreateText(Path.Combine(path, fileName + ".gltf"));
				_root.Serialize(gltfFile);

#if WINDOWS_UWP
			gltfFile.Dispose();
			binFile.Dispose();
#else
				gltfFile.Close();
				binFile.Close();
#endif
				ExportImages(path);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				UnityEditor.EditorUtility.DisplayDialog("Error", "There was an error exporting the scene. See the Console for details", "Ok");
			}
			finally
			{
				foreach (var v in LODDisabledRenderers)
				{
					if (v != null)
						v.enabled = true;
				}
			}

		}

		private void ExportImages(string outputPath)
		{
			for (int t = 0; t < _imageInfos.Count; ++t)
			{
				//var image = _imageInfos[t].texture;
				//int height = image.height;
				//int width = image.width;

				ExportTexture(_imageInfos[t].texture, outputPath, _imageInfos[t].Linear, _imageInfos[t].ShaderOverrideName);
			}
		}

		private void ExportTexture(Texture2D texture, string outputPath, bool linear, string ShaderOverride)
		{
			RenderTextureReadWrite textureType = RenderTextureReadWrite.sRGB;
			if (linear)
				textureType = RenderTextureReadWrite.Linear;

			var destRenderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, textureType);

			bool writtenWithCustomShader = false;
			Material blitMaterial = null;
			if (!String.IsNullOrEmpty(ShaderOverride))
			{
				var blitShader = Shader.Find(ShaderOverride);
				if (blitShader != null)
				{
					blitMaterial = new Material(blitShader);
					Graphics.Blit(texture, destRenderTexture, blitMaterial);
					writtenWithCustomShader = true;
				}
			}

			if (!writtenWithCustomShader)
			{
				Graphics.Blit(texture, destRenderTexture);
			}

			var exportTexture = new Texture2D(texture.width, texture.height);
			exportTexture.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);
			exportTexture.Apply();

			var finalFilenamePath = ConstructImageFilenamePath(texture, outputPath);
			File.WriteAllBytes(finalFilenamePath, exportTexture.EncodeToPNG());

			RenderTexture.active = null;
			destRenderTexture.Release();
			if (Application.isEditor)
			{
				GameObject.DestroyImmediate(exportTexture);
			}
			else
			{
				GameObject.Destroy(exportTexture);
			}
		}

		private string ConstructImageFilenamePath(Texture2D texture, string outputPath)
		{
			var imagePath = _retrieveTexturePathDelegate(texture);
			var filenamePath = Path.Combine(outputPath, imagePath);
			if (texture.name != Uri.EscapeUriString(texture.name).Replace('#', '_'))
			{
				string texturenamehash = Mathf.Abs(texture.name.GetHashCode()).ToString();
				filenamePath = outputPath + "/" + Mathf.Abs(imagePath.GetHashCode()) + texturenamehash;
			}
			else
			{
				filenamePath = outputPath + "/" + Mathf.Abs(imagePath.GetHashCode()) + texture.name;
			}
			var file = new FileInfo(filenamePath);
			file.Directory.Create();
			return filenamePath + ".png";
		}

		private SceneId ExportScene(string name, Transform[] rootObjTransforms)
		{
			var scene = new GLTFScene();

			if (ExportNames)
			{
				scene.Name = name;
			}

			scene.Nodes = new List<NodeId>(rootObjTransforms.Length); //skip dynamic objects here //TODO
			CognitiveVR.DynamicObject dyn;

			foreach (var transform in rootObjTransforms)
			{
				dyn = transform.GetComponent<CognitiveVR.DynamicObject>();
				//skip dynamics
				if ((Dynamic == null && dyn != null) //export scene and skip all dynamics
					|| (Dynamic != null && dyn != Dynamic)) continue; //exporting selected dynamic and found a non-dynamic
				if (!transform.gameObject.activeInHierarchy) continue;
				scene.Nodes.Add(ExportNode(transform));
			}

			_root.Scenes.Add(scene);

			return new SceneId
			{
				Id = _root.Scenes.Count - 1,
				Root = _root
			};
		}

		List<Renderer> LODDisabledRenderers = new List<Renderer>();

		private NodeId ExportNode(Transform nodeTransform)
		{
			var node = new Node();

			if (ExportNames)
			{
				node.Name = nodeTransform.name;
			}

			LODGroup lodgroup = nodeTransform.GetComponent<LODGroup>();
			if (lodgroup != null && lodgroup.enabled)
			{
				var lods = lodgroup.GetLODs();
				var lodCount = lodgroup.lodCount;
				if (lods.Length > 0)
				{
					if (CognitiveVR.CognitiveVR_Preferences.Instance.ExportSceneLODLowest)
					{
						for (int i = 0; i < lodCount - 1; i++)
						{
							foreach (var renderer in lods[i].renderers)
							{
								if (renderer.enabled)
								{
									renderer.enabled = false;
									LODDisabledRenderers.Add(renderer);
								}
							}
						}
					}
					else
					{
						for (int i = 1; i < lodCount; i++)
						{
							foreach (var renderer in lods[i].renderers)
							{
								if (renderer.enabled)
								{
									renderer.enabled = false;
									LODDisabledRenderers.Add(renderer);
								}
							}
						}
					}
				}
			}

			//export camera attached to node
			Camera unityCamera = nodeTransform.GetComponent<Camera>();
			if (unityCamera != null && unityCamera.enabled)
			{
				node.Camera = ExportCamera(unityCamera);
			}

			Light unityLight = nodeTransform.GetComponent<Light>();
			if (unityLight != null && unityLight.enabled)
			{
				node.Light = ExportLight(unityLight);

				nodeTransform.rotation *= new Quaternion(0, -1, 0, 0);

				node.SetUnityTransform(nodeTransform);

				nodeTransform.rotation *= new Quaternion(0, 1, 0, 0);

				//node.SetUnityTransformForce(nodeTransform,false); //forward is flipped
			}
			else
			{
				node.SetUnityTransform(nodeTransform);
			}

			var id = new NodeId
			{
				Id = _root.Nodes.Count,
				Root = _root
			};
			_root.Nodes.Add(node);

			// children that are primitives get put in a mesh
			GameObject[] primitives, nonPrimitives;
			FilterPrimitives(nodeTransform, out primitives, out nonPrimitives);
			if (primitives.Length > 0)
			{
				node.Mesh = ExportMesh(nodeTransform.name, primitives);

				// associate unity meshes with gltf mesh id
				foreach (var prim in primitives)
				{
					var filter = prim.GetComponent<MeshFilter>();
					var renderer = prim.GetComponent<MeshRenderer>();
					_primOwner[new PrimKey { Mesh = filter.sharedMesh, Material = renderer.sharedMaterial }] = node.Mesh;
				}
			}

			// children that are not primitives get added as child nodes
			if (nonPrimitives.Length > 0)
			{
				node.Children = new List<NodeId>(nonPrimitives.Length);
				CognitiveVR.DynamicObject dyn;
				foreach (var child in nonPrimitives)
				{
					dyn = child.GetComponent<CognitiveVR.DynamicObject>();
					//skip dynamics
					//if (child.GetComponent<CognitiveVR.DynamicObject>() != null)
					if ((Dynamic == null && dyn != null) //exporting scene and found dynamic in non-root
						|| (Dynamic != null && (dyn != null && dyn != Dynamic))) //this shouldn't ever happen. if find any dynamic as child, should skip
					{ continue; }
					if (!child.gameObject.activeInHierarchy) continue;
					node.Children.Add(ExportNode(child.transform));
				}
			}

			return id;
		}

		private CameraId ExportCamera(Camera unityCamera)
		{
			GLTFCamera camera = new GLTFCamera();
			//name
			camera.Name = unityCamera.name;

			//type
			bool isOrthographic = unityCamera.orthographic;
			camera.Type = isOrthographic ? CameraType.orthographic : CameraType.perspective;
			Matrix4x4 matrix = unityCamera.projectionMatrix;

			//matrix properties: compute the fields from the projection matrix
			if (isOrthographic)
			{
				CameraOrthographic ortho = new CameraOrthographic();

				ortho.XMag = 1 / matrix[0, 0];
				ortho.YMag = 1 / matrix[1, 1];

				float farClip = (matrix[2, 3] / matrix[2, 2]) - (1 / matrix[2, 2]);
				float nearClip = farClip + (2 / matrix[2, 2]);
				ortho.ZFar = farClip;
				ortho.ZNear = nearClip;

				camera.Orthographic = ortho;
			}
			else
			{
				CameraPerspective perspective = new CameraPerspective();
				float fov = 2 * Mathf.Atan(1 / matrix[1, 1]);
				float aspectRatio = matrix[1, 1] / matrix[0, 0];
				perspective.YFov = fov;
				perspective.AspectRatio = aspectRatio;

				if (matrix[2, 2] == -1)
				{
					//infinite projection matrix
					float nearClip = matrix[2, 3] * -0.5f;
					perspective.ZNear = nearClip;
				}
				else
				{
					//finite projection matrix
					float farClip = matrix[2, 3] / (matrix[2, 2] + 1);
					float nearClip = farClip * (matrix[2, 2] + 1) / (matrix[2, 2] - 1);
					perspective.ZFar = farClip;
					perspective.ZNear = nearClip;
				}
				camera.Perspective = perspective;
			}

			var id = new CameraId
			{
				Id = _root.Cameras.Count,
				Root = _root
			};

			_root.Cameras.Add(camera);

			return id;
		}

		private LightId ExportLight(Light unityLight)
		{
			GLTFLight light;

			if (unityLight.type == LightType.Spot)
			{
				light = new GLTFSpotLight() { innerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad * 0.8f, outerConeAngle = unityLight.spotAngle / 2 * Mathf.Deg2Rad };
				//name
				light.Name = unityLight.name;

				light.type = unityLight.type.ToString().ToLower();
				light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
				light.range = unityLight.range;
				light.intensity = unityLight.intensity;
			}
			else if (unityLight.type == LightType.Directional)
			{
				light = new GLTFDirectionalLight();
				//name
				light.Name = unityLight.name;

				light.type = unityLight.type.ToString().ToLower();
				light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
				light.intensity = unityLight.intensity;
			}
			else if (unityLight.type == LightType.Point)
			{
				light = new GLTFPointLight();
				//name
				light.Name = unityLight.name;

				light.type = unityLight.type.ToString().ToLower();
				light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
				light.range = unityLight.range;
				light.intensity = unityLight.intensity;
			}
			else
			{
				light = new GLTFLight();
				//name
				light.Name = unityLight.name;

				light.type = unityLight.type.ToString().ToLower();
				light.color = new GLTF.Math.Color(unityLight.color.r, unityLight.color.g, unityLight.color.b, 1);
			}

			if (_root.Lights == null)
			{
				_root.Lights = new List<GLTFLight>();
			}

			var id = new LightId
			{
				Id = _root.Lights.Count,
				Root = _root
			};

			//list of lightids should be in extensions object
			_root.Lights.Add(light);

			return id;
		}

		private void FilterPrimitives(Transform transform, out GameObject[] primitives, out GameObject[] nonPrimitives)
		{
			var childCount = transform.childCount;
			var prims = new List<GameObject>(childCount + 1);
			var nonPrims = new List<GameObject>(childCount);

			var mf = transform.gameObject.GetComponent<MeshFilter>();
			var mr = transform.gameObject.GetComponent<MeshRenderer>();

			// add another primitive if the root object also has a mesh
			if (mf != null
				&& mr != null
				&& mr.enabled
				&& mf.sharedMesh != null
				&& mf.sharedMesh.vertexCount > 0
				&& (OverrideBakeables.Find(delegate (CognitiveVR.BakeableMesh bm) { return bm.meshFilter == mf; }) != null || !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh)))
				&& transform.gameObject.activeInHierarchy)
			{
				prims.Add(transform.gameObject);
			}

			for (var i = 0; i < childCount; i++)
			{
				var go = transform.GetChild(i).gameObject;
				nonPrims.Add(go);
			}

			primitives = prims.ToArray();
			nonPrimitives = nonPrims.ToArray();
		}

		private bool IsPrimitive(GameObject gameObject)
		{
			var mf = gameObject.GetComponent<MeshFilter>();
			var mr = gameObject.GetComponent<MeshRenderer>();

			/*
			 * Primitives have the following properties:
			 * - have no children
			 * - have no non-default local transform properties
			 * - have MeshFilter and MeshRenderer components
			 */
			return gameObject.activeInHierarchy
				&& gameObject.transform.childCount == 0
				&& gameObject.transform.localPosition == Vector3.zero
				&& gameObject.transform.localRotation == Quaternion.identity
				&& gameObject.transform.localScale == Vector3.one
				&& mf != null
				&& mr != null
				&& (OverrideBakeables.Find(delegate (CognitiveVR.BakeableMesh bm) { return bm.meshFilter == mf; }) != null || !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh)))
				&& mr.enabled;
		}

		private MeshId ExportMesh(string name, GameObject[] primitives)
		{
			// check if this set of primitives is already a mesh
			MeshId existingMeshId = null;
			var key = new PrimKey();
			foreach (var prim in primitives)
			{
				var filter = prim.GetComponent<MeshFilter>();
				var renderer = prim.GetComponent<MeshRenderer>();
				key.Mesh = filter.sharedMesh;
				key.Material = renderer.sharedMaterial;

				MeshId tempMeshId;
				if (_primOwner.TryGetValue(key, out tempMeshId) && (existingMeshId == null || tempMeshId == existingMeshId))
				{
					existingMeshId = tempMeshId;
				}
				else
				{
					existingMeshId = null;
					break;
				}
			}

			// if so, return that mesh id
			if (existingMeshId != null)
			{
				return existingMeshId;
			}

			// if not, create new mesh and return its id
			var mesh = new GLTFMesh();

			if (ExportNames)
			{
				mesh.Name = name;
			}

			mesh.Primitives = new List<MeshPrimitive>(primitives.Length);
			foreach (var prim in primitives)
			{
				mesh.Primitives.AddRange(ExportPrimitive(prim));
			}

			var id = new MeshId
			{
				Id = _root.Meshes.Count,
				Root = _root
			};
			_root.Meshes.Add(mesh);

			return id;
		}

		// a mesh *might* decode to multiple prims if there are submeshes
		private MeshPrimitive[] ExportPrimitive(GameObject gameObject)
		{
			var filter = gameObject.GetComponent<MeshFilter>();
			var meshObj = filter.sharedMesh;

			var renderer = gameObject.GetComponent<MeshRenderer>();

			var materialsObj = renderer.sharedMaterials;

			var prims = new MeshPrimitive[meshObj.subMeshCount];

			// don't export any more accessors if this mesh is already exported
			MeshPrimitive[] primVariations;
			if (_meshToPrims.TryGetValue(meshObj, out primVariations)
				&& meshObj.subMeshCount == primVariations.Length)
			{
				for (var i = 0; i < primVariations.Length; i++)
				{
					prims[i] = new MeshPrimitive(primVariations[i], _root)
					{
						Material = ExportMaterial(materialsObj[i])
					};
				}

				return prims;
			}

			AccessorId aPosition = null, aNormal = null, aTangent = null,
				aTexcoord0 = null, aTexcoord1 = null, aColor0 = null;

			aPosition = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(meshObj.vertices, SchemaExtensions.CoordinateSpaceConversionScale));

			if (meshObj.normals.Length != 0)
				aNormal = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(meshObj.normals, SchemaExtensions.CoordinateSpaceConversionScale));

			if (meshObj.tangents.Length != 0)
				aTangent = ExportAccessor(SchemaExtensions.ConvertVector4CoordinateSpaceAndCopy(meshObj.tangents, SchemaExtensions.TangentSpaceConversionScale));

			if (meshObj.uv.Length != 0)
				aTexcoord0 = ExportAccessor(SchemaExtensions.FlipTexCoordArrayVAndCopy(meshObj.uv));

			if (meshObj.uv2.Length != 0)
				aTexcoord1 = ExportAccessor(SchemaExtensions.FlipTexCoordArrayVAndCopy(meshObj.uv2));

			if (meshObj.colors.Length != 0)
				aColor0 = ExportAccessor(meshObj.colors);

			MaterialId lastMaterialId = null;

			for (var submesh = 0; submesh < meshObj.subMeshCount; submesh++)
			{
				var primitive = new MeshPrimitive();

				var triangles = meshObj.GetTriangles(submesh);
				if (triangles.Length == 0) { continue; } //empty submesh
				primitive.Indices = ExportAccessor(SchemaExtensions.FlipFacesAndCopy(triangles), true);

				primitive.Attributes = new Dictionary<string, AccessorId>();
				primitive.Attributes.Add(SemanticProperties.POSITION, aPosition);

				if (aNormal != null)
					primitive.Attributes.Add(SemanticProperties.NORMAL, aNormal);
				if (aTangent != null)
					primitive.Attributes.Add(SemanticProperties.TANGENT, aTangent);
				if (aTexcoord0 != null)
					primitive.Attributes.Add(SemanticProperties.TexCoord(0), aTexcoord0);
				if (aTexcoord1 != null)
					primitive.Attributes.Add(SemanticProperties.TexCoord(1), aTexcoord1);
				if (aColor0 != null)
					primitive.Attributes.Add(SemanticProperties.Color(0), aColor0);

				if (submesh < materialsObj.Length)
				{
					primitive.Material = ExportMaterial(materialsObj[submesh]);
					lastMaterialId = primitive.Material;
				}
				else
				{
					primitive.Material = lastMaterialId;
				}

				prims[submesh] = primitive;
			}
			//remove any prims that have empty triangles
			List<MeshPrimitive> listPrims = new List<MeshPrimitive>(prims);
			listPrims.RemoveAll(EmptyPrimitive);
			prims = listPrims.ToArray();

			_meshToPrims[meshObj] = prims;

			return prims;
		}

		private static bool EmptyPrimitive(MeshPrimitive prim)
		{
			if (prim == null || prim.Attributes == null)
			{
				return true;
			}
			return false;
		}

		private MaterialId ExportMaterial(Material materialObj)
		{
			//TODO if material is null
			MaterialId id = GetMaterialId(_root, materialObj);
			if (id != null)
			{
				return id;
			}

			var material = new GLTFMaterial();

			if (materialObj == null)
			{
				if (ExportNames)
				{
					material.Name = "null";
				}
				_materials.Add(materialObj);
				material.PbrMetallicRoughness = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };

				id = new MaterialId
				{
					Id = _root.Materials.Count,
					Root = _root
				};
				_root.Materials.Add(material);
				return id;
			}

			if (ExportNames)
			{
				material.Name = materialObj.name;
			}


			//pass GLTFMaterial into shader property collection and fill in the properties (albedo, metalic, roughness, normal, occlusion, emission)
			ShaderPropertyCollection shaderProperties = null;
			if (MaterialExportPropertyCollection.TryGetValue(materialObj.shader.name, out shaderProperties))
			{
				shaderProperties.FillProperties(this, material, materialObj);
			}
			else
			{
				//fall back to unity's standard shader properties
				shaderProperties = MaterialExportPropertyCollection["Standard"];
				shaderProperties.FillProperties(this, material, materialObj);
			}

			_materials.Add(materialObj);

			id = new MaterialId
			{
				Id = _root.Materials.Count,
				Root = _root
			};
			_root.Materials.Add(material);

			return id;
		}

		private void ExportTextureTransform(TextureInfo def, Material mat, string texName)
		{
			Vector2 offset = mat.GetTextureOffset(texName);
			Vector2 scale = mat.GetTextureScale(texName);

			if (offset == Vector2.zero && scale == Vector2.one) return;

			if (_root.ExtensionsUsed == null)
			{
				_root.ExtensionsUsed = new List<string>(
					new[] { ExtTextureTransformExtensionFactory.EXTENSION_NAME }
				);
			}
			else if (!_root.ExtensionsUsed.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME))
			{
				_root.ExtensionsUsed.Add(ExtTextureTransformExtensionFactory.EXTENSION_NAME);
			}

			if (RequireExtensions)
			{
				if (_root.ExtensionsRequired == null)
				{
					_root.ExtensionsRequired = new List<string>(
						new[] { ExtTextureTransformExtensionFactory.EXTENSION_NAME }
					);
				}
				else if (!_root.ExtensionsRequired.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME))
				{
					_root.ExtensionsRequired.Add(ExtTextureTransformExtensionFactory.EXTENSION_NAME);
				}
			}

			if (def.Extensions == null)
				def.Extensions = new Dictionary<string, IExtension>();

			def.Extensions[ExtTextureTransformExtensionFactory.EXTENSION_NAME] = new ExtTextureTransformExtension(
				new GLTF.Math.Vector2(offset.x, -offset.y),
				0,
				new GLTF.Math.Vector2(scale.x, scale.y),
				0 // TODO: support UV channels
			);
		}

		private NormalTextureInfo ExportNormalTextureInfo(
			Texture texture,
			TextureMapType textureMapType,
			Material material,
			string shaderOverride)
		{
			var info = new NormalTextureInfo();

			info.Index = ExportTexture(texture, textureMapType, true, shaderOverride);

			if (material.HasProperty("_BumpScale"))
			{
				info.Scale = material.GetFloat("_BumpScale");
			}

			return info;
		}

		private OcclusionTextureInfo ExportOcclusionTextureInfo(
			Texture texture,
			TextureMapType textureMapType,
			Material material)
		{
			var info = new OcclusionTextureInfo();

			info.Index = ExportTexture(texture, textureMapType, true, "");

			if (material.HasProperty("_OcclusionStrength"))
			{
				info.Strength = material.GetFloat("_OcclusionStrength");
			}

			return info;
		}

		private TextureInfo ExportTextureInfo(Texture texture, TextureMapType textureMapType, bool linear, string shaderOverrideName)
		{
			var info = new TextureInfo();

			info.Index = ExportTexture(texture, textureMapType, linear, shaderOverrideName);

			return info;
		}

		private TextureId ExportTexture(Texture textureObj, TextureMapType textureMapType, bool linear, string shaderOverrideName)
		{
			TextureId id = GetTextureId(_root, textureObj);
			if (id != null)
			{
				return id;
			}

			var texture = new GLTFTexture();

			//If texture name not set give it a unique name using count
			if (textureObj.name == "")
			{
				textureObj.name = (_root.Textures.Count + 1).ToString();
			}

			if (ExportNames)
			{
				texture.Name = textureObj.name;
			}

			texture.Source = ExportImage(textureObj, textureMapType, linear, shaderOverrideName);
			texture.Sampler = ExportSampler(textureObj);

			_textures.Add(textureObj);

			id = new TextureId
			{
				Id = _root.Textures.Count,
				Root = _root
			};

			_root.Textures.Add(texture);

			return id;
		}

		private ImageId ExportImage(Texture texture, TextureMapType texturMapType, bool linear, string shaderOverrideName)
		{
			ImageId id = GetImageId(_root, texture);
			if (id != null)
			{
				return id;
			}

			var image = new GLTFImage();

			if (ExportNames)
			{
				image.Name = texture.name;
			}

			if (texture.GetType() == typeof(RenderTexture))
			{
				Texture2D tempTexture = new Texture2D(texture.width, texture.height);
				tempTexture.name = texture.name;

				RenderTexture.active = texture as RenderTexture;
				tempTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
				tempTexture.Apply();
				texture = tempTexture;
			}
#if UNITY_2017_1_OR_NEWER
			if (texture.GetType() == typeof(CustomRenderTexture))
			{
				Texture2D tempTexture = new Texture2D(texture.width, texture.height);
				tempTexture.name = texture.name;

				RenderTexture.active = texture as CustomRenderTexture;
				tempTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
				tempTexture.Apply();
				texture = tempTexture;
			}
#endif

			_imageInfos.Add(new ImageInfo
			{
				texture = texture as Texture2D,
				textureMapType = texturMapType,
				Linear = linear,
				ShaderOverrideName = shaderOverrideName
			}); ;

			var imagePath = _retrieveTexturePathDelegate(texture);

			string texturenamehash = "";

			if (texture.name != Uri.EscapeUriString(texture.name).Replace('#', '_'))
			{
				texturenamehash = Mathf.Abs(imagePath.GetHashCode()) + Mathf.Abs(texture.name.GetHashCode()).ToString();
				image.Name = texturenamehash + ".png";
			}
			else
			{
				texturenamehash = Mathf.Abs(imagePath.GetHashCode()) + texture.name;
			}



			if (string.IsNullOrEmpty(imagePath))
			{
				image.Uri = texturenamehash + ".png";
			}
			else
			{
				var filenamePath = texturenamehash + ".png";
				image.Uri = filenamePath;
			}

			id = new ImageId
			{
				Id = _root.Images.Count,
				Root = _root
			};

			_root.Images.Add(image);

			return id;
		}

		private SamplerId ExportSampler(Texture texture)
		{
			var samplerId = GetSamplerId(_root, texture);
			if (samplerId != null)
				return samplerId;

			var sampler = new Sampler();

			if (texture.wrapMode == TextureWrapMode.Clamp)
			{
				sampler.WrapS = WrapMode.ClampToEdge;
				sampler.WrapT = WrapMode.ClampToEdge;
			}
			else
			{
				sampler.WrapS = WrapMode.Repeat;
				sampler.WrapT = WrapMode.Repeat;
			}

			if (texture.filterMode == FilterMode.Point)
			{
				sampler.MinFilter = MinFilterMode.NearestMipmapNearest;
				sampler.MagFilter = MagFilterMode.Nearest;
			}
			else if (texture.filterMode == FilterMode.Bilinear)
			{
				sampler.MinFilter = MinFilterMode.NearestMipmapLinear;
				sampler.MagFilter = MagFilterMode.Linear;
			}
			else
			{
				sampler.MinFilter = MinFilterMode.LinearMipmapLinear;
				sampler.MagFilter = MagFilterMode.Linear;
			}

			samplerId = new SamplerId
			{
				Id = _root.Samplers.Count,
				Root = _root
			};

			_root.Samplers.Add(sampler);

			return samplerId;
		}

		private AccessorId ExportAccessor(int[] arr, bool isIndices = false)
		{
			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			int min = arr[0];
			int max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}

			uint byteOffset = (uint)_bufferWriter.BaseStream.Position;

			if (max <= byte.MaxValue && min >= byte.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedByte;

				foreach (var v in arr)
				{
					_bufferWriter.Write((byte)v);
				}
			}
			else if (max <= sbyte.MaxValue && min >= sbyte.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Byte;

				foreach (var v in arr)
				{
					_bufferWriter.Write((sbyte)v);
				}
			}
			else if (max <= short.MaxValue && min >= short.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Short;

				foreach (var v in arr)
				{
					_bufferWriter.Write((short)v);
				}
			}
			else if (max <= ushort.MaxValue && min >= ushort.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedShort;

				foreach (var v in arr)
				{
					_bufferWriter.Write((ushort)v);
				}
			}
			else if (min >= uint.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedInt;

				foreach (var v in arr)
				{
					_bufferWriter.Write((uint)v);
				}
			}
			else
			{
				accessor.ComponentType = GLTFComponentType.Float;

				foreach (var v in arr)
				{
					_bufferWriter.Write((float)v);
				}
			}

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			uint byteLength = (uint)_bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Vector2[] arr)
		{
			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC2;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float maxX = arr[0].x;
			float maxY = arr[0].y;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
			}

			accessor.Min = new List<double> { minX, minY };
			accessor.Max = new List<double> { maxX, maxY };

			uint byteOffset = (uint)_bufferWriter.BaseStream.Position;

			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
			}

			uint byteLength = (uint)_bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Vector3[] arr)
		{
			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC3;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ };
			accessor.Max = new List<double> { maxX, maxY, maxZ };

			uint byteOffset = (uint)_bufferWriter.BaseStream.Position;

			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
			}

			uint byteLength = (uint)_bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Vector4[] arr)
		{
			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float minW = arr[0].w;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;
			float maxW = arr[0].w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			uint byteOffset = (uint)_bufferWriter.BaseStream.Position;

			foreach (var vec in arr)
			{
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
				_bufferWriter.Write(vec.w);
			}

			uint byteLength = (uint)_bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Color[] arr)
		{
			uint count = (uint)arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minR = arr[0].r;
			float minG = arr[0].g;
			float minB = arr[0].b;
			float minA = arr[0].a;
			float maxR = arr[0].r;
			float maxG = arr[0].g;
			float maxB = arr[0].b;
			float maxA = arr[0].a;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.r < minR)
				{
					minR = cur.r;
				}
				if (cur.g < minG)
				{
					minG = cur.g;
				}
				if (cur.b < minB)
				{
					minB = cur.b;
				}
				if (cur.a < minA)
				{
					minA = cur.a;
				}
				if (cur.r > maxR)
				{
					maxR = cur.r;
				}
				if (cur.g > maxG)
				{
					maxG = cur.g;
				}
				if (cur.b > maxB)
				{
					maxB = cur.b;
				}
				if (cur.a > maxA)
				{
					maxA = cur.a;
				}
			}

			accessor.Min = new List<double> { minR, minG, minB, minA };
			accessor.Max = new List<double> { maxR, maxG, maxB, maxA };

			uint byteOffset = (uint)_bufferWriter.BaseStream.Position;

			foreach (var color in arr)
			{
				_bufferWriter.Write(color.r);
				_bufferWriter.Write(color.g);
				_bufferWriter.Write(color.b);
				_bufferWriter.Write(color.a);
			}

			uint byteLength = (uint)_bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView(byteOffset, byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private BufferViewId ExportBufferView(uint byteOffset, uint byteLength)
		{
			var bufferView = new BufferView
			{
				Buffer = _bufferId,
				ByteOffset = byteOffset,
				ByteLength = byteLength
			};

			var id = new BufferViewId
			{
				Id = _root.BufferViews.Count,
				Root = _root
			};

			_root.BufferViews.Add(bufferView);

			return id;
		}

		public MaterialId GetMaterialId(GLTFRoot root, Material materialObj)
		{
			for (var i = 0; i < _materials.Count; i++)
			{
				if (_materials[i] == materialObj)
				{
					return new MaterialId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		public TextureId GetTextureId(GLTFRoot root, Texture textureObj)
		{
			for (var i = 0; i < _textures.Count; i++)
			{
				if (_textures[i] == textureObj)
				{
					return new TextureId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		public ImageId GetImageId(GLTFRoot root, Texture imageObj)
		{
			for (var i = 0; i < _imageInfos.Count; i++)
			{
				if (_imageInfos[i].texture == imageObj)
				{
					return new ImageId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		public SamplerId GetSamplerId(GLTFRoot root, Texture textureObj)
		{
			for (var i = 0; i < root.Samplers.Count; i++)
			{
				bool filterIsNearest = root.Samplers[i].MinFilter == MinFilterMode.Nearest
					|| root.Samplers[i].MinFilter == MinFilterMode.NearestMipmapNearest
					|| root.Samplers[i].MinFilter == MinFilterMode.LinearMipmapNearest;

				bool filterIsLinear = root.Samplers[i].MinFilter == MinFilterMode.Linear
					|| root.Samplers[i].MinFilter == MinFilterMode.NearestMipmapLinear;

				bool filterMatched = textureObj.filterMode == FilterMode.Point && filterIsNearest
					|| textureObj.filterMode == FilterMode.Bilinear && filterIsLinear
					|| textureObj.filterMode == FilterMode.Trilinear && root.Samplers[i].MinFilter == MinFilterMode.LinearMipmapLinear;

				bool wrapMatched = textureObj.wrapMode == TextureWrapMode.Clamp && root.Samplers[i].WrapS == WrapMode.ClampToEdge
					|| textureObj.wrapMode == TextureWrapMode.Repeat && root.Samplers[i].WrapS != WrapMode.ClampToEdge;

				if (filterMatched && wrapMatched)
				{
					return new SamplerId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

	}
}
