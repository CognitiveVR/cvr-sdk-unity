using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//indicates the implementor can be 'focused'
//primarily used by controller pointer
//'MonoBehaviour' property allows accessing the monobehaviour from this interface

namespace CognitiveVR
{
    public interface IPointerFocus
    {
        void SetPointerFocus();
        Vector3 GetPosition();
        MonoBehaviour MonoBehaviour { get; }
    }
}