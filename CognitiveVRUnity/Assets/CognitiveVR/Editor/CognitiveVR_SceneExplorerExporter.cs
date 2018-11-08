using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

//this exporter was built using code from these three scripts
//http://wiki.unity3d.com/index.php/ObjExporter KeliHlodversson
//http://wiki.unity3d.com/index.php/TerrainObjExporter Eric Haines (Eric5h5) Yun Kyu Choi
//http://wiki.unity3d.com/index.php/TextureScale Eric Haines (Eric5h5)
//CC BY SA 3.0

//TODO clean up inputs. make an interface for different formats
//this should not have any references to directories or where to save files

namespace CognitiveVR
{
    struct ObjMaterial
    {
        public string name;
        public string textureName;
        public Material material;
    }

    public class CognitiveVR_SceneExplorerExporter
    {
        public class MeshContainer
        {
            public Transform t;
            public string name;
            public bool active; //active in heirarchy and has renderer
            public Mesh mesh;

            public MeshContainer(string _name, Transform _t, bool _active, Mesh _mesh)
            {
                t = _t;
                name = _name;
                active = _active;
                mesh = _mesh;
            }

            public MeshContainer(MeshFilter mf)
            {
                t = mf.transform;
                name = mf.name;
                active = mf.GetComponent<Renderer>() != null && mf.GetComponent<Renderer>().enabled && mf.gameObject.activeInHierarchy;
                mesh = mf.sharedMesh;
            }

            public MeshContainer(SkinnedMeshRenderer sm, Mesh snapshot)
            {
                t = sm.transform;
                name = sm.name;
                active = sm.GetComponent<Renderer>() != null && sm.GetComponent<Renderer>().enabled && sm.gameObject.activeInHierarchy;
                mesh = snapshot;
            }
        }

        private static int vertexOffset = 0;
        private static int normalOffset = 0;
        private static int uvOffset = 0;

        enum SaveResolution { Full = 0, Half, Quarter, Eighth, Sixteenth }
        static SaveResolution saveResolution = SaveResolution.Quarter;

        static string folder;
        static Dictionary<string, ObjMaterial> materialList;
        static string TerrainAppendMaterial;

        static void WriteTerrainTexture(TerrainData data)
        {
            float[,,] maps = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);

            //LIMIT to 6 layers for now! rbga + black + transparency?
            int layerCount = Mathf.Min(maps.GetLength(2), 6);


            //set terrain textures to readable
            bool[] textureReadable = new bool[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                try
                {
                    if (GetTextureImportFormat(data.splatPrototypes[i].texture, out textureReadable[i]))
                    {
                        Texture2D originalTexture = data.splatPrototypes[i].texture as Texture2D;
                        SetTextureImporterFormat(originalTexture, true);
                    }
                }
                catch
                {

                }
            }

            //8 times higher res than the heightmap data, up to 4096 texture
            Texture2D outTex = new Texture2D(Mathf.Min(4096, data.alphamapWidth * 8), Mathf.Min(4096, data.alphamapHeight * 8));

            float upscalewidth = outTex.width / data.alphamapWidth;
            float upscaleheight = outTex.height / data.alphamapHeight;

            for (int y = 0; y < outTex.height; y++)
            {
                for (int x = 0; x < outTex.width; x++)
                {

                    float[] colorAtLayer = new float[layerCount];
                    for (int i = 0; i < colorAtLayer.Length; i++)
                    {
                        //put layers into colours
                        colorAtLayer[i] = maps[(int)(x / upscalewidth), (int)(y / upscaleheight), i];
                    }


                    int highestMap = 0;
                    float highestMapValue = 0;

                    for (int i = 0; i < colorAtLayer.Length; i++)
                    {
                        if (colorAtLayer[i] > highestMapValue)
                        {
                            highestMapValue = colorAtLayer[i];
                            highestMap = i;
                        }
                    }

                    SplatPrototype sourceSplat = data.splatPrototypes[highestMap];
                    //TODO figure out correct tiling for textures
                    Color color = sourceSplat.texture.GetPixel(x,y);
                    outTex.SetPixel(x, y, color);

                }
            }



            //write material into list
            StringBuilder sb = new StringBuilder();

            sb.Append("\n");
            sb.Append("newmtl Terrain_Generated\n");
            sb.Append("Ka  0.6 0.6 0.6\n");
            sb.Append("Kd  1.0 1.0 1.0\n");
            sb.Append("Ks  0.0 0.0 0.0\n");
            sb.Append("d  1.0\n");
            sb.Append("Ns  96.0\n");
            sb.Append("Ni  1.0\n");
            sb.Append("illum 1\n");
            sb.Append("map_Kd Terrain_Generated.png");

            TerrainAppendMaterial = sb.ToString();




            //write texture to file

            string destinationFile = "";
            int stripIndex = destinationFile.LastIndexOf('/');
            if (stripIndex >= 0)
                destinationFile = destinationFile.Substring(stripIndex + 1).Trim();
            destinationFile = folder + "/" + destinationFile;


            byte[] bytes = outTex.EncodeToPNG();
            File.WriteAllBytes(destinationFile + "Terrain_Generated.png", bytes);




            //texture importer to original

            for (int i = 0; i < layerCount; i++)
            {
                try
                {
                    bool ignored;
                    if (GetTextureImportFormat(data.splatPrototypes[i].texture, out ignored))
                    {
                        SetTextureImporterFormat(data.splatPrototypes[i].texture, textureReadable[i]);
                    }
                }
                catch
                {

                }
            }

        }

        private static string Export(TerrainData terrainData, Vector3 offset, int id)
        {
            int w = terrainData.heightmapWidth;
            int h = terrainData.heightmapHeight;
            Vector3 meshScale = terrainData.size;
            int tRes = (int)Mathf.Pow(2, (int)saveResolution);
            meshScale = new Vector3(meshScale.x / (w - 1) * tRes, meshScale.y, meshScale.z / (h - 1) * tRes);
            Vector2 uvScale = new Vector2(1.0f / (w - 1), 1.0f / (h - 1));
            float[,] tData = terrainData.GetHeights(0, 0, w, h);

            w = (w - 1) / tRes + 1;
            h = (h - 1) / tRes + 1;
            Vector3[] tVertices = new Vector3[w * h];
            Vector2[] tUV = new Vector2[w * h];

            int[] tPolys;

            tPolys = new int[(w - 1) * (h - 1) * 6];

            // Build vertices and UVs
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tVertices[y * w + x] = Vector3.Scale(meshScale, new Vector3(-y, tData[x * tRes, y * tRes], x));
                    tUV[y * w + x] = Vector2.Scale(new Vector2(x * tRes, y * tRes), uvScale);
                }
            }

            int index = 0;
            // Build triangle indices: 3 indices into vertex array for each triangle
            for (int y = 0; y < h - 1; y++)
            {
                for (int x = 0; x < w - 1; x++)
                {
                    // For each grid cell output two triangles
                    tPolys[index++] = (y * w) + x;
                    tPolys[index++] = ((y + 1) * w) + x;
                    tPolys[index++] = (y * w) + x + 1;

                    tPolys[index++] = ((y + 1) * w) + x;
                    tPolys[index++] = ((y + 1) * w) + x + 1;
                    tPolys[index++] = (y * w) + x + 1;
                }
            }

            // Export to .obj
            StringBuilder outputStringBuilder = new StringBuilder();

            //StreamWriter sw = new StreamWriter(fileName);
            try
            {
                outputStringBuilder.Append("o ").Append("terrain" + id).Append("\n");

                // Write vertices
                for (int i = 0; i < tVertices.Length; i++)
                {
                    StringBuilder sb = new StringBuilder("v ", 20);

                    //x is flipped
                    sb.Append((tVertices[i].x - offset.x).ToString()).Append(" ").
                    Append((tVertices[i].y + offset.y).ToString()).Append(" ").
                    Append((tVertices[i].z + offset.z).ToString());
                    outputStringBuilder.Append(sb);
                    outputStringBuilder.Append("\n");
                }

                //write fake normals
                for (int i = 0; i < tVertices.Length; i++)
                {
                    StringBuilder sb = new StringBuilder("vn ", 22);
                    sb.Append("0 0 1");
                    outputStringBuilder.Append(sb);
                    outputStringBuilder.Append("\n");
                }

                // Write UVs
                for (int i = 0; i < tUV.Length; i++)
                {
                    StringBuilder sb = new StringBuilder("vt ", 22);
                    sb.Append(tUV[i].x.ToString()).Append(" ").
                    Append(tUV[i].y.ToString());
                    outputStringBuilder.Append(sb);
                    outputStringBuilder.Append("\n");
                }

                outputStringBuilder.Append("usemtl ").Append("Terrain_Generated").Append("\n");

                // Write triangles
                for (int i = 0; i < tPolys.Length; i += 3)
                {
                    StringBuilder sb = new StringBuilder("f ", 43);
                    sb.Append(tPolys[i] + 1 + vertexOffset).Append("/").Append(tPolys[i] + 1 + uvOffset).Append(" ").
                    Append(tPolys[i + 1] + 1 + vertexOffset).Append("/").Append(tPolys[i + 1] + 1 + uvOffset).Append(" ").
                    Append(tPolys[i + 2] + 1 + vertexOffset).Append("/").Append(tPolys[i + 2] + 1 + uvOffset);

                    outputStringBuilder.Append(sb);
                    outputStringBuilder.Append("\n");
                }

                vertexOffset += tVertices.Length;
                normalOffset += tVertices.Length;
                uvOffset += tUV.Length;
            }
            catch (Exception err)
            {
                Debug.Log("Error saving file: " + err.Message);
            }

            return outputStringBuilder.ToString();
        }

        //reads the mesh and writes obj verts, normals and uvs to string
        public static string GetObjContentsFromMesh(MeshContainer mc, Vector3 origin, Quaternion rotation)
        {
            return MeshToString(mc, origin, rotation, "");
        }

        private static string MeshToString(MeshContainer mc, Vector3 origin, Quaternion rotation, string textureName)
        {
            //TODO rotate mesh inverse of rotation
            //rotate the mf transform, export, then rotate it back?

            Mesh m = mc.mesh;
            if (m == null)
            {
                Debug.Log("Mesh container has no mesh " + mc.name);
                return "";
            }
            if (!mc.active)
            {
                Debug.Log("Export skipping " + mc.name + ". Is not active in hierarchy or does not have an enabled renderer");
                return "";
            }
            //if (mc.GetComponent<MeshRenderer>() == null || !mc.GetComponent<MeshRenderer>().enabled || !mc.gameObject.activeInHierarchy) { return ""; }

            if (m.uv.Length == 0)
            {
                m.uv = new Vector2[m.vertexCount];
                Debug.LogWarning(m.name + " does not have UV data. Generating uv data could lead to artifacts!");
            }

            if (m.normals.Length == 0)
            {
                m.normals = new Vector3[m.vertexCount];
                Debug.LogWarning(m.name + " does not have normals data. Generating normal data could lead to artifacts!");
            }

            Material[] mats = mc.t.GetComponent<Renderer>().sharedMaterials;

            StringBuilder sb = new StringBuilder();

            sb.Append("o ").Append(mc.name).Append("\n");
            foreach (Vector3 lv in m.vertices)
            {
                Vector3 wv = mc.t.TransformPoint(lv) - origin;
                //TODO this doesn't take scale into account

                //flips the vertex around by the rotation
                wv = rotation * wv;

                //invert x axis
                sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");

            foreach (Vector3 lv in m.normals)
            {
                Vector3 wv = mc.t.TransformDirection(lv);

                //flips the vertex around by the rotation
                wv = rotation * wv;

                sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");

            Vector2 textureScale = Vector3.one;
            if (mats.Length > 0 && mats[0] != null && mats[0].HasProperty(textureName))
                textureScale = mats[0].GetTextureScale(textureName);

            foreach (Vector3 v in m.uv)
            {
                //scale uvs to deal with tiled textures
                sb.Append(string.Format("vt {0} {1}\n", v.x * textureScale.x, v.y * textureScale.y));
            }

            for (int material = 0; material < m.subMeshCount; material++)
            {
                sb.Append("\n");
                if (material >= mats.Length) { continue; }
                if (mats[material] == null)
                {
                    sb.Append("usemtl ").Append("null").Append("\n");
                }
                else
                {
                    sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                }

                //See if this material is already in the materiallist.
                try
                {
                    if (mats[material] == null)
                    {
                        ObjMaterial objMaterial = new ObjMaterial();

                        objMaterial.name = "null";
                        objMaterial.textureName = null;
                        objMaterial.material = new Material(Shader.Find("Unlit/Color"));
                        objMaterial.material.color = Color.magenta;

                        materialList.Add(objMaterial.name, objMaterial);
                    }
                    else
                    {
                        ObjMaterial objMaterial = new ObjMaterial();

                        objMaterial.name = mats[material].name;

                        if (mats[material].HasProperty(textureName) && mats[material].GetTexture(textureName))
                        {
                            objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].GetTexture(textureName));
                        }
                        else
                        {
                            objMaterial.textureName = null;
                        }
                        objMaterial.material = mats[material];

                        materialList.Add(objMaterial.name, objMaterial);
                    }
                }
                catch (ArgumentException)
                {
                    //Already in the dictionary
                }


                int[] triangles = m.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    //Because we inverted the x-component, we also needed to alter the triangle winding.
                    sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n",
                        triangles[i] + 1 + vertexOffset, triangles[i + 1] + 1 + normalOffset, triangles[i + 2] + 1 + uvOffset));
                }
            }

            vertexOffset += m.vertices.Length;
            normalOffset += m.normals.Length;
            uvOffset += m.uv.Length;

            return sb.ToString();
        }

        private static void Clear()
        {
            vertexOffset = 0;
            normalOffset = 0;
            uvOffset = 0;
        }

        private static string MeshToString(MeshContainer mc, string materialName)
        {
            //TODO rotate mesh inverse of rotation
            //rotate the mf transform, export, then rotate it back?

            Mesh m = mc.mesh;
            if (m == null)
            {
                Debug.Log("Mesh container has no mesh " + mc.name);
                return "";
            }
            if (!mc.active)
            {
                Debug.Log("Mesh container is not active " + mc.name);
                return "";
            }
            //if (mc.GetComponent<MeshRenderer>() == null || !mc.GetComponent<MeshRenderer>().enabled || !mc.gameObject.activeInHierarchy) { return ""; }

            if (m.uv.Length == 0)
            {
                m.uv = new Vector2[m.vertexCount];
                Debug.LogWarning(m.name + " does not have UV data. Generating uv data could lead to artifacts!");
            }

            if (m.normals.Length == 0)
            {
                m.normals = new Vector3[m.vertexCount];
                Debug.LogWarning(m.name + " does not have normals data. Generating normal data could lead to artifacts!");
            }

            //Material[] mats = mc.t.GetComponent<Renderer>().sharedMaterials;

            StringBuilder sb = new StringBuilder();

            sb.Append("o ").Append(mc.name).Append("\n");
            foreach (Vector3 lv in m.vertices)
            {
                Vector3 wv = lv;
                //TODO this doesn't take scale into account

                //flips the vertex around by the rotation
                //wv = wv;

                //invert x axis
                sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");

            foreach (Vector3 lv in m.normals)
            {
                Vector3 wv = lv;

                //flips the vertex around by the rotation
                //wv = wv;

                sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");

            Vector2 textureScale = Vector3.one;
            //if (mat != null && mat.HasProperty(textureName))
            //    textureScale = mats[0].GetTextureScale(textureName);

            foreach (Vector3 v in m.uv)
            {
                //scale uvs to deal with tiled textures
                sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
            }

            for (int material = 0; material < m.subMeshCount; material++)
            {
                sb.Append("\n");
                sb.Append("usemtl ").Append(materialName).Append("\n");

                int[] triangles = m.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    //Because we inverted the x-component, we also needed to alter the triangle winding.
                    sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n",
                        triangles[i] + 1 + vertexOffset, triangles[i + 1] + 1 + normalOffset, triangles[i + 2] + 1 + uvOffset));
                }
            }

            vertexOffset += m.vertices.Length;
            normalOffset += m.normals.Length;
            uvOffset += m.uv.Length;

            return sb.ToString();
        }

        private static Dictionary<string, ObjMaterial> PrepareFileWrite()
        {
            Clear();

            return new Dictionary<string, ObjMaterial>();
        }

        //TODO pass around a struct with all the details about how to export meshes/materials/textures instead of all these args
        private static bool MaterialsToFile(string filename, int textureDivisor, string textureName)
        {
            bool canceled = false;
            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".mtl"))
            {
                int materialCount = materialList.Count;
                int i = 0;

                foreach (KeyValuePair<string, ObjMaterial> kvp in materialList)
                {
                    i++;
                    if (EditorUtility.DisplayCancelableProgressBar("Scene Explorer Export", kvp.Key + " Material", (i / (float)materialCount) / 2 + 0.5f))
                    {
                        canceled = true;
                        break;
                    }

                    Material m = kvp.Value.material;

                    Color c = Color.white;
                    if (m.HasProperty("_Color"))
                        c = m.GetColor("_Color");

                    //TODO deal with additive particle property names(tintcolor,particletexture). use transparency maps
                    float opacity = 1.0f;
                    if (m.renderQueue >= 3000) //3000 is the default transparent queue
                        opacity = m.color.a;

                    sw.Write("\n");
                    sw.Write("newmtl {0}\n", kvp.Key);
                    sw.Write("Ka  0.6 0.6 0.6\n");
                    sw.Write("Kd  " + c.r + " " + c.g + " " + c.b + "\n");
                    sw.Write("Ks  0.0 0.0 0.0\n");
                    sw.Write("d  " + opacity + "\n");
                    sw.Write("Ns  96.0\n");
                    sw.Write("Ni  1.0\n");
                    sw.Write("illum 1\n");

                    if (kvp.Value.textureName != null)
                    {
                        string destinationFile = "";

                        int stripIndex = destinationFile.LastIndexOf('/');

                        if (stripIndex >= 0)
                            destinationFile = destinationFile.Substring(stripIndex + 1).Trim();

                        destinationFile = folder + "/" + destinationFile;

                        try
                        {
                            bool readable;
                            if (GetTextureImportFormat((Texture2D)m.GetTexture(textureName), out readable))
                            {
                                Texture2D originalTexture = m.GetTexture(textureName) as Texture2D;

                                SetTextureImporterFormat(originalTexture, true);
                                Texture2D outputMiniTexture = RescaleForExport(originalTexture, Mathf.NextPowerOfTwo(originalTexture.width) / textureDivisor, Mathf.NextPowerOfTwo(originalTexture.height) / textureDivisor);

                                byte[] bytes = outputMiniTexture.EncodeToPNG();
                                File.WriteAllBytes(destinationFile + m.GetTexture(textureName).name.Replace(' ', '_') + ".png", bytes);
                                SetTextureImporterFormat(originalTexture, readable);
                            }
                            else
                            {
                                Texture2D tex = new Texture2D(2, 2);
                                tex.SetPixel(0, 0, Color.grey);
                                tex.SetPixel(1, 1, Color.grey);

                                byte[] bytes = tex.EncodeToPNG();
                                File.WriteAllBytes(destinationFile + m.GetTexture(textureName).name.Replace(' ', '_') + ".png", bytes);
                                //this sometimes happens when exporting built-in unity textures, such as Default Checker
                                Debug.LogWarning("CognitiveVR Scene Export could not find texture '" + m.GetTexture(textureName).name + "'. Creating placeholder texture");
                            }
                        }
                        catch
                        {

                        }
                        sw.Write("map_Kd {0}", m.GetTexture(textureName).name.Replace(' ', '_') + ".png");
                    }

                    sw.Write("\n\n\n");
                }
                if (!string.IsNullOrEmpty(TerrainAppendMaterial))
                {
                    sw.Write(TerrainAppendMaterial);
                }
                TerrainAppendMaterial = null;
            }
            EditorUtility.ClearProgressBar();
            return !canceled;
        }

        private static bool MeshesToFile(MeshContainer[] mc, string filename, bool includeTextures, int textureDivisor, Vector3 origin, Quaternion originRot, string textureName,bool skipTerrain)
        {
            bool canceled = false;
            materialList = PrepareFileWrite();

            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".obj")) //TODO mac export filestream ioexception error
            {
                sw.Write("mtllib ./" + filename + ".mtl\n");
                if (!skipTerrain)
                {
                    Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
                    for (int i = 0; i < terrains.Length; i++)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("Scene Explorer Export", mc[i].name + " Terrain", 0.05f))
                        {
                            canceled = true;
                            break;
                        }
                        if (terrains[i].terrainData != null)
                        {
                            sw.Write(Export(terrains[i].terrainData, terrains[i].transform.position, i));
                            if (includeTextures)
                                WriteTerrainTexture(terrains[i].terrainData);
                        }
                    }
                }

                int meshCount = mc.Length;
                int currentMeshIndex = 0;

                for (int i = 0; i < mc.Length; i++)
                {
                    currentMeshIndex++;
                    if (includeTextures)
                    {
                        if (canceled) break;
                        if (EditorUtility.DisplayCancelableProgressBar("Scene Explorer Export", mc[i].name + " Mesh", (currentMeshIndex / (float)meshCount) / 2))
                        {
                            canceled = true;
                        }
                    }
                    else
                    {
                        if (canceled) break;
                        if (EditorUtility.DisplayCancelableProgressBar("Scene Explorer Export", mc[i].name + " Mesh", (currentMeshIndex / (float)meshCount)))
                        {
                            canceled = true;
                        }
                    }
                    sw.Write(MeshToString(mc[i], origin, originRot,textureName));
                }
            }
            EditorUtility.ClearProgressBar();

            bool materialSuccess = false;

            if (includeTextures && !canceled)
                materialSuccess = MaterialsToFile(filename, textureDivisor,textureName);

            return materialSuccess && !canceled;
        }

        //retrun path to CognitiveVR_SceneExplorerExport. create if it doesn't exist
        public static string GetDirectory(string fullName)
        {
            CreateTargetFolder(fullName);

            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName + Path.DirectorySeparatorChar;
        }

        private static bool CreateTargetFolder(string fullName)
        {
            try
            {
                Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName);
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Failed to create folder: CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName, "Ok");
                return false;
            }

            return true;
        }

        /// <summary>
        /// returns true if successfully exported scene
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="includeTextures"></param>
        /// <param name="staticGeoOnly"></param>
        /// <param name="minSize"></param>
        /// <param name="textureDivisor"></param>
        /// <returns></returns>
        public static bool ExportScene(string fullName, bool includeTextures, bool staticGeoOnly, float minSize, int textureDivisor, string textureName)
        {
            if (!CreateTargetFolder(fullName))
            {
                Debug.LogError("Scene Explorer Exporter failed to create target folder: " + fullName);
                return false;
            }

            MeshFilter[] meshes = UnityEngine.Object.FindObjectsOfType<MeshFilter>();

            SkinnedMeshRenderer[] skinnedMeshes = UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>();

            if (meshes.Length + skinnedMeshes.Length == 0)
            {
                EditorUtility.DisplayDialog("No meshes found!", "Scene has been exported with no models", "Ok");
                return false;
            }

            int exportedObjects = 0;
            int smallObjectCount = 0;
            int nonstaticObjectCount = 0;

            //List<MeshFilter> mfList = new List<MeshFilter>();
            List<MeshContainer> mcList = new List<MeshContainer>();

            for (int i = 0; i < meshes.Length; i++)
            {
                if (staticGeoOnly && !meshes[i].gameObject.isStatic) { nonstaticObjectCount++; continue; }
                Renderer r = meshes[i].GetComponent<Renderer>();
                if (r == null || r.bounds.size.magnitude < minSize) { smallObjectCount++; continue; }

                if (meshes[i].GetComponentInParent<DynamicObject>()) { continue; }

                exportedObjects++;
                
                mcList.Add(new MeshContainer(meshes[i]));
            }

            for (int i = 0; i < skinnedMeshes.Length; i++)
            {


                if (staticGeoOnly && !skinnedMeshes[i].gameObject.isStatic) { nonstaticObjectCount++; continue; }
                Renderer r = skinnedMeshes[i].GetComponent<Renderer>();
                if (r == null || r.bounds.size.magnitude < minSize) { smallObjectCount++; continue; }

                if (skinnedMeshes[i].GetComponentInParent<DynamicObject>()) { continue; }

                exportedObjects++;
                
                Mesh tempMesh = new Mesh();


                //bake scaled skinned mesh renderer

                Dictionary<Transform, Vector3> scales = new Dictionary<Transform, Vector3>();

                scales.Add(skinnedMeshes[i].transform, skinnedMeshes[i].transform.localScale);

                skinnedMeshes[i].transform.localPosition = Vector3.zero;
                skinnedMeshes[i].transform.localRotation = Quaternion.identity;
                skinnedMeshes[i].transform.localScale = Vector3.one;

                Vector3 parentbonusscales = Vector3.one;
                Transform currentParent = skinnedMeshes[i].transform.parent;
                int maxrecursive = 1000;
                while (currentParent != null && maxrecursive > 0)
                {
                    scales.Add(currentParent, currentParent.localScale);
                    currentParent.localScale = Vector3.one;
                    currentParent = currentParent.parent;
                    maxrecursive--;
                }

                skinnedMeshes[i].BakeMesh(tempMesh);

                foreach (var kvp in scales)
                {
                    kvp.Key.localScale = kvp.Value;
                }

                //reset scale to normal

                //meshContainers.Add(new MeshContainer(sm.name, sm.transform, sm.GetComponent<Renderer>() != null && sm.GetComponent<Renderer>().enabled, tempMesh));
                //skinnedMeshes[i].BakeMesh(tempMesh);
                mcList.Add(new MeshContainer(skinnedMeshes[i], tempMesh));
            }

            bool success = false;

            //TODO bug export scene with no geometry, then exporting the scene again will display the wrong error message

            if (exportedObjects > 0)
            {
                mcList.RemoveAll(delegate (MeshContainer obj) { return obj == null; });
                mcList.RemoveAll(delegate (MeshContainer obj) { return obj.mesh == null; });
                //mcList.RemoveAll(delegate (MeshContainer obj) { return string.IsNullOrEmpty(obj.mesh.name); });

                folder = "CognitiveVR_SceneExplorerExport/" + fullName;
                success = MeshesToFile(mcList.ToArray(), fullName, includeTextures, textureDivisor, Vector3.zero, Quaternion.identity, textureName,false);
                return success;
            }
            else
            {
                if (staticGeoOnly && nonstaticObjectCount > smallObjectCount)
                    EditorUtility.DisplayDialog("Scene not exported", "Make sure at your meshes are marked as static, or disable ExportStaticMeshesOnly!", "Ok");
                else
                    EditorUtility.DisplayDialog("Scene not exported", "Make sure your mesh has a renderer and is larger than MinimumExportSize", "Ok");
                return false;
            }
        }

        public static Texture2D RescaleForExport(Texture2D tex, int newWidth, int newHeight)
        {
            Color[] texColors;
            Color[] newColors;
            float ratioX;
            float ratioY;

            newHeight = Mathf.Max(1, newHeight);
            newWidth = Mathf.Max(1, newWidth);

            texColors = tex.GetPixels();
            newColors = new Color[newWidth * newHeight];
            ratioX = ((float)tex.width) / newWidth;
            ratioY = ((float)tex.height) / newHeight;

            int w = tex.width;
            int w2 = newWidth;

            for (var y = 0; y < newHeight; y++)
            {
                var thisY = (int)(ratioY * y) * w;
                var yw = y * w2;
                for (var x = 0; x < w2; x++)
                {
                    newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
                }
            }

            Texture2D newText = new Texture2D(newWidth, newHeight);
            newText.SetPixels(newColors);
            return newText;
        }

        public static bool GetTextureImportFormat(Texture2D texture, out bool isReadable)
        {
            isReadable = false;
            if (null == texture)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;

                isReadable = tImporter.isReadable;
                return true;
            }
            return false;
        }

        public static void SetTextureImporterFormat(Texture2D texture, bool isReadable)
        {
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Default;

                tImporter.isReadable = isReadable;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }

        private static List<MeshFilter> RecursivelyGetMeshes(Transform transform)
        {
            List<MeshFilter> filters = new List<MeshFilter>();

            var filter = transform.GetComponent<MeshFilter>();
            if (filter != null)
            {
                filters.Add(filter);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<DynamicObject>() != null){ continue; }
                if (!transform.GetChild(i).gameObject.activeSelf || !transform.GetChild(i).gameObject.activeInHierarchy) { continue; } //skip disabled objects

                filters.AddRange(RecursivelyGetMeshes(transform.GetChild(i)));
            }

            return filters;
        }

        private static List<SkinnedMeshRenderer> RecursivelyGetSkinnedMeshes(Transform transform)
        {
            List<SkinnedMeshRenderer> skinnedMeshes = new List<SkinnedMeshRenderer>();

            var filter = transform.GetComponent<SkinnedMeshRenderer>();
            if (filter != null)
            {
                skinnedMeshes.Add(filter);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<DynamicObject>() != null) { continue; }
                if (!transform.GetChild(i).gameObject.activeSelf || !transform.GetChild(i).gameObject.activeInHierarchy) { continue; } //skip disabled objects

                skinnedMeshes.AddRange(RecursivelyGetSkinnedMeshes(transform.GetChild(i)));
            }

            return skinnedMeshes;
        }

        private static List<Canvas> RecursivelyGetCanvases(Transform transform)
        {
            List<Canvas> canvases = new List<Canvas>();

            var filter = transform.GetComponent<Canvas>();
            if (filter != null)
            {
                canvases.Add(filter);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<DynamicObject>() != null) { continue; }
                if (!transform.GetChild(i).gameObject.activeSelf || !transform.GetChild(i).gameObject.activeInHierarchy) { continue; } //skip disabled objects

                canvases.AddRange(RecursivelyGetCanvases(transform.GetChild(i)));
            }

            return canvases;
        }

        /// <summary>
        /// write canvas mesh to file
        /// write material and texture to file
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="meshName"></param>
        /// <param name="tex"></param>
        /// <param name="uniqueMaterialId">id for material and texture names. if there are multiple canvases as children</param>
        public static void ExportDynamicObject(Mesh mesh, string meshName, Texture2D tex, string uniqueMaterialId)
        {
            //write mesh verts/uvs/whatever to string
            string meshString = MeshToString(new MeshContainer(meshName, null, true, mesh), "GeneratedUIMaterial" + uniqueMaterialId);

            //write meshstring to file
            Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + meshName + Path.DirectorySeparatorChar);
            File.WriteAllText("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + meshName + Path.DirectorySeparatorChar + meshName + ".obj", meshString);

            //write material to string
            StringBuilder sb = new StringBuilder();
            sb.Append("\n");
            sb.Append("newmtl GeneratedUIMaterial" + uniqueMaterialId + "\n");
            sb.Append("Ka  0.6 0.6 0.6\n");
            sb.Append("Kd  1.0 1.0 1.0\n");
            sb.Append("Ks  0.0 0.0 0.0\n");
            sb.Append("d  1.0\n");
            sb.Append("Ns  96.0\n");
            sb.Append("Ni  1.0\n");
            sb.Append("illum 1\n");
            sb.Append("map_Kd GeneratedUITexture_" + uniqueMaterialId + ".png");
            string materialString = sb.ToString();

            //write materialstring to file
            File.WriteAllText("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + meshName + Path.DirectorySeparatorChar + meshName + ".mtl", materialString);

            //save texture to file. likely already there from snapshot but whatever
            File.WriteAllBytes("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + meshName + Path.DirectorySeparatorChar + "GeneratedUITexture_" + uniqueMaterialId + ".png", tex.EncodeToPNG());
        }

        public static Mesh ExportQuad(string meshName, float width, float height, Transform transform, string sceneName, Texture2D tex = null)
        {
            //this writes data into a mesh class then passes it through to dynamic object exporter. to futureproof when we move from obj to gltf

            Vector3 size = new Vector3(width, height, 0);
            Vector3 pivot = size / 2;

            Mesh m = new Mesh();

            //GameObject go = new GameObject("TEMP_MESH");
            //go.AddComponent<MeshFilter>().mesh = m;

            /*//DEBUGGING
            go.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
            if (tex != null)
            {
                go.GetComponent<MeshRenderer>().material.mainTexture = tex;
                go.GetComponent<MeshRenderer>().material.SetInt("_Mode", 2);
            }
            */

            var verts = new Vector3[4];
            verts[0] = new Vector3(0, 0, 0) - pivot;
            verts[1] = new Vector3(width, 0, 0) - pivot;
            verts[2] = new Vector3(0, height, 0) - pivot;
            verts[3] = new Vector3(width, height, 0) - pivot;
            m.vertices = verts;

            var tris = new int[6];
            tris[0] = 0;
            tris[1] = 2;
            tris[2] = 1;
            tris[3] = 2;
            tris[4] = 3;
            tris[5] = 1;
            m.triangles = tris;

            var norms = new Vector3[4];
            norms[0] = -Vector3.forward;
            norms[1] = -Vector3.forward;
            norms[2] = -Vector3.forward;
            norms[3] = -Vector3.forward;
            m.normals = norms;

            //if width == height, 0-1 uvs are fine
            if (Mathf.Approximately(width, height))
            {
                var uvs = new Vector2[4];
                uvs[0] = new Vector2(0, 0);
                uvs[1] = new Vector2(1, 0);
                uvs[2] = new Vector2(0, 1);
                uvs[3] = new Vector2(1, 1);
                m.uv = uvs;
            }
            else if (height > width)
            {
                var uvs = new Vector2[4];
                uvs[0] = new Vector2(0, 0);
                uvs[1] = new Vector2(width / height, 0);
                uvs[2] = new Vector2(0, 1);
                uvs[3] = new Vector2(width / height, 1);
                m.uv = uvs;
            }
            else
            {
                var uvs = new Vector2[4];
                uvs[0] = new Vector2(0, 0);
                uvs[1] = new Vector2(1, 0);
                uvs[2] = new Vector2(0, height / width);
                uvs[3] = new Vector2(1, height / width);
                m.uv = uvs;
            }

            return m;
        }

        public static Texture2D Snapshot(Transform target, int resolution = 128)
        {
            //var sceneview = (SceneView)SceneView.sceneViews[0];
            //target = Selection.activeTransform;

            GameObject cameraGo = new GameObject("Temp_Camera");
            Camera cam = cameraGo.AddComponent<Camera>();

            //put camera in editor camera position

            cameraGo.transform.rotation = target.rotation;
            cameraGo.transform.position = target.position - target.forward * 0.05f;

            var width = target.GetComponent<RectTransform>().sizeDelta.x * target.localScale.x;
            var height = target.GetComponent<RectTransform>().sizeDelta.y * target.localScale.y;
            if (Mathf.Approximately(width, height))
            {
                //whatever. centered
            }
            else if (height > width) //tall
            {
                //half of the difference between width and height
                cameraGo.transform.position += (cameraGo.transform.right) * (height - width) / 2;
            }
            else //wide
            {
                //half of the difference between width and height
                cameraGo.transform.position += (cameraGo.transform.up) * (width - height) / 2;
            }

            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 0.06f;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(target.GetComponent<RectTransform>().sizeDelta.x * target.localScale.x, target.GetComponent<RectTransform>().sizeDelta.y * target.localScale.y) / 2;
            cam.clearFlags = CameraClearFlags.Depth;

            //create render texture and assign to camera
            RenderTexture rt = new RenderTexture(resolution, resolution, 16);
            cam.targetTexture = rt;

            cam.Render();

            //TODO write non-square textures, do full 0-1 uvs instead of saving blank space

            //write rendertexture to png
            Texture2D tex = new Texture2D(resolution, resolution);
            RenderTexture.active = rt;

            tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex.Apply();

            RenderTexture.active = null;

            //delete stuff
            UnityEngine.Object.DestroyImmediate(cameraGo);

            return tex;
        }

        //TODO remove doubles, decimate?
        //used to export dynamic objects
        public static bool ExportDynamicObject(Transform transform)
        {
            if (transform == null)
            {
                EditorUtility.DisplayDialog("Export Dynamic Object", "No source object selected! Please select one or more Dynamic Objects", "Ok");
                return false;
            }

            //int exportedObjectCount = 0;

            if (!CreateTargetFolder("Dynamic"))
            {
                Debug.LogWarning("Failed to create folder Dynamic");
                return false;
            }

            DynamicObject dynamic = transform.GetComponent<DynamicObject>();
            if (dynamic == null)
            {
                Debug.Log("Skipping " + transform.gameObject + ". Needs a Dynamic Object component");
                return false;
            }
            if (!dynamic.UseCustomMesh)
            {
                //Debug.Log("Skipping " + transform.gameObject + ". Common Meshes for Dynamic Objects don't need to be exported");
                return false;
            }

            //recusively loop thorugh children, adding mesh filters unless dynamic object is found
            MeshFilter[] meshfilters = RecursivelyGetMeshes(transform).ToArray();

            List<MeshContainer> meshContainers = new List<MeshContainer>();
            foreach (var mf in meshfilters)
            {
                meshContainers.Add(new MeshContainer(mf.name, mf.transform, mf.GetComponent<Renderer>() != null && mf.GetComponent<Renderer>().enabled, mf.sharedMesh));
            }


            //recursively loop through children, adding canvases unless dynamic is found
            //exporting nested canvases requires some rework to these utilities

            /*Canvas[] canvases = RecursivelyGetCanvases(transform).ToArray();

            List<MeshContainer> canvasContainers = new List<MeshContainer>();
            foreach (var c in canvases)
            {
                //meshContainers.Add(new MeshContainer(mf.name, mf.transform, mf.GetComponent<Renderer>() != null && mf.GetComponent<Renderer>().enabled, mf.sharedMesh));
                //meshcon

                

                var width = c.GetComponent<RectTransform>().sizeDelta.x * transform.localScale.x;
                var height = c.GetComponent<RectTransform>().sizeDelta.y * c.transform.localScale.y;

                var screenshot = CognitiveVR_SceneExplorerExporter.Snapshot(c.transform);

                var mesh = CognitiveVR_SceneExplorerExporter.ExportQuad(dynamic.MeshName, width, height, c.transform, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, screenshot);
                string randomId = UnityEngine.Random.Range(0, 10000).ToString();
                CognitiveVR_SceneExplorerExporter.ExportDynamicObject(mesh, dynamic.MeshName, screenshot, randomId);
            }*/


            //recusively loop thorugh children, adding mesh filters unless dynamic object is found
            SkinnedMeshRenderer[] skinnedMeshes = RecursivelyGetSkinnedMeshes(transform).ToArray();

            foreach (var sm in skinnedMeshes)
            {
                Mesh tempMesh = new Mesh();
                Dictionary<Transform, Vector3> scales = new Dictionary<Transform, Vector3>();

                scales.Add(sm.transform, sm.transform.localScale);
                //Vector3 scale = sm.transform.localScale;
                //Quaternion rot = sm.transform.localRotation;
                //Vector3 pos = sm.transform.localPosition;

                sm.transform.localPosition = Vector3.zero;
                sm.transform.localRotation = Quaternion.identity;
                sm.transform.localScale = Vector3.one;

                

                Vector3 parentbonusscales = Vector3.one;
                Transform currentParent = sm.transform.parent;
                int maxrecursive = 1000;
                while (currentParent != null && maxrecursive > 0)
                {
                    scales.Add(currentParent, currentParent.localScale);
                    currentParent.localScale = Vector3.one;
                    currentParent = currentParent.parent;
                    maxrecursive--;
                }

                sm.BakeMesh(tempMesh);

                foreach(var kvp in scales)
                {
                    kvp.Key.localScale = kvp.Value;
                }

                //sm.transform.localPosition = pos;
                //sm.transform.localRotation = rot;
                //sm.transform.localScale = scale;

                meshContainers.Add(new MeshContainer(sm.name, sm.transform, sm.GetComponent<Renderer>() != null && sm.GetComponent<Renderer>().enabled, tempMesh));
            }

            if (meshfilters.Length + skinnedMeshes.Length == 0)
            {
                Debug.Log("Skipping " + transform.gameObject + ". No mesh filter on gameobject or children");
                return false;
            }

            bool hasActiveMeshContainers = false;
            foreach (var v in meshContainers)
            {
                if (v.active)
                {
                    hasActiveMeshContainers = true;
                    break;
                }
            }

            if (!hasActiveMeshContainers)
            {
                Debug.Log("Skipping " + transform.gameObject + ". Could find active renderers");
                return false;
            }

            if (!CreateTargetFolder("Dynamic/" + dynamic.MeshName))
            {
                Debug.LogWarning("Failed to create folder " + dynamic.MeshName);
                return false;
            }

            //exportedObjectCount++;

            string objectName = dynamic.MeshName;

            folder = "CognitiveVR_SceneExplorerExport/Dynamic/" + objectName;

            return MeshesToFile(meshContainers.ToArray(), objectName, true, 1, transform.position, Quaternion.Inverse(transform.rotation), "_MainTex",true);
        }
    }
}