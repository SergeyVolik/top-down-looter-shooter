using SV.ECS;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class RpcServerSystem : SystemBase
{
    private const uint rndOffset = 100;
    private BeginSimulationEntityCommandBufferSystem m_CommandBufferSystem;


    // User information is just tracked as a single integer (=connection ID) to make this as simple as possible
    private NativeList<Unity.Entities.Hash128> m_Users;

    public uint rndSeed;
    protected override void OnCreate()
    {
        //RequireForUpdate<EnableRPC>();
        m_CommandBufferSystem = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        rndSeed = 100;

        m_Users = new NativeList<Unity.Entities.Hash128>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        m_Users.Dispose();
    }



    protected override void OnUpdate()
    {
        var rnd = Unity.Mathematics.Random.CreateFromIndex(rndSeed);
        rndSeed += rndOffset;

        var buffer = m_CommandBufferSystem.CreateCommandBuffer();
        var connections = GetComponentLookup<NetworkId>(true);
        var userNameLookup = GetComponentLookup<UserName>(isReadOnly: false);
        FixedString32Bytes worldName = World.Name;





        foreach (var (rpcCmd, chat, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<UpdateNameRpc>>().WithEntityAccess())
        {
            var sourceEntity = rpcCmd.ValueRO.SourceConnection;

            if (userNameLookup.HasComponent(sourceEntity))
            {
                userNameLookup.GetRefRW(sourceEntity).ValueRW.Name = chat.ValueRO.Name;
            }
            else
            {
                buffer.AddComponent(sourceEntity, new UserName
                {
                    Name = chat.ValueRO.Name,
                });
            }
            var toClients = buffer.CreateEntity();




            buffer.AddComponent<SendRpcCommandRequest>(toClients);
            buffer.AddComponent(toClients, chat.ValueRO);
            buffer.DestroyEntity(entity);
        }

        foreach (var (rpcCmd, chat, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<GetNameRpc>>().WithEntityAccess())
        {
            var sourceEntity = rpcCmd.ValueRO.SourceConnection;


            var toClients = buffer.CreateEntity();


            foreach (var (pn, username) in SystemAPI.Query<NetworkId, UserName>())
            {
                if (pn.Value == chat.ValueRO.networkId)
                {
                    buffer.AddComponent<SendRpcCommandRequest>(toClients, new SendRpcCommandRequest
                    {
                        TargetConnection = sourceEntity
                    });
                    buffer.AddComponent(toClients, new GetNameResultRpc
                    {
                        networkId = chat.ValueRO.networkId,
                        Name = username.Name
                    });

                }
            }


            buffer.DestroyEntity(entity);
        }

        // New incoming RPCs are placed on an entity with the ReceiveRpcCommandRequestComponent component and the RPC data payload component (ChatMessage)
        // This entity should be deleted when you're done processing it
        // The server RPC broadcasts the chat message to all connections

        foreach (var (rpcCmd, chat, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<ChatMessageRpc>>().WithEntityAccess())
        {
            var conId = connections[rpcCmd.ValueRO.SourceConnection].Value;
            var userName = userNameLookup[rpcCmd.ValueRO.SourceConnection].Name;
            UnityEngine.Debug.Log(
                $"[{worldName}] Received {chat.ValueRO.Message} on connection {conId}.");


            var broadcastEntity = buffer.CreateEntity();
            buffer.AddComponent(broadcastEntity, new ChatMessageRpc() { Message = FixedString.Format("{0}: {1}", userName, chat.ValueRO.Message) });
            buffer.AddComponent<SendRpcCommandRequest>(broadcastEntity);


            buffer.DestroyEntity(entity);
        }

        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);

        var users = m_Users;

        //add new user and send message
        foreach (var (id, userName, entity) in SystemAPI.Query<RefRW<NetworkId>, RefRO<UserName>>().WithNone<ChatUserInitialized>().WithEntityAccess())
        {
            var connectionId = id.ValueRO.Value;
            var message = $"{userName.ValueRO.Name} connected!";
            // Notify all connections about new chat user (including himself)
            var broadcastEntity = buffer.CreateEntity();
            buffer.AddComponent(broadcastEntity, new ChatMessageRpc() { Message = message });
            buffer.AddComponent<SendRpcCommandRequest>(broadcastEntity);
            UnityEngine.Debug.Log($"[{worldName}] New user 'User {connectionId}' connected. Broadcasting user entry to all connections;");

            var hash = new Unity.Entities.Hash128(rnd.NextUInt4());

            // Add connection to user list
            users.Add(hash);



            // Mark this connection/user so we don't process again
            buffer.AddComponent(entity, new ChatUserInitialized
            {
                userGuid = hash,
                Name = userName.ValueRO.Name
            });

        }


        //remove user and send reconnected message
        foreach (var (userData, e) in SystemAPI.Query<ChatUserInitialized>().WithNone<NetworkId>().WithEntityAccess())
        {
            var message = $"User{userData.Name} disconnected!";

            users.Remove(userData.userGuid);

            var broadcastEntity = buffer.CreateEntity();
            buffer.AddComponent(broadcastEntity, new ChatMessageRpc() { Message = message });
            buffer.AddComponent<SendRpcCommandRequest>(broadcastEntity);


            buffer.RemoveComponent<ChatUserInitialized>(e);
        }


    }
}