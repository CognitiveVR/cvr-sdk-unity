using UnityEngine;

namespace Cognitive3D.Components
{
    public class GazeSphere360 : MonoBehaviour
    {
        public Camera userCamera;

        // Update is called once per frame
        void Update()
        {
            this.gameObject.transform.position = userCamera.transform.position;
        }
    }
}
