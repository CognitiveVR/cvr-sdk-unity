using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CognitiveVR;
using UnityEngine.Video;

namespace CognitiveVR
{
public class Setup360Window : EditorWindow
{
    VideoClip selectedClip;
    bool latlong = true;

    public static void Init()
    {
        Setup360Window window = (Setup360Window)EditorWindow.GetWindow(typeof(Setup360Window), true, "360 Video Setup");
        window.Show();
    }

    int _choiceIndex = 0;

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (selectedClip != null)
        {
            GUILayout.Label(AssetPreview.GetMiniThumbnail(selectedClip), GUILayout.Height(128), GUILayout.Width(128));
        }
        else
        {
            GUILayout.Box("", GUILayout.Height(128), GUILayout.Width(128));
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        selectedClip = (VideoClip)EditorGUILayout.ObjectField(selectedClip, typeof(UnityEngine.Video.VideoClip),true);

        if (EditorCore.MediaSources.Length == 0)
        {
            if (GUILayout.Button("Refresh Media Sources"))
            {
                EditorCore.RefreshMediaSources();
            }
            return;
        }

        //media source
        string[] displayOptions = new string[EditorCore.MediaSources.Length];
        for(int i = 0; i< EditorCore.MediaSources.Length;i++)
        {
            displayOptions[i] = EditorCore.MediaSources[i].name;
        }
        _choiceIndex = EditorGUILayout.Popup("Select Media Source", _choiceIndex, displayOptions);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Description:", GUILayout.Width(100));
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextArea(EditorCore.MediaSources[_choiceIndex].description);
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Projection Type");
        if (latlong) { GUI.color = Color.green; }
        if (GUILayout.Button("Latitude Longitude", EditorStyles.miniButtonLeft)) { latlong = true; }
        GUI.color = Color.white;
        if (!latlong) { GUI.color = Color.green; }
        if (GUILayout.Button("Cubemap", EditorStyles.miniButtonRight)) { latlong = false; }
        GUI.color = Color.white;
        GUILayout.EndHorizontal();


        EditorGUI.BeginDisabledGroup(selectedClip == null || string.IsNullOrEmpty(EditorCore.MediaSources[_choiceIndex].name));
        if (GUILayout.Button("Create"))
        {
            CreateAssets();
        }
        EditorGUI.EndDisabledGroup();
    }

    void CreateAssets()
    {
        Shader skyshader = Shader.Find("Skybox/Panoramic");

        if (skyshader == null)
        {
            Debug.LogError("360 media setup couldn't find panoramic skybox shader!");
            //TODO set up inverted sky sphere mesh for older versions of unity
            return;
        }

        string path = AssetDatabase.GetAssetPath(selectedClip);
        var split = path.Split('/');
        string p = path.Replace(split[split.Length - 1], "");

        //create render texture next to video asset
        //set render texture resolution
        RenderTexture rt = new RenderTexture((int)selectedClip.width, (int)selectedClip.height, 0);
        AssetDatabase.CreateAsset(rt, p + "skyboxrt.renderTexture");

        //create skybox material next to video asset
        Material material = new Material(skyshader);
        if (latlong)
        {
            string[] s = material.shaderKeywords;
            ArrayUtility.Add<string>(ref s, "_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            material.shaderKeywords = s;
        }
        else
        {
            string[] s = material.shaderKeywords;
            ArrayUtility.Add<string>(ref s, "_MAPPING_6_FRAMES_LAYOUT");
            material.shaderKeywords = s;
        }
        //set skybox material texture to render texture
        material.SetTexture("_MainTex", rt);
        AssetDatabase.CreateAsset(material, p + "skyboxmat.mat");

        //apply skybox material to skybox
        RenderSettings.skybox = material;

        //instantiate latlong/cube sphere
        GameObject sphere;
        if (latlong)
        {
            sphere = (GameObject)Instantiate(Resources.Load("invertedsphereslices"));
        }
        else
        {
            sphere = (GameObject)Instantiate(Resources.Load("invertedspherecube"));
        }

        //setup video source to write to render texture
        VideoPlayer vp = new GameObject("Video Player").AddComponent<VideoPlayer>();
        vp.clip = selectedClip;
        vp.source = VideoSource.VideoClip;
        vp.targetTexture = rt;

        //attach media component to sphere
        //add meshcollider to sphere
        sphere = sphere.transform.GetChild(0).gameObject;
        sphere.GetComponent<MeshRenderer>().enabled = false;
        var media = sphere.AddComponent<MediaComponent>();
        media.MediaSource = EditorCore.MediaSources[_choiceIndex].uploadId;
        media.VideoPlayer = vp;
        if (!sphere.GetComponent<MeshCollider>())
            sphere.AddComponent<MeshCollider>();

        if (!sphere.GetComponent<DynamicObject>())
            sphere.AddComponent<DynamicObject>();

        var dyn = sphere.GetComponent<DynamicObject>();
        dyn.UseCustomMesh = false;
        if (latlong)
            dyn.CommonMesh = DynamicObject.CommonDynamicMesh.VideoSphereLatitude;
        else
            dyn.CommonMesh = DynamicObject.CommonDynamicMesh.VideoSphereCubemap;



        var camMain = Camera.main;
        if (camMain == null)
        {
            Debug.LogError("could not find camera.main!");
        }
        else
        {
            camMain.transform.position = Vector3.zero;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = sphere;

        Close();
    }
}
}