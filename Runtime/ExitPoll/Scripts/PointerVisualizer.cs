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
        public void ConstructDefaultLineRenderer()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.widthMultiplier = 0.03f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = PointerLineMaterial != null ? PointerLineMaterial : Resources.Load<Material>("ExitPollPointerLine");
            lineRenderer.textureMode = LineTextureMode.Tile;
        }

        public void UpdatePointer(Vector3 start, Vector3 end)
        {
            pointsArray[0] = start;
            pointsArray[1] = end;

            lineRenderer.SetPositions(pointsArray);
        }
    }
}
