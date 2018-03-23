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
        public override void OnInspectorGUI()
        {
            var p = (CognitiveVR_Preferences)target;

            p.APIKey = EditorGUILayout.TextField("API Key", p.APIKey);
            p.EnableLogging = EditorGUILayout.Toggle("Enable Logging", p.EnableLogging);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3D Player Tracking",EditorStyles.boldLabel);
            p.SnapshotInterval = Mathf.Clamp(EditorGUILayout.FloatField("Snapshot Interval", p.SnapshotInterval),0,10);
            p.DynamicObjectSearchInParent = EditorGUILayout.Toggle(new GUIContent("Dynamic Object Search in Parent", "When capturing gaze on a Dynamic Object, also search in the collider's parent for the dynamic object component"), p.DynamicObjectSearchInParent);
            //p.TrackGazePoint

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("360 Player Tracking", EditorStyles.boldLabel);
            p.SnapshotInterval = Mathf.Clamp(EditorGUILayout.FloatField("Snapshot Interval", p.SnapshotInterval), 0, 10);
            p.VideoSphereDynamicObjectId = EditorGUILayout.TextField("Video Sphere Dynamic Object Id", p.VideoSphereDynamicObjectId);
            p.GazeDirectionMultiplier = Mathf.Clamp(EditorGUILayout.FloatField("Video Sphere Radius", p.GazeDirectionMultiplier), 0, 1000);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sending Data Batches", EditorStyles.boldLabel);
            p.GazeSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField("Gaze Snapshot Batch Size", p.GazeSnapshotCount),0,1000);
            p.TransactionSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField("Event Snapshot Batch Size", p.TransactionSnapshotCount), 0, 1000);
            p.DynamicSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField("Dynamic Snapshot Batch Size", p.DynamicSnapshotCount), 0, 1000);
            p.SensorSnapshotCount = Mathf.Clamp(EditorGUILayout.IntField("Sensor Snapshot Batch Size", p.SensorSnapshotCount), 0, 1000);

            //TODO local storage
            //EditorGUILayout.Toggle("Save data locally if no internet connection", false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sending Data Events", EditorStyles.boldLabel);
            p.SendDataOnHMDRemove = EditorGUILayout.Toggle("Send Data on HMD Remove", p.SendDataOnHMDRemove);
            p.SendDataOnLevelLoad = EditorGUILayout.Toggle("Send Data on Level Load", p.SendDataOnLevelLoad);
            p.SendDataOnQuit = EditorGUILayout.Toggle("Send Data on Quit", p.SendDataOnQuit);
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

            GUILayout.BeginHorizontal();
            EditorCore.BlenderPath = EditorGUILayout.TextField("Blender Path", EditorCore.BlenderPath);
            if (GUILayout.Button("...",GUILayout.Width(40)))
            {
                EditorCore.BlenderPath = EditorUtility.OpenFilePanel("Select Blender", string.IsNullOrEmpty(EditorCore.BlenderPath) ? "c:\\" : EditorCore.BlenderPath, "");
            }
            GUILayout.EndHorizontal();

            EditorCore.ExportSettings.ExportStaticOnly = EditorGUILayout.Toggle(new GUIContent("Export Static Meshes Only", "Only export meshes marked as static. Dynamic objects (such as vehicles, doors, etc) will not be exported"), EditorCore.ExportSettings.ExportStaticOnly);
            EditorCore.ExportSettings.MinExportGeoSize = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Minimum Export size", "Ignore exporting meshes that are below this size(pebbles, grass,etc)"), EditorCore.ExportSettings.MinExportGeoSize),0,100);
            EditorCore.ExportSettings.ExplorerMinimumFaceCount = Mathf.Max(EditorGUILayout.IntField(new GUIContent("Minimum Face Count", "Ignore decimating objects with fewer faces than this value"), EditorCore.ExportSettings.ExplorerMinimumFaceCount),0);
            EditorCore.ExportSettings.ExplorerMaximumFaceCount = Mathf.Max(EditorGUILayout.IntField(new GUIContent("Maximum Face Count", "Objects with this many faces will be decimated to 10% of their original face count"), EditorCore.ExportSettings.ExplorerMaximumFaceCount), EditorCore.ExportSettings.ExplorerMinimumFaceCount);
            EditorCore.ExportSettings.DiffuseTextureName = EditorGUILayout.TextField(new GUIContent("Diffuse Texture Name", "The name of the main diffuse texture to export. Generally _MainTex, but possibly something else if you are using a custom shader"), EditorCore.ExportSettings.DiffuseTextureName);

            GUIContent[] textureQualityNames = new GUIContent[] { new GUIContent("Full"), new GUIContent("Half"), new GUIContent("Quarter"), new GUIContent("Eighth"), new GUIContent("Sixteenth") };
            int[] textureQualities = new int[] { 1, 2, 4, 8, 16 };
            EditorCore.ExportSettings.TextureQuality = EditorGUILayout.IntPopup(new GUIContent("Texture Export Quality", "Reduce textures when uploading to scene explorer"), EditorCore.ExportSettings.TextureQuality, textureQualityNames, textureQualities);
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

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
                CognitiveVR.CognitiveVR_SceneExportWindow.ExportScene(true, EditorCore.ExportSettings.ExportStaticOnly, EditorCore.ExportSettings.MinExportGeoSize, EditorCore.ExportSettings.TextureQuality, EditorCore.DeveloperKey, EditorCore.ExportSettings.DiffuseTextureName);
                CognitiveVR_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.AssetDatabase.SaveAssets();
            }

            bool hasUploadFiles = EditorCore.HasSceneExportFolder(CognitiveVR_Preferences.FindCurrentScene());
            
            EditorGUI.BeginDisabledGroup(!hasUploadFiles);
            if (GUILayout.Button("Upload", "ButtonRight"))
            {
                System.Action completedRefreshSceneVersion2 = delegate ()
                {
                    ManageDynamicObjects.UploadManifest();
                };

                //upload dynamics
                System.Action completeSceneUpload = delegate () {
                    //CognitiveVR_Preferences.SceneSettings current = CognitiveVR_Preferences.FindCurrentScene();
                    CognitiveVR_SceneExportWindow.UploadAllDynamicObjects(true);
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
                            CognitiveVR_SceneExportWindow.UploadDecimatedScene(current, completeSceneUpload);
                        }
                    }
                    else
                    {
                        //new version
                        if (EditorUtility.DisplayDialog("Upload New Version","Upload a new version of this existing scene? Will archive previous version","Ok","Cancel"))
                        {
                            CognitiveVR_SceneExportWindow.UploadDecimatedScene(current, completeSceneUpload);
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