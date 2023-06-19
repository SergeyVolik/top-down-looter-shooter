using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UnloadSubSceneOnClick : MonoBehaviour
{
    public SubSceneSO scene;


    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => {

            scene.UnloadSubScene();


        });
    }



}
