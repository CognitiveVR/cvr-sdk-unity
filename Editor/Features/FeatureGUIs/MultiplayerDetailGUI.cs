using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class MultiplayerDetailGUI : IFeatureDetailGUI
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Multiplayer", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Multiplayer documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/multiplayer/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "This feature adds extra analytics based on the multiplayer packages used in the project. It includes custom events for player connections and disconnections, as well as sensor data like Round-Trip Time (RTT/ping), and more.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            GUILayout.Label("Add to Cognitive3D_Manager prefab", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the necessary components to the Cognitive3D_Manager prefab.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

#region Photon Fusion
            bool photonFusionDetected = false;
    # if FUSION2
            photonFusionDetected = true;
    #endif
            GUI.enabled = photonFusionDetected;
            var photonFusionLabel = "Add Photon Fusion Support";
    # if FUSION2
            photonFusionLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.PhotonFusionMultiplayer>() ? "Remove Photon Fusion Support" : "Add Photon Fusion Support";
    #endif
            if (GUILayout.Button(photonFusionLabel, GUILayout.Height(30)))
            {
    # if FUSION2
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.PhotonFusionMultiplayer>();
    #endif
            }
            GUI.enabled = true;
#endregion

            EditorGUILayout.Space(5);

#region Photon PUN 2
            bool photonPunDetected = false;
    #if PHOTON_UNITY_NETWORKING
            photonPunDetected = true;
    #endif
            GUI.enabled = photonPunDetected;
            var photonPunLabel = "Add Photon PUN 2 Support";
    #if PHOTON_UNITY_NETWORKING
            photonPunLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.PhotonPunMultiplayer>() ? "Remove Photon PUN 2 Support" : "Add Photon PUN 2 Support";
    #endif
            if (GUILayout.Button(photonPunLabel, GUILayout.Height(30)))
            {
    #if PHOTON_UNITY_NETWORKING
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.PhotonPunMultiplayer>();
                FeatureLibrary.AddOrRemoveComponent<Photon.Pun.PhotonView>();
    #endif
            }
            GUI.enabled = true;
#endregion

            EditorGUILayout.Space(5);

#region Unity Netcode for GameObjects
            bool netcodeDetected = false;
    #if COGNITIVE3D_INCLUDE_UNITY_NETCODE
            netcodeDetected = true;
    #endif
            var netcodeLabel = "Add Netcode Support";
            GUI.enabled = netcodeDetected;
    #if COGNITIVE3D_INCLUDE_UNITY_NETCODE
            netcodeLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.NetcodeMultiplayer>() ? "Remove Netcode Support" : "Add Netcode Support";
    #endif
            if (GUILayout.Button(netcodeLabel, GUILayout.Height(30)))
            {
    #if COGNITIVE3D_INCLUDE_UNITY_NETCODE
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.NetcodeMultiplayer>();
                FeatureLibrary.AddOrRemoveComponent<Unity.Netcode.NetworkObject>();
    #endif
            }
            GUI.enabled = true;
#endregion

            EditorGUILayout.Space(5);

#region Normcore
            bool normcoreDetected = false;
    #if COGNITIVE3D_INCLUDE_NORMCORE
            normcoreDetected = true;
    #endif
            GUI.enabled = normcoreDetected;
            var normcoreLabel = "Add NormCore Support";
    #if COGNITIVE3D_INCLUDE_NORMCORE
            normcoreLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.NormcoreMultiplayer>() ? "Remove NormCore Support" : "Add NormCore Support";
    #endif
            if (GUILayout.Button(normcoreLabel, GUILayout.Height(30)))
            {
    #if COGNITIVE3D_INCLUDE_NORMCORE
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.NormcoreMultiplayer>();
                var existing = Cognitive3D_Manager.Instance.gameObject.GetComponent<Cognitive3D.Components.NormcoreMultiplayer>();
                if (existing != null)
                {
                    string localPath = "Assets/Resources/Cognitive3D_NormcoreSync.prefab";
                    string folderPath = System.IO.Path.GetDirectoryName(localPath);
                    if (!System.IO.Directory.Exists(folderPath)) System.IO.Directory.CreateDirectory(folderPath);

                    if (!Resources.Load<GameObject>("Cognitive3D_NormcoreSync"))
                    {
                        GameObject temp = new GameObject("Cognitive3D_NormcoreSync");
                        PrefabUtility.SaveAsPrefabAsset(temp, localPath);
                        Object.DestroyImmediate(temp);
                    }

                    GameObject prefab = Resources.Load<GameObject>("Cognitive3D_NormcoreSync");
                    if (!prefab.GetComponent<Cognitive3D.NormcoreSync>())
                        prefab.AddComponent<Cognitive3D.NormcoreSync>();
                }
    #endif
            }
            GUI.enabled = true;
#endregion

            // If none detected, show warning
            if (!photonFusionDetected && !photonPunDetected && !netcodeDetected && !normcoreDetected)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("No multiplayer frameworks detected. Install a package to enable support.", MessageType.Error);
            }
        }
    }
}
