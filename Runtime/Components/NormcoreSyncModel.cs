using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    [RealtimeModel]
    public partial class NormcoreSyncModel
    {
        [RealtimeProperty(1, true, true)] 
        public string _lobbyId;
    }
}
