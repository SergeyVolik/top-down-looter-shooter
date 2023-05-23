using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct AgentPathBuffer : IBufferElementData
{
    public float3 wayPoint;
}

public struct AgentPathValidityBuffer : IBufferElementData
{
    public bool isPathInvalid;
}

public struct Agent : IComponentData
{
    public float3 toLocation;
    public bool pathCalculated;
    public bool usingGlobalRelativeLoction;
    public float elapsedSinceLastPathCalculation;
    public int pathFindingQueryIndex;
    public bool pathFindingQueryDisposed;
}

public struct AgentMovement : IComponentData
{
    public int currentBufferIndex;
    public bool reached;
    public float3 waypointDirection;
}

public class AgentAuthoring : MonoBehaviour
{
    
}

public class AgentBaker : Baker<AgentAuthoring>
{
    public override void Bake(AgentAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new Agent
        {
            
        });
        AddComponent(entity, new AgentMovement
        {
            currentBufferIndex = 0
        });
        AddBuffer<AgentPathBuffer>(entity);
        AddBuffer<AgentPathValidityBuffer>(entity);
    }
}
