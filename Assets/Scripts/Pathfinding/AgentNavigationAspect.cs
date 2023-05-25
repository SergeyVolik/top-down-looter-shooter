using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using System.Diagnostics;

public readonly partial struct AgentNavigationAspect : IAspect
{
    public readonly RefRW<Agent> agent;
    public readonly RefRW<AgentMovement> agentMovement;
    public readonly DynamicBuffer<AgentPathBuffer> agentBuffer;
    public readonly DynamicBuffer<AgentPathValidityBuffer> agentPathValidityBuffer;
    public readonly RefRW<LocalTransform> trans;

    public void moveAgent(float deltaTime, float minDistanceReached, float agentSpeed, float agentRotationSpeed)
    {
        if (agentBuffer.Length > 0 && agent.ValueRO.pathCalculated && !agentMovement.ValueRO.reached)
        {
            agentMovement.ValueRW.waypointDirection = math.normalize(agentBuffer[agentMovement.ValueRO.currentBufferIndex].wayPoint - trans.ValueRO.Position);
            if (!float.IsNaN(agentMovement.ValueRW.waypointDirection.x))
            {
                trans.ValueRW.Position += agentMovement.ValueRW.waypointDirection * agentSpeed * deltaTime;
                trans.ValueRW.Rotation = math.slerp(
                                        trans.ValueRW.Rotation, 
                                        quaternion.LookRotation(agentMovement.ValueRW.waypointDirection, math.up()), 
                                        deltaTime * agentRotationSpeed);
                if (math.distance(trans.ValueRO.Position, agentBuffer[agentBuffer.Length - 1].wayPoint) <= minDistanceReached)
                {
                    agentMovement.ValueRW.reached = true;
                }
                else if (math.distance(trans.ValueRO.Position, agentBuffer[agentMovement.ValueRO.currentBufferIndex].wayPoint) <= minDistanceReached)
                {
                    agentMovement.ValueRW.currentBufferIndex = agentMovement.ValueRW.currentBufferIndex + 1;
                }
            }
            else if (!agentMovement.ValueRO.reached)
            {
                agentMovement.ValueRW.currentBufferIndex = agentMovement.ValueRW.currentBufferIndex + 1;
            }
        }
    }
}

[BurstCompile]
public struct PathValidityJob : IJob
{
    public NavMeshQuery query;
    public float3 extents;
    public int currentBufferIndex;
    public LocalTransform trans;
    public float unitsInDirection;
    [NativeDisableContainerSafetyRestriction] public DynamicBuffer<AgentPathBuffer> ab;
    [NativeDisableContainerSafetyRestriction] public DynamicBuffer<AgentPathValidityBuffer> apvb;
    NavMeshLocation startLocation;
    UnityEngine.AI.NavMeshHit navMeshHit;
    PathQueryStatus status;

    public void Execute()
    {
        if (currentBufferIndex < ab.Length)
        {
            if (!query.IsValid(query.MapLocation(ab.ElementAt(currentBufferIndex).wayPoint, extents, 0)))
            {
                apvb.Add(new AgentPathValidityBuffer { isPathInvalid = true });
            }
            else
            {
                startLocation = query.MapLocation(trans.Position + (trans.Forward() * unitsInDirection), extents, 0);
                status = query.Raycast(out navMeshHit, startLocation, ab.ElementAt(currentBufferIndex).wayPoint);
                
                if (status == PathQueryStatus.Success)
                {
                    if ((math.ceil(navMeshHit.position).x != math.ceil(ab.ElementAt(currentBufferIndex).wayPoint.x)) &&
                        (math.ceil(navMeshHit.position).z != math.ceil(ab.ElementAt(currentBufferIndex).wayPoint.z)))
                    {
                        apvb.Add(new AgentPathValidityBuffer { isPathInvalid = true });
                    }
                }
                else
                {
                    apvb.Add(new AgentPathValidityBuffer { isPathInvalid = true });
                }
            }
        }
    }
}


public static class NavMeshQueryExt
{
    public static void DebugPathStatus(this NavMeshQuery qury, string debugstr, PathQueryStatus status)
    {
        UnityEngine.Debug.Log(debugstr +
               $"Failure: {status.HasFlag(PathQueryStatus.Failure)} " +
               $"InProgress: {status.HasFlag(PathQueryStatus.InProgress)} " +
               $"Success: {status.HasFlag(PathQueryStatus.Success)} " +
               $"OutOfNodes: {status.HasFlag(PathQueryStatus.OutOfNodes)} " +
               $"OutOfMemory: {status.HasFlag(PathQueryStatus.OutOfMemory)} " +
               $"BufferTooSmall: {status.HasFlag(PathQueryStatus.BufferTooSmall)} " +
               $"PartialResult: {status.HasFlag(PathQueryStatus.PartialResult)}");
    }
}

[BurstCompile]
public struct NavigateJob : IJob
{
    public NavMeshQuery query;
    [NativeDisableContainerSafetyRestriction] public DynamicBuffer<AgentPathBuffer> ab;
    public float3 fromLocation;
    public float3 toLocation;
    public float3 extents;
    public int maxIteration;
    public int maxPathSize;


   
    public void Execute()
    {
        var nml_FromLocation = query.MapLocation(fromLocation, extents, 0);
        var nml_ToLocation = query.MapLocation(toLocation, extents, 0);
        PathQueryStatus status;
        PathQueryStatus returningStatus;

        var starLocIsValid = query.IsValid(nml_FromLocation);
        var toLocIsValid = query.IsValid(nml_ToLocation);


        if (starLocIsValid && toLocIsValid)
        {

            status = query.BeginFindPath(nml_FromLocation, nml_ToLocation);
            query.DebugPathStatus("BeginPath flags:", status);
           

            if (status.HasFlag(PathQueryStatus.InProgress))
            {
               
                status = query.UpdateFindPath(maxIteration, out int iterationPerformed);
                UnityEngine.Debug.Log($"UpdateFindPath status: {status}");

                if (status.HasFlag(PathQueryStatus.Success))
                {
                    status = query.EndFindPath(out int polygonSize);
                    UnityEngine.Debug.Log($"EndFindPath status: {status}");
                    NativeArray<NavMeshLocation> res = new NativeArray<NavMeshLocation>(polygonSize, Allocator.Temp);
                    NativeArray<StraightPathFlags> straightPathFlag = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                    NativeArray<float> vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                    NativeArray<PolygonId> polys = new NativeArray<PolygonId>(polygonSize, Allocator.Temp);
                    int straightPathCount = 0;
                    int a = query.GetPathResult(polys);
                    returningStatus = PathUtils.FindStraightPath(
                        query,
                        fromLocation,
                        toLocation,
                        polys,
                        polygonSize,
                        ref res,
                        ref straightPathFlag,
                        ref vertexSide,
                        ref straightPathCount,
                        maxPathSize
                    );
                    if (returningStatus.HasFlag(PathQueryStatus.Success))
                    {
                        UnityEngine.Debug.Log($"Finish status: {returningStatus}");
                        for (int i = 0; i < straightPathCount; i++)
                        {
                            if (!(math.distance(fromLocation, res[i].position) < 1) && query.IsValid(query.MapLocation(res[i].position, extents, 0)))
                            {
                                ab.Add(new AgentPathBuffer { wayPoint = new float3(res[i].position.x, fromLocation.y, res[i].position.z) });
                            }

                        }
                    }
                    else {
                        UnityEngine.Debug.Log($"failed status: {returningStatus}");
                    }
                    UnityEngine.Debug.Log($"path points {straightPathCount}");
                    res.Dispose();
                    straightPathFlag.Dispose();
                    polys.Dispose();
                    vertexSide.Dispose();
                }
            }
        }
        else {
            UnityEngine.Debug.LogError($"starLocIsValid {starLocIsValid} toLocIsValid {toLocIsValid}");
        }
    }
}