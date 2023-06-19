using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using UnityEngine;

namespace SV.ECS
{
    public class GameScenesAuthroing : MonoBehaviour
    {

        public SceneSO scene;

        public class Baker : Baker<GameScenesAuthroing>
        {
            public override void Bake(GameScenesAuthroing authoring)
            {

                Debug.Log("Scene WeakObjectSceneReferenceData Created");
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new WeakObjectSceneReferenceData
                {

                    weakSceneRef = authoring.scene.scene,


                });
                
               
            }
        }

    }

    public struct LoadScene : IComponentData
    {
        internal WeakObjectSceneReference scene;
    }

    public struct UnloadScene : IComponentData
    {
        internal WeakObjectSceneReference scene;
    }



    public struct WeakObjectSceneReferenceData : IComponentData
    {

        public WeakObjectSceneReference weakSceneRef;
        public UnityEngine.SceneManagement.Scene sceneInstance;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct LoadSceneFromWeakObjectReferenceSystem : ISystem
    {
       
        public void OnCreate(ref SystemState state)
        {

            
        }
        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state)
        {

            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<LoadScene>().Build(ref state);
            var query1 = new EntityQueryBuilder(Allocator.Temp).WithAll<UnloadScene>().Build(ref state);

            

            foreach (var sceneData in SystemAPI.Query<RefRO<LoadScene>>())
            {
                foreach (var weakScenes in SystemAPI.Query<RefRW<WeakObjectSceneReferenceData>>())
                {
                    if (sceneData.ValueRO.scene.Equals(weakScenes.ValueRO.weakSceneRef))
                    {
                        var sceneInstance = weakScenes.ValueRW.weakSceneRef.LoadAsync(new Unity.Loading.ContentSceneParameters()
                        {
                            loadSceneMode = UnityEngine.SceneManagement.LoadSceneMode.Additive
                        });

                        weakScenes.ValueRW.sceneInstance = sceneInstance;
                    }
                }
            }

            foreach (var sceneData in SystemAPI.Query<RefRW<WeakObjectSceneReferenceData>>().WithAll<UnloadScene>())
            {
                Debug.Log("Unload Scene");


                sceneData.ValueRW.weakSceneRef.Unload(ref sceneData.ValueRW.sceneInstance);
             

            }

            state.EntityManager.DestroyEntity(query);
            state.EntityManager.DestroyEntity(query1);
        }
    }











}
