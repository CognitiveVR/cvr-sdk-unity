using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Cognitive3D
{
    public class DataUploaderSettingsProvider : SettingsProvider
    {
        private const string title = "Cognitive3D/Data Uploader";
        public static readonly string SettingsPath = $"Project/{title}";
        private readonly DataUploaderGUI dataUploaderGUI = new DataUploaderGUI();

        internal DataUploaderSettingsProvider(string path, SettingsScope scopes) : base(path, scopes)
        {

        }
        
        internal static void OpenSettingsWindow()
        {
            SegmentAnalytics.TrackEvent("DataUploaderSettingsWindow_Opened", "DataUploaderSettingsWindow", "new");
            SettingsService.OpenProjectSettings(SettingsPath);
        }

        [SettingsProvider]
        internal static SettingsProvider CreateDataUploaderSettingsProvider()
        {
            return new DataUploaderSettingsProvider(SettingsPath, SettingsScope.Project);
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            dataUploaderGUI.OnGUI();
        }
    }
}
