using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    [UpdateAfter(typeof(LocalToWorldSystem))]
    public partial class SpawnerSystem : SystemBase
    {


        protected override void OnCreate()
        {
            base.OnCreate();

        }

        protected override void OnUpdate()
        {
            var delatTime = SystemAPI.Time.DeltaTime;
            var time = SystemAPI.Time.ElapsedTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            Entities.ForEach((ref Entity e, ref EnemySpawnerComponent vel, ref LocalToWorld ltw) =>
            {

                var nextSpawnTime = vel.nextSpawnTime;

                if (time >= nextSpawnTime)
                {
                    var spawnedEntity = ecb.Instantiate(vel.enemyPrefab);
                    nextSpawnTime = (float)time + vel.spawnDelay;

                    vel.nextSpawnTime = nextSpawnTime;

                    ecb.SetComponent(spawnedEntity, new LocalTransform
                    {
                        Position = ltw.Position,
                        Scale = 1f,
                        Rotation = quaternion.identity
                    });
                }

            }).Run();


        }

    }

}