using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Cognitive3D
{
    internal static class RoomCaptureUtil
    {
        /// <summary>
        /// Normalizes an anchor label into a single consistent format across all providers:
        /// lowercase, words separated by single dashes. Handles the various casings each platform
        /// emits — PascalCase ("WallFace"), UPPER_SNAKE ("WALL_FACE"), spaces, commas — so equivalent
        /// labels converge (e.g. "WallFace" and "WALL_FACE" both become "wall-face")
        /// </summary>
        internal static string NormalizeLabel(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            var sb = new StringBuilder(raw.Length + 8);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsLetterOrDigit(c))
                {
                    // Split camelCase/PascalCase: a dash before an uppercase that follows a lower/digit
                    if (char.IsUpper(c) && i > 0 && (char.IsLower(raw[i - 1]) || char.IsDigit(raw[i - 1])))
                        sb.Append('-');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-')
                {
                    // Any other separator (underscore, space, comma, ...) collapses to a single dash
                    sb.Append('-');
                }
            }
            if (sb.Length > 0 && sb[sb.Length - 1] == '-')
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

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
