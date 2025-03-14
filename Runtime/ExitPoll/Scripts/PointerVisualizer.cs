using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    /// <summary>
    /// Manages the visualization of a pointer ray using a LineRenderer component.
    /// The PointerVisualizer class provides functionality to create, configure, 
    /// and update a line that represents where the user is pointing. It supports 
    /// customization of line width, materials, and gradients.
    /// </summary>
    [AddComponentMenu("Cognitive3D/Internal/Pointer Visualizer")]
    public class PointerVisualizer : MonoBehaviour 
    {
        /// <summary>
        /// A reference to the line renderer component
        /// </summary>
        private LineRenderer lineRenderer;

        /// <summary>
        /// The default material for the line renderer
        /// </summary>
        public Material PointerLineMaterial;

        /// <summary>
        /// Holds the start and end points of the pointer ray for the LineRenderer to visualize
        /// </summary>
        private readonly Vector3[] pointsArray = new Vector3[2];

        /// <summary>
        /// Creates and sets up a line renderer to visualize where user is pointing
        /// </summary>
        /// <param name="transform">The controller anchor where the ray starts</param>
        public void ConstructDefaultLineRenderer(float pointerWidth, Gradient pointerGradient)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.widthMultiplier = pointerWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = PointerLineMaterial != null ? PointerLineMaterial : Resources.Load<Material>("ExitPollPointerLine");
            lineRenderer.textureMode = LineTextureMode.Tile;

            lineRenderer.colorGradient = pointerGradient;
        }

        /// <summary>
        /// Updates the start and end positions of the pointer ray and applies them to the LineRenderer
        /// </summary>
        /// <param name="start">The starting position of the pointer ray</param>
        /// <param name="end">The ending position of the pointer ray</param>
        public void UpdatePointer(Vector3 start, Vector3 end)
        {
            pointsArray[0] = start;
            pointsArray[1] = end;

            lineRenderer.SetPositions(pointsArray);
        }
    }
}
