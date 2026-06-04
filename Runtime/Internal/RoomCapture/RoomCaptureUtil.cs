using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    internal static class RoomCaptureUtil
    {
        /// <summary>
        /// Data entry for an anchor's pose + enabled state
        /// </summary>
        internal static RoomDataEntry BuildAnchorData(string anchorId, Vector3 position, Quaternion rotation, Vector3 scale, bool enabled, bool isPlane)
        {
            return new RoomDataEntry
            {
                id = anchorId,
                time = Util.Timestamp(),
                position = position,
                rotation = rotation,
                scale = scale,
                enabled = enabled,
                hasTransform = true,
                isPlane = isPlane,
            };
        }

        /// <summary>
        /// Data entry for a removed anchor, only id/time/enabled meaningful.
        /// </summary>
        internal static RoomDataEntry BuildRemoval(string id)
        {
            return new RoomDataEntry
            {
                id = id,
                time = Util.Timestamp(),
                enabled = false,
                hasTransform = false,
            };
        }

        /// <summary>
        /// Data entry for a room toggle, only id/time/enabled meaningful.
        /// </summary>
        internal static RoomDataEntry BuildRoomToggle(string roomId, bool enabled)
        {
            return new RoomDataEntry
            {
                id = roomId,
                time = Util.Timestamp(),
                enabled = enabled,
                hasTransform = false,
            };
        }
    }
}
