using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace Cognitive3D
{
    internal interface ILocalRemoteControls
    {
        bool GetRemoteControls(out string text);
        void WriteRemoteControls(string text);
    }

    internal class RemoteControlsLocalDataHandler : ILocalRemoteControls
    {
        readonly string localRemoteControlPath;

        public RemoteControlsLocalDataHandler(string path)
        {
            localRemoteControlPath = path;
        }

        public bool GetRemoteControls(out string text)
        {
            try
            {
                if (File.Exists(localRemoteControlPath + "RemoteControls"))
                {
                    text = File.ReadAllText(localRemoteControlPath + "RemoteControls");
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

        public void WriteRemoteControls(string text)
        {
            if (!Directory.Exists(localRemoteControlPath))
                Directory.CreateDirectory(localRemoteControlPath);

            try
            {
                File.WriteAllText(localRemoteControlPath + "RemoteControls", text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
