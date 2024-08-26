using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Xml;
using UnityEditor.Android;

// This adds Wi-Fi android permissions for Cognitive3D plugin
namespace Cognitive3D
{
    public class AndroidManifestPermission : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Path to the AndroidManifest.xml file in the generated project
            string manifestPath = path + "/src/main/AndroidManifest.xml";

            // Load the AndroidManifest.xml file
            XmlDocument manifestDoc = new XmlDocument();
            manifestDoc.Load(manifestPath);

            // Get the manifest root element
            XmlElement manifestRoot = manifestDoc.DocumentElement;

            // Define the Android XML namespace
            string androidNamespaceURI = manifestRoot.GetAttribute("xmlns:android");

            // Ensure the uses-permission elements are added
            AddPermission(manifestDoc, manifestRoot, androidNamespaceURI, "android.permission.ACCESS_WIFI_STATE");
            AddPermission(manifestDoc, manifestRoot, androidNamespaceURI, "android.permission.ACCESS_NETWORK_STATE");

            // Save the modified AndroidManifest.xml file
            manifestDoc.Save(manifestPath);
        }

        private void AddPermission(XmlDocument doc, XmlElement manifestRoot, string androidNamespaceURI, string permissionName)
        {
            XmlNodeList permissionNodes = manifestRoot.GetElementsByTagName("uses-permission");
            foreach (XmlElement permissionNode in permissionNodes)
            {
                if (permissionNode.GetAttribute("android:name") == permissionName)
                {
                    return; // Permission already exists
                }
            }

            // Create a new uses-permission element and add it to the manifest
            XmlElement permissionElement = doc.CreateElement("uses-permission");
            permissionElement.SetAttribute("name", androidNamespaceURI, permissionName);
            manifestRoot.AppendChild(permissionElement);
        }

    }
}
