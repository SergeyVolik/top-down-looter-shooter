using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial class RpcClientSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_CommandBufferSystem;

    protected override void OnCreate()
    {
        //RequireForUpdate<EnableRPC>();
        // Can't send any RPC/chat messages before connection is established
        RequireForUpdate<NetworkId>();
        m_CommandBufferSystem = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // This is not set up to handle multiple clients/worlds using one or more chat windows but the
        // rpc messages must be consumed or else warnings will be emitted
        if (World.IsThinClient())
        {
            EntityManager.DestroyEntity(GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] { ComponentType.ReadOnly<ReceiveRpcCommandRequest>() },
                Any = new[] { ComponentType.ReadOnly<ChatMessageRpc>() }
            }));
        }

        // When a user or chat message RPCs arrive they are added the queues for consumption
        // in the UI system.
        var buffer = m_CommandBufferSystem.CreateCommandBuffer();

        //recive message
        foreach (var (rpcCmd, chat, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<ChatMessageRpc>>().WithEntityAccess())
        {
            buffer.DestroyEntity(entity);
            // Not thread safe, so all UI logic is kept on main thread
            RpcUiData.Messages.Data.Enqueue(chat.ValueRO.Message);
        }





        foreach (var (network, e) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<UserName>().WithEntityAccess())
        {
            FixedString128Bytes name = LocalPlayerData.Player.DisplayName.Value;
            buffer.AddComponent(e, new UserName { Name = name });

            var toServerEntity = buffer.CreateEntity();
            buffer.AddComponent<SendRpcCommandRequest>(toServerEntity);
            buffer.AddComponent(toServerEntity, new UpdateNameRpc
            {
                Name = name,
                networkIdToUpdate = network.ValueRO.Value
            });
        }


        foreach (var (rpcCmd, updateName, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<UpdateNameRpc>>().WithEntityAccess())
        {

            foreach (var (pn, owner) in SystemAPI.Query<PlayerNickName, GhostOwner>())
            {
                if (owner.NetworkId == updateName.ValueRO.networkIdToUpdate)
                {
                    var tm = SystemAPI.ManagedAPI.GetComponent<TextMesh>(pn.nickName);
                    tm.text = updateName.ValueRO.Name.ToString();
                }
            }

            buffer.DestroyEntity(entity);


        }

        foreach (var (rpcCmd, updateName, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<GetNameResultRpc>>().WithEntityAccess())
        {

            foreach (var (pn, owner) in SystemAPI.Query<PlayerNickName, GhostOwner>())
            {
                if (owner.NetworkId == updateName.ValueRO.networkId)
                {
                    var tm = SystemAPI.ManagedAPI.GetComponent<TextMesh>(pn.nickName);
                    tm.text = updateName.ValueRO.Name.ToString();
                }
            }

            buffer.DestroyEntity(entity);


        }
        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}