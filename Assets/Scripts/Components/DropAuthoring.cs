using Mono.CSharp;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public class DropAuthoring : MonoBehaviour
    {
        public DropInfo[] dropInfo;

        public float delayBetweenDrops;
        public Transform dropSpawnPoint;
        public bool instantDrop;
        public float dropForce;
    }

    [System.Serializable]
    public class DropInfo
    {
        public GameObject prefab;
        public int number;
    }

    public struct DropDataBuffElement : IBufferElementData
    {
        public Entity prefab;
        public int number;
    }

    public struct DropSettingComponents : IComponentData
    {
        public float delayBetweenDrops;
        public Entity dropSpawnEntity;
        public bool instantDrop;
        public float dropForce;

    }
    public struct ExecuteDropProcessComponent : IComponentData, IEnableableComponent
    {
        public int currentDropIndex;
        public float nextDropTime;
        public int currentItem;

    }
    public struct DropExecutedComponent : IComponentData, IEnableableComponent
    {
    }

    public static class ECBExt
    {
        public static void DisableWithChilds(this ref EntityCommandBuffer ecb, Entity e, ref BufferLookup<Child> childs)
        {
            ecb.AddComponent<Disabled>(e);

            if (childs.TryGetBuffer(e, out var childsData))
            {

                for (int i = 0; i < childsData.Length; i++)
                {
                    DisableWithChilds(ref ecb, childsData[i].Value, ref childs);
                }
            }
        }
    }
    public class DropAuthoringBaker : Baker<DropAuthoring>
    {
        public override void Bake(DropAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var buffer = AddBuffer<DropDataBuffElement>(entity);
            AddComponent<ExecuteDropProcessComponent>(entity);
            SetComponentEnabled<ExecuteDropProcessComponent>(entity, false);
            AddComponent<DropExecutedComponent>(entity);
            SetComponentEnabled<DropExecutedComponent>(entity, false);
            AddComponent(entity, new DropSettingComponents
            {
                delayBetweenDrops = authoring.delayBetweenDrops,
                dropSpawnEntity = GetEntity(authoring.dropSpawnPoint, TransformUsageFlags.Dynamic),
                instantDrop = authoring.instantDrop,
                dropForce = authoring.dropForce
            });

            for (int i = 0; i < authoring.dropInfo.Length; i++)
            {
                var dropInfo = authoring.dropInfo[i];
                buffer.Add(new DropDataBuffElement
                {
                    number = dropInfo.number,
                    prefab = GetEntity(dropInfo.prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public partial struct DropExecuterSystem : ISystem
    {

        public partial struct ExecuteDropJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public ComponentLookup<LocalToWorld> wtlLookup;

            public void Execute(DynamicBuffer<DropDataBuffElement> dropItems, ref ExecuteDropProcessComponent exeteState, ref DropSettingComponents DropSettings, Entity entity)
            {

                ecb.SetComponentEnabled<ExecuteDropProcessComponent>(entity, false);
                ecb.SetComponentEnabled<DropExecutedComponent>(entity, true);


                var spawnPos = wtlLookup.GetRefRW(DropSettings.dropSpawnEntity).ValueRW.Position;

                if (DropSettings.instantDrop)
                {

                    for (int i = 0; i < dropItems.Length; i++)
                    {
                        var dropInfo = dropItems[i];


                        for (int j = 0; j < dropInfo.number; j++)
                        {
                            var droppedEntity = ecb.Instantiate(dropItems[exeteState.currentDropIndex].prefab);

                            //ecb.SetComponent(droppedEntity, new PhysicsVelocity
                            //{
                            //    Linear = DropSettings.dropForce
                            //});

                            ecb.SetComponent(droppedEntity, new LocalTransform
                            {
                                Position = spawnPos,
                                Rotation = quaternion.identity,
                                Scale = 1f
                            });
                        }

                    }

                }

                ecb.DestroyEntity(entity);
            }
        }
        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)(state.WorldUnmanaged.Time.ElapsedTime);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var wtlLookup = SystemAPI.GetComponentLookup<LocalToWorld>();

            foreach (var (dropItems, DropSettings, entity) in SystemAPI.Query<DeadComponent, DropSettingComponents>().WithNone<ExecuteDropProcessComponent, DropExecutedComponent>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<ExecuteDropProcessComponent>(entity, true);
                Debug.Log("ExecuteDropProcessComponent");
            }

            var job = new ExecuteDropJob
            {
                ecb = ecb,
                wtlLookup = wtlLookup,
            };

            state.Dependency = job.Schedule(state.Dependency);

        }
    }

}
