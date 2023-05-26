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

    public void moveAgent(float deltaTime, float distToNextPoint, float reachDist, float agentSpeed, float agentRotationSpeed)
    {
        if (agentBuffer.Length > 0 && agent.ValueRO.pathCalculated && !agentMovement.ValueRO.reached)
        {
            if (math.distance(trans.ValueRO.Position, agentBuffer[agentBuffer.Length - 1].wayPoint) <= reachDist)
            {
                agentMovement.ValueRW.reached = true;
                return;
            }

            agentMovement.ValueRW.waypointDirection = math.normalize(agentBuffer[agentMovement.ValueRO.currentBufferIndex].wayPoint - trans.ValueRO.Position);
            if (!float.IsNaN(agentMovement.ValueRW.waypointDirection.x))
            {
                trans.ValueRW.Position += agentMovement.ValueRW.waypointDirection * agentSpeed * deltaTime;
                trans.ValueRW.Rotation = math.slerp(
                                        trans.ValueRW.Rotation,
                                        quaternion.LookRotation(agentMovement.ValueRW.waypointDirection, math.up()),
                                        deltaTime * agentRotationSpeed);
               
                if (math.distance(trans.ValueRO.Position, agentBuffer[agentMovement.ValueRO.currentBufferIndex].wayPoint) <= distToNextPoint)
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

[WithAll(typeof(UpdateNavigationTarget), typeof(NavQueryStateComponent))]
[BurstCompile]
public partial struct NavigateJob : IJobEntity
{

    [ReadOnly]
    public UnsafeHashMap<Entity, NavMeshQuery>.ReadOnly queryMap;
    [ReadOnly]
    public NavigationGlobalProperties properties;

    public ComponentLookup<NavQueryStateComponent> compLookup;
    public ComponentLookup<UpdateNavigationTarget> updNavPointLookup;

    public void Execute(AgentNavigationAspect ana, Entity entity)
    {

       
        if (!compLookup.HasComponent(entity))
            return;

        if (!queryMap.TryGetValue(entity, out var query))
            return;

        updNavPointLookup.SetComponentEnabled(entity, false);
        float3 fromLocation = ana.trans.ValueRO.Position;
        float3 toLocation = ana.agent.ValueRO.toLocation;
        float3 extents = properties.extents;
        int maxIteration = properties.maxIteration;
        int maxPathSize = properties.maxPathSize;

        var ab = ana.agentBuffer;

        PathQueryStatus status = default;
        PathQueryStatus returningStatus = default;


        var state = compLookup.GetRefRO(entity).ValueRO.Value;

        var nml_FromLocation = query.MapLocation(fromLocation, extents, 0);
        var nml_ToLocation = query.MapLocation(toLocation, extents, 0);




        if (state == NavQueryState.None)
        {
            var starLocIsValid = query.IsValid(nml_FromLocation);
            var toLocIsValid = query.IsValid(nml_ToLocation);

            if (starLocIsValid && toLocIsValid)
            {
                
                status = query.BeginFindPath(nml_FromLocation, nml_ToLocation);
                //query.DebugPathStatus($"BeginPath flags: state: {state}", status);
               
                if ((status & PathQueryStatus.InProgress) == PathQueryStatus.InProgress || (status & PathQueryStatus.Success) == PathQueryStatus.Success)
                {
                    state = NavQueryState.Started;
                }
                else if ((status & PathQueryStatus.Failure) == PathQueryStatus.Failure)
                {
                    state = NavQueryState.Failed;
                    //UnityEngine.Debug.LogError($"BeginPath nav mesh query failed!");
                }
            }
            else
            {
                //UnityEngine.Debug.LogError($"starLocIsValid {starLocIsValid} toLocIsValid {toLocIsValid}");
            }
        }

        if (state == NavQueryState.Started)
        {

            status = query.UpdateFindPath(maxIteration, out int iterationPerformed);


            if ((status & PathQueryStatus.Success) == PathQueryStatus.Success)
            {
                state = NavQueryState.Finished;
            }
            else if ((status & PathQueryStatus.Failure) == PathQueryStatus.Failure)
            {
                //UnityEngine.Debug.LogError($"UpdateFindPath nav mesh query failed!");
            }

            //query.DebugPathStatus($"UpdateFindPath flags: state: {state}", status);
        }

        if (state == NavQueryState.Finished)
        {

            status = query.EndFindPath(out int polygonSize);
            //query.DebugPathStatus($"EndFindPath flags: state: {state}", status);

           
            NativeArray<NavMeshLocation> res = new NativeArray<NavMeshLocation>(polygonSize, Allocator.Temp);
            NativeArray<StraightPathFlags> straightPathFlag = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
            NativeArray<float> vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
            NativeArray<PolygonId> polys = new NativeArray<PolygonId>(polygonSize, Allocator.Temp);
            int straightPathCount = 0;
            int a = query.GetPathResult(polys);

            //UnityEngine.Debug.Log($"polygonSize {polygonSize}");
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
            

            if ((returningStatus & PathQueryStatus.Success) == PathQueryStatus.Success)
            {
                //query.DebugPathStatus($"EndFindPath flags: state: {state}", status);
                //UnityEngine.Debug.Log($"Finish status: {returningStatus}");
                for (int i = 0; i < straightPathCount; i++)
                {
                    if (!(math.distance(fromLocation, res[i].position) < 1) && query.IsValid(query.MapLocation(res[i].position, extents, 0)))
                    {
                        var wayPointPos = new float3(res[i].position.x, fromLocation.y, res[i].position.z);
                        var wayPointPosV2 = new float3(res[i].position.x, res[i].position.y, res[i].position.z);
                        ab.Add(new AgentPathBuffer { wayPoint = wayPointPosV2 });

                        //UnityEngine.Debug.Log($"wayPointPos {wayPointPos} wayPointPosV2 {wayPointPosV2}");
                    }

                }
            }
            else
            {
                //query.DebugPathStatus($"failed flags: state: {state}", status);
               
            }
           
            res.Dispose();
            straightPathFlag.Dispose();
            polys.Dispose();
            vertexSide.Dispose();
            ana.agent.ValueRW.pathCalculated = true;

        }

        compLookup.GetRefRW(entity).ValueRW.Value = state;





    }
}

