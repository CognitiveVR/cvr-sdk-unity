using System;


//used to identify which fields/methods should be displayed in the component window
namespace CognitiveVR.Components
{
    //add max and min values to this
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DisplaySettingAttribute : Attribute
    {
        private float min;
        private float max;

        private int imin;
        private int imax;

        public DisplaySettingAttribute(){}

        public DisplaySettingAttribute(float min, float max)
        {

        }

        public DisplaySettingAttribute(int min, int max)
        {

        }
    }
}