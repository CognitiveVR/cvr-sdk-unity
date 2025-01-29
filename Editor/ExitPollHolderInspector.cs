using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    [CustomEditor(typeof(ExitPollHolder))]
    public class ExitPollHolderInspector : Editor
    {
        Camera _camera;
        Camera Camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = Camera.main;
                    if (_camera == null)
                    {
                        _camera = FindObjectOfType<Camera>();
                    }
                }
                return _camera;
            }
        }

        GameObject boolPanelPrefab;

        void OnSceneGUI()
        {
            ExitPollHolder t = (ExitPollHolder)target;
            ExitPollParameters p = t.Parameters;

            Vector3 panelScale = new Vector3(2, 1.2f, 0.1f);
            if (boolPanelPrefab == null)
            {
                if (p.BoolPanelOverride != null)
                {
                    boolPanelPrefab = p.BoolPanelOverride;
                }
            }

            if (boolPanelPrefab != null)
            {
                var collider = boolPanelPrefab.GetComponent<BoxCollider>();
                if (collider != null)
                    panelScale = collider.size;
            }

            if (p.ExitpollSpawnType == ExitPollManager.SpawnType.WorldSpace)
            {
                if (p.UseAttachTransform && p.AttachTransform != null)
                    Handles.DrawDottedLine(t.transform.position, p.AttachTransform.position,5);

                //need to use matrix here for rotation
                Handles.matrix = Matrix4x4.TRS(t.transform.position, t.transform.rotation, Vector3.one);                
                Handles.DrawWireCube(Vector3.zero, panelScale);
            }
            else if (Camera != null)
            {
                Vector3 pos = (Camera.transform.position + Camera.transform.forward * p.DisplayDistance);

                Handles.DrawWireCube(pos, panelScale);

                //prefered distance
                Handles.color = new Color(0.5f, 1, 0.5f);
                Handles.DrawWireDisc(Camera.transform.position, Vector3.up, p.DisplayDistance);
                if (!p.LockYPosition)
                {
                    Handles.DrawWireDisc(Camera.transform.position, Vector3.forward, p.DisplayDistance);
                    Handles.DrawWireDisc(Camera.transform.position, Vector3.right, p.DisplayDistance);
                }
                
                //minimum distance
                Handles.color = new Color(1f, 0.5f, 0.5f);
                Handles.DrawWireDisc(Camera.transform.position, Vector3.up, p.MinimumDisplayDistance);
            }
        }

        int _choiceIndex;
        private bool hasRefreshedExitPollHooks;
        private bool hasInitExitPollHooks;

        public override void OnInspectorGUI()
        {
            //TODO editor properties - allow multiple selection

            ExitPollHolder t = (ExitPollHolder)target;
            ExitPollParameters p = t.Parameters;

            //display script field
            var script = serializedObject.FindProperty("m_Script");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.LabelField("Current Question Set Hook",p.Hook);

            if (!hasInitExitPollHooks)
            {
                EditorCore.RefreshExitPollHooks();
                hasInitExitPollHooks = true;
            }

            string[] displayOptions = new string[EditorCore.ExitPollHooks.Length];
            if (!string.IsNullOrEmpty(p.Hook) && _choiceIndex == 0 && displayOptions.Length > 0)
            {
                // Try once to select the correct hook
                for (int i = 0; i < EditorCore.ExitPollHooks.Length; i++)
                {
                    if (EditorCore.ExitPollHooks[i].name == p.Hook)
                    {
                        _choiceIndex = i;
                    }
                }
            }
            
            for (int i = 0; i < EditorCore.ExitPollHooks.Length; i++)
            {
                displayOptions[i] = EditorCore.ExitPollHooks[i].name;
            }

            // Drop down button
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(EditorCore.ExitPollHooks.Length == 0);
            _choiceIndex = EditorGUILayout.Popup("Question Set Hooks", _choiceIndex, displayOptions);
            EditorGUI.EndDisabledGroup();

            // Save button
            if (EditorCore.ExitPollHooks.Length > 0)
            {
                bool isSameHook = p.Hook == EditorCore.ExitPollHooks[_choiceIndex].name;

                GUI.enabled = !isSameHook;  // Disable the button if hookname is the same as selected option
                if (GUILayout.Button("Save", GUILayout.Width(40)))
                {
                    p.Hook = EditorCore.ExitPollHooks[_choiceIndex].name;
                }

                GUI.enabled = true;
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Save", GUILayout.Width(40));
                EditorGUI.EndDisabledGroup();
            }

            // GUI style that has less border so the refresh icon is more clear
            int border = 2;
            GUIStyle minimalPaddingButton = new GUIStyle("button");
            minimalPaddingButton.padding = new RectOffset(border, border, border, border);
            if (GUILayout.Button(new GUIContent(EditorCore.RefreshIcon, "Refresh Hooks"),minimalPaddingButton, GUILayout.Width(19),
                    GUILayout.Height(19)))
            {
                EditorCore.RefreshExitPollHooks();
                hasRefreshedExitPollHooks = true;
            }

            EditorGUILayout.EndHorizontal();

            if (hasRefreshedExitPollHooks && EditorCore.ExitPollHooks.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No ExitPoll hooks found for this project. Have you set them up on the dashboard yet?",
                    MessageType.Warning);
            }

            t.ActivateOnEnable = EditorGUILayout.Toggle("Activate on Enable", t.ActivateOnEnable);

            //EditorGUILayout.HelpBox("Customize ExitPoll parameters\nCall public 'Activate' function to create exitpoll", MessageType.Info);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Pointer Settings", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            p.PointerType = (ExitPollManager.PointerType)EditorGUILayout.EnumPopup("Exit Poll Pointer Type", p.PointerType);
            if (p.PointerType == ExitPollManager.PointerType.ControllersAndHands)
            {
                p.PointerActivationButton = (ExitPollManager.PointerInputButton)EditorGUILayout.EnumPopup("Pointer Input Button", p.PointerActivationButton);
            }
            EditorGUILayout.HelpBox(GetPointerDescription(p), MessageType.Info);

            if (p.PointerType == ExitPollManager.PointerType.HMD)
            {
                p.HMDPointerPrefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("HMD Pointer Prefab","Prefab responsible for managing pointer input and visualizing the pointer."), 
                    p.HMDPointerPrefab, typeof(GameObject), true
                );
            }

            if (p.PointerType == ExitPollManager.PointerType.ControllersAndHands)
            {
                p.PointerControllerPrefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Pointer Controller Prefab","Prefab responsible for managing pointer input and visualizing the pointer."), 
                    p.PointerControllerPrefab, typeof(GameObject), true
                );

                p.PointerLineWidth = EditorGUILayout.FloatField(
                    new GUIContent("Pointer Line Width Multiplier","Adjusts the overall scaling factor applied to the LineRenderer.widthCurve, determining the final width of the pointer line."), 
                    p.PointerLineWidth
                );

                p.PointerGradient = EditorGUILayout.GradientField(
                    new GUIContent("Pointer Gradient Color","Defines the gradient color of the pointer line. Customize this to set how the pointer visually transitions between colors."), 
                    p.PointerGradient
                );
            }

            EditorGUI.indentLevel--;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Tracking Space", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            p.ExitpollSpawnType = (ExitPollManager.SpawnType)EditorGUILayout.EnumPopup(p.ExitpollSpawnType);
            
            if (p.ExitpollSpawnType == ExitPollManager.SpawnType.WorldSpace)
            {
                GUILayout.BeginHorizontal();
                p.UseAttachTransform = EditorGUILayout.Toggle(new GUIContent("Use Attach Transform","Attach ExitPoll Panels to this transform in your scene"), p.UseAttachTransform);
                if (p.UseAttachTransform)
                    p.AttachTransform = (Transform)EditorGUILayout.ObjectField(p.AttachTransform, typeof(Transform), true);
                GUILayout.EndHorizontal();
            }
            else
            {
                //try to get a main camera to display this handle around. 

                LayerMask gazeMask = new LayerMask();
                gazeMask.value = p.PanelLayerMask;
                gazeMask = EditorGUILayout.MaskField(new GUIContent("Collision Layer Mask","Sets layers to collide with when trying to find a good spawn position"), UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(gazeMask), (UnityEditorInternal.InternalEditorUtility.layers));
                p.PanelLayerMask = gazeMask.value;

                p.StickWindow = EditorGUILayout.Toggle(new GUIContent("Sticky Window","Retain the offset relative to the player if they teleport"), p.StickWindow);
                p.LockYPosition = EditorGUILayout.Toggle(new GUIContent("Lock Y Position", "Lock the vertical position to match the player's height"), p.LockYPosition);
                p.DisplayDistance = EditorGUILayout.FloatField(new GUIContent("Default Display Distance","Sets the prefered distance away from the player to spawn the panel"), p.DisplayDistance);
                p.DisplayDistance = Mathf.Max(p.MinimumDisplayDistance, p.DisplayDistance);
            }
            EditorGUI.indentLevel--;


            GUILayout.Space(10);
            EditorGUILayout.LabelField("Panel Prefabs", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            p.BoolPanelOverride = (GameObject)EditorGUILayout.ObjectField("Bool Panel", p.BoolPanelOverride, typeof(GameObject), true);
            p.HappyPanelOverride = (GameObject)EditorGUILayout.ObjectField("Happy Panel", p.HappyPanelOverride, typeof(GameObject), true);
            p.ThumbsPanelOverride = (GameObject)EditorGUILayout.ObjectField("Thumbs Panel", p.ThumbsPanelOverride, typeof(GameObject), true);
            p.MultiplePanelOverride = (GameObject)EditorGUILayout.ObjectField("Multiple Panel", p.MultiplePanelOverride, typeof(GameObject), true);
            p.ScalePanelOverride = (GameObject)EditorGUILayout.ObjectField("Scale Panel", p.ScalePanelOverride, typeof(GameObject), true);
            p.VoicePanelOverride = (GameObject)EditorGUILayout.ObjectField("Voice Panel", p.VoicePanelOverride, typeof(GameObject), true);
            EditorGUI.indentLevel--;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Panel UI Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            p.PanelTextMaterial = (Material)EditorGUILayout.ObjectField("Panel Text Material", p.PanelTextMaterial, typeof(Material), true);
            p.PanelErrorTextMaterial = (Material)EditorGUILayout.ObjectField("Panel Error Text Material", p.PanelErrorTextMaterial, typeof(Material), true);
            p.PanelBackgroundMaterial = (Material)EditorGUILayout.ObjectField("Panel Background Material", p.PanelBackgroundMaterial, typeof(Material), true);
            EditorGUI.indentLevel--;

            GUILayout.Space(10);
            var onbegin = serializedObject.FindProperty("Parameters").FindPropertyRelative("OnBegin");
            EditorGUILayout.PropertyField(onbegin);
            var eventRect = GUILayoutUtility.GetLastRect();
            eventRect.height = 15;
            EditorGUI.LabelField(eventRect, new GUIContent("", "Invoked if the ExitPoll recieves a valid question set and displays a panel"));

            var onComplete = serializedObject.FindProperty("Parameters").FindPropertyRelative("OnComplete");
            var onClose = serializedObject.FindProperty("Parameters").FindPropertyRelative("OnClose");
            EditorGUILayout.PropertyField(onComplete);
            eventRect = GUILayoutUtility.GetLastRect();
            eventRect.height = 15;
            EditorGUI.LabelField(eventRect, new GUIContent("", "Invoked when the Exitpoll completes successfully"));
            EditorGUILayout.PropertyField(onClose);
            eventRect = GUILayoutUtility.GetLastRect();
            eventRect.height = 15;
            EditorGUI.LabelField(eventRect, new GUIContent("", "Always invoked when the Exitpoll closes either successfully or from some failure"));

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            if (GUI.changed)
            {
                boolPanelPrefab = null;
                EditorUtility.SetDirty(t);
            }
        }

        private string GetPointerDescription(ExitPollParameters parameters)
        {
            if (parameters.PointerType == ExitPollManager.PointerType.ControllersAndHands)
            {
                return "Users will interact with the buttons by using controllers and/or hands, if available.";
            }
            else if (parameters.PointerType == ExitPollManager.PointerType.Custom)
            {
                return "Users will interact with the buttons by using custom pointer.";
            }
            else
            {
                return "Users will interact with the buttons by focusing their gaze on them.";
            }
        }
    }
}
