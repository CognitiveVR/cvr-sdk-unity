using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//spawns button prefabs in a semi-circle around the player
//swap out button for different fill/activate actions, including different methods of loading scenes

namespace CognitiveVR
{
    public class SceneSelectMenu : AssessmentBase
    {
        [Tooltip("The button prefab to spawn for each scene")]
        public GameObject SceneButtonPrefab;
        public float Height = 2;
        public float SpawnRadius = 3;
        public float ArcSize = 1f;

        public List<SceneInfo> SceneInfos;
        [Tooltip("Destroy these gameobjects before the selected scene is loaded")]
        public List<GameObject> DestroyOnSceneChange = new List<GameObject>();
        [Tooltip("Automatically start a session when this scene changes")]
        public bool StartSessionOnSceneChange = true;

        //calls GetPositions and spawns SceneButtonPrefabs at each position
        public override void BeginAssessment()
        {
            base.BeginAssessment();
            var positions = GetPositions(SceneInfos.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                SpawnSceneInfoButton(SceneInfos[i], positions[i]);
            }
        }

        //spawns the SceneButtonPrefabs and adds SceneInfo to each button
        //these prefabs hold all the logic needed to change scenes
        private void SpawnSceneInfoButton(SceneInfo sceneInfo, Vector3 position)
        {
            Vector3 horizontalPosition = position;
            horizontalPosition.y = 0;
            var button = Instantiate(SceneButtonPrefab, position, Quaternion.LookRotation(horizontalPosition), transform);
            var infoHolder = button.GetComponent<ISceneInfoHolder>();
            if (infoHolder != null)
            {
                infoHolder.ApplySceneInfo(sceneInfo);
                infoHolder.SetSelectCallback(SelectSceneCallback);
            }
        }

        //returns a list of positions to spawn SceneButtonPrefab at
        public List<Vector3> GetPositions(int count)
        {
            List<Vector3> positions = new List<Vector3>();
            for (float i = 1f / count / 2; i < 1; i += 1f / count)
            {
                float angle = Mathf.Lerp(Mathf.PI / 2 + ArcSize, Mathf.PI / 2 - ArcSize, i);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * SpawnRadius, Height, Mathf.Sin(angle) * SpawnRadius);
                positions.Add(pos);
            }
            return positions;
        }

        void SelectSceneCallback(SceneInfo info)
        {
            //TODO some VR SDKs put objects into DontDestroyOnLoad that might cause issues with changing scenes in your project
            //you should customize this scene change function to suit your needs

            foreach (var go in DestroyOnSceneChange)
            {
                if (go != null)
                    Destroy(go);
            }
            if (StartSessionOnSceneChange)
                CognitiveVR_Manager.Instance.Initialize();
            SceneManager.LoadScene(info.ScenePath);
        }

        //this should never be called. instead, the SceneButton prefab the user selects will load the next scene
        public override void CompleteAssessment()
        {
            //base.CompleteAssessment();
        }
    }
}