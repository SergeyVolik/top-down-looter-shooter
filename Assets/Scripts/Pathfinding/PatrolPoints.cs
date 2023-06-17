using ProjectDawn.Navigation;
using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PatrolPoints : MonoBehaviour
{
    public Transform[] points;

}

public struct PatrolPointsComponent : IBufferElementData
{
    public float3 position;
}

public class PatrolPointsBaker : Baker<PatrolPoints>
{
    public override void Bake(PatrolPoints authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        var buffer = AddBuffer<PatrolPointsComponent>(entity);

        if (authoring.points != null)
        {
            foreach (var item in authoring.points)
            {
                var pos = item.position;
                buffer.Add(new PatrolPointsComponent
                {
                    position = new float3(pos.x, pos.y, pos.z)
                });
            }
        }

    }
}


public partial struct PartolSystem : ISystem
{


    public void OnCreate(ref SystemState state)
    {

    }
    uint seedOffset;
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        const int count = 200;
        seedOffset += count;
        var newSeedOffset = seedOffset;
        if (SystemAPI.TryGetSingletonBuffer<PatrolPointsComponent>(out var points))
        {          

            var rnd = Unity.Mathematics.Random.CreateFromIndex(newSeedOffset);

            foreach (var (partolData, agent, e) in SystemAPI.Query<RefRW<PatrolStateComponent>, RefRW<AgentBody>>().WithEntityAccess())
            {
                if (agent.ValueRO.RemainingDistance < 2f || agent.ValueRO.IsStopped == true)
                {
                  
                    var nextPointIndex = partolData.ValueRO.partolIndex;
                    //nextPointIndex++;

                 
                    //if (points.Length <= nextPointIndex)
                    //{
                    //    nextPointIndex = 0;
                    //}

                    //if (!partolData.ValueRO.rndExecuted)
                    //{
                        partolData.ValueRW.rndExecuted = true;
                        nextPointIndex = rnd.NextInt(0, points.Length - 1);
                    //}
                  

                    agent.ValueRW.Destination = points[nextPointIndex].position;
                    agent.ValueRW.IsStopped = false;
                    partolData.ValueRW.partolIndex = nextPointIndex;
                }
            }
        }
    }

}