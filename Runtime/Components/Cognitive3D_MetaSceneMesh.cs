using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Cognitive3D_MetaSceneMesh")]
    public class Cognitive3D_MetaSceneMesh : AnalyticsComponentBase
    {
#if C3D_OCULUS
        OVRSceneManager sceneManager;
        private void Start()
        {
            sceneManager = FindObjectOfType<OVRSceneManager>();
            if (sceneManager != null )
            {
                sceneManager.SceneModelLoadedSuccessfully += SetRoomDimensionsAsSessionProperty;
            }
        }

        private void SetRoomDimensionsAsSessionProperty()
        {
            OVRSceneRoom room = FindObjectOfType<OVRSceneRoom>();
            if (room != null)
            {
                OVRScenePlane floor = room.Floor;
                if (floor != null)
                {
                    Cognitive3D_Manager.SetSessionProperty("c3d.physicalRoom.width", floor.Width);
                    Cognitive3D_Manager.SetSessionProperty("c3d.physicalRoom.height", floor.Height);
                    Cognitive3D_Manager.SetSessionProperty("c3d.physicalRoom.area", floor.Width * floor.Height);
                    Cognitive3D_Manager.SetSessionProperty("c3d.physicalRoom.dimensions", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} x {1:0.0}", floor.Width, floor.Height));
                }
            }
        }

        /// <summary>
        /// Component description for the inspector
        /// </summary>
        public override string GetDescription()
        {
            return "Set a property for the user's physical room size";
        }

        /// <summary>
        /// Warning for incompatible platform to display on inspector
        /// </summary>
        public override bool GetWarning()
        {
            return false;
        }

#else // not C3D_OCULUS

        /// <summary>
        /// Component description for the inspector
        /// </summary>
        public override string GetDescription()
        {
            return "Scene API can only be accessed when using the Oculus Platform";
        }

        /// <summary>
        /// Warning for incompatible platform to display on inspector
        /// </summary>
        public override bool GetWarning()
        {
            return true;
        }

#endif

    }
}

