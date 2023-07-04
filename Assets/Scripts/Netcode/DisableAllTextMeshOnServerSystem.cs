using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class DisableAllTextMeshOnServerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var buffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

        foreach (var (tm, e) in SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<TextMesh>>().WithNone<Disabled>().WithEntityAccess())
        {
            buffer.AddComponent<Disabled>(e);
        }
    }
}
