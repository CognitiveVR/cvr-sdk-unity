using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace Cognitive3D
{
    internal interface ILocalAppVariables
    {
        bool GetAppVariables(out string text);
        void WriteAppVariables(string text);
    }

    internal class AppVariablesLocalDataHandler : ILocalAppVariables
    {
        readonly string localAppVariablePath;

        public AppVariablesLocalDataHandler(string path)
        {
            localAppVariablePath = path;
        }

        public bool GetAppVariables(out string text)
        {
            try
            {
                if (File.Exists(localAppVariablePath + "AppVariables"))
                {
                    text = File.ReadAllText(localAppVariablePath + "AppVariables");
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

        public void WriteAppVariables(string text)
        {
            if (!Directory.Exists(localAppVariablePath))
                Directory.CreateDirectory(localAppVariablePath);

            try
            {
                File.WriteAllText(localAppVariablePath + "AppVariables", text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
