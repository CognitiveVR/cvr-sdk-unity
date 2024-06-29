using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if COGNITIVE3D_INCLUDE_META_XR_UTILITY
using Meta.XR.MRUtilityKit;
#endif

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Cognitive3D_MetaSceneMesh")]
    public class Cognitive3D_MetaSceneMesh : AnalyticsComponentBase
    {
#if C3D_OCULUS

#if !COGNITIVE3D_INCLUDE_META_CORE_65_OR_NEWER
        OVRSceneManager sceneManager;

        private void Start()
        {
            sceneManager = FindObjectOfType<OVRSceneManager>();
            if (sceneManager != null )
            {
                sceneManager.SceneModelLoadedSuccessfully += SetRoomDimensionsAsSessionProperty;
                Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
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

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            sceneManager.SceneModelLoadedSuccessfully -= SetRoomDimensionsAsSessionProperty;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        private void OnDestroy()
        {
            if (sceneManager != null)
            {
                sceneManager.SceneModelLoadedSuccessfully -= SetRoomDimensionsAsSessionProperty;
            }
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

#elif COGNITIVE3D_INCLUDE_META_XR_UTILITY
        MRUK mixedRealityUtility;
        private void Start()
        {
            mixedRealityUtility = FindObjectOfType<MRUK>();
            if (mixedRealityUtility != null )
            {
                mixedRealityUtility.RegisterSceneLoadedCallback(MrukLoaded);
                Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            }
        }

        public void MrukLoaded()
        {
            float width =  mixedRealityUtility.GetCurrentRoom().FloorAnchor.PlaneRect.Value.width;
            float height = mixedRealityUtility.GetCurrentRoom().FloorAnchor.PlaneRect.Value.height;
            Cognitive3D_Manager.SetSessionProperty("c3d.physicalRoom.width", width);
            Cognitive3D_Manager.SetSessionProperty("c3d.physicalRoom.height", height);
            Cognitive3D_Manager.SetSessionProperty("c3d.physicalRoom.area", width * height);
            Cognitive3D_Manager.SetSessionProperty("c3d.physicalRoom.dimensions", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} x {1:0.0}", width, height));
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            mixedRealityUtility.SceneLoadedEvent.RemoveListener(MrukLoaded);
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        private void OnDestroy()
        {
            mixedRealityUtility.SceneLoadedEvent.RemoveListener(MrukLoaded);
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

#endif

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

