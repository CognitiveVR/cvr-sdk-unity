using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    public interface IGazeRecorder
    {
        CircularBuffer<ThreadGazePoint> DisplayGazePoints { get; }

        void Initialize();
    }
}
