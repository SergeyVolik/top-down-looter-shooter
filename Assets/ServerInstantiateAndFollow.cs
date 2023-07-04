using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ServerInstantiateAndFollow : MonoBehaviour
{
    public GameObject[] toFollow;
    public GameObject followTarget;
    public class Baker : Baker<ServerInstantiateAndFollow>
    {
        public override void Bake(ServerInstantiateAndFollow authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var ft = GetEntity(authoring.followTarget, TransformUsageFlags.Dynamic);
            var buffer = AddBuffer<InstantiateAndFollowServerSide>(entity);

            foreach (var item in authoring.toFollow)
            {
                buffer.Add(new InstantiateAndFollowServerSide
                {
                    prefab = GetEntity(item, TransformUsageFlags.Dynamic),
                     followTarget = ft
                });

            }
        }
    }

}
public struct InstantiateAndFollowServerSide : IBufferElementData
{
    public Entity prefab;
    public Entity followTarget;
}


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct InstantiateAndFollowSystem : ISystem
{
   
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


        foreach (var (buffer, e) in SystemAPI.Query<DynamicBuffer<InstantiateAndFollowServerSide>>().WithEntityAccess())
        {
           
            foreach (var item in buffer)
            {
                Debug.Log("InstantiateAndFollowServerSide");

                var entity = ecb.Instantiate(item.prefab);

                ecb.AddComponent(entity, new MoveWithEntityComponent
                {
                    target = item.followTarget
                });
            }

            ecb.RemoveComponent<InstantiateAndFollowServerSide>(e);
           


        }
    }
}