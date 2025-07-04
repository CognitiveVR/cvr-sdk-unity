using System.Collections;
using System.Collections.Generic;

namespace Cognitive3D
{
    [System.Serializable]
    public class ExitPollData
    {
        public string customerId;
        public string id;
        public string name;
        public int version;
        public string title;
        public string status;

        public ExitPollDataEntry[] questions;

        //this is what the panel will display
        [System.Serializable]
        public class ExitPollDataEntry
        {
            public string title;
            public string type;
            //voice
            public int maxResponseLength;
            //scale
            public string minLabel;
            public string maxLabel;
            public ExitPollDataScaleRange range;
            //multiple choice
            public ExitPollDataEntryAnswer[] answers;

            [System.Serializable]
            public class ExitPollDataScaleRange
            {
                public int start;
                public int end;
            }

            [System.Serializable]
            public class ExitPollDataEntryAnswer
            {
                public string answer;
                public bool icon;
            }
        }
    }
}
