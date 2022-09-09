using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    [System.Serializable]
    public struct SceneInfo
    {
        public string DisplayName;
        public string ScenePath;
        public Sprite Icon;
    }

    //for buttons that need to display information about a selected scene + hold data for where the scene is located
    public interface ISceneInfoHolder
    {
        void ApplySceneInfo(SceneInfo info);
        void SetSelectCallback(System.Action<SceneInfo> callback);
    }
}