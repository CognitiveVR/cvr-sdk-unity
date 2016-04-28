using System.Collections.Generic;

namespace CognitiveVR
{
    /*
     * The TuningUpdater class provides a simple interface that's used for processing tuning value updates
     * @exclude
     */
    public interface TuningUpdater
    {
        void onUpdate(string type, string id, IDictionary<string, object> values);
        void onClear(string type, string id);
        void commit();
    }
}
