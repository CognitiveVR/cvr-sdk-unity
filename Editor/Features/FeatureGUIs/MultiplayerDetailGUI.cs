using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class MultiplayerDetailGUI : IFeatureDetailGUI
    {
        private class FrameworkOption
        {
            public MultiplayerDefines.MultiplayerFramework Framework;
            public string DisplayName;
            public System.Func<bool> HasComponent;
            public System.Action AddComponents;
            public System.Action RemoveComponents;
        }

        private static readonly List<FrameworkOption> options = new List<FrameworkOption>
        {
            new FrameworkOption
            {
                Framework = MultiplayerDefines.MultiplayerFramework.PhotonFusion,
                DisplayName = "Photon Fusion",
#if FUSION2
                HasComponent = () => FeatureLibrary.TryGetComponent<Cognitive3D.Components.PhotonFusionMultiplayer>(),
                AddComponents = () => FeatureLibrary.AddComponent<Cognitive3D.Components.PhotonFusionMultiplayer>(),
                RemoveComponents = () => FeatureLibrary.RemoveComponent<Cognitive3D.Components.PhotonFusionMultiplayer>()
#else
                HasComponent = () => false,
                AddComponents = () => { },
                RemoveComponents = () => { },
#endif
            },
            new FrameworkOption
            {
                Framework = MultiplayerDefines.MultiplayerFramework.PhotonPun2,
                DisplayName = "Photon PUN 2",
#if PHOTON_UNITY_NETWORKING
                HasComponent = () => FeatureLibrary.TryGetComponent<Cognitive3D.Components.PhotonPunMultiplayer>(),
                AddComponents = () => FeatureLibrary.AddComponent<Cognitive3D.Components.PhotonPunMultiplayer>(),
                RemoveComponents = () => FeatureLibrary.RemoveComponent<Cognitive3D.Components.PhotonPunMultiplayer>()
#else
                HasComponent = () => false,
                AddComponents = () => { },
                RemoveComponents = () => { },
#endif
            },
            new FrameworkOption
            {
                Framework = MultiplayerDefines.MultiplayerFramework.Netcode,
                DisplayName = "Netcode",
#if COGNITIVE3D_INCLUDE_UNITY_NETCODE
                HasComponent = () => FeatureLibrary.TryGetComponent<Cognitive3D.Components.NetcodeMultiplayer>(),
                AddComponents = () => 
                {
                    FeatureLibrary.AddComponent<Cognitive3D.Components.NetcodeMultiplayer>();
                    FeatureLibrary.AddComponent<Unity.Netcode.NetworkObject>();
                },
                RemoveComponents = () => 
                {
                    FeatureLibrary.RemoveComponent<Cognitive3D.Components.NetcodeMultiplayer>();
                    FeatureLibrary.RemoveComponent<Unity.Netcode.NetworkObject>();
                }
#else
                HasComponent = () => false,
                AddComponents = () => { },
                RemoveComponents = () => { },
#endif
            },
            new FrameworkOption
            {
                Framework = MultiplayerDefines.MultiplayerFramework.Normcore,
                DisplayName = "Normcore",
#if COGNITIVE3D_INCLUDE_NORMCORE
                HasComponent = () => FeatureLibrary.TryGetComponent<Cognitive3D.Components.NormcoreMultiplayer>(),
                AddComponents = () => 
                {
                    FeatureLibrary.AddComponent<Cognitive3D.Components.NormcoreMultiplayer>();

                    string localPath = "Assets/Resources/Cognitive3D_NormcoreSync.prefab";
                    string folderPath = System.IO.Path.GetDirectoryName(localPath);
                    if (!System.IO.Directory.Exists(folderPath)) System.IO.Directory.CreateDirectory(folderPath);

                    if (!Resources.Load<GameObject>("Cognitive3D_NormcoreSync"))
                    {
                        GameObject temp = new GameObject("Cognitive3D_NormcoreSync");
                        PrefabUtility.SaveAsPrefabAsset(temp, localPath);
                        UnityEngine.Object.DestroyImmediate(temp);
                    }

                    GameObject prefab = Resources.Load<GameObject>("Cognitive3D_NormcoreSync");
                    if (!prefab.GetComponent<Cognitive3D.NormcoreSync>())
                    {
                        prefab.AddComponent<Cognitive3D.NormcoreSync>();
                        EditorUtility.SetDirty(prefab);
                        AssetDatabase.SaveAssets();
                    }
                },
                RemoveComponents = () => FeatureLibrary.RemoveComponent<Cognitive3D.Components.NormcoreMultiplayer>()
#else
                HasComponent = () => false,
                AddComponents = () => { },
                RemoveComponents = () => { },
#endif
             }
        };   

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

            bool anyPackageInstalled = false;
            foreach (var option in options)
            {
                bool packageInstalled = MultiplayerDefines.IsPackageInstalled(option.Framework);
                if (packageInstalled) anyPackageInstalled = true;

                GUI.enabled = packageInstalled;

                bool hasComponent = option.HasComponent();
                string buttonLabel = hasComponent ? $"Remove {option.DisplayName} Support" : $"Add {option.DisplayName} Support";
                if (GUILayout.Button(buttonLabel, GUILayout.Height(30)))
                {
                    if (hasComponent) 
                    {
                        Disable(option);
                    }
                    else
                    {
                        Enable(option);
                    }
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = true;
                EditorGUILayout.Space(5);
            }

            // If none detected, show warning
            if (!anyPackageInstalled)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("No multiplayer frameworks detected. Install a package to enable support.", MessageType.Error);
            }
        }

        private static void Enable(FrameworkOption option)
        {
            foreach (var other in options)
            {
                if (other != option) other.RemoveComponents();
            }
            option.AddComponents();
            MultiplayerDefines.SetActiveFramework(option.Framework);
        }

        private static void Disable(FrameworkOption option)
        {
            option.RemoveComponents();
            MultiplayerDefines.SetActiveFramework(MultiplayerDefines.MultiplayerFramework.None);
        }
    }

    internal static class MultiplayerDefines
    {
        internal enum MultiplayerFramework
        {
            None,
            PhotonFusion,
            PhotonPun2,
            Netcode,
            Normcore
        }

        internal static readonly Dictionary<MultiplayerFramework, string> Symbols = new Dictionary<MultiplayerFramework, string>
        {
                { MultiplayerFramework.PhotonFusion, "C3D_PHOTON_FUSION" },
                { MultiplayerFramework.PhotonPun2,   "C3D_PHOTON_PUN_2" },
                { MultiplayerFramework.Netcode,      "C3D_UNITY_NETCODE" },
                { MultiplayerFramework.Normcore,     "C3D_NORMCORE" },
        };

        internal static void SetActiveFramework(MultiplayerFramework framework)
        {
            foreach (var kvp in Symbols)
            {
                if (kvp.Key != framework)
                {
                    EditorCore.RemoveDefine(kvp.Value);
                }
            }
            if (framework != MultiplayerFramework.None)
            {
                EditorCore.AddDefine(Symbols[framework]);
            }
        }

        internal static MultiplayerFramework GetActiveFramework()
        {
            var defines = EditorCore.GetPlayerDefines(EditorCore.GetActiveNamedBuildTarget());
            foreach (var kvp in Symbols)
            {
                if (defines.Contains(kvp.Value))
                {
                    return kvp.Key;
                }
            }
            return MultiplayerFramework.None;
        }

        internal static bool IsPackageInstalled(MultiplayerFramework framework)
        {
            switch (framework)
            {
                case MultiplayerFramework.PhotonFusion:
#if FUSION2
                    return true;
#else
                    return false;
#endif
                case MultiplayerFramework.PhotonPun2:
#if PHOTON_UNITY_NETWORKING
                    return true;
#else
                    return false;
#endif
                case MultiplayerFramework.Netcode:
#if COGNITIVE3D_INCLUDE_UNITY_NETCODE
                    return true;
#else
                    return false;
#endif
                case MultiplayerFramework.Normcore:
#if COGNITIVE3D_INCLUDE_NORMCORE
                    return true;
#else
                    return false;
#endif
                default:
                    return false;
            }
        }
    }
}
