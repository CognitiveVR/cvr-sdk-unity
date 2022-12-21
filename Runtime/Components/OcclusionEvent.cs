using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// sends events when a controller becomes invalid
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Occlusion Event")]
    public class OcclusionEvent : AnalyticsComponentBase
    {
        protected override void OnSessionBegin()
        {
            GameplayReferences.OnControllerValidityChange += GameplayReferences_OnControllerValidityChange;
            Cognitive3D_Manager.OnPostSessionEnd += Cognitive3D_Manager_OnPostSessionEnd;
        }

        private void Cognitive3D_Manager_OnPostSessionEnd()
        {
            GameplayReferences.OnControllerValidityChange -= GameplayReferences_OnControllerValidityChange;
            Cognitive3D_Manager.OnPostSessionEnd -= Cognitive3D_Manager_OnPostSessionEnd;
        }

        private void GameplayReferences_OnControllerValidityChange(UnityEngine.XR.InputDevice device, UnityEngine.XR.XRNode node, bool isValid)
        {
            if (node == UnityEngine.XR.XRNode.LeftHand)
            {
                new CustomEvent("cvr.tracking").SetProperty("device", "left").SetProperty("Is Valid", isValid).Send();
            }
            if (node == UnityEngine.XR.XRNode.RightHand)
            {
                new CustomEvent("cvr.tracking").SetProperty("device", "right").SetProperty("Is Valid", isValid).Send();
            }
        }

        public override bool GetWarning()
        {
            if (GameplayReferences.SDKSupportsControllers)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override string GetDescription()
        {
            return "Sends an event when a controller device becomes invalid (from tracking issue or disconnection)";
        }
    }
}