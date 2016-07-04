using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Builder 
{
	static string[] cognitivevr_files = new string[] {
		"Assets/Plugins/CognitiveVR.dll",
		"Assets/Plugins/CognitiveVR",
		"Assets/CognitiveVR",
	};

	[MenuItem("CognitiveVR/Export CognitiveVR Package")]
	static void MakeCognitiveVRPackage()
	{
		System.IO.Directory.CreateDirectory(Application.dataPath + "/../../dist");

        string sdkversion = CognitiveVR.Core.SDK_Version.Replace('.', '_');

		AssetDatabase.ExportPackage(cognitivevr_files, "../dist/CognitiveVR_"+ sdkversion + ".unitypackage", ExportPackageOptions.Recurse);
	}
}
