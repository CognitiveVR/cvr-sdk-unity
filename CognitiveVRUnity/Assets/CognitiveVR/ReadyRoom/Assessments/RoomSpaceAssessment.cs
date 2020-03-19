using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if CVR_STEAMVR
using Valve.VR;
#endif

//complete when the user moves to each destination in the VR Room Space

namespace CognitiveVR
{
    public class RoomSpaceAssessment : AssessmentBase
    {
        public List<RoomSpaceDestination> Destinations = new List<RoomSpaceDestination>();
        int destinationsRemaining;
        public RoomBoundsDisplay RoomBoundsDisplay;

        //Returns the bounds of the Room as defined by the selected SDK

#if CVR_STEAMVR
    public Bounds CalculateBounds()
    {
        var playArea = FindObjectOfType<SteamVR_PlayArea>();

        var bounds = new Bounds();
        for (int i = 0; i < 4; i++)
        {
            bounds.Encapsulate(playArea.vertices[i]);
            bounds.Encapsulate(playArea.vertices[i] + Vector3.up * 10);
            bounds.Encapsulate(playArea.vertices[i] - Vector3.up * 10);
        }
        //shift bounds off to play area offset
        bounds.SetMinMax(bounds.min + playArea.transform.position, bounds.max + playArea.transform.position);
        return bounds;
    }
#elif CVR_OCULUS
    public Bounds CalculateBounds()
    {
        var bounds = new Bounds();
        var dimensions = OVRPlugin.GetBoundaryDimensions(OVRPlugin.BoundaryType.PlayArea);
        bounds.extents = new Vector3(dimensions.x, dimensions.y, dimensions.z);
        return bounds;
    }
#else
        public Bounds CalculateBounds()
        {
            var bounds = new Bounds();
            bounds.extents = Vector3.one * 3;
            return bounds;
        }
#endif

        //calls CalculateBounds and SetDestinationPositions before base startup logic
        public override void BeginAssessment()
        {
            Bounds bounds = CalculateBounds();

            if (RoomBoundsDisplay != null)
                RoomBoundsDisplay.Activate(bounds);
            SetDestinationPositions(bounds);

            destinationsRemaining = 0;
            for (int i = 0; i < Destinations.Count; i++)
            {
                if (Destinations[i] != null)
                    destinationsRemaining++;
            }
            base.BeginAssessment();

            if (destinationsRemaining == 0)
            {
                Debug.LogWarning("Ready Room RoomSpaceAssessment does not contain any destinations!");
                CompleteAssessment();
            }
        }

        //moves each destination to a valid position within the Room Bounds
        void SetDestinationPositions(Bounds bounds)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Destinations.Count <= i || Destinations[i] == null) { break; }
                Vector3 offset = (bounds.extents * 3f / 4f);
                offset.y = 0;
                if (i == 0) { }
                else if (i == 1) { offset.x *= -1; }
                else if (i == 2) { offset.z *= -1; }
                else if (i == 3) { offset.x *= -1; offset.z *= -1; }
                Destinations[i].transform.position = bounds.center + offset;
            }
        }

        //called from RoomSpaceDestination component
        public void ActivateDestination()
        {
            destinationsRemaining--;
            if (destinationsRemaining <= 0)
            {
                CompleteAssessment();
            }
        }
    }
}