using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;

namespace Cognitive3D
{
    [CustomEditor(typeof(Cognitive3D_Preferences))]
    public class PreferencesInspector : Editor
    {
        bool hasCheckedRenderType = false;

        public static void CheckGazeRenderType(Cognitive3D_Preferences p)
        {
            if (PlayerSettings.stereoRenderingPath == StereoRenderingPath.MultiPass && p.RenderPassType != 0)
            {
                p.RenderPassType = 0;
                EditorUtility.SetDirty(p);
            }
            else if (PlayerSettings.stereoRenderingPath == StereoRenderingPath.SinglePass && p.RenderPassType != 1)
            {
                p.RenderPassType = 1;
                EditorUtility.SetDirty(p);
            }
            else if (PlayerSettings.stereoRenderingPath == StereoRenderingPath.Instancing)
            {
                if (p.GazeType == GazeType.Command)
                    Debug.LogError("Cognitive3D Analytics does not support Command Buffer Gaze with SinglePass (Instanced) stereo rendering. Please change the gaze type in Cognitive3D->Advanced Options menu");
            }
        }

        public override void OnInspectorGUI()
        {
            var p = (Cognitive3D_Preferences)target;
            if (!hasCheckedRenderType)
            {
                CheckGazeRenderType(p);
                hasCheckedRenderType = true;
            }

            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("SDK Version " + Cognitive3D_Manager.SDK_VERSION);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Debug Info"))
            {
                DebugInformationWindow.Init();
            }
            GUILayout.EndHorizontal();

            p.ApplicationKey = EditorGUILayout.TextField("Application Key", p.ApplicationKey);
            p.AttributionKey = EditorGUILayout.TextField("Attribution Key", p.AttributionKey);
            p.EnableLogging = EditorGUILayout.Toggle("Enable Logging", p.EnableLogging);
            p.EnableDevLogging = EditorGUILayout.Toggle("Enable Development Logging", p.EnableDevLogging);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player Tracking",EditorStyles.boldLabel);
            p.EnableGaze = EditorGUILayout.Toggle(new GUIContent("Record Gaze", "Use a raycast to find the gaze point in world space.\nDisabling this will still record HMD position and rotation"), p.EnableGaze);
            p.DynamicObjectSearchInParent = EditorGUILayout.Toggle(new GUIContent("Dynamic Object Search in Parent", "When capturing gaze on a Dynamic Object, also search in the collider's parent for the dynamic object component"), p.DynamicObjectSearchInParent);

            if (p.GazeType == GazeType.Physics)
            {
                LayerMask gazeMask = new LayerMask();
                gazeMask.value = p.GazeLayerMask;
                gazeMask = EditorGUILayout.MaskField("Gaze Layer Mask", gazeMask, (UnityEditorInternal.InternalEditorUtility.layers));
                p.GazeLayerMask = gazeMask.value;
            }

            LayerMask dynamicMask = new LayerMask();
            dynamicMask.value = p.DynamicLayerMask;
            dynamicMask = EditorGUILayout.MaskField("Dynamic Object Layer Mask", dynamicMask, (UnityEditorInternal.InternalEditorUtility.layers));
            p.DynamicLayerMask = dynamicMask.value;

            p.TriggerInteraction = (QueryTriggerInteraction)EditorGUILayout.EnumPopup("Gaze Query Trigger Interaction", p.TriggerInteraction);
            p.RecordFloorPosition = EditorGUILayout.Toggle(new GUIContent("Record Floor Position", "Includes the floor position below the HMD in a VR experience"), p.RecordFloorPosition);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Location", EditorStyles.boldLabel);
            bool disableGeoSettings = false;
#if !C3D_LOCATION
            EditorGUILayout.HelpBox("Location data is currently disabled. Add C3D_LOCATION to scripting define symbols in Project Settings", MessageType.Info);
            disableGeoSettings = true;
#else
            disableGeoSettings = false;
#endif
            EditorGUI.BeginDisabledGroup(disableGeoSettings);
            p.TrackGPSLocation = EditorGUILayout.Toggle(new GUIContent("Record GPS Location with HMD Position", "Record GPS location and compass direction at the same rate as HMD position"), p.TrackGPSLocation);
            p.GPSAccuracy = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("GPS Accuracy", "Desired accuracy in meters. Using higher values like 500 may not require GPS and may save battery power"), p.GPSAccuracy), 1f, 500f);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sending Data Batches", EditorStyles.boldLabel);

            p.AutomaticSendTimer = EditorGUILayout.IntField(new GUIContent("Automatic Send Timer", "The time (in seconds) to automatically send any outstanding Data"), p.AutomaticSendTimer);
            p.AutomaticSendTimer = Mathf.Clamp(p.AutomaticSendTimer, 1, 60);
            p.GazeSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Gaze Snapshot Batch Size","The number of Gaze datapoints to record before automatically sending a web request to the dashboard"), p.GazeSnapshotCount),64,1500);
            p.EventDataThreshold = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Event Snapshot Batch Size", "The number of Events to record before automatically sending a web request to the dashboard"), p.EventDataThreshold), 1, 1000);
            p.DynamicSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Dynamic Snapshot Batch Size", "The number of Dynamic snapshots and manifest entries to record before automatically sending a web request to the dashboard"), p.DynamicSnapshotCount), 16, 1500);
            p.SensorSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Sensor Snapshot Batch Size", "The number of Sensor datapoints to record before automatically sending a web request to the dashboard"), p.SensorSnapshotCount), 64, 1500);
            p.FixationSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Fixation Snapshot Batch Size", "The number of Fixations to record before automatically sending a web request to the dashboard"), p.FixationSnapshotCount), 1, 1000);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local Data Cache", EditorStyles.boldLabel);
            //local storage
            GUILayout.BeginHorizontal();
            p.LocalStorage = EditorGUILayout.Toggle("Save data to Local Cache if no internet connection", p.LocalStorage);
            if (GUILayout.Button("Open Local Cache Folder"))
            {
                string path = Application.persistentDataPath + "/c3dlocal/";
                if (System.IO.Directory.Exists(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    EditorUtility.RevealInFinder(Application.persistentDataPath);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!p.LocalStorage);
            p.UploadCacheOnEndPlay = EditorGUILayout.Toggle("Upload Cache on End Play (in Editor)", p.UploadCacheOnEndPlay);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Upload Local Cache Data"))
            {
                ICache ic = new DualFileCache(Application.persistentDataPath + "/c3dlocal/");
                if (ic.HasContent())
                    new EditorDataUploader(ic);
                else
                    Debug.Log("No data in Local Cache to upload!");
            }
            GUILayout.EndHorizontal();
            EditorGUI.BeginDisabledGroup(!p.LocalStorage);
            GUILayout.BeginHorizontal();
            p.LocalDataCacheSize = EditorGUILayout.LongField("Cache Size", p.LocalDataCacheSize);
            if (p.LocalDataCacheSize < 1048576) { p.LocalDataCacheSize = 1048576; } //at least 1mb of storage (1048576 bytes)
            EditorGUILayout.LabelField(EditorUtility.FormatBytes(p.LocalDataCacheSize),GUILayout.Width(100));
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sending Data", EditorStyles.boldLabel);
            p.Protocol = EditorGUILayout.TextField(new GUIContent("Custom Protocol", "https"), p.Protocol);
            p.Gateway = EditorGUILayout.TextField(new GUIContent("Custom Gateway", "data.cognitive3d.com"), p.Gateway);
            p.Viewer = EditorGUILayout.TextField(new GUIContent("Custom Viewer", "viewer.cognitive3d.com/scene/"), p.Viewer);
            p.Dashboard = EditorGUILayout.TextField(new GUIContent("Custom Dashboard", "app.cognitive3d.com"), p.Dashboard);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Export", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var v = Cognitive3D_Preferences.FindCurrentScene();
            if (v == null || string.IsNullOrEmpty(v.SceneId))
            {
                EditorGUILayout.LabelField("Current Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "     Version: not uploaded");
            }
            else
            {
                EditorGUILayout.LabelField("Current Scene: " + v.SceneName + "     Version: " + v.VersionNumber);
            }
            EditorGUILayout.Space();

            GUIContent[] textureQualityNames = new GUIContent[] { new GUIContent("Full"), new GUIContent("Half"), new GUIContent("Quarter")/*, new GUIContent("Eighth"), new GUIContent("Sixteenth"), new GUIContent("Thirty Second"), new GUIContent("Sixty Fourth") */};
            int[] textureQualities = new int[] { 1, 2, 4/*, 8, 16, 32, 64 */};
            p.TextureResize = EditorGUILayout.IntPopup(new GUIContent("Texture Export Quality", "Reduce textures when uploading to scene explorer"), p.TextureResize, textureQualityNames, textureQualities);
            p.ExportSceneLODLowest = EditorGUILayout.Toggle("Export Lowest LOD from LODGroup components", p.ExportSceneLODLowest);
            p.ExportAOMaps = EditorGUILayout.Toggle("Export AO Maps", p.ExportAOMaps);
            GUILayout.BeginHorizontal();

            //TODO move this logic into editorcore and reference from both setup window and here
            if (GUILayout.Button("Export","ButtonLeft"))
            {
                if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                {
                    if (EditorUtility.DisplayDialog("Export Failed", "Cannot export scene that is not saved.\n\nDo you want to save now?", "Save", "Cancel"))
                    {
                        if (UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
                        {
                        }
                        else
                        {
                            return;//cancel from save scene window
                        }
                    }
                    else
                    {
                        return;//cancel from 'do you want to save' popup
                    }
                }
                ExportUtility.ExportGLTFScene();

                string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                string objPath = EditorCore.GetSubDirectoryPath(fullName);
                string jsonSettingsContents = "{ \"scale\":1,\"sceneName\":\"" + fullName + "\",\"sdkVersion\":\"" + Cognitive3D_Manager.SDK_VERSION + "\"}";
                System.IO.File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

                string debugContent = DebugInformationWindow.GetDebugContents();
                System.IO.File.WriteAllText(objPath + "debug.log", debugContent);
                Cognitive3D_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.AssetDatabase.SaveAssets();
            }

            bool hasUploadFiles = EditorCore.HasSceneExportFolder(Cognitive3D_Preferences.FindCurrentScene());
            
            EditorGUI.BeginDisabledGroup(!hasUploadFiles);
            if (GUILayout.Button("Upload", "ButtonRight"))
            {
                System.Action completedmanifestupload = delegate ()
                {
                    ExportUtility.UploadAllDynamicObjectMeshes(true);
                };

                System.Action completedRefreshSceneVersion2 = delegate ()
                {
                    ManageDynamicObjects.AggregationManifest manifest = new ManageDynamicObjects.AggregationManifest();
                    ManageDynamicObjects.AddOrReplaceDynamic(manifest, ManageDynamicObjects.GetDynamicObjectsInScene());
                    ManageDynamicObjects.UploadManifest(manifest,completedmanifestupload);
                };

                //upload dynamics
                System.Action completeSceneUpload = delegate () {
                    EditorCore.RefreshSceneVersion(completedRefreshSceneVersion2); //likely completed in previous step, but just in case
                };

                //upload scene
                System.Action completedRefreshSceneVersion1 = delegate () {
                    Cognitive3D_Preferences.SceneSettings current = Cognitive3D_Preferences.FindCurrentScene();

                    if (current == null || string.IsNullOrEmpty(current.SceneId))
                    {
                        //new scene
                        if (EditorUtility.DisplayDialog("Upload New Scene", "Upload " + current.SceneName + " to SceneExplorer?", "Ok", "Cancel"))
                        {
                            ExportUtility.UploadDecimatedScene(current, completeSceneUpload);
                        }
                    }
                    else
                    {
                        //new version
                        if (EditorUtility.DisplayDialog("Upload New Version","Upload a new version of this existing scene? Will archive previous version","Ok","Cancel"))
                        {
                            ExportUtility.UploadDecimatedScene(current, completeSceneUpload);
                        }
                    }
                };

                //get the latest verion of the scene
                if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                {
                    if (EditorUtility.DisplayDialog("Upload Failed", "Cannot upload scene that is not saved.\n\nDo you want to save now?", "Save", "Cancel"))
                    {
                        if (UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
                        {
                            EditorCore.RefreshSceneVersion(completedRefreshSceneVersion1);
                        }
                        else
                        {
                            return;//cancel from save scene window
                        }
                    }
                    else
                    {
                        return;//cancel from 'do you want to save' popup
                    }
                }
                else
                {
                    EditorCore.RefreshSceneVersion(completedRefreshSceneVersion1);
                }
                
            }

            GUIContent ButtonContent = new GUIContent("Upload Screenshot");
            if (v == null)
            {
                GUILayout.Button(ButtonContent);
            }
            else
            {
                if (GUILayout.Button(ButtonContent))
                {
                    EditorCore.UploadCustomScreenshot();
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
            GUILayout.EndHorizontal();

            if (GUILayout.Button(new GUIContent("Refresh Loaded Scene Versions", "Get the latest versionnumber and versionid for this scene"))) //ask scene explorer for all the versions of this active scene. happens automatically post scene upload
            {
                EditorCore.RefreshSceneVersion(null);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneSettings"),true);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            if (GUI.changed)
                EditorUtility.SetDirty(p);
        }

        //currently UNUSED. useful with the scene export window that shows all scenes regardless of if they were exported
        private static void UpdateSceneNames()
        {
            var prefs = EditorCore.GetPreferences();

            //save these to a temp list
            List<Cognitive3D_Preferences.SceneSettings> oldSettings = new List<Cognitive3D_Preferences.SceneSettings>();
            foreach (var v in prefs.sceneSettings)
            {
                oldSettings.Add(v);
            }


            //clear then rebuild the list in preferences
            prefs.sceneSettings.Clear();

            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);

                prefs.sceneSettings.Add(new Cognitive3D_Preferences.SceneSettings(name, path));
            }

            //match up dictionary keys from temp list
            foreach (var oldSetting in oldSettings)
            {
                foreach (var newSetting in prefs.sceneSettings)
                {
                    if (newSetting.SceneName == oldSetting.SceneName)
                    {
                        newSetting.SceneId = oldSetting.SceneId;
                        newSetting.LastRevision = oldSetting.LastRevision;
                        newSetting.SceneName = oldSetting.SceneName;
                        newSetting.ScenePath = oldSetting.ScenePath;
                        newSetting.VersionId = oldSetting.VersionId;
                        newSetting.VersionNumber = oldSetting.VersionNumber;
                    }
                }
            }
        }
    }
}
