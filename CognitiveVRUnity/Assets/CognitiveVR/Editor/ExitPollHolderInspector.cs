using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
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
                if (p.BoolPanelOverride == null)
                {
                    boolPanelPrefab = ExitPoll.ExitPollTrueFalse;
                    
                }
                else
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

            if (p.ExitpollSpawnType == ExitPoll.SpawnType.World)
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

        bool displayPrefabPointerWarning = false;
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

            if (string.IsNullOrEmpty(p.Hook))
            {
                EditorGUI.indentLevel++;
                p.Hook = EditorGUILayout.TextField("Question Set Hook", p.Hook);
                var rect = GUILayoutUtility.GetLastRect();
                rect.width = 20;
                GUI.Label(rect, new GUIContent(EditorCore.Alert, "Hook should not be empty!"));
                EditorGUI.indentLevel--;
            }
            else
            {
                p.Hook = EditorGUILayout.TextField("Question Set Hook", p.Hook);
            }

            t.ActivateOnEnable = EditorGUILayout.Toggle("Activate on Enable", t.ActivateOnEnable);

            //EditorGUILayout.HelpBox("Customize ExitPoll parameters\nCall public 'Activate' function to create exitpoll", MessageType.Info);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Controller Tracking", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            p.PointerType = (ExitPoll.PointerType)EditorGUILayout.EnumPopup("Exit Poll Pointer Type", p.PointerType);
            if (p.PointerType == ExitPoll.PointerType.CustomPointer)
            {
                p.PointerOverride = (GameObject)EditorGUILayout.ObjectField("Pointer Prefab Override", p.PointerOverride, typeof(GameObject), true);
            }
            else if (p.PointerType == ExitPoll.PointerType.SceneObject)
            {
                p.PointerOverride = (GameObject)EditorGUILayout.ObjectField("Pointer Instance", p.PointerOverride, typeof(GameObject), true);

                if (p.PointerOverride != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(p.PointerOverride)))
                {
                    displayPrefabPointerWarning = true;
                }
                else if (p.PointerOverride != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(p.PointerOverride)))
                {
                    displayPrefabPointerWarning = false;
                }

                if (displayPrefabPointerWarning)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    rect.width = 20;
                    GUI.Label(rect, new GUIContent(EditorCore.Alert, "This should reference to a scene asset!\nSelect Custom Pointer if you want to spawn a prefab"));
                    p.PointerOverride = null;
                }
            }

            p.PointerParent = (ExitPoll.PointerSource)EditorGUILayout.EnumPopup("Exit Poll Pointer Parent", p.PointerParent);
            if (p.PointerParent == ExitPoll.PointerSource.Other)
            {
                p.PointerParentOverride = (Transform)EditorGUILayout.ObjectField("Exit Poll Pointer Parent Override", p.PointerParentOverride, typeof(Transform), true);
            }

            EditorGUILayout.HelpBox(GetPointerDescription(p), MessageType.Info);
            EditorGUI.indentLevel--;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Tracking Space", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            p.ExitpollSpawnType = (ExitPoll.SpawnType)EditorGUILayout.EnumPopup(p.ExitpollSpawnType);
            if (p.ExitpollSpawnType == ExitPoll.SpawnType.World)
            {
                GUILayout.BeginHorizontal();
                p.UseAttachTransform = EditorGUILayout.Toggle(new GUIContent("Use Attach Transform","Attach ExitPoll Panels to this transform in your scene"), p.UseAttachTransform);
                if (p.UseAttachTransform)
                    p.AttachTransform = (Transform)EditorGUILayout.ObjectField(p.AttachTransform, typeof(Transform), true);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                p.UseTimeout = EditorGUILayout.Toggle(new GUIContent("Automatic Timeout", "Automatically skip this question if this timer expires"), p.UseTimeout);
                if (p.UseTimeout)
                {
                    p.Timeout = EditorGUILayout.FloatField(p.Timeout);
                    p.Timeout = Mathf.Max(2, p.Timeout);
                }
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
                GUILayout.BeginHorizontal();
                p.UseTimeout = EditorGUILayout.Toggle(new GUIContent("Automatic Timeout","Automatically skip this question if this timer expires"), p.UseTimeout);
                if (p.UseTimeout)
                {
                    p.Timeout = EditorGUILayout.FloatField(p.Timeout);
                    p.Timeout = Mathf.Max(2, p.Timeout);
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;


            GUILayout.Space(10);
            EditorGUILayout.LabelField("Panel Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            p.BoolPanelOverride = (GameObject)EditorGUILayout.ObjectField("Bool Panel Override", p.BoolPanelOverride, typeof(GameObject), true);
            p.HappyPanelOverride = (GameObject)EditorGUILayout.ObjectField("Happy Panel Override", p.HappyPanelOverride, typeof(GameObject), true);
            p.ThumbsPanelOverride = (GameObject)EditorGUILayout.ObjectField("Thumbs Panel Override", p.ThumbsPanelOverride, typeof(GameObject), true);
            p.MultiplePanelOverride = (GameObject)EditorGUILayout.ObjectField("Multiple Panel Override", p.MultiplePanelOverride, typeof(GameObject), true);
            p.ScalePanelOverride = (GameObject)EditorGUILayout.ObjectField("Scale Panel Override", p.ScalePanelOverride, typeof(GameObject), true);
            p.VoicePanelOverride = (GameObject)EditorGUILayout.ObjectField("Voice Panel Override", p.VoicePanelOverride, typeof(GameObject), true);
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
            }
        }

        private string GetPointerDescription(ExitPollParameters parameters)
        {
            string thingToSpawn = "";
            if (parameters.PointerType == ExitPoll.PointerType.ControllerPointer)
            {
                thingToSpawn = "Spawn ExitPollControllerPointer";
            }
            else if(parameters.PointerType == ExitPoll.PointerType.HMDPointer)
            {
                thingToSpawn = "Spawn ExitPollHMDPointer";
            }
            else if (parameters.PointerType == ExitPoll.PointerType.CustomPointer)
            {
                if (parameters.PointerOverride != null)
                    thingToSpawn = "Spawn " + parameters.PointerOverride.name;
                else
                    thingToSpawn = "Spawn Nothing";
            }
            else if (parameters.PointerType == ExitPoll.PointerType.SceneObject)
            {
                if (parameters.PointerOverride != null)
                    thingToSpawn = "Select " + parameters.PointerOverride.name + " from scene";
                else
                    thingToSpawn = "Select Nothing from scene";
            }

            string howToAttach = "";
            if (parameters.PointerParent == ExitPoll.PointerSource.HMD)
            {
                howToAttach = " and attach to HMD";
            }
            if (parameters.PointerParent == ExitPoll.PointerSource.LeftHand)
            {
                howToAttach = " and attach to Left Controller";
            }
            if (parameters.PointerParent == ExitPoll.PointerSource.RightHand)
            {
                howToAttach = " and attach to Right Controller";
            }
            if (parameters.PointerParent == ExitPoll.PointerSource.Other)
            {
                if (parameters.PointerParentOverride != null)
                    howToAttach = " and Attach to " + parameters.PointerParentOverride.name + " in scene";
                else
                    howToAttach = " and Attach to Nothing in scene";
            }

            string result = "";
            if (parameters.PointerType == ExitPoll.PointerType.SceneObject)
            {
                if (parameters.PointerParent == ExitPoll.PointerSource.Other && parameters.PointerParentOverride == null)
                {
                    result = "\nPointer parenting will not change after ExitPoll closes";
                }
                else
                {
                    result = "\nPointer will be un-attached after ExitPoll closes";
                }
            }
            else
            {
                result = "\nPointer will be destroyed after ExitPoll closes";
            }
            
            return thingToSpawn + howToAttach + result;
        }
    }
}