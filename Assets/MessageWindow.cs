using SV.ECS;
using System;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class MessageWindow : MonoBehaviour
{
    private Canvas m_Canvas;
    private GraphicRaycaster m_CanvasRaycaster;

    [SerializeField]
    private TMPro.TextMeshProUGUI text;
    [SerializeField]
    private TMPro.TMP_InputField inputField;


    public TMPro.TMP_InputField InputField => inputField;

    [SerializeField]
    private Button m_Send;

    public TMPro.TextMeshProUGUI Text => text;
    private void Awake()
    {
        m_Canvas = GetComponent<Canvas>();
        m_CanvasRaycaster = GetComponent<GraphicRaycaster>();


        var world = WorldExt.GetClientWorld();
        var system = world.GetOrCreateSystemManaged<MessageWindowGroup>();
        system.Setup(this);

        Activate(false);
        text.text = "";

        m_Send.onClick.AddListener(() =>
        {
            world.GetOrCreateSystemManaged<MessageWindowInputSystem>().SendRPC(InputField.text);
        });
    }





    public void Activate(bool activate)
    {
        m_Canvas.enabled = activate;
        m_CanvasRaycaster.enabled = activate;
        enabled = activate;
    }

}


public static class WorldExt
{
    public static World GetClientWorld()
    {
        foreach (var item in World.All)
        {
            if (item.IsClient())
                return item;
        }

        return null;
    }

    public static World GetServerWorld()
    {
        foreach (var item in World.All)
        {
            if (item.IsServer())
                return item;
        }

        return null;
    }
}

public struct ClearMWComponent : IComponentData
{

}

public struct ChatMessage : IRpcCommand
{
    public FixedString128Bytes Message;
}
public struct UpdateNameRpc : IRpcCommand
{
    public FixedString128Bytes Name;
    public int networkIdToUpdate;
}
public struct UserName : IComponentData
{
    public FixedString128Bytes Name;
}

public struct ChatUserInitialized : ICleanupComponentData
{
    public Unity.Entities.Hash128 userGuid;
    public FixedString128Bytes Name;
}

// Management for the queue which passes data between DOTS and GameObject systems, this way
// the two are decoupled a bit cleaner
[UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class RpcUiDataSystem : SystemBase
{
    private bool m_OwnsData;
    protected override void OnCreate()
    {
        m_OwnsData = !RpcUiData.Messages.Data.IsCreated;
        if (m_OwnsData)
        {

            RpcUiData.Messages.Data = new UnsafeRingQueue<FixedString128Bytes>(32, Allocator.Persistent);
        }
        Enabled = false;
    }

    protected override void OnUpdate() { }

    protected override void OnDestroy()
    {
        if (m_OwnsData)
        {
            RpcUiData.Messages.Data.Dispose();

        }
    }
}


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
                Any = new[] { ComponentType.ReadOnly<ChatMessage>() }
            }));
        }

        // When a user or chat message RPCs arrive they are added the queues for consumption
        // in the UI system.
        var buffer = m_CommandBufferSystem.CreateCommandBuffer();

        //recive message
        foreach (var (rpcCmd, chat, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<ChatMessage>>().WithEntityAccess())
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
        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}

public abstract class RpcUiData
{
    public static readonly SharedStatic<UnsafeRingQueue<FixedString128Bytes>> Messages = SharedStatic<UnsafeRingQueue<FixedString128Bytes>>.GetOrCreate<MessagesKey>();


    // Identifiers for the shared static fields
    private class MessagesKey { }

}

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

        // New incoming RPCs are placed on an entity with the ReceiveRpcCommandRequestComponent component and the RPC data payload component (ChatMessage)
        // This entity should be deleted when you're done processing it
        // The server RPC broadcasts the chat message to all connections

        foreach (var (rpcCmd, chat, entity) in SystemAPI.Query<RefRW<ReceiveRpcCommandRequest>, RefRW<ChatMessage>>().WithEntityAccess())
        {
            var conId = connections[rpcCmd.ValueRO.SourceConnection].Value;
            var userName = userNameLookup[rpcCmd.ValueRO.SourceConnection].Name;
            UnityEngine.Debug.Log(
                $"[{worldName}] Received {chat.ValueRO.Message} on connection {conId}.");


            var broadcastEntity = buffer.CreateEntity();
            buffer.AddComponent(broadcastEntity, new ChatMessage() { Message = FixedString.Format("{0}: {1}", userName, chat.ValueRO.Message) });
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
            buffer.AddComponent(broadcastEntity, new ChatMessage() { Message = message });
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
            buffer.AddComponent(broadcastEntity, new ChatMessage() { Message = message });
            buffer.AddComponent<SendRpcCommandRequest>(broadcastEntity);


            buffer.RemoveComponent<ChatUserInitialized>(e);
        }


    }
}

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


public partial class MessageWindowGroup : ComponentSystemGroup
{
    public void Setup(MessageWindow window)
    {
        var sys = EntityManager.World.GetOrCreateSystemManaged<MessageWindowInputSystem>();
        var sys1 = EntityManager.World.GetOrCreateSystemManaged<ShowMessageWindowExecutorSystem>();

        sys.Setup(window);
        sys1.Setup(window);
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(MessageWindowGroup))]
public partial class MessageWindowInputSystem : SystemBase
{
    private MessageWindow m_Window;
    public void Setup(MessageWindow window)
    {
        m_Window = window;

    }

    protected override void OnCreate()
    {
        base.OnCreate();

    }
    protected override void OnUpdate()
    {
        if (m_Window == null)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            m_Window.Activate(!m_Window.enabled);
            m_Window.InputField.text = "";
            m_Window.InputField.Select();
            m_Window.InputField.ActivateInputField();
        }


        if (!m_Window.enabled)
            return;

        if (Input.GetKeyDown(KeyCode.Return))
        {

            SendRPC(m_Window.InputField.text);
            m_Window.InputField.text = "";

            m_Window.InputField.Select();
            m_Window.InputField.ActivateInputField();

        }


    }

    public void SendRPC(FixedString128Bytes message, Entity targetEntity = default)
    {

        if (!m_Window.isActiveAndEnabled)
            return;
        if (message.IsEmpty)
            return;

        var entity = EntityManager.CreateEntity();
        EntityManager.AddComponentData(entity, new ChatMessage() { Message = message });
        EntityManager.AddComponent<SendRpcCommandRequest>(entity);
        if (targetEntity != Entity.Null)
            EntityManager.SetComponentData(entity,
                new SendRpcCommandRequest() { TargetConnection = targetEntity });
    }
}



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(MessageWindowGroup))]
public partial class ShowMessageWindowExecutorSystem : SystemBase
{
    private MessageWindow m_Window;

    protected override void OnCreate()
    {
        base.OnCreate();


    }

    private const string SystemTag = "[System]";

    private StringBuilder m_StringBuilder = new StringBuilder();


    public void Setup(MessageWindow window)
    {
        m_Window = window;

    }

    protected override void OnUpdate()
    {
        if (m_Window == null)
            return;

        var clearMW = SystemAPI.QueryBuilder().WithAll<ClearMWComponent>().Build();

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

        var buffer = ecb.CreateCommandBuffer(this.CheckedStateRef.WorldUnmanaged);

        bool build = false;

        if (!RpcUiData.Messages.Data.IsEmpty)
        {
            build = true;
        }

        if (RpcUiData.Messages.Data.IsCreated && RpcUiData.Messages.Data.TryDequeue(out var message))
        {
            m_StringBuilder.AppendLine($"{message}");

        }


        if (build)
        {
            m_Window.Text.text = m_StringBuilder.ToString();
        }

        if (clearMW.CalculateEntityCount() != 0)
        {

            m_StringBuilder.Clear();
            m_Window.Text.text = "";
        }




        buffer.DestroyEntity(clearMW);
    }
}

public static class NativeListExtensions
{
    /// <summary>
    /// Reverses a <see cref="NativeList{T}"/>.
    /// </summary>
    /// <typeparam name="T"><see cref="NativeList{T}"/>.</typeparam>
    /// <param name="list">The <see cref="NativeList{T}"/> to reverse.</param>
    public static void Reverse<T>(this NativeList<T> list)
        where T : unmanaged
    {
        var length = list.Length;
        var index1 = 0;

        for (var index2 = length - 1; index1 < index2; --index2)
        {
            var obj = list[index1];
            list[index1] = list[index2];
            list[index2] = obj;
            ++index1;
        }
    }

    /// <summary>
    /// Insert an element into a list.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="list">The list.</param>
    /// <param name="item">The element.</param>
    /// <param name="index">The index.</param>
    public static unsafe void Insert<T>(this NativeList<T> list, T item, int index) where T : unmanaged
    {
        if (list.Length == list.Capacity - 1)
        {
            list.Capacity *= 2;
        }

        // Inserting at end same as an add
        if (index == list.Length)
        {
            list.Add(item);
            return;
        }

        if (index < 0 || index > list.Length)
        {
            throw new IndexOutOfRangeException();
        }

        // add a default value to end to list to increase length by 1
        list.Add(default);

        int elemSize = UnsafeUtility.SizeOf<T>();
        byte* basePtr = (byte*)list.GetUnsafePtr();

        var from = (index * elemSize) + basePtr;
        var to = (elemSize * (index + 1)) + basePtr;
        var size = elemSize * (list.Length - index - 1); // -1 because we added an extra fake element

        UnsafeUtility.MemMove(to, from, size);

        list[index] = item;
    }

    /// <summary>
    /// Remove an element from a <see cref="NativeList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of NativeList.</typeparam>
    /// <typeparam name="TI">The type of element.</typeparam>
    /// <param name="list">The NativeList.</param>
    /// <param name="element">The element.</param>
    /// <returns>True if removed, else false.</returns>
    public static bool Remove<T, TI>(this NativeList<T> list, TI element)
        where T : unmanaged, IEquatable<TI>
        where TI : struct
    {
        var index = list.IndexOf(element);
        if (index < 0)
        {
            return false;
        }

        list.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Remove an element from a <see cref="NativeList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="list">The list to remove from.</param>
    /// <param name="index">The index to remove.</param>
    public static void RemoveAt<T>(this NativeList<T> list, int index)
        where T : unmanaged
    {
        list.RemoveRange(index, 1);
    }

    /// <summary>
    /// Removes a range of elements from a <see cref="NativeList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="list">The list to remove from.</param>
    /// <param name="index">The index to remove.</param>
    /// <param name="count">Number of elements to remove.</param>
    public static unsafe void RemoveRange<T>(this NativeList<T> list, int index, int count)
        where T : unmanaged
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if ((uint)index >= (uint)list.Length)
        {
            throw new IndexOutOfRangeException(
                $"Index {index} is out of range in NativeList of '{list.Length}' Length.");
        }
#endif

        int elemSize = UnsafeUtility.SizeOf<T>();
        byte* basePtr = (byte*)list.GetUnsafePtr();

        UnsafeUtility.MemMove(basePtr + (index * elemSize), basePtr + ((index + count) * elemSize), elemSize * (list.Length - count - index));

        // No easy way to change length so we just loop this unfortunately.
        for (var i = 0; i < count; i++)
        {
            list.RemoveAtSwapBack(list.Length - 1);
        }
    }

    /// <summary>
    /// Resizes a <see cref="NativeList{T}"/> and then clears the memory.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="buffer">The <see cref="NativeList{T}"/> to resize.</param>
    /// <param name="length">Size to resize to.</param>
    public static unsafe void ResizeInitialized<T>(this NativeList<T> buffer, int length)
        where T : unmanaged
    {
        buffer.ResizeUninitialized(length);
        UnsafeUtility.MemClear(buffer.GetUnsafePtr(), length * UnsafeUtility.SizeOf<T>());
    }

    /// <summary>
    /// Resizes a <see cref="NativeList{T}"/> and then sets all the bits to 1.
    /// For an integer array this is the same as setting the entire thing to -1.
    /// </summary>
    /// <param name="buffer">The <see cref="NativeList{T}"/> to resize.</param>
    /// <param name="length">Size to resize to.</param>
    public static void ResizeInitializeNegativeOne(this NativeList<int> buffer, int length)
    {
        buffer.ResizeUninitialized(length);

#if UNITY_2019_3_OR_NEWER
        unsafe
        {
            UnsafeUtility.MemSet(buffer.GetUnsafePtr(), byte.MaxValue, length * UnsafeUtility.SizeOf<int>());
        }
#else
            for (var i = 0; i < length; i++)
            {
                buffer[i] = -1;
            }
#endif
    }
}