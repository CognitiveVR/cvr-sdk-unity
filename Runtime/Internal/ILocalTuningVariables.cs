using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace Cognitive3D
{
    internal interface ILocalTuningVariables
    {
        bool GetTuningVariables(out string text);
        void WriteTuningVariables(string text);
    }

    internal class TuningVariablesLocalDataHandler : ILocalTuningVariables
    {
        string localTuningVariablePath;

        public TuningVariablesLocalDataHandler(string path)
        {
            localTuningVariablePath = path;
        }

        public bool GetTuningVariables(out string text)
        {
            try
            {
                if (File.Exists(localTuningVariablePath + "tuningVariables"))
                {
                    text = File.ReadAllText(localTuningVariablePath + "tuningVariables");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            text = "";
            return false;
        }

        public void WriteTuningVariables(string text)
        {
            if (!Directory.Exists(localTuningVariablePath))
                Directory.CreateDirectory(localTuningVariablePath);

            try
            {
                File.WriteAllText(localTuningVariablePath + "tuningVariables", text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
