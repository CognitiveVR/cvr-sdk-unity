using System.Collections.Generic;
using UnityEngine;

// Platform-neutral room-layout data shared by every IRoomCaptureProvider.
// Providers convert their native anchor/room representations into these structs and pass
// them to CoreInterface.
namespace Cognitive3D
{
    internal struct RoomManifestEntry
    {
        public string id;
        public string label;                          // may be empty on partial updates
        public List<AnchorManifestEntry> anchors;     // may be empty or partial
    }

    internal struct AnchorManifestEntry
    {
        public string id;
        public string label;     // "WALL_FACE", "DESK", "COUCH", ...
        public string shape;     // "plane" | "volume"
        public bool isPlane;     // drives the z=0 convention on the data side's "s"
    }

    internal struct RoomDataEntry
    {
        public string id;
        public double time;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool enabled;
        public bool hasTransform;  // when false, only id/time/enabled are serialized
        public bool isPlane;       // when true, "s" gets z forced to 0 on the wire
    }
}
