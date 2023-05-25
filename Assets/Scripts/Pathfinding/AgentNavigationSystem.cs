using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Experimental.AI;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;

[BurstCompile]
public partial struct MoveJob : IJobEntity
{
    public float deltaTime;
    public float minDistance;
    public float reachDistance;
    public float agentSpeed;
    public float agentRotationSpeed;
    public void Execute(AgentNavigationAspect ana)
    {
        ana.moveAgent(deltaTime, minDistance, reachDistance, agentSpeed, agentRotationSpeed);
    }
}


[UpdateBefore(typeof(AgentNavigationSystemV2))]
public partial struct UpdateFollowTargetSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FollowTargetComponent>();
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var worldToLocalLookUp = SystemAPI.GetComponentLookup<LocalToWorld>(true);

        var time = SystemAPI.Time.ElapsedTime;

        var updNavPointLookup = SystemAPI.GetComponentLookup<UpdateNavigationTarget>();
        foreach (var (ft, entity) in 
            SystemAPI.Query<RefRW<FollowTargetComponent>>()
            .WithEntityAccess())
        {

            if (time > ft.ValueRO.nextUpdateTime)
            {
                ft.ValueRW.nextUpdateTime = (float)time + ft.ValueRO.updateRate;
                UnityEngine.Debug.Log($"Update Follow Target");
                var pos = worldToLocalLookUp.GetRefRO(ft.ValueRO.entity).ValueRO.Position;
                updNavPointLookup.GetRefRW(entity).ValueRW.Position = pos;
                updNavPointLookup.SetComponentEnabled(entity, true);
            }
            
        }
    }
}

public struct DebugPath : IComponentData
{
    
}

public partial struct DebugPathSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DebugPath>();
    }
    public void OnUpdate(ref SystemState state)
    {
    
        foreach (var (path, entity) in SystemAPI.Query<DynamicBuffer<AgentPathBuffer>>().WithEntityAccess())
        {

            Debug.Log($"len {path.Length}");
            for (int i = 0; i < path.Length - 1; i++)
            {
                Debug.DrawLine(path[i].wayPoint, path[i + 1].wayPoint, color: Color.green, duration: 1f);
            }
        }
    }
}
    public partial struct AgentNavigationSystemV2 : ISystem, ISystemStartStop
{
    NativeArray<JobHandle> pathFindingJobs;
    NativeArray<JobHandle> pathValidyJobs;
    EntityQuery eq;
 
   
    NativeArray<NavMeshQuery> pathRecaclulatingQueries;

    UnsafeHashMap<Entity, NavMeshQuery> allNavMeshQueries;

    public void OnCreate(ref SystemState state)
    {
        allNavMeshQueries = new UnsafeHashMap<Entity, NavMeshQuery>(10, Allocator.Persistent);
    }
    public void OnStartRunning(ref SystemState state)
    {
        eq = state.EntityManager.CreateEntityQuery(typeof(Agent), typeof(UpdateNavigationTarget));
    }

    public void OnStopRunning(ref SystemState state)
    {

    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      
        var entitiesCount = eq.CalculateEntityCount();
      
        
        var updNavPointLookup = SystemAPI.GetComponentLookup<UpdateNavigationTarget>();
        var navQueryStateLookUp = SystemAPI.GetComponentLookup<NavQueryStateComponent>();

        var propertiesRW = SystemAPI.GetSingletonRW<NavigationGlobalProperties>();
        var propertiesRO = SystemAPI.GetSingleton<NavigationGlobalProperties>();

        foreach (var (ana, entity) in SystemAPI.Query<AgentNavigationAspect>().WithEntityAccess().WithAll<UpdateNavigationTarget>())
        {
            var updateCOmp = updNavPointLookup.GetRefRO(entity);
           
            ana.agent.ValueRW.toLocation = updateCOmp.ValueRO.Position;
            ana.agentBuffer.Clear();           
            ana.agent.ValueRW.pathCalculated = false;
            ana.agentPathValidityBuffer.Clear();
            ana.agentMovement.ValueRW.reached = false;
            ana.agent.ValueRW.pathFindingQueryDisposed = false;
            ana.agentMovement.ValueRW.currentBufferIndex = 0;

            if (allNavMeshQueries.TryGetValue(entity, out var query))
            {
                
                query.Dispose();
                allNavMeshQueries.Remove(entity);

               
            }

            query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, propertiesRW.ValueRO.maxPathNodePoolSize);

            allNavMeshQueries.Add(entity, query);   
            

            navQueryStateLookUp.SetComponentEnabled(entity, true);
            navQueryStateLookUp.GetRefRW(entity).ValueRW.Value = NavQueryState.None;
            
        }

        foreach (var (ana, e) in SystemAPI.Query<AgentNavigationAspect>().WithEntityAccess().WithAll<UpdateNavigationTarget>())
        {
            if (ana.agent.ValueRO.pathCalculated && !ana.agent.ValueRW.pathFindingQueryDisposed)
            {

                if (allNavMeshQueries.TryGetValue(e, out var query))
                {
                    allNavMeshQueries.Remove(e);
                    query.Dispose();
                    ana.agent.ValueRW.pathFindingQueryDisposed = true;
                }
            }
        }

        var job = new NavigateJob
        {
            properties = propertiesRO,
            queryMap = allNavMeshQueries.AsReadOnly(),
            compLookup = navQueryStateLookUp,
            updNavPointLookup = updNavPointLookup,
        };
        job.Run();
        //state.Dependency = job.Schedule(state.Dependency);

       

        //if (propertiesRW.ValueRO.dynamicPathFinding)
        //{
        //    int j = 0;
        //    pathRecaclulatingQueries = new NativeArray<NavMeshQuery>(eq.CalculateEntityCount(), Allocator.Temp);
        //    pathValidyJobs = new NativeArray<JobHandle>(eq.CalculateEntityCount(), Allocator.Temp);
        //    foreach (var (ana, e) in SystemAPI.Query<AgentNavigationAspect>().WithEntityAccess().WithAll<UpdateNavigationTarget>())
        //    {
        //        if (!ana.agentMovement.ValueRO.reached)
        //        {
        //            ana.agent.ValueRW.elapsedSinceLastPathCalculation += SystemAPI.Time.DeltaTime;
        //            if (ana.agent.ValueRW.elapsedSinceLastPathCalculation > propertiesRW.ValueRO.dynamicPathRecalculatingFrequency)
        //            {
        //                pathRecaclulatingQueries[j] = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, propertiesRW.ValueRO.maxPathNodePoolSize);
        //                ana.agent.ValueRW.elapsedSinceLastPathCalculation = 0;

        //                pathValidyJobs[j] = new PathValidityJob
        //                {
        //                    query = pathRecaclulatingQueries[j],
        //                    extents = propertiesRW.ValueRO.extents,
        //                    currentBufferIndex = ana.agentMovement.ValueRW.currentBufferIndex,
        //                    trans = ana.trans.ValueRW,
        //                    unitsInDirection = propertiesRW.ValueRO.unitsInForwardDirection,
        //                    ab = ana.agentBuffer,
        //                    apvb = ana.agentPathValidityBuffer
        //                }.Schedule(state.Dependency);

        //                j++;
        //            }
        //        }



        //    }
        //    JobHandle.CompleteAll(pathValidyJobs);

        //    for (int k = 0; k < j; k++)
        //    {
        //        pathRecaclulatingQueries[k].Dispose();
        //    }

        //    pathRecaclulatingQueries.Dispose();
        //}


        if (propertiesRW.ValueRO.agentMovementEnabled)
        {
            state.Dependency = new MoveJob
            {
                deltaTime = SystemAPI.Time.DeltaTime,
                minDistance = propertiesRW.ValueRO.minimumDistanceToWaypoint,
                agentSpeed = propertiesRW.ValueRO.agentSpeed,
                agentRotationSpeed = propertiesRW.ValueRO.rotationSpeed,
                reachDistance = propertiesRW.ValueRO.reachDistance
            }.ScheduleParallel(state.Dependency);
        }
    }
}


//public partial struct AgentNavigationSystem1 : ISystem, ISystemStartStop
//{
//    NativeArray<JobHandle> pathFindingJobs;
//    NativeArray<JobHandle> pathValidyJobs;
//    EntityQuery eq;
//    RefRW<NavigationGlobalProperties> properties;
//    NativeArray<NavMeshQuery> pathFindingQueries;
//    NativeArray<NavMeshQuery> pathRecaclulatingQueries;



//    public void OnStartRunning(ref SystemState state)
//    {
//        eq = state.EntityManager.CreateEntityQuery(typeof(Agent));

//    }

//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<NavigationGlobalProperties>();
//    }


//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {

//        state.Enabled = false;
//        return;
//        int i = 0;
//        var entitiesCount = eq.CalculateEntityCount();
//        pathFindingQueries = new NativeArray<NavMeshQuery>(entitiesCount, Allocator.Temp);


//        properties = SystemAPI.GetSingletonRW<NavigationGlobalProperties>();
//        pathFindingJobs = new NativeArray<JobHandle>(entitiesCount, Allocator.Temp);
//        foreach (var (ana, entity) in SystemAPI.Query<AgentNavigationAspect>().WithEntityAccess())
//        {

//            if (properties.ValueRO.dynamicPathFinding && ana.agentPathValidityBuffer.Length > 0 && ana.agentPathValidityBuffer.ElementAt(0).isPathInvalid)
//            {
//                ana.agentBuffer.Clear();
//                ana.agentMovement.ValueRW.currentBufferIndex = 0;
//                ana.agent.ValueRW.pathCalculated = false;
//                ana.agentPathValidityBuffer.Clear();
//            }
//            if (properties.ValueRO.agentMovementEnabled && properties.ValueRO.retracePath && ana.agent.ValueRW.usingGlobalRelativeLoction && ana.agentMovement.ValueRO.reached)
//            {
//                ana.agent.ValueRW.toLocation = new float3(ana.agent.ValueRW.toLocation.x, ana.agent.ValueRW.toLocation.y, -ana.agent.ValueRW.toLocation.z);
//                ana.agentBuffer.Clear();
//                ana.agentMovement.ValueRW.currentBufferIndex = 0;
//                ana.agent.ValueRW.pathCalculated = false;
//                ana.agentPathValidityBuffer.Clear();
//                ana.agentMovement.ValueRW.reached = false;
//            }
//            if (!ana.agent.ValueRO.pathCalculated || ana.agentBuffer.Length == 0)
//            {
//                pathFindingQueries[i] = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, properties.ValueRO.maxPathNodePoolSize);
//                ana.agent.ValueRW.pathFindingQueryIndex = i;

//                if (properties.ValueRO.setGlobalRelativeLocation && !ana.agent.ValueRO.usingGlobalRelativeLoction)
//                {
//                    ana.agent.ValueRW.toLocation = ana.trans.ValueRO.Position + properties.ValueRO.units;
//                    ana.agent.ValueRW.usingGlobalRelativeLoction = true;
//                }

//                var job = new NavigateJob
//                {
//                    query = pathFindingQueries[i],
//                    ab = ana.agentBuffer,
//                    fromLocation = ana.trans.ValueRO.Position,
//                    toLocation = ana.agent.ValueRO.toLocation,
//                    extents = properties.ValueRO.extents,
//                    maxIteration = properties.ValueRO.maxIteration,
//                    maxPathSize = properties.ValueRO.maxPathSize
//                };


//                pathFindingJobs[i] = job.Schedule(state.Dependency);
//                ana.agent.ValueRW.pathCalculated = true;
//                ana.agent.ValueRW.pathFindingQueryDisposed = false;
//            }
//            i++;
//        }


//        JobHandle.CompleteAll(pathFindingJobs);

//        foreach (AgentNavigationAspect ana in SystemAPI.Query<AgentNavigationAspect>())
//        {
//            if (ana.agent.ValueRO.pathCalculated && !ana.agent.ValueRW.pathFindingQueryDisposed)
//            {
//                pathFindingQueries[ana.agent.ValueRW.pathFindingQueryIndex].Dispose();
//                ana.agent.ValueRW.pathFindingQueryDisposed = true;
//            }
//        }
//        pathFindingQueries.Dispose();

//        if (properties.ValueRO.dynamicPathFinding)
//        {
//            int j = 0;
//            pathRecaclulatingQueries = new NativeArray<NavMeshQuery>(eq.CalculateEntityCount(), Allocator.Temp);
//            pathValidyJobs = new NativeArray<JobHandle>(eq.CalculateEntityCount(), Allocator.Temp);
//            foreach (AgentNavigationAspect ana in SystemAPI.Query<AgentNavigationAspect>())
//            {
//                if (!ana.agentMovement.ValueRO.reached)
//                {
//                    ana.agent.ValueRW.elapsedSinceLastPathCalculation += SystemAPI.Time.DeltaTime;
//                    if (ana.agent.ValueRW.elapsedSinceLastPathCalculation > properties.ValueRO.dynamicPathRecalculatingFrequency)
//                    {
//                        pathRecaclulatingQueries[j] = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, properties.ValueRO.maxPathNodePoolSize);
//                        ana.agent.ValueRW.elapsedSinceLastPathCalculation = 0;
//                        pathValidyJobs[j] = new PathValidityJob
//                        {
//                            query = pathRecaclulatingQueries[j],
//                            extents = properties.ValueRO.extents,
//                            currentBufferIndex = ana.agentMovement.ValueRW.currentBufferIndex,
//                            trans = ana.trans.ValueRW,
//                            unitsInDirection = properties.ValueRO.unitsInForwardDirection,
//                            ab = ana.agentBuffer,
//                            apvb = ana.agentPathValidityBuffer
//                        }.Schedule(state.Dependency);
//                        j++;
//                    }
//                }
//            }
//            JobHandle.CompleteAll(pathValidyJobs);

//            for (int k = 0; k < j; k++)
//            {
//                pathRecaclulatingQueries[k].Dispose();
//            }
//            pathRecaclulatingQueries.Dispose();
//        }

//        if (properties.ValueRO.agentMovementEnabled)
//        {
//            new MoveJob
//            {
//                deltaTime = SystemAPI.Time.DeltaTime,
//                minDistance = properties.ValueRO.minimumDistanceToWaypoint,
//                agentSpeed = properties.ValueRO.agentSpeed,
//                agentRotationSpeed = properties.ValueRO.rotationSpeed
//            }.ScheduleParallel();
//        }

//    }

//    public void OnStopRunning(ref SystemState state)
//    {

//    }
//}
   

