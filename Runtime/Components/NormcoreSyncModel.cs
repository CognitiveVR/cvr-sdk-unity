using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    // Represents a model for syncing Normcore data, including a lobby ID.
    [RealtimeModel]
    public partial class NormcoreSyncModel
    {
        [RealtimeProperty(1, true, true)] 
        public string _lobbyId;
    }
}
