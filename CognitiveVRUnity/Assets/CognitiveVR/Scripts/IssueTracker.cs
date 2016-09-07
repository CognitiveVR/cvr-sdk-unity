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
        //Input keys
        KeyCode ConsoleKey = KeyCode.BackQuote;
        KeyCode EscapeKey = KeyCode.Escape;
        KeyCode SendKey = KeyCode.Return;
        char SendChar = '\n';
        bool SendShiftModifier = true;



        //console display
        string _title = "";
        string _description = "";
        string _repro = "";
        bool _consoleOpen;
        bool _consoleKeyDown;


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
            _commonIssues.Add(new CommonIssue("Collision", "Object collision doesn't act as expected", ""));
            _commonIssues.Add(new CommonIssue("Typo", "There is a spelling or grammar error in text", ""));
            _commonIssues.Add(new CommonIssue("Navmesh", "AI Pathfinding does not work as expected", ""));
            _commonIssues.Add(new CommonIssue("Lighting", "Lighting is not baked on an object", ""));
        }

        void OnGUI()
        {
            Event e = Event.current;

            if (e.keyCode == ConsoleKey && e.type == EventType.KeyDown && !_consoleKeyDown)
            {
                _consoleKeyDown = true;
                return;
            }

            if (e.keyCode == ConsoleKey && e.type == EventType.keyUp)
            {
                _consoleKeyDown = false;

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

            if (e.keyCode == EscapeKey && e.type == EventType.KeyDown)
            {
                ClearConsole(false);
                return;
            }

            //display text fields
            if (_consoleOpen && !_consoleKeyDown)
            {
                float width = Mathf.Max(200, Screen.width / 4);

                if ((e.character == SendChar || e.keyCode == SendKey) && e.type == EventType.KeyDown)
                {
                    if (e.shift && SendShiftModifier || !SendShiftModifier)
                    {
                        ClearConsole(true);
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

                GUILayout.Box("<size=10><color=red>" + EscapeKey.ToString() + " to cancel          </color></size>" + "<size=10><color=white>" + (SendShiftModifier ? "Shift + " : "") + SendKey.ToString() + " to submit</color></size>");

                GUI.Label(new Rect(Input.mousePosition.x+20,Screen.height - Input.mousePosition.y+40, 240, 80), GUI.tooltip);

                if (string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
                {
                    GUI.FocusControl("title");
                }
            }
        }

        void ClearConsole(bool send)
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

            //TODO integrate with JIRA rest api
        }

        public static string GetDescription()
        {
            return "Enable a console that can send user-created notes to SceneExplorer";
        }
    }
}