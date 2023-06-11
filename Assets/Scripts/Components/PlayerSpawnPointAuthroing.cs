using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public class PlayerSpawnPointAuthroing : MonoBehaviour
    {
        public Transform spawnPoint;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    public struct SpawnPlayerOnPointComponent : IComponentData
    {
        public Entity spawnPointEntity;
    }

    public struct SpawnPlayerComponent : IComponentData
    {
       
    }

    public class PlayerSpawnPointBaker : Baker<PlayerSpawnPointAuthroing>
    {
        public override void Bake(PlayerSpawnPointAuthroing authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpawnPlayerOnPointComponent
            {
                spawnPointEntity = GetEntity(authoring.spawnPoint, TransformUsageFlags.Dynamic)
            });
            AddComponent(entity, new SpawnPlayerComponent
            {
                
            });
        }
    }


    public partial class PlayerSpawnSystem : SystemBase
    {
        private EntityQuery playerQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<GamePrefabsComponent>();
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<SpawnPlayerOnPointComponent, SpawnPlayerComponent>().Build());

            playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerComponent, HealthComponent>().Build();
        }

        protected override void OnUpdate()
        {
          
            var gamePrefabs = SystemAPI.GetSingleton<GamePrefabsComponent>();
          

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

            ecb.DestroyEntity(playerQuery);

            var componentLookUp = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true);

            foreach (var (spawnPoint, spAction, e) in SystemAPI.Query<SpawnPlayerOnPointComponent, SpawnPlayerComponent>().WithEntityAccess())
            {
                var playerEntity = ecb.Instantiate(gamePrefabs.playerPrefab);


                var spawnPointltw = componentLookUp.GetRefRO(spawnPoint.spawnPointEntity).ValueRO;

                ecb.SetComponent(playerEntity, LocalTransform.FromPosition(spawnPointltw.Position));

                ecb.RemoveComponent<SpawnPlayerComponent>(e);
            }

      






        }
    }



}
