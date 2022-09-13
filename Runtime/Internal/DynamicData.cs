using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//instance data about how this specific dynamic thing should work
namespace Cognitive3D
{
    internal struct DynamicData
    {
        public string Name;
        public string MeshName;
        public Transform Transform;

        /// <summary>
        /// true if id is not empty and data should be updated
        /// </summary>
        public bool active;

        //true if customid was set on dynamicobject component. used to skip setting dynamicobjectid from shared list
        public bool UseCustomId;
        public string Id;

        public Vector3 LastPosition;
        public Quaternion LastRotation;
        public Vector3 LastScale;
        public float PositionThreshold;
        public float RotationThreshold;
        public float ScaleThreshold;

        /// <summary>
        /// how often this dynamic should check for a changed position
        /// </summary>
        public float DesiredUpdateRate;
        /// <summary>
        /// the internally tracked time since last position change check
        /// </summary>
        public float UpdateInterval;

        /// <summary>
        /// if the dynamic data should write the next time tick reaches it. if set externally, doesn't need to wait for update time
        /// </summary>
        public bool dirty;

        /// <summary>
        /// set to true when this object has done a snapshot with the 'enabled'=true property
        /// </summary>
        public bool hasEnabled;

        /// <summary>
        /// if true, sets various properties to false after tick
        /// </summary>
        public bool remove;

        /// <summary>
        /// true if any properties have been set. for faster checking than a null check
        /// </summary>
        public bool HasProperties;
        public List<KeyValuePair<string, object>> Properties;

        public bool IsController;
        public bool IsRightHand;
        public string ControllerType;

        public DynamicData(string name, string customid, string meshname, Transform transform, Vector3 position, Quaternion rotation, Vector3 scale, float posThreshold, float rotThreshold, float scaleThreshold, float updateInterval, bool iscontroller, string controllerType, bool isRightHand)
        {
            if (string.IsNullOrEmpty(customid))
            {
                Id = Cognitive3D.DynamicManager.GetUniqueObjectId(meshname);
                UseCustomId = false;
            }
            else
            {
                Id = customid;
                UseCustomId = true;
            }
            Name = name;
            MeshName = meshname;
            Transform = transform;
            LastPosition = position;
            LastRotation = rotation;
            LastScale = scale;
            PositionThreshold = posThreshold;
            RotationThreshold = rotThreshold;
            ScaleThreshold = scaleThreshold;
            active = true;
            dirty = true;
            remove = false;
            hasEnabled = false;

            HasProperties = false;
            Properties = null;
            IsController = iscontroller;
            ControllerType = controllerType;
            IsRightHand = isRightHand;

            DesiredUpdateRate = updateInterval;
            UpdateInterval = 0;
        }
    }
}