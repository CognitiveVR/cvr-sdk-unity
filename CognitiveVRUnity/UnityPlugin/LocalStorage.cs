namespace CognitiveVR
{
    internal static class LocalStorage
    {
        [System.Obsolete("LocalStorage.Load() is no longer used")]
        internal static T Load<T>(string fileName, bool deleteFile)
        {
            // No implementation, just return the default
            return default(T);
        }

        [System.Obsolete("LocalStorage.Save() is no longer used")]
        internal static void Save(string fileName, object data)
        {
            // No implementation
        }
    }
}
