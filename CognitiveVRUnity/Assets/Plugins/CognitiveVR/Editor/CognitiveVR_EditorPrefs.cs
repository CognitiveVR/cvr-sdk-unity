using UnityEngine;
using UnityEditor;
using System.IO;

namespace CognitiveVR
{
    public class CognitiveVR_EditorPrefs
    {
        // Have we loaded the prefs yet
        private static bool prefsLoaded = false;

        // other tracking options
        private static bool trackTeleportDistance = true;
        private static float objectSendInterval = 10;


        [PreferenceItem("cognitiveVR")]
        private static void CustomPreferencesGUI()
        {
            if (!prefsLoaded)
            {
                CognitiveVR_Preferences prefs = GetPreferences();

                //teleport
                trackTeleportDistance = prefs.trackTeleportDistance;

                //gaze object
                objectSendInterval = prefs.objectSendInterval;

                prefsLoaded = true;
            }

            EditorGUILayout.LabelField("Version: " + Core.SDK_Version);

            GUI.color = Color.blue;
            if (GUILayout.Button("Documentation", EditorStyles.whiteLabel))
                Application.OpenURL("https://github.com/CognitiveVR/cvr-sdk-unity/wiki");
            GUI.color = Color.white;

            GUILayout.Space(20);

            trackTeleportDistance = EditorGUILayout.Toggle(new GUIContent("Track Teleport Distance", "Track the distance of a player's teleport. Requires the CognitiveVR_TeleportTracker"), trackTeleportDistance);
            objectSendInterval = EditorGUILayout.FloatField(new GUIContent("Gaze Object Send Interval", "How many seconds of gaze data are batched together when reporting CognitiveVR_GazeObject look durations"), objectSendInterval);

            if (GUI.changed)
            {
                CognitiveVR_Preferences prefs = GetPreferences();

                //teleport
                prefs.trackTeleportDistance = trackTeleportDistance;
                
                //gaze object
                prefs.objectSendInterval = objectSendInterval;
                AssetDatabase.SaveAssets();
            }
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
    }
}