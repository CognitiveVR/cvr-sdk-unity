using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    /// <summary>
    /// Write summary for this class
    /// </summary>
    [AddComponentMenu("Cognitive3D/Internal/Pointer Visualizer")]
    public class PointerVisualizer : MonoBehaviour 
    {
        public static PointerVisualizer Instance { get; private set; }

        /// <summary>
        /// A reference to the line renderer component
        /// </summary>
        private LineRenderer lineRenderer;

        /// <summary>
        /// The default material for the line renderer
        /// </summary>
        public Material PointerLineMaterial;

        private Vector3[] pointsArray = new Vector3[2];

        /// <summary>
        /// Creates and sets up a line renderer to visualize where user is pointing
        /// </summary>
        /// <param name="transform">The controller anchor where the ray starts</param>
        public void ConstructDefaultLineRenderer(float pointerWidth, bool useDefaultGradient, Gradient pointerGradient)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.widthMultiplier = pointerWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = PointerLineMaterial != null ? PointerLineMaterial : Resources.Load<Material>("ExitPollPointerLine");
            lineRenderer.textureMode = LineTextureMode.Tile;

            lineRenderer.colorGradient = useDefaultGradient ? new Gradient { colorKeys = new GradientColorKey[] { new GradientColorKey(new Color(0.286f, 0.106f, 0.631f, 1f), 0f), new GradientColorKey(new Color(0.055f, 0.416f, 0.624f, 1f), 0.5f), new GradientColorKey(new Color(0.039f, 0.557f, 0.259f, 1f), 1f) } } : pointerGradient;
        }

        public void UpdatePointer(Vector3 start, Vector3 end)
        {
            pointsArray[0] = start;
            pointsArray[1] = end;

            lineRenderer.SetPositions(pointsArray);
        }
    }
}
