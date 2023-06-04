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

    public struct SpawnPlayerComponent : IComponentData
    {
       
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
            AddComponent(entity, new SpawnPlayerComponent
            {
                
            });
        }
    }


    public partial class PlayerSpawnSystem : SystemBase
    {
     

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<GamePrefabsComponent>();
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlayerSpawnPointComponent, SpawnPlayerComponent>().Build());
      
          
        }

        protected override void OnUpdate()
        {
          
            var gamePrefabs = SystemAPI.GetSingleton<GamePrefabsComponent>();
          

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

            foreach (var (spawnPoint, spAction, e) in SystemAPI.Query<PlayerSpawnPointComponent, SpawnPlayerComponent>().WithEntityAccess())
            {
                var playerEntity = ecb.Instantiate(gamePrefabs.playerPrefab);


                var spawnPointltw = EntityManager.GetComponentData<LocalToWorld>(spawnPoint.spawnPointEntity);

                ecb.SetComponent(playerEntity, LocalTransform.FromPosition(spawnPointltw.Position));

                ecb.RemoveComponent<SpawnPlayerComponent>(e);
            }
    


        

            

        }
    }



}
