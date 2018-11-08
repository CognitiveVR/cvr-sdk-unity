using System;
using UnityEditor;
using UnityGLTF;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class GLTFExportMenu : EditorWindow
{
    public static string RetrieveTexturePath(UnityEngine.Texture texture)
    {
        return AssetDatabase.GetAssetPath(texture);
    }

    [MenuItem("GLTF/Settings")]
    static void Init()
    {
        GLTFExportMenu window = (GLTFExportMenu)EditorWindow.GetWindow(typeof(GLTFExportMenu), false, "GLTF Settings");
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Exporter", EditorStyles.boldLabel);
        GLTFSceneExporter.ExportFullPath = EditorGUILayout.Toggle("Export using original path", GLTFSceneExporter.ExportFullPath);
        GLTFSceneExporter.ExportNames = EditorGUILayout.Toggle("Export names of nodes", GLTFSceneExporter.ExportNames);
        GLTFSceneExporter.RequireExtensions= EditorGUILayout.Toggle("Require extensions", GLTFSceneExporter.RequireExtensions);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Importer", EditorStyles.boldLabel);
        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("UnityGLTF version 0.1", MessageType.Info);
        EditorGUILayout.HelpBox("Supported extensions: KHR_material_pbrSpecularGlossiness, ExtTextureTransform", MessageType.Info);
    }

    [MenuItem("GLTF/Export Selected")]
	static void ExportSelected()
	{
        foreach(var selected in Selection.transforms)
        {
            //exporting nested dynamics gets messy

            var exporter = new GLTFSceneExporter(new Transform[1] { selected }, RetrieveTexturePath);

        }

		string name;
		//if (Selection.transforms.Length > 1)
		//	name = SceneManager.GetActiveScene().name;
		//else if (Selection.transforms.Length == 1)
		//	name = Selection.activeGameObject.name;
		//else
		//	throw new Exception("No objects selected, cannot export.");

		//var exporter = new GLTFSceneExporter(Selection.transforms, RetrieveTexturePath);



        //string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamics" + Path.DirectorySeparatorChar + ;
        //
        //var path = EditorUtility.OpenFolderPanel("glTF Export Path", "", "");
		//if (!string.IsNullOrEmpty(path)) {
		//	exporter.SaveGLTFandBin (path, name);
		//}
	}

    class BakeableMesh
    {
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public bool useOriginalscale;
        public Vector3 originalScale;
    }

	[MenuItem("GLTF/Export Scene")]
	static void ExportScene()
	{
		var scene = SceneManager.GetActiveScene();
		var gameObjects = scene.GetRootGameObjects();

        List<Transform> t = new List<Transform>();
        foreach (var v in gameObjects)
        {
            if (v.GetComponent<MeshFilter>() != null && v.GetComponent<MeshFilter>().sharedMesh == null) { continue; }
            if (v.activeInHierarchy) { t.Add(v.transform); }
            //check for mesh renderers here, before nodes are constructed for invalid objects?
        }

        List<BakeableMesh> temp = new List<BakeableMesh>();

        //bake, create and add skeletal meshes
        foreach (var v in FindObjectsOfType<SkinnedMeshRenderer>())
        {
            BakeableMesh bm = new BakeableMesh();
            bm.meshRenderer = v.gameObject.AddComponent<MeshRenderer>();
            bm.meshRenderer.sharedMaterial = v.sharedMaterial;
            bm.meshFilter = v.gameObject.AddComponent<MeshFilter>();
            bm.meshFilter.sharedMesh = new Mesh();
            bm.originalScale = v.transform.localScale;
            bm.useOriginalscale = true;
            v.BakeMesh(bm.meshFilter.sharedMesh);
            v.transform.localScale = Vector3.one;
            temp.Add(bm); 
        }

        string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + scene.name;

        foreach (var v in FindObjectsOfType<Terrain>())
        {
            if (!v.isActiveAndEnabled) { continue; }

            //generate mesh from heightmap
            BakeableMesh bm = new BakeableMesh();
            bm.meshRenderer = v.gameObject.AddComponent<MeshRenderer>();
            bm.meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            bm.meshRenderer.sharedMaterial.mainTexture = TerrainMeshHelper.BakeTerrainTexture(path,v.terrainData);
            bm.meshFilter = v.gameObject.AddComponent<MeshFilter>();
            bm.meshFilter.sharedMesh = TerrainMeshHelper.GenerateMesh(v);
            temp.Add(bm);
        }

        //var transforms = Array.ConvertAll(gameObjects, gameObject => gameObject.transform);

        var exporter = new GLTFSceneExporter(t.ToArray(), RetrieveTexturePath);
        //var path = EditorUtility.OpenFolderPanel("glTF Export Path", "", "");
        

        //make directories
        Directory.CreateDirectory(path);

        Debug.Log(path);
        Debug.Log(Application.dataPath + "CognitiveVR_SceneExplorerExport");

        exporter.SaveGLTFandBin(path, scene.name);

        for (int i = 0; i < temp.Count; i++)
        {
            if (temp[i].useOriginalscale)
                temp[i].meshRenderer.transform.localScale = temp[i].originalScale;
            DestroyImmediate(temp[i].meshFilter);
            DestroyImmediate(temp[i].meshRenderer);
        }
    }

    
}
