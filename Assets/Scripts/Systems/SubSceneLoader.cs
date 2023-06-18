using SV.ECS;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

public struct LoadSubScene : IComponentData
{
    public Hash128 value;
}

public struct UnloadSubScene : IComponentData
{
    public Hash128 value;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SubSceneLoader : ISystem
{
    private EntityQuery unloadSubSceneQuery;
    private EntityQuery loadSubSceneQuery;

    public void OnCreate(ref SystemState state)
    {
        unloadSubSceneQuery = SystemAPI.QueryBuilder().WithAll<UnloadSubScene>().Build();
        loadSubSceneQuery = SystemAPI.QueryBuilder().WithAll<LoadSubScene>().Build();

    }

    public void OnUpdate(ref SystemState state)
    {
        var unloadComponents = unloadSubSceneQuery.ToComponentDataArray<UnloadSubScene>(Allocator.Temp);
        var loadComponents = loadSubSceneQuery.ToComponentDataArray<LoadSubScene>(Allocator.Temp);

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        for (int i = 0; i < loadComponents.Length; i += 1)
        {
            SceneSystem.LoadSceneAsync(state.WorldUnmanaged, loadComponents[i].value);
        }

        var unloadParameters = SceneSystem.UnloadParameters.DestroyMetaEntities;
        for (int i = 0; i < unloadComponents.Length; i += 1)
        {
            SceneSystem.UnloadScene(state.WorldUnmanaged, unloadComponents[i].value, unloadParameters);
        }

        unloadComponents.Dispose();
        loadComponents.Dispose();
        ecb.DestroyEntity(unloadSubSceneQuery);
        ecb.DestroyEntity(loadSubSceneQuery);

      

     
    }
    

}
