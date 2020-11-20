using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;

namespace CognitiveVR
{
    [CustomEditor(typeof(CognitiveVR_Preferences))]
    public class PreferencesInspector : Editor
    {
        bool gpsFoldout;
        bool hasCheckedRenderType = false;

        [UnityEditor.Callbacks.DidReloadScripts]
        public static void CheckGazeRenderType()
        {
            var p = EditorCore.GetPreferences();

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
            if (!hasCheckedRenderType)
            {
                CheckGazeRenderType();
                hasCheckedRenderType = true;
            }

            var p = (CognitiveVR_Preferences)target;

            p.ApplicationKey = EditorGUILayout.TextField("Application Key", p.ApplicationKey);
            p.AttributionKey = EditorGUILayout.TextField("Attribution Key", p.AttributionKey);
            p.EnableLogging = EditorGUILayout.Toggle("Enable Logging", p.EnableLogging);
            p.EnableDevLogging = EditorGUILayout.Toggle("Enable Development Logging", p.EnableDevLogging);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3D Player Tracking",EditorStyles.boldLabel);
            //TODO change tooltip based on selected gaze type
            p.GazeType = (GazeType)EditorGUILayout.EnumPopup("Gaze Type", p.GazeType);
            if (GUI.changed)
            {
                CheckGazeRenderType();
            }
            p.SnapshotInterval = EditorGUILayout.FloatField("Snapshot Interval", p.SnapshotInterval);
            p.DynamicObjectSearchInParent = EditorGUILayout.Toggle(new GUIContent("Dynamic Object Search in Parent", "When capturing gaze on a Dynamic Object, also search in the collider's parent for the dynamic object component"), p.DynamicObjectSearchInParent);

            bool eyetracking = GameplayReferences.SDKSupportsEyeTracking;

            if (p.GazeType == GazeType.Physics || eyetracking)
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

            p.TrackGPSLocation = EditorGUILayout.Toggle(new GUIContent("Track GPS Location", "Record GPS location and compass direction at the interval below"), p.TrackGPSLocation);

            EditorGUI.BeginDisabledGroup(!p.TrackGPSLocation);
            EditorGUI.indentLevel++;
            gpsFoldout = EditorGUILayout.Foldout(gpsFoldout,"GPS Options");
            if (gpsFoldout)
            {
                p.SyncGPSWithGaze = EditorGUILayout.Toggle(new GUIContent("Sync with Player Update", "Request new GPS location every time the player position and gaze is recorded"), p.SyncGPSWithGaze);
                EditorGUI.BeginDisabledGroup(p.SyncGPSWithGaze);
                p.GPSInterval = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("GPS Update Interval","Interval in seconds to record new GPS location data"), p.GPSInterval), 0.1f, 60f);
                EditorGUI.EndDisabledGroup();
                p.GPSAccuracy = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("GPS Accuracy","Desired accuracy in meters. Using higher values like 500 may not require GPS and may save battery power"), p.GPSAccuracy), 1f, 500f);
            }
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            p.RecordFloorPosition = EditorGUILayout.Toggle(new GUIContent("Record Floor Position", "Includes the floor position below the HMD in a VR experience"), p.RecordFloorPosition);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("360 Player Tracking", EditorStyles.boldLabel);
            p.SnapshotInterval = EditorGUILayout.FloatField("Snapshot Interval", p.SnapshotInterval);
            p.SnapshotInterval = Mathf.Clamp(p.SnapshotInterval, 0.1f, 10);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sending Data Batches", EditorStyles.boldLabel);
            
            //gaze
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Gaze", EditorStyles.boldLabel);
            EditorGUI.indentLevel--;
            p.GazeSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Gaze Snapshot Batch Size","The number of Gaze datapoints to record before automatically sending a web request to the dashboard"), p.GazeSnapshotCount),64,1500);

            //transactions
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUI.indentLevel--;
            p.TransactionSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Event Snapshot Batch Size", "The number of Events to record before automatically sending a web request to the dashboard"), p.TransactionSnapshotCount), 1, 1000);
            p.TransactionExtremeSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Event Extreme Batch Size", "Threshold for ignoring the Event Minimum Timer. If this many Events have been recorded, immediately send"), p.TransactionExtremeSnapshotCount), p.TransactionSnapshotCount, 1000);
            p.TransactionSnapshotMinTimer = EditorGUILayout.IntField(new GUIContent("Event Minimum Timer", "Time (in seconds) that must be elapsed before sending a new batch of Event data. Ignored if the batch size reaches Event Extreme Limit"), Mathf.Clamp(p.TransactionSnapshotMinTimer, 1, 10));
            p.TransactionSnapshotMaxTimer = EditorGUILayout.IntField(new GUIContent("Event Automatic Send Timer", "The time (in seconds) to automatically send any outstanding Event data"), Mathf.Clamp(p.TransactionSnapshotMaxTimer, p.TransactionSnapshotMinTimer, 60));

            //dynamics
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Dynamics", EditorStyles.boldLabel);
            EditorGUI.indentLevel--;
            p.DynamicSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Dynamic Snapshot Batch Size", "The number of Dynamic snapshots and manifest entries to record before automatically sending a web request to the dashboard"), p.DynamicSnapshotCount), 16, 1500);
            p.DynamicExtremeSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Dynamic Extreme Batch Size", "Threshold for ignoring the Dynamic Minimum Timer. If this many Dynamic snapshots have been recorded, immediately send"), p.DynamicExtremeSnapshotCount), p.DynamicSnapshotCount, 1500);
            p.DynamicSnapshotMinTimer = EditorGUILayout.IntField(new GUIContent("Dynamic Minimum Timer", "Time (in seconds) that must be elapsed before sending a new batch of Dynamic data. Ignored if the batch size reaches Dynamic Extreme Limit"), Mathf.Clamp(p.DynamicSnapshotMinTimer, 1, 60));
            p.DynamicSnapshotMaxTimer = EditorGUILayout.IntField(new GUIContent("Dynamic Automatic Send Timer", "The time (in seconds) to automatically send any outstanding Dynamic snapshots or Manifest entries"), Mathf.Clamp(p.DynamicSnapshotMaxTimer, 1, 600));

            //sensors
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Sensors", EditorStyles.boldLabel);
            EditorGUI.indentLevel--;
            p.SensorSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Sensor Snapshot Batch Size", "The number of Sensor datapoints to record before automatically sending a web request to the dashboard"), p.SensorSnapshotCount), 64, 1500);
            p.SensorExtremeSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Sensor Extreme Batch Size", "Threshold for ignoring the Sensor Minimum Timer. If this many Sensor datapoints have been recorded, immediately send"), p.SensorExtremeSnapshotCount), p.SensorSnapshotCount, 1500);
            p.SensorSnapshotMinTimer = EditorGUILayout.IntField(new GUIContent("Sensor Minimum Timer", "Time (in seconds) that must be elapsed before sending a new batch of Sensor data. Ignored if the batch size reaches Sensor Extreme Limit"), Mathf.Clamp(p.SensorSnapshotMinTimer, 1, 60));
            p.SensorSnapshotMaxTimer = EditorGUILayout.IntField(new GUIContent("Sensor Automatic Send Timer", "The time (in seconds) to automatically send any outstanding Sensor data"), Mathf.Clamp(p.SensorSnapshotMaxTimer, p.SensorSnapshotMinTimer, 600));

            //fixations
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Fixations", EditorStyles.boldLabel);
            EditorGUI.indentLevel--;
            p.FixationSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Fixation Snapshot Batch Size", "The number of Fixations to record before automatically sending a web request to the dashboard"), p.FixationSnapshotCount), 1, 1000);
            p.FixationExtremeSnapshotCount= Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Fixation Extreme Batch Size", "Threshold for ignoring the Fixation Minimum Timer. If this many Fixations have been recorded, immediately send"), p.FixationExtremeSnapshotCount), p.FixationSnapshotCount, 1000);
            p.FixationSnapshotMinTimer = EditorGUILayout.IntField(new GUIContent("Fixation Minimum Timer", "Time (in seconds) that must be elapsed before sending a new batch of Fixation data. Ignored if the batch size reaches Fixation Extreme Limit"), Mathf.Clamp(p.FixationSnapshotMinTimer, 1, 10));
            p.FixationSnapshotMaxTimer = EditorGUILayout.IntField(new GUIContent("Fixation Automatic Send Timer", "The time (in seconds) to automatically send any outstanding Fixation data"), Mathf.Clamp(p.FixationSnapshotMaxTimer, p.FixationSnapshotMinTimer, 60));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local Data Cache", EditorStyles.boldLabel);
            //local storage
            GUILayout.BeginHorizontal();
            p.LocalStorage = EditorGUILayout.Toggle("Save data to Local Cache if no internet connection", p.LocalStorage);
            if (GUILayout.Button("Open Local Cache Folder"))
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
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
            CognitiveVR_Preferences.Instance.Protocol = EditorGUILayout.TextField(new GUIContent("Custom Protocol", "https"), CognitiveVR_Preferences.Instance.Protocol);
            CognitiveVR_Preferences.Instance.Gateway = EditorGUILayout.TextField(new GUIContent("Custom Gateway", "data.cognitive3d.com"), CognitiveVR_Preferences.Instance.Gateway);
            CognitiveVR_Preferences.Instance.Viewer = EditorGUILayout.TextField(new GUIContent("Custom Viewer", "viewer.cognitive3d.com/scene/"), CognitiveVR_Preferences.Instance.Viewer);
            CognitiveVR_Preferences.Instance.Dashboard = EditorGUILayout.TextField(new GUIContent("Custom Dashboard", "app.cognitive3d.com"), CognitiveVR_Preferences.Instance.Dashboard);
            p.SendDataOnHMDRemove = EditorGUILayout.Toggle("Send Data on HMD Remove", p.SendDataOnHMDRemove);
            p.SendDataOnLevelLoad = EditorGUILayout.Toggle("Send Data on Level Load", p.SendDataOnLevelLoad);
            p.SendDataOnQuit = EditorGUILayout.Toggle("Send Data on Quit", p.SendDataOnQuit);
            p.SendDataOnPause = EditorGUILayout.Toggle("Send Data on Pause", p.SendDataOnPause);
            p.SendDataOnHotkey = EditorGUILayout.Toggle("Send Data on Hotkey", p.SendDataOnHotkey);
            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!p.SendDataOnHotkey);
            GUILayout.BeginHorizontal();

            p.SendDataHotkey = (KeyCode)EditorGUILayout.EnumPopup("Hotkey", p.SendDataHotkey);

            if (p.HotkeyShift){GUI.color = Color.green;}
            if (GUILayout.Button("Shift", EditorStyles.miniButtonLeft)) { p.HotkeyShift = !p.HotkeyShift; }
            GUI.color = Color.white;

            if (p.HotkeyCtrl) { GUI.color = Color.green; }
            if (GUILayout.Button("Ctrl", EditorStyles.miniButtonMid)) { p.HotkeyCtrl = !p.HotkeyCtrl; }
            GUI.color = Color.white;

            if (p.HotkeyAlt) { GUI.color = Color.green; }
            if (GUILayout.Button("Alt", EditorStyles.miniButtonRight)) { p.HotkeyAlt = !p.HotkeyAlt; }
            GUI.color = Color.white;

            /*if (remapHotkey)
            {
                GUILayout.Button("Any Key", EditorStyles.miniButton, GUILayout.Width(100));
                Event e = Event.current;

                if (e.type == EventType.keyDown && e.keyCode != KeyCode.None && e.keyCode != KeyCode.LeftShift && e.keyCode != KeyCode.RightShift && e.keyCode != KeyCode.LeftControl && e.keyCode != KeyCode.RightControl && e.keyCode != KeyCode.LeftAlt && e.keyCode != KeyCode.RightAlt)
                {
                    p.HotkeyAlt = e.alt;
                    p.HotkeyShift = e.shift;
                    p.HotkeyCtrl = e.control;
                    p.SendDataHotkey = e.keyCode;
                    remapHotkey = false;
                    Repaint();
                }
            }
            else
            {
                if (GUILayout.Button("Remap", EditorStyles.miniButton,GUILayout.Width(100)))
                {
                    remapHotkey = true;
                }
            }*/

            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Export", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var v = CognitiveVR_Preferences.FindCurrentScene();
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
            //EditorCore.ExportSettings.TextureQuality = EditorGUILayout.IntPopup(new GUIContent("Texture Export Quality", "Reduce textures when uploading to scene explorer"), EditorCore.ExportSettings.TextureQuality, textureQualityNames, textureQualities);
            p.ExportSceneLODLowest = EditorGUILayout.Toggle("Export Lowest LOD from LODGroup components", p.ExportSceneLODLowest);
            p.ExportAOMaps = EditorGUILayout.Toggle("Export AO Maps", p.ExportAOMaps);
            GUILayout.BeginHorizontal();
            //GUILayout.Space(15);

            //TODO texture export settings

            //the full process
            //refresh scene versions
            //save screenshot
            //export scene
            //decimate scene
            //confirm upload of scene. new scene? new version?
            //export dynamics
            //confirm uploading dynamics
            //confirm upload manifest

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
                string jsonSettingsContents = "{ \"scale\":1,\"sceneName\":\"" + fullName + "\",\"sdkVersion\":\"" + Core.SDK_VERSION + "\"}";
                System.IO.File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

                string debugContent = DebugInformationWindow.GetDebugContents();
                System.IO.File.WriteAllText(objPath + "debug.log", debugContent);
                CognitiveVR_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.AssetDatabase.SaveAssets();
            }

            bool hasUploadFiles = EditorCore.HasSceneExportFolder(CognitiveVR_Preferences.FindCurrentScene());
            
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
                    CognitiveVR_Preferences.SceneSettings current = CognitiveVR_Preferences.FindCurrentScene();

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

            if (GUILayout.Button(new GUIContent("Refresh Latest Scene Versions", "Get the latest versionnumber and versionid for this scene"))) //ask scene explorer for all the versions of this active scene. happens automatically post scene upload
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
            List<CognitiveVR_Preferences.SceneSettings> oldSettings = new List<CognitiveVR_Preferences.SceneSettings>();
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

                prefs.sceneSettings.Add(new CognitiveVR_Preferences.SceneSettings(name, path));
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
