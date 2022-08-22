using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//TODO mesh combine?
//TODO submeshes
//TODO URP support

#if UNITY_EDITOR
namespace CognitiveVR
{
    public class CustomRenderExporter : MonoBehaviour
    {
        public class CustomRender
        {
            //public GameObject tempGo;
            public Mesh meshdata;
            public Material material;
            public Transform transform;
            public string name;
            public GameObject tempGameObject;
        }

        //return class with texture + material + instance mesh? swap uv2 with uv1
        public CustomRender RenderMeshCustom()
        {
            //should return a mesh and material. should not leave mesh/gameobjects in scene

            if (!gameObject.GetComponent<MeshFilter>())
            {
                Debug.LogWarning("CustomRenderExporter attached to object without mesh filter " + gameObject.name, gameObject);
                return null;
            }
            if (!gameObject.GetComponent<MeshRenderer>())
            {
                Debug.LogWarning("CustomRenderExporter attached to object without mesh renderer " + gameObject.name, gameObject);
                return null;
            }

            var cr = new CustomRender();
            //duplicate object. NOPE! MIGHT HAVE ISSUES WITH COMPONENTS
            var uv2copy = new GameObject();// Instantiate(gameObject);
            //copy meshrenderer, mesh filter, materials from source
            uv2copy.name = gameObject.name;
            uv2copy.transform.SetParent(gameObject.transform.parent);
            uv2copy.transform.localPosition = gameObject.transform.localPosition;
            uv2copy.transform.localRotation = gameObject.transform.localRotation;
            uv2copy.transform.localScale = gameObject.transform.localScale;
            var mr = uv2copy.AddComponent<MeshRenderer>();
            mr.sharedMaterials = gameObject.GetComponent<MeshRenderer>().sharedMaterials;
            var mf = uv2copy.AddComponent<MeshFilter>();
            mf.sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            //instance current render mesh
            cr.meshdata = mf.mesh;
            //unwrap uv2 for instance mesh
            if (cr.meshdata.triangles.Length > 0)
                Unwrapping.GenerateSecondaryUVSet(cr.meshdata);
            //set instance mesh as collider
            var tempMeshCollider = uv2copy.AddComponent<MeshCollider>();

            tempMeshCollider.sharedMesh = cr.meshdata;

            //render texture
            var textureOut = RenderCameraToTexture(uv2copy, PixelSamplesPerMeter, OutputResolution, GetInstanceID());

            //apply texture and material to meshrenderer
            cr.material = new Material(Shader.Find("Standard"));
            cr.material.mainTexture = textureOut;

            //copy uv2 to uv
            cr.meshdata.uv = cr.meshdata.uv2;
            mf.sharedMesh = cr.meshdata;

            cr.name = gameObject.name;
            cr.transform = transform;

            mr.material = cr.material;
            cr.tempGameObject = uv2copy;

            return cr;
        }

        private void Reset()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) { return; }

            var bounds = meshRenderer.bounds;
            var magnitude = bounds.size.magnitude;
            if (magnitude < 10)
                OutputResolution = 256;
            else if (magnitude < 20)
                OutputResolution = 512;
            else if (magnitude < 30)
                OutputResolution = 1024;
            else OutputResolution = 2048;

            PixelSamplesPerMeter = Mathf.CeilToInt((OutputResolution * 1.5f) / magnitude);
        }

        //if output resolution is > camera resolution, will see black stripes? ADD WARNING
        public int OutputResolution = 256;

        //higher number = higher resolution
        public int PixelSamplesPerMeter = 20; //10, 20

        //generate a texture from orthographic cameras
        static Texture2D RenderCameraToTexture(GameObject target, int pixelSamplesPerMeter, int outputResolution, int sourceInstanceId)
        {
            float sampleDensity = 1;
            Texture2D generatedUV2Texture;
            //like dynamic object thumbnails - set to layer, create camera, SET LIGHTING and

            var bounds = target.GetComponent<MeshRenderer>().bounds;

            float XcameraResolution = Mathf.Max(bounds.extents.y, bounds.extents.z) * pixelSamplesPerMeter;
            float YcameraResolution = Mathf.Max(bounds.extents.x, bounds.extents.z) * pixelSamplesPerMeter;
            float ZcameraResolution = Mathf.Max(bounds.extents.x, bounds.extents.y) * pixelSamplesPerMeter;

            //Debug.Log("x camera resolution " + bounds.extents.y * pixelSamplesPerMeter + " x " + bounds.extents.z * pixelSamplesPerMeter);
            //Debug.Log("y camera resolution " + bounds.extents.x * pixelSamplesPerMeter + " x " + bounds.extents.z * pixelSamplesPerMeter);
            //Debug.Log("z camera resolution " + bounds.extents.x * pixelSamplesPerMeter + " x " + bounds.extents.y * pixelSamplesPerMeter);

            float XOrthoScale = Mathf.Max(bounds.extents.y, bounds.extents.z);
            float YOrthoScale = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float ZOrthoScale = Mathf.Max(bounds.extents.x, bounds.extents.y);

            XOrthoScale *= 1.05f;
            YOrthoScale *= 1.05f;
            ZOrthoScale *= 1.05f;

            //position cameras from bounding box

            generatedUV2Texture = new Texture2D(outputResolution, outputResolution);
            generatedUV2Texture.name = target.name + sourceInstanceId;
            Color[] black = new Color[generatedUV2Texture.width * generatedUV2Texture.height];
            generatedUV2Texture.SetPixels(black);

            Vector3 position = bounds.center;
            float xoffset = bounds.extents.x + 1;
            float yoffset = bounds.extents.y + 1;
            float zoffset = bounds.extents.z + 1;

            float farClipDistance = bounds.size.magnitude;

            SaveDynamicThumbnail(target, position + Vector3.back * zoffset, Quaternion.Euler(0, 0, 180), farClipDistance, ZOrthoScale, "z+", (int)ZcameraResolution, sampleDensity, generatedUV2Texture); //forward
            SaveDynamicThumbnail(target, position + Vector3.forward * zoffset, Quaternion.Euler(0, 180, 0), farClipDistance, ZOrthoScale, "z-", (int)ZcameraResolution, sampleDensity, generatedUV2Texture);
            SaveDynamicThumbnail(target, position + Vector3.right * xoffset, Quaternion.Euler(0, -90, 0), farClipDistance, XOrthoScale, "x+", (int)XcameraResolution, sampleDensity, generatedUV2Texture);
            SaveDynamicThumbnail(target, position + Vector3.left * xoffset, Quaternion.Euler(0, 90, 0), farClipDistance, XOrthoScale, "x-", (int)XcameraResolution, sampleDensity, generatedUV2Texture);
            SaveDynamicThumbnail(target, position + Vector3.up * yoffset, Quaternion.Euler(90, 0, 180), farClipDistance, YOrthoScale, "y+", (int)YcameraResolution, sampleDensity, generatedUV2Texture);
            SaveDynamicThumbnail(target, position + Vector3.down * yoffset, Quaternion.Euler(-90, 0, 180), farClipDistance, YOrthoScale, "y-", (int)YcameraResolution, sampleDensity, generatedUV2Texture);

            Color[] pixels = generatedUV2Texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b, 1);
            }
            generatedUV2Texture.SetPixels(pixels);
            generatedUV2Texture.Apply();
            //save

            //System.IO.File.WriteAllBytes(Application.dataPath + "/"+ target.name + "generatedUV2Texture.png", generatedUV2Texture.EncodeToPNG());
            //AssetDatabase.Refresh();
            return generatedUV2Texture;
        }

        public static int FindUnusedLayer()
        {
            for (int i = 31; i > 0; i--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                {
                    return i;
                }
            }
            return -1;
        }

        public static void RecursivelyGetChildren(List<Transform> transforms, Transform current)
        {
            transforms.Add(current);
            for (int i = 0; i < current.childCount; i++)
            {
                RecursivelyGetChildren(transforms, current.GetChild(i));
            }
        }

        static void SaveDynamicThumbnail(GameObject target, Vector3 position, Quaternion rotation, float farClipDistance, float orthographicsize, string saveTextureName, int cameraResolution, float sampleDensity, Texture2D generatedUV2Texture)
        {
            Dictionary<GameObject, int> originallayers = new Dictionary<GameObject, int>();

            //choose layer
            int layer = FindUnusedLayer();
            if (layer == -1) { Debug.LogWarning("couldn't find layer, don't set layers"); }

            //create camera stuff
            GameObject go = new GameObject("temp dynamic camera " + saveTextureName);
            var renderCam = go.AddComponent<Camera>();
            renderCam.clearFlags = CameraClearFlags.Color;
            renderCam.backgroundColor = Color.black;
            renderCam.nearClipPlane = 0.01f;
            renderCam.farClipPlane = farClipDistance;
            renderCam.orthographic = true;
            renderCam.orthographicSize = orthographicsize;
            if (layer != -1)
            {
                renderCam.cullingMask = 1 << layer;
            }
            var rt = RenderTexture.GetTemporary(cameraResolution, cameraResolution);
            renderCam.targetTexture = rt;
            Texture2D cameraRenderTexture = new Texture2D(rt.width, rt.height);

            //position camera
            go.transform.position = position;
            go.transform.rotation = rotation;
            //Debug.DrawRay(go.transform.position, go.transform.forward * 10, Color.blue, 50);
            //Debug.DrawRay(go.transform.position, go.transform.up, Color.green, 50);
            //Debug.DrawRay(go.transform.position, go.transform.right, Color.red, 50);

            //do this recursively, skipping nested dynamic objects
            List<Transform> relayeredTransforms = new List<Transform>();
            RecursivelyGetChildren(relayeredTransforms, target.transform);
            //set dynamic gameobject layers
            try
            {
                if (layer != -1)
                {
                    foreach (var v in relayeredTransforms)
                    {
                        originallayers.Add(v.gameObject, v.gameObject.layer);
                        v.gameObject.layer = layer;
                    }
                }
                //render to texture
                renderCam.Render();
                RenderTexture.active = rt;
                cameraRenderTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                cameraRenderTexture.Apply();
                RenderTexture.active = null;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            //System.IO.File.WriteAllBytes(Application.dataPath + "/"+ target.name+saveTextureName+".png", cameraRenderTexture.EncodeToPNG());

            try
            {
                for (int x = 0; x < cameraResolution; x++)
                {
                    for (int y = 0; y < cameraResolution; y++)
                    {
                        Color renderedColor = cameraRenderTexture.GetPixel(x, y);
                        if (Mathf.Approximately(renderedColor.r + renderedColor.g + renderedColor.b, 0)) { continue; }

                        Ray ray = renderCam.ViewportPointToRay(new Vector3((float)x / cameraResolution, (float)y / cameraResolution, 0));
                        RaycastHit hit = new RaycastHit();
                        if (Physics.Raycast(ray, out hit, farClipDistance, 1 << layer))
                        {
                            //what the current color is at uv2
                            Vector2 uv2HitPoint = hit.textureCoord2;
                            Color currentUV2Color = generatedUV2Texture.GetPixel((int)(uv2HitPoint.x * generatedUV2Texture.width), (int)(uv2HitPoint.y * generatedUV2Texture.height));
                            float hitDot = Vector3.Dot(ray.direction, -hit.normal);
                            if (hitDot >= currentUV2Color.a)
                            {
                                Color writeColor = renderedColor;
                                writeColor.a = hitDot;
                                generatedUV2Texture.SetPixel((int)(uv2HitPoint.x * generatedUV2Texture.width), (int)(uv2HitPoint.y * generatedUV2Texture.height), writeColor);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            if (layer != -1)
            {
                //reset dynamic object layers
                foreach (var v in originallayers)
                {
                    v.Key.layer = v.Value;
                }
            }

            DestroyImmediate(cameraRenderTexture);
            //remove camera
            GameObject.DestroyImmediate(renderCam.gameObject);
        }
    }
} //namespace

namespace CognitiveVR
{
    [CustomEditor(typeof(CustomRenderExporter))]
    [CanEditMultipleObjects]
    public class CustomRenderExporterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var t = (CustomRenderExporter)target;
            var meshRenderer = t.GetComponent<MeshRenderer>();
            if (meshRenderer == null) { return; }

            //get mesh renderer bounds
            var bounds = meshRenderer.bounds;
            var magnitude = bounds.size.magnitude;

            GUILayout.Label((t.PixelSamplesPerMeter * magnitude).ToString());

            if (t.OutputResolution * 1.5f > t.PixelSamplesPerMeter * magnitude)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Some artifacting may occur\nIncrease Pixel Samples or decrease Output Resolution", MessageType.Warning);
                if (GUILayout.Button("Fix"))
                {
                    t.PixelSamplesPerMeter = Mathf.CeilToInt((t.OutputResolution * 1.5f)/magnitude);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}

#endif