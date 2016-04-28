using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using System.Collections;
using System.IO;
using System;

using System.Text.RegularExpressions;

public class CognitiveVRPostProcessBuild
{
	[PostProcessBuild]
	public static void AddCognitiveVRFramework(BuildTarget target, string path)
	{
		#if UNITY_IPHONE
		if(target != BuildTarget.iPhone)
		{
			Debug.LogWarning("Trying to build " + target.ToString() + " during an iPhone build?  Skipping Post-process");
			return;
		}

		string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
		string frameworkPath = Application.dataPath + "/Plugins/iOS/CognitiveVR.framework";
		
		// try to use relative paths, in case folks want to share a pbxproj
		//  technique adapted from: http://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
		Uri fpath = new Uri(frameworkPath);
		Uri ppath = new Uri(path + "/Unity-iPhone.xcodeproj");
		Uri rpath = ppath.MakeRelativeUri(fpath);
		if(null != rpath)
			frameworkPath = Uri.UnescapeDataString(rpath.ToString());

		// read in the file, for processing
		string[] lines = System.IO.File.ReadAllLines(projPath);

		// if the project already includes CognitiveVR.framework, we don't need to do anything
		if(null != Array.Find(lines, delegate(string line) { return line.Contains("CognitiveVR.framework"); }))
			return;

		Debug.Log("CognitiveVR.framework - not found, adding to PBXPROJ");

		// Each section starts with comment: Begin <name> section
		string sectionBeginMatch = "Begin (.*) section";
		// Each section ends with comment: End <name> section
		string sectionEndMatch = "End (.*) section";

		string section = "";
		bool inFrameworks = false;
		bool inBuildSettings = false;
		bool inFrameworkSearchPath = false;
		bool addedSearch = false;

		using(StreamWriter proj = new StreamWriter(projPath, false))
		{
			foreach(string line in lines)
			{
				// build configuration changes are based on the END of a block, so these must be processed BEFORE printing the current line
				if(section == "XCBuildConfiguration")
				{
					if(line.Trim().StartsWith("FRAMEWORK_SEARCH_PATHS"))
					{
						inFrameworkSearchPath = true;
					}
					if(line.Trim().StartsWith("buildSettings"))
					{
						addedSearch = false;
						inBuildSettings = true;
					}
					if(inBuildSettings && line.Trim().StartsWith("}"))
					{
						if(!addedSearch)
						{
							Debug.Log("CognitiveVR.framework - adding FULL framework search path");
							proj.WriteLine("                FRAMEWORK_SEARCH_PATHS = (");
							proj.WriteLine("                    \"$(inherited)\",");
							proj.WriteLine("                    \"\\\"$(SRCROOT)/" + Path.GetDirectoryName(frameworkPath) + "\\\"\",");
							proj.WriteLine("                );");
						}
						inBuildSettings = false;
						addedSearch = true;
					}
					if(inFrameworkSearchPath && line.Trim().StartsWith(")"))
					{
						Debug.Log("CognitiveVR.framework - adding framework search path");
						proj.WriteLine("                    \"\\\"$(SRCROOT)/" + Path.GetDirectoryName(frameworkPath) + "\\\"\",");
						inFrameworkSearchPath = false;
						addedSearch = true;
					}
				}

				proj.WriteLine(line);

				Match match = Regex.Match(line, sectionBeginMatch);
				if(null != match && match.Success)
				{
					section = match.Groups[1].Value;

					// at the beginning of a couple of sections, we need to patch in framework references
					switch(section)
					{
					case "PBXBuildFile":
						proj.WriteLine("		3F474F21187DCB8E0047C5E2 /* CognitiveVR.framework in Frameworks */ = {isa = PBXBuildFile; fileRef = 3F474F1F187DCB200047C5E2 /* CognitiveVR.framework */; };");
						Debug.Log("CognitiveVR.framework - adding build file");
						break;
						
					case "PBXFileReference":
						proj.WriteLine("		3F474F1F187DCB200047C5E2 /* CognitiveVR.framework */ = {isa = PBXFileReference; lastKnownFileType = wrapper.framework; name = CognitiveVR.framework; path = \"" + frameworkPath + "\"; sourceTree = \"<group>\"; };");
						Debug.Log("CognitiveVR.framework - adding file reference");
						break;
					}
				}

				match = Regex.Match(line, sectionEndMatch);
				if(null != match && match.Success)
				{
					section = "";
				}

				switch(section)
				{
				case "PBXFrameworksBuildPhase":
					if(line.Trim().StartsWith("files"))
					{
						proj.WriteLine("				3F474F21187DCB8E0047C5E2 /* CognitiveVR.framework in Frameworks */,");
						Debug.Log("CognitiveVR.framework - adding to build phase");
					}
					break;
				case "PBXGroup":
					if(line.Contains("/* Frameworks */"))
					{
						inFrameworks = true;
					}
					if(line.Trim().StartsWith("}"))
					{
						inFrameworks = false;
					}

					if(inFrameworks && line.Trim().StartsWith("children"))
					{
						proj.WriteLine("				3F474F1F187DCB200047C5E2 /* CognitiveVR.framework */,");
						Debug.Log("CognitiveVR.framework - adding to frameworks group");
					}
					break;
				}
			}
			proj.Close();
		}
		Debug.Log("CognitiveVR.framework - done adding framework to PBXPROJ");
		#endif
	}
}