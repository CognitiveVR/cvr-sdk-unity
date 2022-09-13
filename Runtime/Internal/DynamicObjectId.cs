using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//a reference to an ID that can be shared between similar objects
//only used by spawned dynamic objects or objects without a customid

namespace Cognitive3D
{
    internal struct DynamicObjectId
    {
        public string Id;
        public bool Used;
        public string MeshName;
        public bool MeshSet;

        public DynamicObjectId(string id)
        {
            Id = id;
            MeshName = string.Empty;
            MeshSet = false;
            Used = false;
        }

        public DynamicObjectId(string id, string meshName)
        {
            Id = id;
            MeshName = meshName;
            MeshSet = true;
            Used = false;
        }
    }
}