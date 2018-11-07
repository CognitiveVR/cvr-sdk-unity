using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TerrainMeshHelper
{
    public static Mesh GenerateMesh(Terrain terrain)
    {
        float downsample = 4f;

        Mesh mesh = new Mesh();
        mesh.name = "temp";

        var w = (int)(terrain.terrainData.heightmapWidth / downsample);
        var h = (int)(terrain.terrainData.heightmapHeight / downsample);

        Vector3[] vertices = new Vector3[w * h];
        Vector2[] uv = new Vector2[w * h];
        Vector4[] tangents = new Vector4[w * h];
        
        //all points

        Vector2 uvScale = new Vector2(1.0f / (w - 1), 1.0f / (w - 1));
        Vector3 sizeScale = new Vector3(terrain.terrainData.size.x / (w - 1), 1/*terrain.terrainData.size.y*/, terrain.terrainData.size.z / (h - 1));

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float pixelHeight = terrain.terrainData.GetHeight((int)(x* downsample), (int)(y* downsample));
                Vector3 vertex = new Vector3(x, pixelHeight, y);
                vertices[y * w + x] = Vector3.Scale(sizeScale, vertex);
                uv[y * w + x] = Vector2.Scale(new Vector2(x, y), uvScale);

                // Calculate tangent vector: a vector that goes from previous vertex
                // to next along X direction. We need tangents if we intend to
                // use bumpmap shaders on the mesh.
                //Vector3 vertexL = new Vector3(x - 1, heightMap.GetPixel(x - 1, y).grayscale, y);
                //Vector3 vertexR = new Vector3(x + 1, heightMap.GetPixel(x + 1, y).grayscale, y);
                //Vector3 tan = Vector3.Scale(sizeScale, vertexR - vertexL).normalized;
                //tangents[y * w + x] = new Vector4(tan.x, tan.y, tan.z, -1.0f);

                tangents[y * w + x] = new Vector4(1,1,1, -1.0f);
            }
        }

        //generate mesh strips
        // Assign them to the mesh
        mesh.vertices = vertices;
        mesh.uv = uv;

        // Build triangle indices: 3 indices into vertex array for each triangle
        int[] triangles = new int[(h - 1) * (w - 1) * 6];
        int index = 0;
        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                // For each grid cell output two triangles
                triangles[index++] = (y * w) + x;
                triangles[index++] = ((y + 1) * w) + x;
                triangles[index++] = (y * w) + x + 1;

                triangles[index++] = ((y + 1) * w) + x;
                triangles[index++] = ((y + 1) * w) + x + 1;
                triangles[index++] = (y * w) + x + 1;
            }
        }

        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.tangents = tangents;

        return mesh;
    }

    public static Texture2D BakeTerrainTexture(string destinationFolder, TerrainData data)
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
        outTex.name = data.name.Replace(' ','_');
        float upscalewidth = outTex.width / data.alphamapWidth;
        float upscaleheight = outTex.height / data.alphamapHeight;

        float[] colorAtLayer = new float[layerCount];
        SplatPrototype[] prototypes = data.splatPrototypes;

        for (int y = 0; y < outTex.height; y++)
        {
            for (int x = 0; x < outTex.width; x++)
            {
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
                
                //TODO figure out correct tiling for textures
                Color color = prototypes[highestMap].texture.GetPixel(y, x);
                outTex.SetPixel(y, x, color);

            }
        }



        //write material into list
        //StringBuilder sb = new StringBuilder();
        //
        //sb.Append("\n");
        //sb.Append("newmtl Terrain_Generated\n");
        //sb.Append("Ka  0.6 0.6 0.6\n");
        //sb.Append("Kd  1.0 1.0 1.0\n");
        //sb.Append("Ks  0.0 0.0 0.0\n");
        //sb.Append("d  1.0\n");
        //sb.Append("Ns  96.0\n");
        //sb.Append("Ni  1.0\n");
        //sb.Append("illum 1\n");
        //sb.Append("map_Kd Terrain_Generated.png");
        //
        //TerrainAppendMaterial = sb.ToString();
        //
        //
        //
        //
        ////write texture to file
        //
        //string destinationFile = "";
        //int stripIndex = destinationFile.LastIndexOf('/');
        //if (stripIndex >= 0)
        //    destinationFile = destinationFile.Substring(stripIndex + 1).Trim();
        //destinationFile = folder + "/" + destinationFile;
        //
        //
        byte[] bytes = outTex.EncodeToPNG(); //TODO replace ' ' with '_'
        System.IO.File.WriteAllBytes(destinationFolder + "/" + data.name.Replace(' ', '_') + ".png", bytes);
        //AssetDatabase.Refresh();
        //var t = AssetDatabase.LoadAssetAtPath<Texture2D>("Terrain_Generated" + data.name + ".png");

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

        return outTex;
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
}