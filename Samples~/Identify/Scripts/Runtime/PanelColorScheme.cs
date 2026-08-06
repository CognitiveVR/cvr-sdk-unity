using UnityEngine;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Implemented by any panel that can be re-tinted from a PanelColorScheme
    /// </summary>
    public interface IPanelColorScheme
    {
        void ApplyColorScheme(PanelColorScheme scheme);
    }

    /// <summary>
    /// Reusable color scheme for identification panel prefabs.
    /// Assign this to any panel that supports theming.
    /// </summary>
    [CreateAssetMenu(fileName = "NewColorScheme", menuName = "Cognitive3D/Identify/Panel Color Scheme")]
    public class PanelColorScheme : ScriptableObject
    {
        [Header("Panel")]
        public Color panelBackground = new Color32(18, 22, 40, 250);

        [Header("Text")]
        public Color headerText = Color.white;
        public Color subtitleText = new Color32(160, 165, 180, 255);
        public Color instructionText = new Color32(130, 135, 155, 255);

        [Header("Digit Display")]
        public Color digitBoxBackground = new Color32(30, 40, 65, 255);
        public Color digitBoxOutline = new Color32(107, 63, 160, 255);
        public Color digitText = new Color32(180, 210, 230, 255);
        public Color dashText = new Color32(100, 105, 120, 255);

        [Header("Instruction Row")]
        public Color instructionRowBackground = new Color32(25, 30, 50, 255);

        [Header("Error State")]
        public Color errorText = new Color32(230, 80, 80, 255);
        public Color errorBoxBackground = new Color32(80, 25, 30, 255);
        public Color errorBoxOutline = new Color32(180, 60, 60, 255);
        public Color errorDotColor = new Color32(210, 90, 70, 255);

        [Header("Button")]
        public Color buttonBackground = new Color32(200, 175, 45, 255);
        public Color buttonText = new Color32(20, 20, 20, 255);
        public Color buttonHighlighted = new Color32(255, 240, 180, 255);
        public Color buttonPressed = new Color32(170, 145, 25, 255);
        public Color buttonDisabled = new Color32(90, 90, 90, 128);

        [Header("Back Button")]
        public Color backButtonBackground = new Color32(35, 42, 68, 200);
        public Color backButtonIcon = new Color32(160, 165, 180, 255);
    }
}
