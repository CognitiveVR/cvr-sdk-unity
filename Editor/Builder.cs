using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CognitiveVR
{
public class Builder 
{
	static string[] cognitivevr_files = new string[] {
		"Assets/CognitiveVR",
	};

	[MenuItem("Dist/Export CognitiveVR Package")]
	static void MakeCognitiveVRPackage()
	{
        //preferences should not be exported
        //TODO search through nested folders to find CognitiveVR_Preferences
        AssetDatabase.DeleteAsset("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");

        System.IO.Directory.CreateDirectory(Application.dataPath + "/../../dist");

        string sdkversion = CognitiveVR.Core.SDK_VERSION.Replace('.', '_');

		AssetDatabase.ExportPackage(cognitivevr_files, "../dist/CognitiveVR_"+ sdkversion + ".unitypackage", ExportPackageOptions.Recurse);
	}
}
}