using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LoadScene : MonoBehaviour
{
    [FormerlySerializedAs("single")]
    public LoadSceneMode loadMode = LoadSceneMode.Single;
    public int sceneIndex;

    public bool unloadScene;
    public int unloadSceneIndex;

    public bool awakeLoad;

    private void Awake()
    {
        if (awakeLoad)
        {
            Load();
        }

        GetComponent<Button>()?.onClick.AddListener(() =>
        {
            Load();
        });
    }

    private void Load()
    {
        if (unloadScene && loadMode == LoadSceneMode.Additive)
        {
            var oper = SceneManager.UnloadSceneAsync(unloadSceneIndex);

            oper.completed += (aop) => {
                SceneManager.LoadSceneAsync(sceneIndex, loadMode);
            };

            return;
        }

        SceneManager.LoadSceneAsync(sceneIndex, loadMode);
    }
}
