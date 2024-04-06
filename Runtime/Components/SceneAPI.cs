using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.Components
{
    public class SceneAPI : AnalyticsComponentBase
    {
        OVRSceneManager sceneManager;
        private void Start()
        {
            sceneManager = FindObjectOfType<OVRSceneManager>();
            sceneManager.SceneModelLoadedSuccessfully += SetRoomDimensionsAsSessionProperty;
        }

        private void SetRoomDimensionsAsSessionProperty()
        {   
            new CustomEvent("LOADED").Send();
            OVRSceneRoom room = FindObjectOfType<OVRSceneRoom>();
            if (room != null)
            {
                OVRScenePlane floor = room.Floor;
                Cognitive3D_Manager.SetSessionProperty("c3d.meta.room.width", floor.Width);
                Cognitive3D_Manager.SetSessionProperty("c3d.meta.room.height", floor.Height);
                Cognitive3D_Manager.SetSessionProperty("c3d.meta.room.dimensions", $"{floor.Width} x {floor.Height}");
            }
            else
            {
                new CustomEvent("OVR Scene Room is null").Send();
            }
        }
    }
}

