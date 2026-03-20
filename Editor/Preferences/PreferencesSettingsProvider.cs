using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Cognitive3D
{
    public class PreferencesSettingsProvider : SettingsProvider
    {
        private const string title = "Cognitive3D/Preferences";
        public static readonly string SettingsPath = $"Project/{title}";
        private readonly PreferencesGUI preferencesGUI = new PreferencesGUI();

        internal PreferencesSettingsProvider(string path, SettingsScope scopes) : base(path, scopes)
        {

        }

        internal static void OpenSettingsWindow()
        {
            SegmentAnalytics.TrackEvent("PreferencesSettingsWindow_Opened", "PreferencesSettingsWindow");
            SettingsService.OpenProjectSettings(SettingsPath);
        }

        [SettingsProviderGroup]
        internal static SettingsProvider[] CreateSettingsProviders()
        {
            return new SettingsProvider[]
            {
                new PreferencesSettingsProvider("Project/Cognitive3D", SettingsScope.Project),
                new PreferencesSettingsProvider("Project/Cognitive3D/Preferences", SettingsScope.Project)
            };
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            preferencesGUI.OnGUI();
        }
    }
}
