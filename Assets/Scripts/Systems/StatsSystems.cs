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

    

    public partial struct InitStatsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var entity = state.EntityManager.CreateEntity();

            state.EntityManager.AddComponent<KilledEnemies>(entity);
            state.EntityManager.SetName(entity, new Unity.Collections.FixedString64Bytes("Stats"));
        }
    }
}