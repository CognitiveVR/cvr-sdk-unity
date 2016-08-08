using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace CognitiveVR
{
    public class CognitiveVR_EditorPrefs
    {
        // Have we loaded the prefs yet
        //private static bool prefsLoaded = false;

        // other tracking options
        //private static bool trackTeleportDistance = true;
        //private static float objectSendInterval = 10;


        //static bool showDevice = true;
        //static bool showOptions = true;
        //static bool showSceneExplorer;


        [PreferenceItem("CognitiveVR")]
        public static void CustomPreferencesGUI()
        {
            CognitiveVR_Preferences prefs = GetPreferences();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Version: " + Core.SDK_Version);

            GUI.color = Color.blue;
            if (GUILayout.Button("Documentation", EditorStyles.whiteLabel))
                Application.OpenURL("https://github.com/CognitiveVR/cvr-sdk-unity/wiki");
            GUI.color = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent("Customer ID", "The identifier for your company and product on the CognitiveVR Dashboard"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            prefs.CustomerID = EditorGUILayout.TextField(prefs.CustomerID);

            GUILayout.Space(10);


            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(prefs);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("Open Component Setup"))
            {
                CognitiveVR_ComponentSetup.Init();
            }

            GUILayout.Space(10);
            /*            prefs.ScreenResolution = EditorGUILayout.Toggle(new GUIContent("Record Screen Resolution", "Record the screen size in pixels"), prefs.ScreenResolution);
            #if CVR_STEAMVR
                        prefs.ChaperoneRoomSize = EditorGUILayout.Toggle(new GUIContent("Record SteamVR Room Size", "Record the room size in meters"), prefs.ChaperoneRoomSize);
            #else
                        EditorGUI.BeginDisabledGroup(true);
                        prefs.ChaperoneRoomSize = EditorGUILayout.Toggle(new GUIContent("Record SteamVR Room Size", "REQUIRES STEAMVR\nRecord the room size in meters"), prefs.ChaperoneRoomSize);
                        EditorGUI.EndDisabledGroup();
            #endif*/

            prefs.SnapshotInterval = EditorGUILayout.FloatField(new GUIContent("Interval for Player Snapshots", "Delay interval for:\nHMD Height\nHMD Collsion\nArm Length\nController Collision"), prefs.SnapshotInterval);
            prefs.LowFramerateThreshold = EditorGUILayout.IntField(new GUIContent("Low Framerate Threshold", "Falling below and rising above this threshold will send events"), prefs.LowFramerateThreshold);
            prefs.CollisionLayerMask = LayerMaskField(new GUIContent("Collision Layer", "LayerMask for HMD and Controller Collision events"), prefs.CollisionLayerMask);
            prefs.GazeObjectSendInterval = EditorGUILayout.FloatField(new GUIContent("Gaze Object Send Interval", "How many seconds of gaze data are batched together when reporting CognitiveVR_GazeObject look durations"), prefs.GazeObjectSendInterval);

            prefs.TrackArmLengthSamples = EditorGUILayout.IntField(new GUIContent("Arm Length Samples", "Number of samples taken. The max is assumed to be maximum arm length"), prefs.TrackArmLengthSamples);
            prefs.TrackHMDHeightSamples = EditorGUILayout.IntField(new GUIContent("HMD Height Samples", "Number of samples taken. The average is assumed to be the player's eye height"), prefs.TrackHMDHeightSamples);


            /*if ( FoldoutButton("SceneExplorer",showSceneExplorer)) { showSceneExplorer = !showSceneExplorer; }
            if (showSceneExplorer)
            {
                EditorGUILayout.HelpBox("SceneExplorer exporting is only available to CognitiveVR Beta customers", MessageType.Warning);
                EditorGUI.BeginDisabledGroup(true);
                prefs.ExportStaticOnly = EditorGUILayout.Toggle(new GUIContent("Export Static Geometry Only", "Only export meshes marked as static. Dynamic objects (such as vehicles, doors, etc) will not be exported"), prefs.ExportStaticOnly);
                prefs.MinExportGeoSize = EditorGUILayout.FloatField(new GUIContent("Min Export Geo Size", "Ignore exporting meshes that are below this size(pebbles, grass,etc)"), prefs.MinExportGeoSize);
                prefs.AppendFileName = EditorGUILayout.TextField(new GUIContent("Append to File Name", "Append the exported level with this text"), prefs.AppendFileName);
                prefs.MinFaceCount = EditorGUILayout.IntField(new GUIContent("Min Face Count", "Ignore decimating objects with fewer faces than this value"), prefs.MinFaceCount);
                prefs.MaxFaceCount = EditorGUILayout.IntField(new GUIContent("Max Face Count", "Objects with this many faces will be decimated to 10% of their original face count"), prefs.MaxFaceCount);
                EditorGUI.EndDisabledGroup();

                if (prefs.MinFaceCount < 0) { prefs.MinFaceCount = 0; }
                if (prefs.MaxFaceCount < 1) { prefs.MaxFaceCount = 1; }
                if (prefs.MinFaceCount > prefs.MaxFaceCount) { prefs.MinFaceCount = prefs.MaxFaceCount; }
            }*/

            if (GUI.changed)
            {
                EditorUtility.SetDirty(prefs);
                
            }
        }

        public static bool FoldoutButton(string title, bool showing)
        {
            string fullTitle = showing ? "Hide ":"Show " ;
            fullTitle += title + " Options";
            GUILayout.Space(4);
            return GUILayout.Button(fullTitle, EditorStyles.toolbarButton);
        }

        public static CognitiveVR_Preferences GetPreferences()
        {
            CognitiveVR_Preferences asset = AssetDatabase.LoadAssetAtPath<CognitiveVR_Preferences>("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CognitiveVR_Preferences>();
                AssetDatabase.CreateAsset(asset, "Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
            }
            return asset;
        }

        static List<int> layerNumbers = new List<int>();

        static LayerMask LayerMaskField(GUIContent content, LayerMask layerMask)
        {
            var layers = UnityEditorInternal.InternalEditorUtility.layers;

            layerNumbers.Clear();

            for (int i = 0; i < layers.Length; i++)
                layerNumbers.Add(LayerMask.NameToLayer(layers[i]));

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }

            maskWithoutEmpty = UnityEditor.EditorGUILayout.MaskField(content, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;

            return layerMask;
        }
    }
}