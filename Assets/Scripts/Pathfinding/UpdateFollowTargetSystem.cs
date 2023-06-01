using ProjectDawn.Navigation;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;



public partial struct UpdateFollowTargetSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FollowTargetComponent>();
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {


        var time = SystemAPI.Time.ElapsedTime;

      
        var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

        var job = new UpdateFollowJob()
        {
            time = (float)time,          
            ltwLookup = ltwLookup
        };

        state.Dependency = job.Schedule(state.Dependency);
    }


    [BurstCompile]
    public partial struct UpdateFollowJob : IJobEntity
    {
       
        [ReadOnly]
        public ComponentLookup<LocalToWorld> ltwLookup;

        public float time;

        [BurstCompile]
        public void Execute(Entity entity, ref FollowTargetComponent ft, ref AgentBody agent)
        {
            if (time > ft.nextUpdateTime)
            {
                ft.nextUpdateTime = (float)time + ft.updateRate;
                var pos = ltwLookup.GetRefRO(ft.entity).ValueRO.Position;
                agent.IsStopped = false;
                agent.Destination = pos;

            }
        }
    }
}