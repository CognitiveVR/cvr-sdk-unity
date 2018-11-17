using System;
using UnityEditor;
using UnityGLTF;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using CognitiveVR;

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

    static void RecurseThroughChildren(Transform t, List<DynamicObject> dynamics)
    {
        var d = t.GetComponent<DynamicObject>();
        if (d != null)
        {
            dynamics.Add(d);
        }
        for(int i = 0; i<t.childCount;i++)
        {
            RecurseThroughChildren(t.GetChild(i), dynamics);
        }
    }

    [MenuItem("GLTF/Export Selected")]
	static void ExportSelected()
	{
        //recursively get all dynamic objects to export
        List<DynamicObject> AllDynamics = new List<DynamicObject>();

        foreach (var selected in Selection.transforms)
        {
            RecurseThroughChildren(selected, AllDynamics);
        }

        string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar;
        //create directory

        foreach (var v in AllDynamics)
        {
            //bake skin, terrain, canvas

            Debug.Log("path " + path + v.MeshName + Path.DirectorySeparatorChar + "   mesh " + v.gameObject.name);

            Directory.CreateDirectory(path + v.MeshName + Path.DirectorySeparatorChar);

            List<BakeableMesh> temp = new List<BakeableMesh>();

            //string path2 = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + scene.name;
            BakeNonstandardRenderers(v, temp, path + v.MeshName + Path.DirectorySeparatorChar);

            var exporter = new GLTFSceneExporter(new Transform[1] { v.transform }, RetrieveTexturePath, v);
            exporter.SaveGLTFandBin(path + v.MeshName + Path.DirectorySeparatorChar, v.MeshName);

            

            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i].useOriginalscale)
                    temp[i].meshRenderer.transform.localScale = temp[i].originalScale;
                DestroyImmediate(temp[i].meshFilter);
                DestroyImmediate(temp[i].meshRenderer);
            }

            EditorCore.SaveDynamicThumbnailAutomatic(v.gameObject);

            //destroy baked skin, terrain, canvases
        }
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

        string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + scene.name;
        BakeNonstandardRenderers(null, temp, path);

        var exporter = new GLTFSceneExporter(t.ToArray(), RetrieveTexturePath,null);

        //make directories
        Directory.CreateDirectory(path);

        Debug.Log(path);
        Debug.Log(Application.dataPath + "CognitiveVR_SceneExplorerExport");

        exporter.SaveGLTFandBin(path, "scene");

        for (int i = 0; i < temp.Count; i++)
        {
            if (temp[i].useOriginalscale)
                temp[i].meshRenderer.transform.localScale = temp[i].originalScale;
            DestroyImmediate(temp[i].meshFilter);
            DestroyImmediate(temp[i].meshRenderer);
        }
    }

    static void BakeNonstandardRenderers(DynamicObject rootDynamic, List<BakeableMesh> meshes, string path)
    {
        SkinnedMeshRenderer[] SkinnedMeshes = FindObjectsOfType<SkinnedMeshRenderer>();
        Terrain[] Terrains = FindObjectsOfType<Terrain>();
        Canvas[] Canvases = FindObjectsOfType<Canvas>();
        if (rootDynamic != null)
        {
            SkinnedMeshes = rootDynamic.GetComponentsInChildren<SkinnedMeshRenderer>();
            Terrains = rootDynamic.GetComponentsInChildren<Terrain>();
            Canvases = rootDynamic.GetComponentsInChildren<Canvas>();
        }

        foreach (var v in SkinnedMeshes)
        {
            if (!v.gameObject.activeInHierarchy) { continue; }
            if (rootDynamic == null && v.GetComponentInParent<DynamicObject>() != null)
            {
                //skinned mesh as child of dynamic when exporting scene
                continue;
            }
            else if (rootDynamic != null && v.GetComponentInParent<DynamicObject>() != rootDynamic)
            {
                //exporting dynamic, found skinned mesh in some other dynamic
                continue;
            }

            BakeableMesh bm = new BakeableMesh();
            bm.meshRenderer = v.gameObject.AddComponent<MeshRenderer>();
            bm.meshRenderer.sharedMaterial = v.sharedMaterial;
            bm.meshFilter = v.gameObject.AddComponent<MeshFilter>();
            bm.meshFilter.sharedMesh = new Mesh();
            bm.originalScale = v.transform.localScale;
            bm.useOriginalscale = true;
            v.BakeMesh(bm.meshFilter.sharedMesh);
            v.transform.localScale = Vector3.one;
            meshes.Add(bm);
        }

        //TODO ignore parent rotation and scale
        foreach (var v in Terrains)
        {
            if (!v.isActiveAndEnabled) { continue; }
            if (rootDynamic == null && v.GetComponentInParent<DynamicObject>() != null)
            {
                //terrain as child of dynamic when exporting scene
                continue;
            }
            else if (rootDynamic != null && v.GetComponentInParent<DynamicObject>() != rootDynamic)
            {
                //exporting dynamic, found terrain in some other dynamic
                continue;
            }

            //generate mesh from heightmap
            BakeableMesh bm = new BakeableMesh();
            bm.meshRenderer = v.gameObject.AddComponent<MeshRenderer>();
            bm.meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            bm.meshRenderer.sharedMaterial.mainTexture = TerrainMeshHelper.BakeTerrainTexture(path, v.terrainData);
            bm.meshFilter = v.gameObject.AddComponent<MeshFilter>();
            bm.meshFilter.sharedMesh = TerrainMeshHelper.GenerateMesh(v);
            meshes.Add(bm);
        }

        foreach (var v in Canvases)
        {
            if (!v.isActiveAndEnabled) { continue; }
            if (v.renderMode != RenderMode.WorldSpace) { continue; }
            if (rootDynamic == null && v.GetComponentInParent<DynamicObject>() != null)
            {
                //canvas as child of dynamic when exporting scene
                continue;
            }
            else if (rootDynamic != null && v.GetComponentInParent<DynamicObject>() != rootDynamic)
            {
                //exporting dynamic, found canvas in some other dynamic
                continue;
            }

            BakeableMesh bm = new BakeableMesh();
            bm.meshRenderer = v.gameObject.AddComponent<MeshRenderer>();
            bm.meshRenderer.sharedMaterial = new Material(Shader.Find("Transparent/Diffuse"));

            var width = v.GetComponent<RectTransform>().sizeDelta.x * v.transform.localScale.x;
            var height = v.GetComponent<RectTransform>().sizeDelta.y * v.transform.localScale.y;

            //bake texture from render
            var screenshot = CognitiveVR_SceneExplorerExporter.Snapshot(v.transform);
            Debug.Log("bake canvas texture for " + v.gameObject.name);
            screenshot.name = v.gameObject.name.Replace(' ', '_');
            bm.meshRenderer.sharedMaterial.mainTexture = screenshot;
            byte[] bytes = screenshot.EncodeToPNG();
            Debug.Log("write file " + path + "/" + screenshot.name + ".png");
            //System.IO.File.WriteAllBytes(path + "/" + screenshot.name + ".png", bytes);

            bm.meshFilter = v.gameObject.AddComponent<MeshFilter>();
            //write simple quad
            var mesh = CognitiveVR_SceneExplorerExporter.ExportQuad(v.gameObject.name + "_canvas", width, height, v.transform, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, screenshot);
            bm.meshFilter.sharedMesh = mesh;
            meshes.Add(bm);
        }
    }
    
}
