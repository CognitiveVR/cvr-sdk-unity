using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// this is meant to be used by QA or developers to drop notes in scene explorer
/// this uses OnGUI - which can impact performance! this could be rolled into your own existing console
/// </summary>

namespace CognitiveVR
{
    public class IssueTracker : CognitiveVRAnalyticsComponent
    {
        string _title = "";
        string _description = "";
        string _repro = "";
        bool _consoleOpen;
        bool _backquotedown;
        List<CommonIssue> _commonIssues = new List<CommonIssue>();

        private class CommonIssue
        {
            public string Title;
            public string Desc;
            public string Repro;

            public CommonIssue(string title, string desc, string repro)
            {
                Title = title;
                Desc = desc;
                Repro = repro;
            }
        }

        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            _commonIssues.Add(new CommonIssue("Collision", "Object collision doesn't act as expected\nOr Object collision is missing", ""));
            _commonIssues.Add(new CommonIssue("Typo", "There is a spelling or grammar error in text", ""));
            _commonIssues.Add(new CommonIssue("Navmesh", "AI Pathfinding does not work as expected through this point", ""));
            _commonIssues.Add(new CommonIssue("Lighting", "Lighting is not baked on an object", ""));
        }

        void OnGUI()
        {
            Event e = Event.current;

            if (e.keyCode == KeyCode.BackQuote && e.type == EventType.KeyDown && !_backquotedown)
            {
                _backquotedown = true;
                return;
            }

            if (e.keyCode == KeyCode.BackQuote && e.type == EventType.keyUp)
            {
                _backquotedown = false;

                if (_consoleOpen)
                {
                    //CloseConsole(true);
                    _consoleOpen = false;
                }
                else
                {
                    _consoleOpen = true;
                }
                return;
            }

            if (e.keyCode == KeyCode.Escape && e.type == EventType.KeyDown)
            {
                CloseConsole(false);
                return;
            }

            //display text fields
            if (_consoleOpen && !_backquotedown)
            {
                float width = Mathf.Max(200, Screen.width / 4);

                if (e.character == '\n' && e.type == EventType.KeyDown)
                {
                    if (e.shift)
                    {
                        CloseConsole(true);
                        return;
                    }
                }

                GUILayout.BeginHorizontal();
                for (int i = 0; i<_commonIssues.Count; i++)
                {
                    if (GUILayout.Button(new GUIContent(_commonIssues[i].Title,_commonIssues[i].Desc)))
                    {
                        SendIssue(_commonIssues[i].Title, _commonIssues[i].Desc, _commonIssues[i].Repro);
                    }
                }
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Label("Title");
                GUI.SetNextControlName("title");
                _title = GUILayout.TextField(_title, GUILayout.Width(width));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Desc");
                GUI.SetNextControlName("desc");
                _description = GUILayout.TextArea(_description, GUILayout.Width(width), GUILayout.Height(50));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Repro");
                GUI.SetNextControlName("repro");
                _repro = GUILayout.TextArea(_repro, GUILayout.Width(width), GUILayout.Height(50));
                GUILayout.EndHorizontal();
                GUI.color = Color.green;
                GUILayout.Label("Shift + Return to submit");
                GUI.color = Color.red;
                GUILayout.Label("Escape to cancel");
                GUI.color = Color.white;

                GUI.Label(new Rect(Input.mousePosition.x+20,Screen.height - Input.mousePosition.y+40, 240, 80), GUI.tooltip);

                if (string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
                {
                    GUI.FocusControl("title");
                }
            }
        }

        void CloseConsole(bool send)
        {
            if (send)
            {
                SendIssue(_title,_description,_repro);
            }
            _consoleOpen = false;
            _title = string.Empty;
            _description = string.Empty;
            _repro = string.Empty;
        }

        public void SendIssue(string title, string description = null, string repro = null)
        {
            if (string.IsNullOrEmpty(title)) { return; }

            Transaction t = Instrumentation.Transaction("Issue").setProperty("Title", title);
            if (!string.IsNullOrEmpty(description)) { t.setProperty("Description", description); }
            if (!string.IsNullOrEmpty(repro)) { t.setProperty("Reproduction", repro); }
            t.beginAndEnd();

            //TODO integration with JIRA
        }

        public static string GetDescription()
        {
            return "Enable a console that can send user-created notes to SceneExplorer";
        }
    }
}