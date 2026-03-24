using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class MediaDetailGUI : IFeatureDetailGUI
    {
        UnityEngine.Video.VideoClip selectedClip;
        UnityEngine.Camera userCamera;
        bool latlong = true;

        int _choiceIndex;

        #region Visual Elements
        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Scene Media", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Media documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/media/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Scene Media allows you detect and aggregate gaze data on media objects like images, videos, and 360 degree videos.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            GUILayout.Label("1. Create Media", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Create and manage your media assets in the Dashboard.", EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Open Dashboard Media Manager", GUILayout.Height(30)) && FeatureLibrary.projectID > 0)
            {
                Application.OpenURL(CognitiveStatics.GetMediaSettingsUrl(FeatureLibrary.projectID));
            }

            EditorGUILayout.Space(10);

            GUILayout.Label("2. (Optional) Add Media Component", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Select a GameObject in your scene and click below to add the Media component.", EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Add Media to Selected", GUILayout.Height(30)))
            {
                AttachMediaToSelected();
            }

            EditorGUILayout.Space(10);

            GUILayout.Label("3. (Optional) Build Media", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Use the tool below to build media interactively. Components will be added automatically.", EditorStyles.wordWrappedLabel);

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(10);
            
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

            selectedClip = (UnityEngine.Video.VideoClip)EditorGUILayout.ObjectField("Video Clip", selectedClip, typeof(UnityEngine.Video.VideoClip), true);
            userCamera = (UnityEngine.Camera)EditorGUILayout.ObjectField("Main Camera", userCamera, typeof(UnityEngine.Camera), true);

            if (GUILayout.Button("Refresh Media Sources"))
            {
                EditorCore.RefreshMediaSources();
            }

            //media source
            string[] displayOptions = new string[EditorCore.MediaSources.Length];
            for (int i = 0; i < EditorCore.MediaSources.Length; i++)
            {
                displayOptions[i] = EditorCore.MediaSources[i].name;
            }
            _choiceIndex = EditorGUILayout.Popup("Select Media Source", _choiceIndex, displayOptions);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Description:", GUILayout.Width(100));
            EditorGUI.BeginDisabledGroup(true);
            if (EditorCore.MediaSources.Length > 0)
            {
                EditorGUILayout.TextArea(EditorCore.MediaSources[_choiceIndex].description);
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            var validChoice = _choiceIndex >= 0
                  && _choiceIndex < EditorCore.MediaSources.Length
                  && EditorCore.MediaSources[_choiceIndex] != null
                  && !string.IsNullOrEmpty(EditorCore.MediaSources[_choiceIndex].name);

            bool allSet = selectedClip != null && validChoice && userCamera != null;

            if (!allSet)
            {
                EditorGUILayout.HelpBox(
                    "Ensure all required fields are configured: Video Clip, valid Media Source, and Main Camera",
                    MessageType.Error
                );
            }

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

            EditorGUI.BeginDisabledGroup(!allSet);
            if (GUILayout.Button("Create"))
            {
                CreateAssets();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Media Utilities
        void CreateAssets()
        {
            Shader skyshader = Shader.Find("Skybox/Panoramic");

            if (skyshader == null)
            {
                Debug.LogError("Cognitive3D 360 Setup: Couldn't find panoramic skybox shader!");
                //TODO set up inverted sky sphere mesh for older versions of unity
                return;
            }

            string path = AssetDatabase.GetAssetPath(selectedClip);
            var split = path.Split('/');
            string p = path.Replace(split[split.Length - 1], "");

            //create render texture next to video asset
            //set render texture resolution
            RenderTexture rt = new RenderTexture((int)selectedClip.width, (int)selectedClip.height, 0);
            AssetDatabase.CreateAsset(rt, p + "Cognitive3D_skyboxrt.renderTexture");

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
            AssetDatabase.CreateAsset(material, p + "Cognitive3D_skyboxmat.mat");

            //apply skybox material to skybox
            RenderSettings.skybox = material;

            //instantiate latlong/cube sphere prefab
            GameObject sphere;
            if (latlong)
            {
                sphere = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load("invertedsphereslices"));
            }
            else
            {
                sphere = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load("invertedspherecube"));
            }
            sphere.gameObject.name = "360 Video Player";
            sphere.transform.position = userCamera.transform.position;
            Cognitive3D.Components.GazeSphere360 myGaze = sphere.AddComponent<Cognitive3D.Components.GazeSphere360>();
            myGaze.UserCamera = userCamera;

            //setup video source to write to render texture
            var vp = sphere.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
            if (vp == null)
            {
                var videoPlayerGo = new GameObject("Video Player");
                videoPlayerGo.transform.SetParent(sphere.transform);
                vp = videoPlayerGo.AddComponent<UnityEngine.Video.VideoPlayer>();
            }
            vp.clip = selectedClip;
            vp.source = UnityEngine.Video.VideoSource.VideoClip;
            vp.targetTexture = rt;

            //check that a media component is present on the sphere
            GameObject internalGo;
            var media = sphere.GetComponentInChildren<MediaComponent>();
            if (media == null)
            {
                internalGo = new GameObject("Internal");
                internalGo.transform.SetParent(sphere.transform);
                media = internalGo.AddComponent<MediaComponent>();
            }
            else
            {
                internalGo = media.gameObject;
            }
            media.MediaId = EditorCore.MediaSources[_choiceIndex].uploadId;
            media.VideoPlayer = vp;

            //check other required components
            if (!internalGo.GetComponent<MeshCollider>())
            {
                internalGo.AddComponent<MeshCollider>();
            }
            if (!internalGo.GetComponent<DynamicObject>())
            {
                internalGo.AddComponent<DynamicObject>();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Selection.activeGameObject = internalGo;
        }

        internal static void AttachMediaToSelected()
        {
            foreach (var obj in Selection.gameObjects)
            {
                if (obj.GetComponent<MediaComponent>() == null)
                {
                    Undo.AddComponent<MediaComponent>(obj);
                }
            }
        }
        #endregion
    }
}
