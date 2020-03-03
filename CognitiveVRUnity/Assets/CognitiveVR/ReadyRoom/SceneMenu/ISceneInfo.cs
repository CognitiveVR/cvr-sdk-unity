﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
