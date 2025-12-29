using Cognitive3D;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// a tool for organizing data collected from the SDK in real time to be PULLED into other tools (like ML models)
/// TODO examples of pushing data
///     1. sending requests to a local server
///     2. a C# CSV logging tool to a file
/// TODO warnings and limits for memory usage (if a cache gets too large)
/// </summary>


/// To use:
/// add this script anywhere in the scene
/// In the inspector, choose which types of data you want to record
/// start your application as usual with Cognitive3D
/// read data from gazeData, fixationData, sensorData, etc
/// call ClearCaches() after you've read your data! otherwise the cache sizes will include duplicate data

public class DataConnector : MonoBehaviour
{
    #region serialization helpers
    //classes for formating and serializing the data (to json, csv, etc)
    [Serializable]
    public class GazeData
    {
        public double timestamp;
        public string objectid;
        public Vector3 gazepoint;
        public Vector3 hmdpoint;
        public Quaternion hmdrotation;
        public bool hitSky;

        public GazeData(double timestamp, string objectid, Vector3 gazepoint,bool hitSky, Vector3 hmdpoint, Quaternion hmdrotation)
        {
            this.timestamp = timestamp;
            this.objectid = objectid;
            this.gazepoint = gazepoint;
            this.hitSky = hitSky;
            this.hmdpoint = hmdpoint;
            this.hmdrotation = hmdrotation;
        }
        public static string CSVHeader()
        {
            var sb = new StringBuilder();
            sb.Append("timestamp");
            sb.Append(",");
            sb.Append("objectid");
            sb.Append(",");
            sb.Append("gazepoint");
            sb.Append(",");
            sb.Append("hmdpoint");
            sb.Append(",");
            sb.Append("hmdrotation");
            sb.Append(",");
            sb.Append("hitSky");
            return sb.ToString();
        }
        public string ToCSV()
        {
            var sb = new StringBuilder();
            sb.Append(timestamp);
            sb.Append(",");
            sb.Append(objectid);
            sb.Append(",");
            sb.Append(gazepoint);
            sb.Append(",");
            sb.Append(hmdpoint);
            sb.Append(",");
            sb.Append(hmdrotation);
            sb.Append(",");
            sb.Append(hitSky);
            return sb.ToString();
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class FixationData
    {
        public double timestamp;
        public long DurationMs;
        public bool IsLocal;
        public string DynamicObjectId;
        public Vector3 WorldPosition;
        public Vector3 LocalPosition;
        public FixationData(double timestamp, long DurationMs, bool IsLocal, string DynamicObjectId, Vector3 WorldPosition, Vector3 LocalPosition)
        {
            this.timestamp = timestamp;
            this.DurationMs = DurationMs;
            this.IsLocal = IsLocal;
            this.DynamicObjectId = DynamicObjectId;
            this.WorldPosition = WorldPosition;
            this.LocalPosition = LocalPosition;
        }
        public static string CSVHeader()
        {
            var sb = new StringBuilder();
            sb.Append("timestamp");
            sb.Append(",");
            sb.Append("DurationMs");
            sb.Append(",");
            sb.Append("IsLocal");
            sb.Append(",");
            sb.Append("DynamicObjectId");
            sb.Append(",");
            sb.Append("WorldPosition");
            sb.Append(",");
            sb.Append("LocalPosition");
            return sb.ToString();
        }
        public string ToCSV()
        {
            var sb = new StringBuilder();
            sb.Append(timestamp);
            sb.Append(",");
            sb.Append(DurationMs);
            sb.Append(",");
            sb.Append(IsLocal);
            sb.Append(",");
            sb.Append(DynamicObjectId);
            sb.Append(",");
            sb.Append(WorldPosition);
            sb.Append(",");
            sb.Append(LocalPosition);
            return sb.ToString();
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class EyeData
    {
        public FixationRecorder.EyeDataType type;
        public Vector3 hmdPos;
        public Vector3 worldPoint;
        public string hitDynamicId;
        public Vector3 localPoint;
        public Vector2 screenPos;
        public Vector2 viewportPos;
        public double timestamp;

        public EyeData(FixationRecorder.EyeDataType type, Vector3 hmdPos, Vector3 worldPoint, string hitDynamicId, Vector3 localPoint, Vector2 screenPos, Vector2 viewportPos, double timestamp)
        {
            this.type = type;
            this.hmdPos = hmdPos;
            this.worldPoint = worldPoint;
            this.hitDynamicId = hitDynamicId;
            this.localPoint = localPoint;
            this.screenPos = screenPos;
            this.viewportPos = viewportPos;
            this.timestamp = timestamp;
        }
        public static string CSVHeader()
        {
            var sb = new StringBuilder();
            sb.Append("timestamp");
            sb.Append(",");
            sb.Append("type");
            sb.Append(",");
            sb.Append("hmdPos");
            sb.Append(",");
            sb.Append("worldPoint");
            sb.Append(",");
            sb.Append("hitDynamicId");
            sb.Append(",");
            sb.Append("localPoint");
            sb.Append(",");
            sb.Append("screenPos");
            sb.Append(",");
            sb.Append("viewportPos");
            return sb.ToString();
        }
        public string ToCSV()
        {
            var sb = new StringBuilder();
            sb.Append(timestamp);
            sb.Append(",");
            sb.Append(type);
            sb.Append(",");
            sb.Append(hmdPos);
            sb.Append(",");
            sb.Append(worldPoint);
            sb.Append(",");
            sb.Append(hitDynamicId);
            sb.Append(",");
            sb.Append(localPoint);
            sb.Append(",");
            sb.Append(screenPos);
            sb.Append(",");
            sb.Append(viewportPos);
            return sb.ToString();
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }


    [Serializable]
    public class EventData
    {
        public string name;
        public Vector3 pos;
        public string dynamicObjectId;
        public double timestamp;
        public int propertyCount;
        public KeyValuePair<string, object>[] keyValuePairs;

        public EventData(string name, Vector3 pos, string dynamicObjectId, double timestamp, int propertyCount, KeyValuePair<string, object>[] keyValuePairs)
        {
            this.name = name;
            this.pos = pos;
            this.dynamicObjectId = dynamicObjectId;
            this.timestamp = timestamp;
            this.propertyCount = propertyCount;
            this.keyValuePairs = keyValuePairs;
        }
        public static string CSVHeader()
        {
            var sb = new StringBuilder();
            sb.Append("timestamp");
            sb.Append(",");
            sb.Append("name");
            sb.Append(",");
            sb.Append("pos");
            sb.Append(",");
            sb.Append("dynamicObjectId");
            sb.Append(",");
            sb.Append("propertyCount");
            sb.Append(",");
            sb.Append("keyValuePairs");
            return sb.ToString();
        }
        public string ToCSV()
        {
            var sb = new StringBuilder();
            sb.Append(timestamp);
            sb.Append(",");
            sb.Append(name);
            sb.Append(",");
            sb.Append(pos);
            sb.Append(",");
            sb.Append(dynamicObjectId);
            sb.Append(",");
            sb.Append(propertyCount);
            sb.Append(",");
            sb.Append(keyValuePairs);
            return sb.ToString();
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class DynamicData
    {
        public string name;
        public string mesh;
        public double timestamp;
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;

        public DynamicData(string name, string mesh, double time, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            this.name = name;
            this.mesh = mesh;
            this.timestamp = time;
            this.pos = pos;
            this.rot = rot;
            this.scale = scale;
        }
        public static string CSVHeader()
        {
            var sb = new StringBuilder();
            sb.Append("timestamp");
            sb.Append(",");
            sb.Append("name");
            sb.Append(",");
            sb.Append("mesh");
            sb.Append(",");
            sb.Append("pos");
            sb.Append(",");
            sb.Append("rot");
            sb.Append(",");
            sb.Append("scale");
            return sb.ToString();
        }
        public string ToCSV()
        {
            var sb = new StringBuilder();
            sb.Append(timestamp);
            sb.Append(",");
            sb.Append(name);
            sb.Append(",");
            sb.Append(mesh);
            sb.Append(",");
            sb.Append(pos);
            sb.Append(",");
            sb.Append(rot);
            sb.Append(",");
            sb.Append(scale);
            return sb.ToString();
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class SensorData
    {
        public string sensorName;
        public float sensorValue;
        public double timestamp;

        public SensorData(string sensorName, float sensorValue, double time)
        {
            this.sensorName = sensorName;
            this.sensorValue = sensorValue;
            this.timestamp = time;
        }
        public static string CSVHeader()
        {
            var sb = new StringBuilder();
            sb.Append("timestamp");
            sb.Append(",");
            sb.Append("sensorName");
            sb.Append(",");
            sb.Append("sensorValue");
            return sb.ToString();
        }
        public string ToCSV()
        {
            var sb = new StringBuilder();
            sb.Append(timestamp);
            sb.Append(",");
            sb.Append(sensorName);
            sb.Append(",");
            sb.Append(sensorValue);
            return sb.ToString();
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class BoundaryData
    {
        public double timestamp;
        public Vector3 pos;
        public Quaternion rot;
        public int pointCount;
        public Vector3[] points;

        public BoundaryData(double timestamp, Vector3 pos, Quaternion rot, int pointCount, Vector3[] points)
        {
            this.timestamp = timestamp;
            this.pos = pos;
            this.rot = rot;
            this.pointCount = pointCount;
            this.points = points;
        }
        public static string CSVHeader()
        {
            var sb = new StringBuilder();
            sb.Append("timestamp");
            sb.Append(",");
            sb.Append("pos");
            sb.Append(",");
            sb.Append("rot");
            sb.Append(",");
            sb.Append("pointCount");
            sb.Append(",");
            sb.Append("points");
            return sb.ToString();
        }
        public string ToCSV()
        {
            var sb = new StringBuilder();
            sb.Append(timestamp);
            sb.Append(",");
            sb.Append(pos);
            sb.Append(",");
            sb.Append(rot);
            sb.Append(",");
            sb.Append(pointCount);
            sb.Append(",");
            sb.Append(points);
            return sb.ToString();
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    #endregion

    [HideInInspector]
    public List<GazeData> gazeData = new List<GazeData>();
    [HideInInspector]
    public List<FixationData> fixationData = new List<FixationData>();
    [HideInInspector]
    public List<EyeData> eyeData = new List<EyeData>();
    [HideInInspector]
    public List<EventData> customEventData = new List<EventData>();
    [HideInInspector]
    public List<DynamicData> dynamicData = new List<DynamicData>();
    [HideInInspector]
    public List<SensorData> sensorData = new List<SensorData>();
    [HideInInspector]
    public List<BoundaryData> boundaryData = new List<BoundaryData>();

    public bool IncludeGaze;
    public bool IncludeFixation;
    public bool IncludeEyeTracking;
    public bool IncludeEvents;
    public bool IncludeDynamics;
    public bool IncludeSensors;
    public bool IncludeBoundary;

    #region utilities

    public static string SessionID
    {
        get
        {
            return Cognitive3D_Manager.SessionID;
        }
    }

    static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static double CurrentTimestamp()
    {
        TimeSpan span = DateTime.UtcNow - epoch;
        return span.TotalSeconds;
    }

    #endregion

    private void OnEnable()
    {
        BeginListening();
    }

    void BeginListening()
    {
        if (IncludeGaze)
        {
            GazeCore.OnWorldGazeRecord += GazeCore_OnWorldGazeRecord;
            GazeCore.OnDynamicGazeRecord += GazeCore_OnDynamicGazeRecord;
            GazeCore.OnSkyGazeRecord += GazeCore_OnSkyGazeRecord;
        }

        if (IncludeFixation)
        {
            FixationRecorder.OnFixationRecord += FixationRecorder_OnFixationRecord;
        }

        if (IncludeEyeTracking)
        {
            FixationRecorder.OnEyeDataRecorded += FixationRecorder_OnEyeDataRecorded;
        }

        if (IncludeEvents)
        {
            CustomEvent.OnCustomEventRecorded += CustomEvent_OnCustomEventRecorded;
        }

        if (IncludeDynamics)
        {
            DynamicManager.OnDynamicRecorded += DynamicManager_OnDynamicRecorded;
        }

        if (IncludeSensors)
        {
            SensorRecorder.OnNewSensorRecorded += SensorRecorder_OnNewSensorRecorded;
        }

        if (IncludeBoundary)
        {
            Cognitive3D.Components.Boundary.OnBoundaryRecorded += Boundary_OnBoundaryRecorded;
        }
    }

    private void OnDisable()
    {
        EndListening();
    }

    private void OnDestroy()
    {
        EndListening();
    }

    void EndListening()
    {
        GazeCore.OnWorldGazeRecord -= GazeCore_OnWorldGazeRecord;
        GazeCore.OnDynamicGazeRecord -= GazeCore_OnDynamicGazeRecord;
        GazeCore.OnSkyGazeRecord -= GazeCore_OnSkyGazeRecord;

        FixationRecorder.OnFixationRecord -= FixationRecorder_OnFixationRecord;
        FixationRecorder.OnEyeDataRecorded -= FixationRecorder_OnEyeDataRecorded;

        CustomEvent.OnCustomEventRecorded -= CustomEvent_OnCustomEventRecorded;

        DynamicManager.OnDynamicRecorded -= DynamicManager_OnDynamicRecorded;

        SensorRecorder.OnNewSensorRecorded -= SensorRecorder_OnNewSensorRecorded;

        Cognitive3D.Components.Boundary.OnBoundaryRecorded -= Boundary_OnBoundaryRecorded;
    }

    /// <summary>
    /// call this to clear all copied data here after it has been pulled
    /// </summary>
    public void ClearCaches()
    {
        gazeData.Clear();
        fixationData.Clear();
        eyeData.Clear();
        customEventData.Clear();
        dynamicData.Clear();
        sensorData.Clear();
        boundaryData.Clear();
    }

    #region add data points to list caches

    private void GazeCore_OnWorldGazeRecord(double timestamp, string objectid, Vector3 gazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
    {
        gazeData.Add(new GazeData(timestamp, string.Empty, gazepoint, false, hmdpoint, hmdrotation));
    }

    private void GazeCore_OnDynamicGazeRecord(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
    {
        gazeData.Add(new GazeData(timestamp, objectid, localgazepoint, false, hmdpoint, hmdrotation));
    }

    private void GazeCore_OnSkyGazeRecord(double timestamp, string objectid, Vector3 localgazepoint, Vector3 hmdpoint, Quaternion hmdrotation)
    {
        gazeData.Add(new GazeData(timestamp, string.Empty, Vector3.zero, true, hmdpoint, hmdrotation));
    }

    private void FixationRecorder_OnFixationRecord(Fixation fixation)
    {
        fixationData.Add(new FixationData(((double)fixation.StartMs / 1000.0), fixation.DurationMs, fixation.IsLocal, fixation.DynamicObjectId, fixation.WorldPosition, fixation.LocalPosition));
    }

    private void FixationRecorder_OnEyeDataRecorded(FixationRecorder.EyeDataType type, Vector3 start, Vector3 worldPoint, bool isLocal, string hitDynamicId, Vector3 localPoint, Vector2 screenPos, Vector2 viewportPos, double unixTime)
    {
        eyeData.Add(new EyeData(type, start, worldPoint, hitDynamicId, localPoint, screenPos, viewportPos, unixTime));
    }

    private void CustomEvent_OnCustomEventRecorded(string name, Vector3 pos, List<KeyValuePair<string, object>> properties, string dynamicObjectId, double time)
    {
        customEventData.Add(new EventData(name, pos, dynamicObjectId, time, properties != null ? properties.Count : 0, properties != null ? properties.ToArray() : null));
    }

    private void DynamicManager_OnDynamicRecorded(string name, string mesh, double time, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        dynamicData.Add(new DynamicData(name, mesh, time, pos, rot, scale));
    }

    private void SensorRecorder_OnNewSensorRecorded(string sensorName, float sensorValue, double time)
    {
        sensorData.Add(new SensorData(sensorName, sensorValue, time));
    }

    private void Boundary_OnBoundaryRecorded(double time, Vector3 pos, Quaternion rot, Vector3[] points)
    {
        boundaryData.Add(new BoundaryData(time, pos, rot, points.Length, points));
    }

    #endregion
}
