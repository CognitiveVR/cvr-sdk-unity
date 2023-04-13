using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace Cognitive3D
{
    internal interface ILocalExitpoll
    {
        bool GetExitpoll(string hookname, out string text);
        void WriteExitpoll(string hookname, string text);
    }

    internal class ExitPollLocalDataHandler : ILocalExitpoll
    {
        string localExitPollPath;

        public ExitPollLocalDataHandler(string path)
        {
            localExitPollPath = path;
        }

        public bool GetExitpoll(string hookname, out string text)
        {
            try
            {
                if (File.Exists(localExitPollPath + hookname))
                {
                    text = File.ReadAllText(localExitPollPath + hookname);
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

        public void WriteExitpoll(string hookname, string text)
        {
            if (!Directory.Exists(localExitPollPath))
                Directory.CreateDirectory(localExitPollPath);

            try
            {
                File.WriteAllText(localExitPollPath + hookname, text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
