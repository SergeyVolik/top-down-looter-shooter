using Sirenix.OdinInspector;
using SV.ECS;
using Unity.Entities;
using Unity.Entities.Content;
#if UNITY_EDITOR
#endif
using UnityEngine;

[CreateAssetMenu]
public class SceneSO : ScriptableObject
{

    public WeakObjectSceneReference scene;
   


    [Button]
    public void LoadScene()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entity = em.CreateEntity();

        em.AddComponentData(entity, new LoadScene
        {
            scene = scene

        });

        Debug.Log("Load scene");
    }

    [Button]
    public void UnloadScene()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        var entity = em.CreateEntity();
        em.AddComponentData(entity, new UnloadScene
        {

             scene = scene
        });

        Debug.Log("Unload scene");

    }



}

