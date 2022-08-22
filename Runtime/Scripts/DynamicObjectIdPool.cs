using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this holds a bunch of ids for dynamic objects. these are uploaded to the dashboard
//this pool makes sure ids are consistent across sessions, so data can be aggregated correctly
//dynamic objects get a new id when spawned, if they have a reference to this pool
//if getid runs out of free ids, return runtime id (1,2,3, etc). unique

namespace CognitiveVR
{
    [CreateAssetMenu(fileName = "New Dynamic Object Id Pool", menuName = "Cognitive3D/Dynamic Object Id Pool")]
    public class DynamicObjectIdPool : ScriptableObject
    {
        //CONSIDER use prefab reference instead of prefab name
        public string PrefabName; //friendly prefab name to be displayed on dashboard. will be appended 1,2,3,etc
        public string MeshName; //mesh name
        public string[] Ids;

        private int freeId = 0;

        private void OnEnable()
        {
            freeId = 0;
        }

        public string GetId()
        {
            if (freeId >= Ids.Length) //beyond list size, generate unique ids
            {
                return CognitiveVR.DynamicManager.GetUniqueObjectId(MeshName);
            }

            string newId = Ids[freeId];
            freeId++;
            return newId;
        }

        //IMPROVEMENT return id to pool?
    }
}