using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

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
            for (int i = 0; i< layerCount; i++)
            {
                try
                {
                    TextureImporterFormat format;
                    if (GetTextureImportFormat(data.splatPrototypes[i].texture, out textureReadable[i], out format))
                    {
                        Texture2D originalTexture = data.splatPrototypes[i].texture as Texture2D;
                        SetTextureImporterFormat(originalTexture, true, TextureImporterFormat.RGBA32);
                    }
                }
                catch
                {

                }
            }

            //get the highest value layer and write pixels to texture
            //Texture2D tempTex = new Texture2D(data.alphamapWidth, data.alphamapHeight);
            Texture2D outTex = new Texture2D(data.alphamapWidth, data.alphamapHeight);
            for (int y = 0; y < data.alphamapHeight; y++)
            {
                for (int x = 0; x < data.alphamapWidth; x++)
                {
                    float[] colorAtLayer = new float[layerCount];
                    for (int i = 0; i<colorAtLayer.Length; i++)
                    {
                        //put layers into colours
                        colorAtLayer[i] = maps[x, y, i];
                    }


                    int highestMap=0;
                    float highestMapValue=0;

                    

                    for (int i = 0; i<colorAtLayer.Length; i++)
                    {
                        if (colorAtLayer[i] > highestMapValue)
                        {
                            highestMapValue = colorAtLayer[i];
                            highestMap = i;
                        }
                    }

                    outTex.SetPixel(x, y, data.splatPrototypes[highestMap].texture.GetPixel(x * 10 % (data.splatPrototypes[highestMap].texture.width * 1), y * 10 % (data.splatPrototypes[highestMap].texture.height * 1)));


                    /*
                    float a0 = maps[x, y, 0];
                    float a1 = maps[x, y, 1];
                    float a2 = maps[x, y, 2];

                    if (a0 > a1 && a0 > a2)
                    {
                        //red
                        //outTex = data.splatPrototypes[0].texture;
                        outTex.SetPixel(x, y, data.splatPrototypes[0].texture.GetPixel(x * 10 % (data.splatPrototypes[0].texture.width * 1), y * 10 % (data.splatPrototypes[0].texture.height * 1)));
                    }
                    else if (a1 > a0 && a1 > a2)
                    {
                        //green
                        outTex.SetPixel(x, y, data.splatPrototypes[1].texture.GetPixel(x * 10 % (data.splatPrototypes[1].texture.width * 1), y * 10 % (data.splatPrototypes[1].texture.height * 1)));
                    }
                    else
                    {
                        //blue
                        outTex.SetPixel(x, y, data.splatPrototypes[2].texture.GetPixel(x * 10 % (data.splatPrototypes[2].texture.width * 1), y * 10 % (data.splatPrototypes[2].texture.height * 1)));
                    }
                    */

                    //debug splat
                    //Color c = new Color(a0, a1, a2);
                    //tempTex.SetPixel(x, y, c);
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
                    TextureImporterFormat format;
                    if (GetTextureImportFormat(data.splatPrototypes[i].texture, out ignored, out format))
                    {
                        SetTextureImporterFormat(data.splatPrototypes[i].texture, textureReadable[i], TextureImporterFormat.RGBA32);
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
                    tVertices[y * w + x] = Vector3.Scale(meshScale, new Vector3(-y, tData[x * tRes, y * tRes], x)) + offset;
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


        private static string MeshToString(MeshFilter mf)
        {
            Mesh m = mf.sharedMesh;
            if (m == null) return "";
            if (mf.GetComponent<MeshRenderer>() == null || !mf.GetComponent<MeshRenderer>().enabled || !mf.gameObject.activeInHierarchy) { return ""; }

            if (m.uv.Length == 0)
            {
                //TODO sometimes explodes when vertex/uv/normal counts don't line up - faces are written assuming these all have the right count!
                Debug.LogError("Skipping export of mesh \"" + m.name + "\". Exporting meshes must be unwrapped");
                return "";
            }

            Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

            StringBuilder sb = new StringBuilder();

            sb.Append("o ").Append(mf.name).Append("\n");
            foreach (Vector3 lv in m.vertices)
            {
                Vector3 wv = mf.transform.TransformPoint(lv);

                //invert x axis
                sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");

            foreach (Vector3 lv in m.normals)
            {
                Vector3 wv = mf.transform.TransformDirection(lv);

                sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");

            Vector2 textureScale = Vector3.one;
            if (mats.Length > 0 && mats[0] != null && mats[0].HasProperty("_MainTex"))
                textureScale = mats[0].GetTextureScale("_MainTex");

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

                        if (mats[material].mainTexture)
                            objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
                        else
                            objMaterial.textureName = null;
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

        private static Dictionary<string, ObjMaterial> PrepareFileWrite()
        {
            Clear();

            return new Dictionary<string, ObjMaterial>();
        }

        private static void MaterialsToFile(string filename)
        {
            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".mtl"))
            {
                int materialCount = materialList.Count;
                int i = 0;

                foreach (KeyValuePair<string, ObjMaterial> kvp in materialList)
                {
                    i++;
                    EditorUtility.DisplayProgressBar("Scene Explorer Export", kvp.Key + " Material", (i / (float)materialCount) / 2 + 0.5f);

                    Material m = kvp.Value.material;

                    Color c = Color.white;
                    if (m.HasProperty("_Color"))
                        c = m.GetColor("_Color");

                    sw.Write("\n");
                    sw.Write("newmtl {0}\n", kvp.Key);
                    sw.Write("Ka  0.6 0.6 0.6\n");
                    sw.Write("Kd  " + c.r + " " + c.g + " " + c.b + "\n");
                    sw.Write("Ks  0.0 0.0 0.0\n");
                    sw.Write("d  1.0\n");
                    sw.Write("Ns  96.0\n");
                    sw.Write("Ni  1.0\n");
                    sw.Write("illum 1\n");

                    //TODO some bug where unused textures are still exported?
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
                            TextureImporterFormat format;
                            if (GetTextureImportFormat((Texture2D)m.mainTexture, out readable, out format))
                            {
                                Texture2D originalTexture = m.mainTexture as Texture2D;

                                SetTextureImporterFormat(originalTexture, true, TextureImporterFormat.RGBA32);
                                int size = 4; //TODO have this adjustable in editorprefs
                                Texture2D outputMiniTexture = RescaleForExport(originalTexture, Mathf.NextPowerOfTwo(originalTexture.width) / size, Mathf.NextPowerOfTwo(originalTexture.height) / size);

                                byte[] bytes = outputMiniTexture.EncodeToPNG();
                                File.WriteAllBytes(destinationFile + m.mainTexture.name + ".png", bytes);

                                SetTextureImporterFormat(originalTexture, readable, format);
                            }
                            else
                            {
                                Texture2D tex = new Texture2D(2, 2);
                                tex.SetPixel(0, 0, Color.grey);
                                tex.SetPixel(1, 1, Color.grey);

                                byte[] bytes = tex.EncodeToPNG();
                                File.WriteAllBytes(destinationFile + m.mainTexture.name + ".png", bytes);
                                //this sometimes happens when exporting built-in unity textures, such as Default Checker
                                Debug.LogWarning("CognitiveVR Scene Export could not find texture '" + m.mainTexture.name + "'. Creating placeholder texture");
                            }
                        }
                        catch
                        {

                        }
                        sw.Write("map_Kd {0}", m.mainTexture.name + ".png");
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
        }

        private static void MeshesToFile(MeshFilter[] mf, string filename, bool includeTextures)
        {
            materialList = PrepareFileWrite();

            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".obj"))
            {
                sw.Write("mtllib ./" + filename + ".mtl\n");

                Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
                for (int i = 0; i < terrains.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Scene Explorer Export", mf[i].name + " Terrain", 0.05f);
                    sw.Write(Export(terrains[i].terrainData, terrains[i].transform.position, i));
                    if (includeTextures)
                        WriteTerrainTexture(terrains[i].terrainData);
                }

                int meshCount = mf.Length;
                int currentMeshIndex = 0;

                for (int i = 0; i < mf.Length; i++)
                {
                    currentMeshIndex++;
                    if (includeTextures)
                        EditorUtility.DisplayProgressBar("Scene Explorer Export", mf[i].name + " Mesh", (currentMeshIndex / (float)meshCount) / 2);
                    else
                        EditorUtility.DisplayProgressBar("Scene Explorer Export", mf[i].name + " Mesh", (currentMeshIndex / (float)meshCount));
                    sw.Write(MeshToString(mf[i]));
                }
            }
            EditorUtility.ClearProgressBar();

            if (includeTextures)
                MaterialsToFile(filename);
        }

        //retrun path to CognitiveVR_SceneExplorerExport. create if it doesn't exist
        public static string GetDirectory(string fullName)
        {
            CreateTargetFolder(fullName);

            return Directory.GetCurrentDirectory() + "\\CognitiveVR_SceneExplorerExport\\" + fullName + "\\";
        }

        private static bool CreateTargetFolder(string fullName)
        {
            try
            {
                Directory.CreateDirectory("CognitiveVR_SceneExplorerExport");
                Directory.CreateDirectory("CognitiveVR_SceneExplorerExport\\" + fullName);
            }
            catch
            {
                EditorUtility.DisplayDialog("Error!", "Failed to create target folder!", "Ok then");
                return false;
            }

            return true;
        }


        public static void ExportWholeSelectionToSingle(string fullName, bool includeTextures)
        {
            if (!CreateTargetFolder(fullName))
            {
                Debug.LogError("Scene Explorer Exporter failed to create target folder: " + fullName);
                return;
            }

            MeshFilter[] meshes = UnityEngine.Object.FindObjectsOfType<MeshFilter>();

            if (meshes.Length == 0)
            {
                EditorUtility.DisplayDialog("No meshes found!", "Please add a mesh filter to the scene", "");
                return;
            }

            int exportedObjects = 0;

            List<MeshFilter> mfList = new List<MeshFilter>();

            CognitiveVR_Preferences prefs = CognitiveVR_EditorPrefs.GetPreferences();
            bool staticGeoOnly = prefs.ExportStaticOnly;
            float minSize = prefs.MinExportGeoSize;

            for (int i = 0; i < meshes.Length; i++)
            {
                if (staticGeoOnly && !meshes[i].gameObject.isStatic) { continue; }
                Renderer r = meshes[i].GetComponent<Renderer>();
                if (r == null || r.bounds.size.magnitude < minSize) { continue; }

                exportedObjects++;
                mfList.Add(meshes[i]);
            }

            if (exportedObjects > 0)
            {
                MeshFilter[] mf = new MeshFilter[mfList.Count];

                for (int i = 0; i < mfList.Count; i++)
                {
                    mf[i] = mfList[i];
                }
                folder = "CognitiveVR_SceneExplorerExport/" + fullName;
                MeshesToFile(mf, fullName, includeTextures);
            }
            else
                EditorUtility.DisplayDialog("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "");
        }

        public static Texture2D RescaleForExport(Texture2D tex, int newWidth, int newHeight)
        {
            Color[] texColors;
            Color[] newColors;
            float ratioX;
            float ratioY;

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

        public static bool GetTextureImportFormat(Texture2D texture, out bool isReadable, out TextureImporterFormat format)
        {
            isReadable = false;
            format = TextureImporterFormat.Alpha8;
            if (null == texture)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Advanced;

                isReadable = tImporter.isReadable;
                format = tImporter.textureFormat;
                return true;
            }
            return false;
        }

        public static void SetTextureImporterFormat(Texture2D texture, bool isReadable, TextureImporterFormat format)
        {
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Advanced;

                tImporter.isReadable = isReadable;
                tImporter.textureFormat = format;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }
    }
}