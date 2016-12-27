using System;


//used to identify which fields/methods should be displayed in the component window
namespace CognitiveVR.Components
{
    //add max and min values to this
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DisplaySettingAttribute : Attribute
    {
        private bool UseFloatLimits;
        private float fmin;
        private float fmax;

        private bool UseIntLimits;
        private int imin;
        private int imax;

        public DisplaySettingAttribute(){}

        public DisplaySettingAttribute(float min)
        {
            fmin = min;
            UseFloatLimits = true;
        }

        public DisplaySettingAttribute(float min, float max)
        {
            fmin = min;
            fmax = max;
            UseFloatLimits = true;
        }

        public DisplaySettingAttribute(int min)
        {
            imin = min;
            UseIntLimits = true;
        }
        public DisplaySettingAttribute(int min, int max)
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