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

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        
        if (SystemAPI.TryGetSingletonBuffer<PatrolPointsComponent>(out var points))
        {
            SystemAPI.TryGetSingletonEntity<PatrolPointsComponent>(out var bufEntity);
            var Lookup = SystemAPI.GetComponentLookup<UpdateNavigationTarget>();
            var rndLookUp = SystemAPI.GetComponentLookup<IndividualRandomComponent>();
            foreach (var (partolData, agent, e) in SystemAPI.Query<RefRW<PatrolStateComponent>, RefRO<AgentMovement>>().WithEntityAccess())
            {
                if (agent.ValueRO.reached)
                {
                    var nextPointIndex = partolData.ValueRO.partolIndex;
                    //nextPointIndex++;

                    var rnd = rndLookUp.GetRefRW(bufEntity).ValueRO.Value;
                    //if (points.Length <= nextPointIndex)
                    //{
                    //    nextPointIndex = 0;
                    //}

                    //if (!partolData.ValueRO.rndExecuted)
                    //{
                        partolData.ValueRW.rndExecuted = true;
                        nextPointIndex = rnd.NextInt(0, points.Length - 1);
                    //}
                    rndLookUp.GetRefRW(bufEntity).ValueRW.Value = rnd;
                    Lookup.SetComponentEnabled(e, true);
                    
                    Lookup.GetRefRW(e).ValueRW.Position = points[nextPointIndex].position;
                    partolData.ValueRW.partolIndex = nextPointIndex;
                }
            }
        }
    }

}