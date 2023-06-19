using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

[CreateAssetMenu]
public class SubSceneSO : ScriptableObject
{


    public EntitySceneReference scene;




    [Button]
    public void LoadSubScene()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        var e = em.CreateEntity();

        em.AddComponent<LoadSubScene>(e);
        em.SetComponentData<LoadSubScene>(e, new LoadSubScene
        {
             value = scene
        });
    }

    [Button]
    public void UnloadSubScene()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        var e = em.CreateEntity();

        em.AddComponent<UnloadSubScene>(e);
        em.SetComponentData(e, new UnloadSubScene
        {
            value = scene
        });
    }


}
