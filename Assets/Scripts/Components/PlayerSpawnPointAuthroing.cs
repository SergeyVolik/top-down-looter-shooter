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

    public struct PlayerSpawnPointComponent : IComponentData
    {
        public Entity spawnPointEntity;
    }

    public class PlayerSpawnPointBaker : Baker<PlayerSpawnPointAuthroing>
    {
        public override void Bake(PlayerSpawnPointAuthroing authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerSpawnPointComponent
            {
                spawnPointEntity = GetEntity(authoring.spawnPoint, TransformUsageFlags.Dynamic)
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
            RequireForUpdate<PlayerSpawnPointComponent>();

            playerQuery = GetEntityQuery(typeof(PlayerComponent));
        }

        protected override void OnUpdate()
        {
            if (playerQuery.CalculateEntityCount() != 0)
                return;


            var gamePrefabs = SystemAPI.GetSingleton<GamePrefabsComponent>();
            var spawnPoint = SystemAPI.GetSingleton<PlayerSpawnPointComponent>();

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);


            var playerEntity = ecb.Instantiate(gamePrefabs.playerPrefab);


            var spawnPointltw = EntityManager.GetComponentData<LocalToWorld>(spawnPoint.spawnPointEntity);

            ecb.SetComponent(playerEntity, LocalTransform.FromPosition(spawnPointltw.Position));

            Enabled = false;

        }
    }



}
