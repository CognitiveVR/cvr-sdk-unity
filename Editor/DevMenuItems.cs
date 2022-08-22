using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DevMenuItems
{
    [MenuItem("Tools/Open Local Cache")]
    static void OpenLocalCacheDirectory()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
    [MenuItem("Tools/Delete Local Cache")]
    static void DeleteLocalCacheDirectory()
    {
        if (System.IO.Directory.Exists(Application.persistentDataPath + "/c3dlocal/"))
        {
            if (EditorUtility.DisplayDialog("Delete Local Cache", "Are you sure?", "Sure", "No"))
            {
                System.IO.Directory.Delete(Application.persistentDataPath + "/c3dlocal/", true);
                Debug.Log("Deleted " + Application.persistentDataPath + "/c3dlocal/");
            }
        }
        else
        {
            Debug.Log("Couldn't find " + Application.persistentDataPath + "/c3dlocal/");
        }
    }
}
