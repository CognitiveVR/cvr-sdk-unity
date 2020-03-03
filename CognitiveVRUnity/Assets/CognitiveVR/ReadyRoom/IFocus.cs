using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//indicates the implementor can be 'focused'
//primarily used by controller pointer

public interface IFocus
{
    void SetFocus();
    Vector3 GetPosition();
}