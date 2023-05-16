using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


namespace SV.ECS
{
    [UpdateBefore(typeof(LocalToWorldSystem))]
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
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

        
            
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            Entities.ForEach((ref Entity e, ref EnemySpawnerComponent vel, ref LocalToWorld ltw, ref IndividualRandomComponent random) =>
            {

                var nextSpawnTime = vel.nextSpawnTime;

                if (time >= nextSpawnTime)
                {
                    var spawnedEntity = ecb.Instantiate(vel.enemyPrefab);
                    nextSpawnTime = (float)time + vel.spawnDelay;

                    vel.nextSpawnTime = nextSpawnTime;

                  


                    var pos = ltw.Position;


                    var min = vel.spawnBound.Min;
                    var max = vel.spawnBound.Max;

                    pos += vel.spawnBound.Center;

                    var rnd = random.Value;
                    pos.x += rnd.NextFloat(min.x, max.x);
                    pos.z += rnd.NextFloat(min.z, max.z);

                    random.Value = rnd;

                    ecb.SetComponent(spawnedEntity, new LocalTransform
                    {
                        Position = pos,
                        Scale = 1f,
                        Rotation = quaternion.identity
                    });
                }

            }).Run();


        }

    }

    [UpdateBefore(typeof(LocalToWorldSystem))]
    public partial class SpawnPointSystem : SystemBase
    {


        protected override void OnCreate()
        {
            base.OnCreate();
           

        }

        protected override void OnUpdate()
        {

            var time = SystemAPI.Time.ElapsedTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();


            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            Entities.ForEach((ref Entity e, in SpawnPointComponent vel) =>
            {

                ecb.AddComponent(e, new SpawnTimeComponent
                {
                    spawnTime = (float)time + vel.spawnDelay
                });

            }).WithNone<SpawnTimeComponent>().Run();


            Entities.ForEach((ref Entity e, in SpawnPointComponent vel, in SpawnTimeComponent spawnTime, in LocalToWorld ltw) =>
            {

                if (spawnTime.spawnTime < time)
                {
                    var spawnedEntity = ecb.Instantiate(vel.prefab);



                    ecb.SetComponent(spawnedEntity, new LocalTransform
                    {
                        Position = ltw.Position,
                        Scale = 1f,
                        Rotation = quaternion.identity

                    });
                    ecb.DestroyEntity(e);
                }

            }).Run();


        }

    }

}