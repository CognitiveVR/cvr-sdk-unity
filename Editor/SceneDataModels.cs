using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    [Serializable]
    public class ScenesCollectionList
    {
        public List<SceneVersionCollection> scenes = new List<SceneVersionCollection>();
    }

    //returned from get scene version. contains info about all versions of a single scene
    [Serializable]
    public class SceneVersionCollection
    {
        public long createdAt;
        public long updatedAt;
        public string id;
        public List<SceneVersion> versions = new List<SceneVersion>();
        public int projectId;
        public string customerId;
        public string sceneName;
        public bool isPublic;
        public bool hidden;
        public string sceneCreationMethod;
        public string sdkFacingId;

        public SceneVersion GetLatestVersion()
        {
            int latest = 0;
            SceneVersion latestscene = null;
            if (versions == null) { Debug.LogError("SceneVersionCollection versions is null!"); return null; }
            for (int i = 0; i < versions.Count; i++)
            {
                if (versions[i].versionNumber > latest)
                {
                    latest = versions[i].versionNumber;
                    latestscene = versions[i];
                }
            }
            return latestscene;
        }

        public SceneVersion GetVersion(int versionnumber)
        {
            var sceneversion = versions.Find(delegate (SceneVersion obj) { return obj.versionNumber == versionnumber; });
            return sceneversion;
        }
    }
}
