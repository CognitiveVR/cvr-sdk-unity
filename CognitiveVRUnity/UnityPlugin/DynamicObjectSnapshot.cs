﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//holds a set of data at a time to describe the dynamic object
//only used internally when holding for coroutine to pull from queue and write to string

namespace CognitiveVR
{
    internal class DynamicObjectSnapshot
    {
        public static Queue<DynamicObjectSnapshot> SnapshotPool = new Queue<DynamicObjectSnapshot>();

        public string Id;
        /// <summary>
        /// \"propname1\":\"propvalue1\",\"propname2\":\"propvalue2\"
        /// </summary>
        public string Properties;
        
        //"rift_abtn": {"buttonPercent": 100.0},"rift_bbtn": {"buttonPercent": 100.0},"rift_joystick": {"buttonPercent": 100.0,"x": 1.0,"y": 1.0}
        public string Buttons;

        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public bool DirtyScale = false;
        public float scaleX, scaleY, scaleZ;
        public double Timestamp;

        public DynamicObjectSnapshot()
        {
            //empty. only used to fill the pool
        }

        private DynamicObjectSnapshot(string dynamicObjectId, Vector3 pos, Quaternion rot, string props)
        {
            Id = dynamicObjectId;
            Properties = props;

            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;

            rotX = rot.x;
            rotY = rot.y;
            rotZ = rot.z;
            rotW = rot.w;

            Timestamp = Util.Timestamp(Time.frameCount);
        }

        private DynamicObjectSnapshot(string dynamicObjectId, float[] pos, float[] rot, string props)
        {
            Id = dynamicObjectId;
            Properties = props;
            posX = pos[0];
            posY = pos[1];
            posZ = pos[2];

            rotX = rot[0];
            rotY = rot[1];
            rotZ = rot[2];
            rotW = rot[3];
            Timestamp = Util.Timestamp(Time.frameCount);
        }

        public DynamicObjectSnapshot Copy()
        {
            var dyn = GetSnapshot();
            dyn.Timestamp = Timestamp;
            dyn.Id = Id;
            dyn.posX = posX;
            dyn.posY = posY;
            dyn.posZ = posZ;
            dyn.rotX = rotX;
            dyn.rotY = rotY;
            dyn.rotZ = rotZ;
            dyn.rotW = rotW;
            dyn.DirtyScale = DirtyScale;
            dyn.scaleX = scaleX;
            dyn.scaleY = scaleY;
            dyn.scaleZ = scaleZ;

            dyn.Properties = Properties;
            dyn.Buttons = Buttons;
            return dyn;
        }

        public void ReturnToPool()
        {
            Properties = null;
            Buttons = null;
            posX = 0;
            posY = 0;
            posZ = 0;
            rotX = 0;
            rotY = 0;
            rotZ = 0;
            rotW = 1;
            scaleX = 1;
            scaleY = 1;
            scaleZ = 1;
            DirtyScale = false;
            SnapshotPool.Enqueue(this);
        }

        public static DynamicObjectSnapshot GetSnapshot()
        {
            if (SnapshotPool.Count > 0)
            {
                DynamicObjectSnapshot dos = SnapshotPool.Dequeue();
                return dos;
            }
            else
            {
                var dos = new DynamicObjectSnapshot();
                return dos;
            }
        }
    }
}