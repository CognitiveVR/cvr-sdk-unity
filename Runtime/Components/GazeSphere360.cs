using UnityEngine;

// Description: This script is attached to the sphere that registers gaze for 360 video
// This will update the position of the sphere to position of the player so that the player
// always remains within the bounds of the sphere

namespace Cognitive3D.Components
{
    public class GazeSphere360 : MonoBehaviour
    {
        public Camera userCamera;

        void Update()
        {
            this.gameObject.transform.position = userCamera.transform.position;
        }
    }
}
