using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class Unit_ComponentAuthoring : MonoBehaviour
{
    public float speed;
    public bool debugPath;
    public float minDistanceReached;
    public Transform targetPos;
}

public class Unit_ComponentAuthoringBaker : Baker<Unit_ComponentAuthoring>
{
    public override void Bake(Unit_ComponentAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Unit_Component
        {
            speed = authoring.speed,
            minDistanceReached = authoring.minDistanceReached,
            fromLocation = authoring.transform.position,
            toLocation = authoring.targetPos.position,
            currentBufferIndex = 0,
        });

        if (authoring.debugPath)
        {
            AddComponent(entity, new DebugPathfindingComponent());
        }

        AddBuffer<Unit_Buffer>(entity);
    }
}

public struct DebugPathfindingComponent : IComponentData
{
    
}

public struct Unit_Component : IComponentData
{
    public float3 toLocation;
    public float3 fromLocation;
    public NavMeshLocation nml_FromLocation;
    public NavMeshLocation nml_ToLocation;
    public bool routed;
    public bool reached;
    public bool usingCachedPath;
    //Movement
    public float3 waypointDirection;
    public float speed;
    public float minDistanceReached;
    public int currentBufferIndex;
}

public partial class DebugPathSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var bufferHandle = GetBufferTypeHandle<Unit_Buffer>(isReadOnly: true);
        var myBufferElementQuery = SystemAPI.QueryBuilder().WithAll<DebugPathfindingComponent, Unit_Buffer>().Build();
        var chunks = myBufferElementQuery.ToArchetypeChunkArray(Allocator.Temp);

        
        foreach (var chunk in chunks)
        {
            var numEntities = chunk.Count;
            var bufferAccessor = chunk.GetBufferAccessor(ref bufferHandle);

            for (int j = 0; j < numEntities; j++)
            {
                var dynamicBuffer = bufferAccessor[j];


                

                
                for (int i = 0; i < dynamicBuffer.Length - 1; i++)
                {
                    var point1 = dynamicBuffer[i].wayPoints;
                    var point2 = dynamicBuffer[i + 1].wayPoints;

                    Debug.DrawLine(point1, point2, Color.green);
                }
                
            }
        }

        chunks.Dispose();


    }
}