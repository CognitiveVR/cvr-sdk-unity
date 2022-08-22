using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    [CustomEditor(typeof(DynamicObjectIdPool))]
    [CanEditMultipleObjects]
    public class DynamicObjectIDPoolInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var idpools = targets;
            var idpool = (DynamicObjectIdPool)target;

            if (!EditorCore.HasDynamicExportFiles(idpool.MeshName))
            {
                EditorGUILayout.HelpBox("Mesh: " + idpool.MeshName + " not found!\nAdd your prefab to the scene and press 'Export Mesh' in the 'advanced' inspector foldout", MessageType.Warning);
            }

            bool holdingShift = Event.current.shift;
            string AddIdString = holdingShift ? "Add Id (x5)" : "Add Id";

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(AddIdString,GUILayout.Width(100)))
            {
                foreach(Object opool in idpools)
                {
                    var pool = (DynamicObjectIdPool)opool;

                    if (pool.Ids == null)
                        pool.Ids = new string[0];

                    if (holdingShift)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            ArrayUtility.Add(ref pool.Ids, System.Guid.NewGuid().ToString());
                        }
                            
                    }
                    else
                    {
                        ArrayUtility.Add(ref pool.Ids, System.Guid.NewGuid().ToString());
                    }
                }
            }

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(idpool.PrefabName) || string.IsNullOrEmpty(idpool.MeshName) || idpool.Ids == null || idpool.Ids.Length == 0);
            if (GUILayout.Button("Upload Ids"))
            {
                EditorCore.RefreshSceneVersion(delegate ()
                {
                    ManageDynamicObjects.AggregationManifest manifest = new CognitiveVR.ManageDynamicObjects.AggregationManifest();
                    foreach (Object opool in idpools)
                    {
                        var pool = (DynamicObjectIdPool)opool;
                        foreach (var id in pool.Ids)
                        {
                            //TODO pools need a reference to a gameobject to aggregate data correctly
                            manifest.objects.Add(new ManageDynamicObjects.AggregationManifest.AggregationManifestEntry(pool.PrefabName, pool.MeshName, id,
                                new float[3] { 1, 1, 1 },
                                new float[3] { 0, 0, 0 },
                                new float[4] { 0,0,0,1 }));
                        }
                    }

                    ManageDynamicObjects.UploadManifest(manifest, null);
                });
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            Repaint();
        }
    }
}