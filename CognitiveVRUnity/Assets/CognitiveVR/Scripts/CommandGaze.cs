using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;
using UnityEngine.Rendering;

//use command buffer
namespace CognitiveVR
{
public class CommandGaze : GazeBase {

    public RenderTexture rt;
    public BuiltinRenderTextureType blitTo = BuiltinRenderTextureType.CurrentActive;
    public CameraEvent camevent = CameraEvent.BeforeForwardOpaque;

    CommandBufferHelper helper;

    public override void Initialize()
    {
        base.Initialize();
        CognitiveVR_Manager.InitEvent += CognitiveVR_Manager_InitEvent;
    }

    private void CognitiveVR_Manager_InitEvent(Error initError)
    {
        if (initError == Error.Success)
        {
            var buf = new CommandBuffer();
            buf.name = "cognitive depth";
            CameraComponent.depthTextureMode = DepthTextureMode.Depth;
            CameraComponent.AddCommandBuffer(camevent, buf);
            var material = new Material(Shader.Find("Hidden/Cognitive/CommandDepth"));

            buf.SetGlobalFloat(Shader.PropertyToID("_DepthScale"), 1f / 1);
            buf.Blit((Texture)null, BuiltinRenderTextureType.CameraTarget, material, (int)0);

#if SRP_LW3_0_0 
            rt = new RenderTexture(Screen.width, Screen.height,0);
#else
            rt = new RenderTexture(256, 256, 0);
#endif


            buf.Blit(blitTo, rt);

            CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_TickEvent;

            helper = CameraTransform.gameObject.AddComponent<CommandBufferHelper>();
            helper.Initialize(rt, CameraComponent, OnHelperPostRender);
        }
    }

    private void CognitiveVR_Manager_TickEvent()
    {
        if (helper == null) //if there's a scene change and camera is destroyed, replace helper
        {
            helper = CameraTransform.gameObject.AddComponent<CommandBufferHelper>();
            helper.Initialize(rt, CameraComponent, OnHelperPostRender);
        }

        Vector3 viewport = GetViewportGazePoint();
        viewport.z = 100;
        var viewportray = CameraComponent.ViewportPointToRay(viewport);

        helper.Begin(GetViewportGazePoint(), viewportray);
    }

    void OnHelperPostRender(Ray ray, Vector3 gazepoint)
    {
        Vector3 gpsloc = new Vector3();
        float compass = 0;
        Vector3 floorPos = new Vector3();

        GetOptionalSnapshotData(ref gpsloc, ref compass, ref floorPos);

        float hitDistance;
        DynamicObject hitDynamic;
        Vector3 hitWorld;
        Vector2 hitcoord;
        string ObjectId = "";
        Vector3 LocalGaze = Vector3.zero;
        
        if (DynamicRaycast(ray.origin, ray.direction, CameraComponent.farClipPlane, 0.05f, out hitDistance, out hitDynamic, out hitWorld, out hitcoord)) //hit dynamic
        {
            ObjectId = hitDynamic.Id;
            LocalGaze = hitDynamic.transform.InverseTransformPointUnscaled(hitWorld);
        }

        float depthDistance = Vector3.Distance(CameraTransform.position, gazepoint);

        if (hitDistance > 0 && hitDistance < depthDistance)
        {
            hitDynamic.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);

            var mediacomponent = hitDynamic.GetComponent<MediaComponent>();
            if (mediacomponent != null)
            {
                var mediatime = mediacomponent.IsVideo ? (int)((mediacomponent.VideoPlayer.frame / mediacomponent.VideoPlayer.frameRate) * 1000) : 0;
                var mediauvs = hitcoord;
                GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, LocalGaze, CameraTransform.position, CameraTransform.rotation, gpsloc, compass, mediacomponent.MediaSource, mediatime, mediauvs, floorPos);
            }
            else
            {
                GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, LocalGaze, CameraTransform.position, CameraTransform.rotation, gpsloc, compass, floorPos);
            }
            Debug.DrawLine(CameraTransform.position, hitWorld, Color.magenta, 1);
            return;
        }

        if (gazepoint.magnitude > CameraComponent.farClipPlane * 0.99f) //compare to farplane. skybox
        {
            Vector3 pos = CameraTransform.position;
            Quaternion rot = CameraTransform.rotation;
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos, rot, gpsloc, compass, floorPos);
            Debug.DrawRay(pos, CameraTransform.forward * CameraComponent.farClipPlane, Color.cyan, 1);
        }
        else
        {
            Vector3 pos = CameraTransform.position;
            Quaternion rot = CameraTransform.rotation;

            //hit world
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos+gazepoint, pos, rot, gpsloc, compass, floorPos);
            Debug.DrawLine(pos, pos + gazepoint, Color.red, 1);
            LastGazePoint = pos + gazepoint;
        }
    }

    /*private void OnDrawGizmos()
    {
        UnityEditor.Handles.BeginGUI();
        GUI.Label(new Rect(0, 0, 128, 128), rt);
        UnityEditor.Handles.EndGUI();
    }*/

    private void OnDestroy()
    {
        Destroy(helper);
        CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
        CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
    }
}
}