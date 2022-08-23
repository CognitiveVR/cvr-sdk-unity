using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cognitive3D
{
public class Builder 
{
	static string[] Cognitive3D_files = new string[] {
		"Assets/Cognitive3D",
	};

	[MenuItem("Dist/Export Cognitive3D Package")]
	static void MakeCognitive3DPackage()
	{
        //preferences should not be exported
        //TODO search through nested folders to find Cognitive3D_Preferences
        AssetDatabase.DeleteAsset("Assets/Cognitive3D/Resources/Cognitive3D_Preferences.asset");

        System.IO.Directory.CreateDirectory(Application.dataPath + "/../../dist");

        string sdkversion = Cognitive3D.Core.SDK_VERSION.Replace('.', '_');

		AssetDatabase.ExportPackage(Cognitive3D_files, "../dist/Cognitive3D_"+ sdkversion + ".unitypackage", ExportPackageOptions.Recurse);
	}
}
}