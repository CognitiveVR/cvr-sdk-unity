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
        public override void OnInspectorGUI()
        {
            var p = (Cognitive3D_Preferences)target;

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
            EditorGUILayout.LabelField("Network", EditorStyles.boldLabel);
            p.Gateway = EditorGUILayout.TextField(new GUIContent("Gateway", "In almost every case, this should be\ndata.cognitive3d.com"), p.Gateway);

            EditorGUILayout.LabelField("Scene Data", EditorStyles.boldLabel);
            p.IncludeDisabledDynamicObjects = EditorGUILayout.Toggle("Include Disabled Dynamic Objects", p.IncludeDisabledDynamicObjects);
            var v = Cognitive3D_Preferences.FindCurrentScene();
            if (v == null || string.IsNullOrEmpty(v.SceneId))
            {
                EditorGUILayout.LabelField("Current Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "     Version: not uploaded");
            }
            else
            {
                EditorGUILayout.LabelField("Current Scene: " + v.SceneName + "     Version: " + v.VersionNumber);
            }
            if (GUILayout.Button(new GUIContent("Refresh Loaded Scene Versions", "Get the latest versionnumber and versionid for this scene"))) //ask scene explorer for all the versions of this active scene. happens automatically post scene upload
            {
                EditorCore.RefreshSceneVersion(null);
            }


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
