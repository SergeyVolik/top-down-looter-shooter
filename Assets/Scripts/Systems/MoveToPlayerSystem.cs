using ProjectDawn.Navigation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    [UpdateAfter(typeof(LocalToWorldSystem))]
    public partial class MoveToPlayerSystem : SystemBase
    {

        protected override void OnCreate()
        {
            base.OnCreate();

        }

        protected override void OnUpdate()
        {
          
            if (SystemAPI.TryGetSingletonEntity<PlayerComponent>(out var player))
            {
                var ltwLookUp = SystemAPI.GetComponentLookup<LocalToWorld>();
                float3 playerPos = default;
                if (ltwLookUp.TryGetComponent(player, out var ltw))
                {
                    playerPos = ltw.Position;
                }
                Entities.ForEach((ref Entity e, ref AgentBody agent, ref AgentSteering stearing, in MoveToPlayerComponent moveToTarget) =>
                {
  
                    
                    agent.SetDestination(playerPos);
                    stearing.StoppingDistance = moveToTarget.stopDistance;

                }).Schedule();
            }
        }

    }
}