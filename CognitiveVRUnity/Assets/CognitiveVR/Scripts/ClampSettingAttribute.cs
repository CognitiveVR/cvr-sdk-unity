using System;
using UnityEngine;


//used to identify which fields/methods should be displayed in the component window
namespace CognitiveVR.Components
{
    //add max and min values to this
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ClampSettingAttribute : PropertyAttribute
    {
        private bool UseFloatLimits;
        private float fmin;
        private float fmax;

        private bool UseIntLimits;
        private int imin;
        private int imax;

        public ClampSettingAttribute(){}

        public ClampSettingAttribute(float min)
        {
            fmin = min;
            UseFloatLimits = true;
        }

        public ClampSettingAttribute(float min, float max)
        {
            fmin = min;
            fmax = max;
            UseFloatLimits = true;
        }

        public ClampSettingAttribute(int min)
        {
            imin = min;
            UseIntLimits = true;
        }
        public ClampSettingAttribute(int min, int max)
        {
            imin = min;
            imax = max;
            UseIntLimits = true;
        }


        public bool GetIntLimits(out int min, out int max)
        {
            min = imin;
            max = imax;
            return UseIntLimits;
        }

        public bool GetFloatLimits(out float min, out float max)
        {
            min = fmin;
            max = fmax;
            return UseFloatLimits;
        }
    }
}