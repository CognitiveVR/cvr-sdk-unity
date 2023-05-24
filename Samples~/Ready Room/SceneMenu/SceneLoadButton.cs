using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//same focus logic as virtual button
//should ignore the OnFill UnityEvent
//instead calls SceneManager.LoadScene based on the scene that was passed to this button
namespace Cognitive3D.ReadyRoom
{
    public class SceneLoadButton : VirtualButton, ISceneInfoHolder
    {
        public Image SceneImage;
        public Text SceneName;
        SceneInfo sceneInfo;
        System.Action<SceneInfo> Callback;

        //holds scene data and display names/images to represent scene
        public void ApplySceneInfo(SceneInfo info)
        {
            sceneInfo = info;
            if (sceneInfo.Icon != null)
                SceneImage.sprite = sceneInfo.Icon;
            SceneName.text = sceneInfo.DisplayName;
        }

        public void SetSelectCallback(System.Action<SceneInfo> callback)
        {
            Callback = callback;
        }

        //loads the scene after the button is filled
        protected override IEnumerator FilledEvent()
        {
            yield return base.FilledEvent();
            Debug.Log("Load Scene: " + sceneInfo.ScenePath);
            if (Callback != null)
                Callback.Invoke(sceneInfo);
        }
    }
}