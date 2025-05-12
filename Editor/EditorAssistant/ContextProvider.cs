using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Codice.Client.Common;

namespace Cognitive3D
{
    internal static class ContextProvider
    {
        static string _docsPath = "Packages/com.cognitive3d.c3d-sdk/Editor/EditorAssistant/Docs";
        static string _folderPath = Path.Combine(Directory.GetCurrentDirectory(), _docsPath);

        static string _userPromptPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages/com.cognitive3d.c3d-sdk/Editor/EditorAssistant/UserPrompt.txt");
        static string _troubleshootPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages/com.cognitive3d.c3d-sdk/Editor/EditorAssistant/Docs/troubleshoot.txt");

        /// <summary>
        /// Full wrapper: Takes a prompt and searches a folder for matching file names.
        /// </summary>
        public static string SearchFilesByPrompt(string prompt)
        {
            var keywords = ExtractKeywords(prompt);
            var files = FindFilesWithKeywords(_folderPath, keywords);
            var updatedPrompt = UpdateUserPrompt(files, prompt);
            return updatedPrompt;
        }

        /// <summary>
        /// Extracts keywords from a user prompt (simple whitespace tokenization and filtering).
        /// </summary>
        private static List<string> ExtractKeywords(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return new List<string>();

            return prompt.Split(new[] { ' ', '.', ',', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(word => word.ToLowerInvariant())
                        .Where(word => word.Length > 2) // filter out small/common words
                        .Distinct()
                        .ToList();
        }

        /// <summary>
        /// Searches for files in the given folder path that contain any of the given keywords in the file name.
        /// </summary>
        private static List<string> FindFilesWithKeywords(string folderPath, List<string> keywords)
        {
            if (!Directory.Exists(folderPath) || keywords == null || keywords.Count == 0)
                return new List<string>();

            string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                        .Where(f => !f.EndsWith(".meta"))
                                        .ToArray();

            return allFiles.Where(file =>
            {
                string fileName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                return keywords.Any(kw => fileName.Contains(kw));
            }).ToList();
        }

        private static string UpdateUserPrompt(List<string> relatedDocs, string userMessage)
        {
            string prompt = "";
            if (File.Exists(_userPromptPath))
            {
                string context = "";
                foreach (string doc in relatedDocs)
                {
                    context += File.ReadAllText(doc);
                }

                prompt = File.ReadAllText(_userPromptPath);
                prompt = prompt.Replace("{context}", context);
                prompt = prompt.Replace("{troubleshoot}", File.ReadAllText(_troubleshootPath));
                prompt = prompt.Replace("{userprompt}", userMessage);
            }
            return prompt;
        }
    }
}
