using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadUnityScene : MonoBehaviour
{
    public int sceneIndex;
    public LoadSceneMode mode;

    private void Awake()
    {

        SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);

    }



}
