using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LoadSubSceneOnClick : MonoBehaviour
{
    public SceneSO scene;


    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            

            var e = em.CreateEntity();
            em.AddComponentData(e, new LoadSubScene
            {
                value = new Unity.Entities.Hash128(scene.sceneGuid)
            });
        });
    }



}
