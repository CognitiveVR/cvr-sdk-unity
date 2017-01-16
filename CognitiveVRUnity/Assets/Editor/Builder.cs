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

	[MenuItem("Dist/Export CognitiveVR Package")]
	static void MakeCognitiveVRPackage()
	{
        //preferences should not be exported
        AssetDatabase.DeleteAsset("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");

        System.IO.Directory.CreateDirectory(Application.dataPath + "/../../dist");

        string sdkversion = CognitiveVR.Core.SDK_Version.Replace('.', '_');

		AssetDatabase.ExportPackage(cognitivevr_files, "../dist/CognitiveVR_"+ sdkversion + ".unitypackage", ExportPackageOptions.Recurse);
	}
}
