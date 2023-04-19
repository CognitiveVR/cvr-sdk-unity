using UnityEngine;

// Description: This script is attached to the sphere that registers gaze for 360 video
// This will update the position of the sphere to position of the player so that the player
// always remains within the bounds of the sphere

namespace Cognitive3D.Components
{
    internal class GazeSphere360 : MonoBehaviour
    {
        [SerializeField]
        private Camera userCamera;
        public Camera UserCamera { get { return userCamera; } set { userCamera = value; } }

        void Update()
        {
            transform.position = userCamera.transform.position;
        }
    }
}
