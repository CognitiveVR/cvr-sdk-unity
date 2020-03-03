using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//indicates the implementor can be 'focused' by HMD gaze
//used to react to eye tracking

public interface IGazeFocus
{
    void SetFocus();
}