using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//only used internally when holding for coroutine to pull from queue and write to string

namespace Cognitive3D
{
    internal class DynamicObjectManifestEntry
    {
        public static string FileType = "gltf";

        public string Id;
        public string Name;
        public string MeshName;

        public bool HasProperties;
        public string Properties; //\"propertyname1\":\"value1\",\"propertyname2\":\"value2\"

        public bool isVideo;
        public string videoURL;

        public bool isController;
        public string controllerType;

        public DynamicObjectManifestEntry(string id, string name, string meshName)
        {
            this.Id = id;
            this.Name = name;
            this.MeshName = meshName;
        }

        public DynamicObjectManifestEntry(string id, string name, string meshName, string props)
        {
            this.Id = id;
            this.Name = name;
            this.MeshName = meshName;
            this.Properties = props;
        }
    }
}