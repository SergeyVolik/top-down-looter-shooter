using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class UpdateCharacterNameSystem : SystemBase
{
    protected override void OnUpdate()
    {



        var buffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
        foreach (var (nick, ownde, entity) in SystemAPI.Query<PlayerNickName, GhostOwner>().WithNone<UserName>().WithEntityAccess())
        {
            var toServet = buffer.CreateEntity();
            buffer.AddComponent<SendRpcCommandRequest>(toServet);
            buffer.AddComponent(toServet, new UpdateNameRpc { Name = LocalPlayerData.Player.DisplayName.Value, networkIdToUpdate = ownde.NetworkId });
            buffer.AddComponent<UserName>(entity);


        }


    }
}
