using Cognitive3D;
using UnityEngine;

/// <summary>
/// This is meant to be a class that only sets a variable in Cognitive3D_Manager - it does nothing else
/// The code in Start() is here as opposed to Cognitive3D_Manager because we want it to work with objects that spawn later
/// This will be added to the TrackingSpace or equivalent GameObject so it can be identified and found at runtime
/// </summary>
/// 

[DisallowMultipleComponent]
public class RoomTrackingSpace : MonoBehaviour
{
    private void Start()
    {
        Cognitive3D_Manager.Instance.trackingSpace = this.gameObject;
    }
}
