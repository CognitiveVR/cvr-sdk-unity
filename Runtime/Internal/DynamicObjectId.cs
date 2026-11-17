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

        /// <summary>
        /// Network identity of the object that first took this id (player avatar owner id or
        /// network sync id). Empty for non-networked objects. A freed id is only reused by an
        /// object with the same identity, so one player's avatar never inherits another's id.
        /// </summary>
        public string NetworkIdentity;

        public DynamicObjectId(string id)
        {
            Id = id;
            MeshName = string.Empty;
            MeshSet = false;
            Used = false;
            NetworkIdentity = string.Empty;
        }

        public DynamicObjectId(string id, string meshName)
        {
            Id = id;
            MeshName = meshName;
            MeshSet = true;
            Used = false;
            NetworkIdentity = string.Empty;
        }
    }
}