using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.Components
{
    public class GazeSphere360 : AnalyticsComponentBase
    {
        public Camera userCamera;

        // Update is called once per frame
        void Update()
        {
            this.gameObject.transform.position = userCamera.transform.position;
        }

        public override string GetDescription()
        {
            return "Updates 360 video sphere to always enclose camera.";
        }
    }
}
