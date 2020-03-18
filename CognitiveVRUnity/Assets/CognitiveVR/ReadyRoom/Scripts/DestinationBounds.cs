using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class DestinationBounds : MonoBehaviour
    {
        public Color displayColor = Color.cyan;

        public Vector3 BoundSize = new Vector3(1, 0.5f, 1);

        void Start()
        {
            Bounds b = new Bounds(Vector3.zero, BoundSize);
            CreateMesh(b);
        }

        void CreateMesh(Bounds bounds)
        {
            //ensure there's a meshfilter and a mesh renderer
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            float height = bounds.extents.y;
            //create a couple quads using the extents of the room bounds

            Mesh mesh = new Mesh();

            #region mesh definition
            Vector3[] verts = new Vector3[8];
            //forward
            verts[0] = new Vector3(bounds.min.x, 0, bounds.max.z);
            verts[1] = new Vector3(bounds.min.x, height, bounds.max.z);
            verts[2] = new Vector3(bounds.max.x, 0, bounds.max.z);
            verts[3] = new Vector3(bounds.max.x, height, bounds.max.z);

            verts[4] = new Vector3(bounds.max.x, 0, bounds.min.z);
            verts[5] = new Vector3(bounds.max.x, height, bounds.min.z);
            verts[6] = new Vector3(bounds.min.x, 0, bounds.min.z);
            verts[7] = new Vector3(bounds.min.x, height, bounds.min.z);
            mesh.vertices = verts;

            int[] tris = new int[24]
            {
            0,1,2,
            2,1,3,
            2,3,4,
            4,3,5,
            4,5,6,
            6,5,7,
            6,7,0,
            0,7,1
            };
            mesh.triangles = tris;
            Vector3[] normals = new Vector3[8]
            {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[8]
            {
              new Vector2(0, 0),
              new Vector2(1, 0),
              new Vector2(0, 1),
              new Vector2(1, 1),
              new Vector2(0, 0),
              new Vector2(1, 0),
              new Vector2(0, 1),
              new Vector2(1, 1)
            };
            mesh.uv = uv;

            Color[] colors = new Color[8]
            {
            displayColor,
            new Color(displayColor.r, displayColor.g, displayColor.b, 0.0f),
            displayColor,
            new Color(displayColor.r, displayColor.g, displayColor.b, 0.0f),
            displayColor,
            new Color(displayColor.r, displayColor.g, displayColor.b, 0.0f),
            displayColor,
            new Color(displayColor.r, displayColor.g, displayColor.b, 0.0f)
            };
            mesh.colors = colors;

            #endregion

            meshFilter.mesh = mesh;

            meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }
    }
}