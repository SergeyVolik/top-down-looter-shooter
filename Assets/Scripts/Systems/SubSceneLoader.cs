using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;

public struct LoadSubScene : IComponentData
{
    public EntitySceneReference value;
}

public struct UnloadSubScene : IComponentData
{
    public EntitySceneReference value;
}

public struct SubSceneInjstanceData : IComponentData
{
    public EntitySceneReference value;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SubSceneLoader : ISystem
{
    private EntityQuery loadRequests;
    private EntityQuery unloadRequests;
    private EntityQuery subSceneDataRequests;

    public void OnCreate(ref SystemState state)
    {

        loadRequests = state.GetEntityQuery(typeof(LoadSubScene));
        unloadRequests = state.GetEntityQuery(typeof(UnloadSubScene));
        subSceneDataRequests = state.GetEntityQuery(typeof(SubSceneInjstanceData));
    }

    

    public void OnUpdate(ref SystemState state)
    {


     

        var loadData = loadRequests.ToComponentDataArray<LoadSubScene>(Allocator.Temp);
        var unloadData = unloadRequests.ToComponentDataArray<UnloadSubScene>(Allocator.Temp);
        var entities = subSceneDataRequests.ToEntityArray(Allocator.Temp);
        var subScenes = subSceneDataRequests.ToComponentDataArray<SubSceneInjstanceData>(Allocator.Temp);

        foreach (var loadSubSCene in loadData)
        {
            var loadParams = new SceneSystem.LoadParameters
            {
                AutoLoad = true,
                //Flags = SceneLoadFlags.NewInstance

            };
            var sceneEntity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, loadSubSCene.value,  parameters: loadParams);
            state.EntityManager.AddComponent<SubSceneInjstanceData>(sceneEntity);
            state.EntityManager.SetComponentData<SubSceneInjstanceData>(sceneEntity, new SubSceneInjstanceData { 
                 value = loadSubSCene.value
            });
            UnityEngine.Debug.Log("Sub Scene loaded");
        }

        var unloadParameters = SceneSystem.UnloadParameters.DestroyMetaEntities;


        foreach (var unloadCOmp in unloadData)
        {

            for (int i = 0; i < entities.Length; i++)
            {
                
                var sceneInfo = subScenes[i];
                if (sceneInfo.value.Equals(unloadCOmp.value))
                {
                    UnityEngine.Debug.Log("Sub Scene unloaded");
                    //SceneSystem.UnloadScene(state.WorldUnmanaged, unloadCOmp.value, unloadParameters)
                    SceneSystem.UnloadScene(state.WorldUnmanaged, entities[i], unloadParameters);
                }
            }



           


        }

        state.EntityManager.DestroyEntity(loadRequests);
        state.EntityManager.DestroyEntity(unloadRequests);


    }


}





