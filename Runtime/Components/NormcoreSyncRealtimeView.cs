using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if COGNITIVE3D_INCLUDE_NORMCORE
using Normal.Realtime;

namespace Cognitive3D
{
    // This class extends the Normcore RealtimeView to enable synchronization of GameObjects 
    // using the Normcore networking framework. It is conditionally compiled and only included 
    // when the Normcore package is present in the project. This prevents missing script errors 
    // in projects that do not have Normcore installed.
    public class NormcoreSyncRealtimeView : RealtimeView
    {

    }
}
#endif
