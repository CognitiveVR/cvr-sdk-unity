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


        private static string MeshToString(MeshFilter mf, Dictionary<string, ObjMaterial> materialList)
        {
            Mesh m = mf.sharedMesh;
            if (m == null) return "";
            if (mf.GetComponent<MeshRenderer>() == null || !mf.GetComponent<MeshRenderer>().enabled || !mf.gameObject.activeInHierarchy) { return ""; }

            if (m.uv.Length == 0)
            {
                //TODO figure out why all the vertices explode when uvs not set
                Debug.LogError("Skipping export of mesh \"" + m.name + "\". Exporting meshes must be unwrapped");
                return "";
            }

            Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

            StringBuilder sb = new StringBuilder();

            sb.Append("o ").Append(mf.name).Append("\n");
            foreach (Vector3 lv in m.vertices)
            {
                Vector3 wv = mf.transform.TransformPoint(lv);

                //This is sort of ugly - inverting x-component since we're in
                //a different coordinate system than "everyone" is "used to".
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
                    //sb.Append("usemap ").Append(mats[material].name).Append("\n");
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

        private static void MaterialsToFile(Dictionary<string, ObjMaterial> materialList, string folder, string filename)
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
            }
            EditorUtility.ClearProgressBar();
        }

        private static void MeshesToFile(MeshFilter[] mf, string folder, string filename, bool includeTextures)
        {
            Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();

            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".obj"))
            {
                sw.Write("mtllib ./" + filename + ".mtl\n");

                int meshCount = mf.Length;
                int currentMeshIndex = 0;

                for (int i = 0; i < mf.Length; i++)
                {
                    currentMeshIndex++;
                    if (includeTextures)
                        EditorUtility.DisplayProgressBar("Scene Explorer Export", mf[i].name + " Mesh", (currentMeshIndex / (float)meshCount) / 2);
                    else
                        EditorUtility.DisplayProgressBar("Scene Explorer Export", mf[i].name + " Mesh", (currentMeshIndex / (float)meshCount));
                    sw.Write(MeshToString(mf[i], materialList));
                }
            }
            EditorUtility.ClearProgressBar();

            if (includeTextures)
                MaterialsToFile(materialList, folder, filename);
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
                Debug.Log("failed to create target folder");
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

                //int stripIndex = fullName.LastIndexOf('/');

                //if (stripIndex >= 0)
                //    fullName = fullName.Substring(stripIndex + 1).Trim();

                Debug.Log("fullName " + fullName);
                MeshesToFile(mf, "CognitiveVR_SceneExplorerExport/" + fullName, fullName, includeTextures);
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