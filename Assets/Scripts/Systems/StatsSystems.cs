using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SV.ECS
{

    public struct KilledEnemies : IComponentData
    {
        public int value;
    }


    public struct DeadStatUpdatedComponent : IComponentData
    {
        
    }
    public partial struct InitStatsSystem : ISystem
    {

        public void OnCreate(ref SystemState state)
        {
            //state.Enabled = false;
            var entity = state.EntityManager.CreateEntity();

            state.EntityManager.AddComponent<KilledEnemies>(entity);
            state.EntityManager.SetName(entity, new Unity.Collections.FixedString64Bytes("Stats"));
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            var killedEnemies = SystemAPI.GetSingleton<KilledEnemies>();


            foreach (var (dc, ec, e) in SystemAPI.Query<DeadComponent, EnemyComponent>().WithNone<DeadStatUpdatedComponent>().WithEntityAccess())
            {
                killedEnemies.value += 1;
                ecb.AddComponent<DeadStatUpdatedComponent>(e);
            }

            SystemAPI.SetSingleton(killedEnemies);
        }
    }
}