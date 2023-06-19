using Sirenix.OdinInspector;
using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Entities.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu]
public class SceneSO : ScriptableObject
{
#if UNITY_EDITOR
    public SceneAsset sceneAsset;
#endif
    [ReadOnly]
    public SerializableGuid sceneGuid;

    public WeakObjectSceneReference scene;
   

    private void OnValidate()
    {
#if UNITY_EDITOR
        var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(sceneAsset));
    
        Debug.Log($"scene guid {guid.ToString()}");
        sceneGuid = guid.ToString();
#endif
    }

    [Button]
    public void LoadScene()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entity = em.CreateEntity();

        em.AddComponentData(entity, new LoadScene
        {
            scene = scene

        });
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
    }

    [Button]
    private void UpdateGuid()
    {
        OnValidate();
    }

}

