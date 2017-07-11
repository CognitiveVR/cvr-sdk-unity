using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_6_OR_NEWER
using UnityEngine.Video;
#endif

public class videoPlayerControls : MonoBehaviour {

    #if UNITY_5_6_OR_NEWER
    public VideoPlayer Player;

	// Use this for initialization
	void Start () {
        Debug.Log("support seeking" + Player.canSetTime);
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Player.Stop();
            Player.GetComponent<CognitiveVR.DynamicObject>().SendVideoTime();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (Player.isPlaying)
            {
                Player.Pause();
            }
            else
            {
                Player.Play();
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Player.frame -= (long)(Player.frameRate);

            Player.GetComponent<CognitiveVR.DynamicObject>().SendVideoTime();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Player.frame += (long)(Player.frameRate);
            Player.GetComponent<CognitiveVR.DynamicObject>().SendVideoTime();
        }

        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            Player.playbackSpeed = 0.25f;
            Player.GetComponent<CognitiveVR.DynamicObject>().SendVideoTime().SetProperty("videospeed", 0.25f);
        }
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            Player.playbackSpeed = 1f;
            Player.GetComponent<CognitiveVR.DynamicObject>().SendVideoTime().SetProperty("videospeed", 1f);
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            Player.playbackSpeed = 2f;
            Player.GetComponent<CognitiveVR.DynamicObject>().SendVideoTime().SetProperty("videospeed", 2f);
        }
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            Player.playbackSpeed = 3f;
            Player.GetComponent<CognitiveVR.DynamicObject>().SendVideoTime().SetProperty("videospeed", 3f);
        }
    }
    #endif
}
