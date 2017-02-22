using UnityEngine;
using System.Collections;

/// <summary>
/// base for CognitiveVR analytics components
/// </summary>

namespace CognitiveVR.Components
{
    public class CognitiveVRAnalyticsComponent : MonoBehaviour
    {
        public virtual void CognitiveVR_Init(Error initError)
        {
            //called after cognitiveVR initializes
        }

        //CognitiveVR Component Setup uses reflection to find these Methods. These help display each component, but are not required
        //public static bool GetWarning() { return true; }
        //public static string GetDescription() { return "description"; }
    }
}