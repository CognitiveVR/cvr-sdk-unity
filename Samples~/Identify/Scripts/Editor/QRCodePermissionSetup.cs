using UnityEditor;
using UnityEditor.Android;
using System.Xml;

namespace Cognitive3D.Auth
{
    /// <summary>
    /// Automatically adds the camera permissions to AndroidManifest.xml on build.
    ///
    /// Two permissions are required because different headsets gate the camera differently:
    ///   - "horizonos.permission.HEADSET_CAMERA" REQUIRED for the Meta Quest 3/3S/Pro
    ///     passthrough camera (Horizon OS v74+) to appear as a WebCamDevice.
    ///   - "android.permission.CAMERA" used by PICO, AndroidXR, and generic Android devices.
    /// </summary>
    public class QRCodePermissionSetup : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 2; // Run after the SDK's permission setup (order 1)

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = path + "/src/main/AndroidManifest.xml";

            XmlDocument doc = new XmlDocument();
            doc.Load(manifestPath);

            XmlElement root = doc.DocumentElement;
            string ns = root.GetAttribute("xmlns:android");

            // Meta Quest passthrough camera (Horizon OS v74+)
            AddPermissionIfMissing(doc, root, ns, "horizonos.permission.HEADSET_CAMERA");
            // PICO / generic Android camera
            AddPermissionIfMissing(doc, root, ns, "android.permission.CAMERA");

            doc.Save(manifestPath);
        }

        private static void AddPermissionIfMissing(XmlDocument doc, XmlElement root, string ns, string permission)
        {
            XmlNodeList existing = root.GetElementsByTagName("uses-permission");
            foreach (XmlElement node in existing)
            {
                if (node.GetAttribute("android:name") == permission)
                    return;
            }

            XmlElement element = doc.CreateElement("uses-permission");
            element.SetAttribute("name", ns, permission);
            root.AppendChild(element);
        }
    }
}
