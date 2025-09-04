using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

namespace Cognitive3D
{
    internal class DataUploaderGUI
    {
        private string folderPath = "";

        internal void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorCore.styles.DetailContainer))
            {
                GUILayout.Space(5);
                GUILayout.Label("Upload offline (cached) data by selecting the folder where the session files are stored. This includes both read and write files.", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Target Folder", EditorStyles.boldLabel);

                using (new GUILayout.HorizontalScope())
                {
                    folderPath = EditorGUILayout.TextField(folderPath);

                    if (GUILayout.Button("Browse", GUILayout.Width(70)))
                    {
                        string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            folderPath = selectedPath;
                        }
                    }
                }

                // Check if the folder has required files
                if (!string.IsNullOrEmpty(folderPath) && !HasRequiredDataFiles(folderPath))
                {
                    EditorGUILayout.HelpBox(
                        "The selected folder must contain both 'data_read' and 'data_write' files.",
                        MessageType.Warning
                    );
                }

                GUILayout.Space(5);

                deleteData = EditorGUILayout.ToggleLeft("Delete data from disk after successful upload", deleteData);

                GUILayout.Space(10);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Upload Data", GUILayout.Width(140)))
                    {
                        UploadData(folderPath);
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private bool HasRequiredDataFiles(string path)
        {
            string readFile = System.IO.Path.Combine(path, "data_read");
            string writeFile = System.IO.Path.Combine(path, "data_write");

            return System.IO.File.Exists(readFile) && System.IO.File.Exists(writeFile);
        }
        
        ICache ic;
        bool deleteData;

        private void UploadData(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Please select a valid folder path before uploading.", "OK");
                return;
            }

            if (ic != null)
            {
                Clear();
            }

            ic = new DualFileCache(path + "/");

            try
            {
                if (ic.HasContent())
                {
                    Util.logDebug($"Sending data from: {path}");
                    StartUpload();
                }
                else
                {
                    Util.logWarning("No data in Local Cache to upload!");
                    Clear();
                    EditorUtility.DisplayDialog("No Data", "There's no data to upload in the selected folder.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                Util.logError($"Upload failed: {ex.Message}");
                EditorUtility.DisplayDialog("Upload Failed", $"Something went wrong:\n{ex.Message}", "OK");
            }
        }

        UnityWebRequest uploadRequest;
        int numberOfBatches;
		int attemptedUploads;

        void StartUpload()
        {
            attemptedUploads = 0;
            numberOfBatches = ic.NumberOfBatches();
			EditorApplication.update += Editor_Update;
        }

        void Editor_Update()
        {
            float progress = numberOfBatches > 0 ? (float)attemptedUploads / numberOfBatches : 0f;
            EditorUtility.DisplayProgressBar("Uploading Data", $"Uploading batch {attemptedUploads} of {numberOfBatches}", progress);

			if (uploadRequest == null)
            {
                string destination = string.Empty;
                string content = string.Empty;
                bool sendAsBytes = false;
                if (ic.PeekContent(ref destination, ref content, ref sendAsBytes))
                {
                    if (!string.IsNullOrEmpty(destination) && !string.IsNullOrEmpty(content))
                    {
                        if (!sendAsBytes)
                        {
                            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(content);
                            uploadRequest = UnityWebRequest.Put(destination, bytes);
                            uploadRequest.SetRequestHeader("Content-Type", "application/json");
                        }
                        else
                        {
                            var bytes = System.Convert.FromBase64String(content);
                            uploadRequest = UnityWebRequest.Put(destination, bytes);
                            uploadRequest.SetRequestHeader("Content-Type", "application/octet-stream");
                        }
                        uploadRequest.method = "POST";
                        uploadRequest.SetRequestHeader("Content-Type", "application/json");
                        uploadRequest.SetRequestHeader("X-HTTP-Method-Override", "POST");
                        uploadRequest.SetRequestHeader("Authorization", "APIKEY:DATA " + Cognitive3D_Preferences.Instance.ApplicationKey);
                        uploadRequest.SendWebRequest();
                        attemptedUploads++;

                        if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                            Util.logDevelopment("EDITOR Upload From Cache " + destination + " " + content);
                    }
                }
            }
            else if (uploadRequest.isDone)
            {
                int responseCode = (int)uploadRequest.responseCode;
                var headers = uploadRequest.GetResponseHeaders();
                bool hasRequestTimeHeader = headers != null && headers.ContainsKey("cvr-request-time");

                if (!hasRequestTimeHeader)
                {
                    responseCode = 307;
                }


                if (responseCode == 200)
                {
                    if (deleteData)
                    {
                        ic.PopContent();
                    }
                    else
                    {
                        string destination = string.Empty;
                        string content = string.Empty;
                        bool sendAsBytes = false;
                        if (ic.PeekContent(ref destination, ref content, ref sendAsBytes))
                        {
                            ic.PopContent();
                            ic.WriteContent(destination, content);
                        }
                    }
                }
                else
                {
                    string destination = string.Empty;
                    string content = string.Empty;
                    bool sendAsBytes = false;
                    if (ic.PeekContent(ref destination, ref content, ref sendAsBytes))
                    {
                        ic.PopContent();
                        ic.WriteContent(destination, content);
                    }
                }

                uploadRequest.Dispose();
                uploadRequest = null;

                if (attemptedUploads >= numberOfBatches || !ic.HasContent())
                {
                    Util.logDevelopment("Editor has finished uploading");
                    EditorApplication.update -= Editor_Update;
                    ic.Close();
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Upload Complete", "All data have been uploaded.", "OK");
                }
            }
        }

        void Clear()
        {
            ic.Close();
            ic = null;
        }
    }
}
