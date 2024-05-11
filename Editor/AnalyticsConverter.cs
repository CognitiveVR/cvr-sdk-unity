using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Cognitive3D
{
    public class AnalyticsConverter : EditorWindow
    {
        // Use a dictionary to store the events and their file paths and line numbers.
        private readonly Dictionary<string, (string FilePath, int LineNumber)> unityEvents =
            new Dictionary<string, (string FilePath, int LineNumber)>();

        private readonly List<string> foundEvents = new List<string>();
        private Vector2 scrollPos;

        [MenuItem("Cognitive3D/AnalyticsConverter")]
        public static void ShowWindow()
        {
            GetWindow<AnalyticsConverter>("Analytics Converter");
        }

        private string selectedEvent;
        private string generatedEvent;

        private void OnGUI()
        {
            GUILayout.Label("Found Unity Analytics Events", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(400), GUILayout.Height(300));

            if (foundEvents.Count == 0)
            {
                GUILayout.Label("None Found", EditorStyles.boldLabel);
            }

            for (int i = 0; i < foundEvents.Count; i++)
            {
                if (GUILayout.Button(foundEvents[i]))
                {
                    // Set the selected event and its generated string.
                    selectedEvent = foundEvents[i];
                    generatedEvent = CreateCognitive3DEvent(selectedEvent);
                }
            }

            EditorGUILayout.EndScrollView();

            // If there is a selected event, display a text area with the generated string and two new buttons.
            if (selectedEvent != null)
            {
                EditorGUILayout.LabelField("Generated Cognitive3D event for the selected Unity Analytics event:");
                generatedEvent = EditorGUILayout.TextArea(generatedEvent, GUILayout.Width(400), GUILayout.Height(100));

                if (GUILayout.Button("Replace Original Event"))
                {
                    ReplaceUnityEvent(selectedEvent, generatedEvent);
                    // Clear the selected event.
                    selectedEvent = null;
                }

                if (GUILayout.Button("Insert Underneath Original Event"))
                {
                    InsertUnityEvent(selectedEvent, generatedEvent);
                    // Clear the selected event.
                    selectedEvent = null;
                }

                if (GUILayout.Button("Open Script"))
                {
                    OpenScript(selectedEvent);
                }
            }

            if (GUILayout.Button("Scan Scripts"))
            {
                ScanScripts();
            }
        }

        private void ScanScripts()
        {
            foundEvents.Clear();
            unityEvents.Clear();
            // Get all C# script files in the project.
            string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            Debug.Log("Scanning " + files.Length + " files");
            // Create a regex to match the Unity Analytics events.
            Regex regex = new Regex(@"Analytics\.CustomEvent\(.+\);");

            // Define the path of the script to exclude.
            // Replace "Path/To/YourScript.cs" with the actual path of your script relative to the Application.dataPath directory.
            string excludedScript = Path.Combine(Application.dataPath, "AnalyticsConverter.cs");

            // Iterate over the files.
            foreach (string file in files)
            {
                // Check if the file is the script to exclude.
                if (file != excludedScript)
                {
                    // Read all lines from the file.
                    string[] lines = File.ReadAllLines(file);

                    // Iterate over the lines.
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();

                        // Check if the line is commented out.
                        if (line.StartsWith("//"))
                        {
                            // If the line is commented out, skip it.
                            continue;
                        }

                        // Check if the line contains a Unity Analytics event.
                        Match match = regex.Match(line);
                        if (match.Success)
                        {
                            // If it does, store the event, file path, and line number in the dictionary.
                            unityEvents[match.Value] = (file, i + 1);
                            foundEvents.Add(match.Value);
                        }
                    }
                }
            }
        }

        private string CreateCognitive3DEvent(string unityEvent)
        {
            // Remove the Analytics.CustomEvent( part and the trailing );
            string trimmedEvent = unityEvent.Replace("Analytics.CustomEvent(", "").TrimEnd(' ', ';', ')');

            // The event name is now the first parameter before the comma or the end of the string if there's no comma.
            int eventNameEndIndex = trimmedEvent.IndexOf(',') > -1 ? trimmedEvent.IndexOf(',') : trimmedEvent.Length;
            string eventName = trimmedEvent.Substring(0, eventNameEndIndex).Trim(' ', '\"');

            // Create the Cognitive3D event string.
            string cognitiveEvent = $"new Cognitive3D.CustomEvent(\"{eventName}\")";

            // Check for parameters or properties.
            if (eventNameEndIndex < trimmedEvent.Length - 1)
            {
                // Get the rest of the parts, assuming they're parameters or properties.
                string rest = trimmedEvent.Substring(eventNameEndIndex + 1).Trim(' ', ')');
                string[] parts = rest.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Iterate over the rest of the parts.
                for (int i = 0; i < parts.Length; i++)
                {
                    // Trim the part to remove leading and trailing whitespace.
                    string part = parts[i].Trim();

                    // Check if the part is a dictionary, indicating it's a property.
                    if (part.StartsWith("new Dictionary"))
                    {
                        // Handle properties as before.
                        // ... (continue as in the previous version of the function)
                    }
                    else
                    {
                        // If the part is not a dictionary, assume it's a parameter and add it to the Cognitive3D event string.
                        if (part.StartsWith("new Vector3"))
                        {
                            // This is a special case where we assume that Vector3 always comes as a single unit.
                            string vector3Param = string.Join(",", parts.Skip(i).Take(3).ToArray());
                            cognitiveEvent += $".Send({vector3Param})";
                            i += 2;  // Skip next two parts as they are part of the Vector3.
                        }
                        else
                        {
                            cognitiveEvent += $".Send({part})";
                        }
                    }
                }
            }
            else
            {
                // If there are no parameters or properties, just add the Send() method.
                cognitiveEvent += ".Send()";
            }

            // Add the final semicolon to the Cognitive3D event string.
            cognitiveEvent += ";";

            // This is where you could write the Cognitive3D event string to a new file.
            // For the purposes of this example, we'll just print it to the console.
            Debug.Log(cognitiveEvent);
            return cognitiveEvent;
        }

        public void ReplaceUnityEvent(string unityEvent, string cognitiveEvent)
        {
            if (unityEvents.TryGetValue(unityEvent, out var fileInfo))
            {
                string filePath = fileInfo.FilePath;
                int lineNumber = fileInfo.LineNumber;

                string[] lines = File.ReadAllLines(filePath);
                lines[lineNumber - 1] = lines[lineNumber - 1].Replace(unityEvent, cognitiveEvent);
                File.WriteAllLines(filePath, lines);
            }
            else
            {
                Debug.LogError("Unity Analytics event not found in the dictionary.");
            }
        }

        public void InsertUnityEvent(string unityEvent, string cognitiveEvent)
        {
            if (unityEvents.TryGetValue(unityEvent, out var fileInfo))
            {
                string filePath = fileInfo.FilePath;
                int lineNumber = fileInfo.LineNumber;

                string[] lines = File.ReadAllLines(filePath);
                List<string> newLines = new List<string>(lines);

                // Use a regular expression to match the leading whitespace on the line.
                Match match = Regex.Match(lines[lineNumber - 1], @"^\s*");
                if (match.Success)
                {
                    // If a match was found, prepend the whitespace to the new event line.
                    cognitiveEvent = match.Value + cognitiveEvent;
                }

                newLines.Insert(lineNumber, cognitiveEvent);
                File.WriteAllLines(filePath, newLines.ToArray());
            }
            else
            {
                Debug.LogError("Unity Analytics event not found in the dictionary.");
            }
        }

        public void OpenScript(string unityEvent)
        {
            if (unityEvents.TryGetValue(unityEvent, out var fileInfo))
            {
                string filePath = fileInfo.FilePath;

                // Open the file with its default application.
                System.Diagnostics.Process.Start(filePath);
            }
            else
            {
                Debug.LogError("Unity Analytics event not found in the dictionary.");
            }
        }
    }

}
