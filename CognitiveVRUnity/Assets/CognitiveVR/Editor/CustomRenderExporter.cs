using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
            //duplicate object
            var uv2copy = Instantiate(gameObject);
            DestroyImmediate(uv2copy.GetComponent<CustomRenderExporter>());
            //remove existing colliders
            var colliders = uv2copy.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
                DestroyImmediate(c);
            //instance current render mesh
            var tempMeshFilter = uv2copy.GetComponent<MeshFilter>();
            cr.meshdata = tempMeshFilter.mesh;
            //unwrap uv2 for instance mesh
            if (cr.meshdata.triangles.Length > 0)
                Unwrapping.GenerateSecondaryUVSet(cr.meshdata);
            //set instance mesh as collider
            var tempMeshCollider = uv2copy.AddComponent<MeshCollider>();

            tempMeshCollider.sharedMesh = cr.meshdata;

            //render texture
            var textureOut = RenderCameraToTexture(uv2copy);

            //apply texture and material to meshrenderer
            //var mr = cr.tempGo.GetComponent<MeshRenderer>();
            cr.material = new Material(Shader.Find("Standard"));
            cr.material.mainTexture = textureOut;

            //copy uv2 to uv
            cr.meshdata.uv = cr.meshdata.uv2;
            tempMeshFilter.sharedMesh = cr.meshdata;

            cr.name = gameObject.name;
            cr.transform = transform;

            //DestroyImmediate(uv2copy);
            var mr = uv2copy.GetComponent<MeshRenderer>();
            mr.material = cr.material;
            cr.tempGameObject = uv2copy;

            return cr;
        }

        //generate a texture from orthographic cameras
        static Texture2D RenderCameraToTexture(GameObject target)
        {
            float sampleDensity = 1;
            Texture2D generatedUV2Texture;
            //like dynamic object thumbnails - set to layer, create camera, SET LIGHTING and

            var bounds = target.GetComponent<MeshRenderer>().bounds;

            int resolution = 512;
            var magnitude = bounds.size.magnitude;
            /*if (magnitude < 5)
                resolution = 128;
            else if (magnitude < 10)
                resolution = 256;
            else if (magnitude < 20)
                resolution = 512;
            else resolution = 1024;*/

            float XOrthoScale = Mathf.Max(bounds.extents.y, bounds.extents.z);
            float YOrthoScale = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float ZOrthoScale = Mathf.Max(bounds.extents.x, bounds.extents.y);

            XOrthoScale *= 1.1f;
            YOrthoScale *= 1.1f;
            ZOrthoScale *= 1.1f;

            //position cameras from bounding box
            //set depth to bounds

            generatedUV2Texture = new Texture2D(resolution, resolution);
            generatedUV2Texture.name = target.name + target.GetInstanceID();
            Color[] black = new Color[generatedUV2Texture.width * generatedUV2Texture.height];
            generatedUV2Texture.SetPixels(black);

            Vector3 position = bounds.center;
            float xoffset = Mathf.Max(YOrthoScale, ZOrthoScale);
            float yoffset = Mathf.Max(XOrthoScale, ZOrthoScale);
            float zoffset = Mathf.Max(YOrthoScale, XOrthoScale);

            float farClipDistance = 10;

            SaveDynamicThumbnail(target, position + Vector3.back * zoffset, Quaternion.Euler(0, 0, 180), farClipDistance, ZOrthoScale, "z+", resolution, sampleDensity, generatedUV2Texture); //forward
            SaveDynamicThumbnail(target, position + Vector3.forward * zoffset, Quaternion.Euler(0, 180, 0), farClipDistance, ZOrthoScale, "z-", resolution, sampleDensity, generatedUV2Texture);
            SaveDynamicThumbnail(target, position + Vector3.right * xoffset, Quaternion.Euler(0, -90, 0), farClipDistance, XOrthoScale, "x+", resolution, sampleDensity, generatedUV2Texture);
            SaveDynamicThumbnail(target, position + Vector3.left * xoffset, Quaternion.Euler(0, 90, 0), farClipDistance, XOrthoScale, "x-", resolution, sampleDensity, generatedUV2Texture);
            SaveDynamicThumbnail(target, position + Vector3.up * yoffset, Quaternion.Euler(90, 0, 180), farClipDistance, YOrthoScale, "y+", resolution, sampleDensity, generatedUV2Texture);
            SaveDynamicThumbnail(target, position + Vector3.down * yoffset, Quaternion.Euler(-90, 0, 180), farClipDistance, YOrthoScale, "y-", resolution, sampleDensity, generatedUV2Texture);

            Color[] pixels = generatedUV2Texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b, 1);
            }
            generatedUV2Texture.SetPixels(pixels);

            generatedUV2Texture.Apply();
            //save

            //System.IO.File.WriteAllBytes(Application.dataPath + "/"+ target.name + "generatedUV2Texture.png", generatedUV2Texture.EncodeToPNG());
            //generatedUV2Texture
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

        static void SaveDynamicThumbnail(GameObject target, Vector3 position, Quaternion rotation, float farClipDistance, float orthographicsize, string saveTextureName, int resolution, float sampleDensity, Texture2D generatedUV2Texture)
        {
            Dictionary<GameObject, int> originallayers = new Dictionary<GameObject, int>();
            //var dynamic = target.GetComponent<CognitiveVR.DynamicObject>();

            //choose layer
            int layer = FindUnusedLayer();
            if (layer == -1) { Debug.LogWarning("couldn't find layer, don't set layers"); }

            //create camera stuff
            GameObject go = new GameObject("temp dynamic camera " + saveTextureName);
            var renderCam = go.AddComponent<Camera>();
            renderCam.clearFlags = CameraClearFlags.Color;
            renderCam.backgroundColor = Color.black;
            renderCam.nearClipPlane = 0.01f;
            renderCam.farClipPlane = 100;// farClipDistance;
            renderCam.orthographic = true;
            renderCam.orthographicSize = orthographicsize;
            if (layer != -1)
            {
                renderCam.cullingMask = 1 << layer;
            }
            var rt = new RenderTexture(512, 512, 16);
            renderCam.targetTexture = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height);

            //position camera
            go.transform.position = position;
            go.transform.rotation = rotation;
            Debug.DrawRay(go.transform.position, go.transform.forward, Color.green, 5);

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
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            //Debug.Log("unused layer " + layer);

            //if (target.name == "city__Build0Small(Clone).005")
            //System.IO.File.WriteAllBytes(Application.dataPath + "/"+ target.name+saveTextureName+".png", tex.EncodeToPNG());

            //write to texture or whatever

            //x_positive = tex;

            /*for (int x = 0; x < 100; x++)
            {
                for (int y = 0; y < 100; y++)
                {
                    Ray ray = renderCam.ViewportPointToRay(new Vector3(x / 100f, y / 100f, 0));
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(LayerMask.LayerToName(layer))))
                    {
                        Vector2 uvHitPoint = hit.textureCoord; //where to write to
                        Color renderedColor = tex.GetPixel((int)(uvHitPoint.x * tex.width), (int)(uvHitPoint.y * tex.height));
                        //Debug.DrawRay(ray.origin, ray.direction * 0.01f, new Color(hit.textureCoord.x, hit.textureCoord.y, 0), 5);
                        Debug.DrawRay(ray.origin, ray.direction * 0.1f, renderedColor, 10);
                    }
                    else
                    {
                        Debug.DrawRay(ray.origin, ray.direction * 0.01f, Color.red, 10);
                    }
                }
            }*/

            //textureBuilder.SetPixels()

            try
            {
                //raycast grid from camera. get uv2s and color from image
                int finalSampleDensity = (int)(resolution * sampleDensity);
                for (int x = 0; x < finalSampleDensity; x++)
                {
                    for (int y = 0; y < finalSampleDensity; y++)
                    {
                        Ray ray = renderCam.ViewportPointToRay(new Vector3((float)x / finalSampleDensity, (float)y / finalSampleDensity, 0));
                        RaycastHit hit = new RaycastHit();
                        //if (Physics.Raycast(ray, out hit))
                        //if (Physics.Raycast(ray, out hit,100, LayerMask.GetMask(LayerMask.LayerToName(layer)))) 
                        if (Physics.Raycast(ray, out hit, 100f, 1 << layer))
                        {
                            Color renderedColor = tex.GetPixel(x, y);

                            if (x % 10 == 0 && y % 10 == 0)
                            {
                                Color c = renderedColor;
                                c.a = 1;
                                //Debug.DrawRay(ray.origin, ray.direction * hit.distance, c, 5);
                            }

                            //what the current color is at uv2
                            Vector2 uv2HitPoint = hit.textureCoord2; //UGH THIS FALLS BACK TO TEXTURECOORD1 silently if MeshCollider.mesh doesn't have uv2 coords
                            Color currentUV2Color = generatedUV2Texture.GetPixel((int)(uv2HitPoint.x * generatedUV2Texture.width), (int)(uv2HitPoint.y * generatedUV2Texture.height));
                            float hitDot = Vector3.Dot(ray.direction, -hit.normal);
                            if (hitDot >= currentUV2Color.a)
                            {
                                Color writeColor = renderedColor;
                                writeColor.a = hitDot;
                                generatedUV2Texture.SetPixel((int)(uv2HitPoint.x * generatedUV2Texture.width), (int)(uv2HitPoint.y * generatedUV2Texture.height), writeColor);
                            }
                        }
                        //else
                        {
                            if (x % 10 == 0 && y % 10 == 0)
                            {
                                //Debug.DrawRay(ray.origin, ray.direction * 0.1f, Color.red, 50);
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

            //remove camera
            GameObject.DestroyImmediate(renderCam.gameObject);
        }
    }
}