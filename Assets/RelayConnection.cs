using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayConnection : MonoBehaviour
{
    HostServer m_HostServerSystem;
    private ConnectingPlayer m_HostClientSystem;

    public static RelayConnection Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    public void HostServer()
    {
        var world = World.All[0];
        m_HostServerSystem = world.GetOrCreateSystemManaged<HostServer>();
        var enableRelayServerEntity = world.EntityManager.CreateEntity(ComponentType.ReadWrite<EnableRelayServer>());
        world.EntityManager.AddComponent<EnableRelayServer>(enableRelayServerEntity);

       
        var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
        simGroup.AddSystemToUpdateList(m_HostServerSystem);
    }

    void SetupClient()
    {
        var world = World.All[0];
        m_HostClientSystem = world.GetOrCreateSystemManaged<ConnectingPlayer>();
        
        var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
        simGroup.AddSystemToUpdateList(m_HostClientSystem);
    }

    public void Disconnect()
    {
        var clientServerWorlds = new List<World>();
        foreach (var world in World.All)
        {
            if (world.IsClient() || world.IsServer())
                clientServerWorlds.Add(world);
        }

        foreach (var world in clientServerWorlds)
            world.Dispose();

       
        ClientServerBootstrap.CreateLocalWorld("DefaultWorld");
    }
}

public struct EnableRelayServer : IComponentData { }

// Each sample system is enabled by adding these types of enable components to the entity
// scene. This prevents all the systems in all the samples to run simultaneously all the time.
// Each sample can then also enable systems from previous samples by adding it's enable component.
public class EnableRelayServerAuthoring : MonoBehaviour
{
    class Baker : Baker<EnableRelayServerAuthoring>
    {
        public override void Bake(EnableRelayServerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EnableRelayServer>(entity);
        }
    }
}

/// <summary>
/// Responsible for contacting relay server and setting up <see cref="RelayServerData"/> and <see cref="JoinCode"/>.
/// Steps include:
/// 1. Initializing services
/// 2. Logging in
/// 3. Allocating number of players that are allowed to join.
/// 4. Retrieving join code
/// 5. Getting relay server information. I.e. IP-address, etc.
/// </summary>
[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class HostServer : SystemBase
{

    const int RelayMaxConnections = 4;
    public string JoinCode;

    public RelayServerData RelayServerData;
    HostStatus m_HostStatus;
    Task<List<Region>> m_RegionsTask;
    Task<Allocation> m_AllocationTask;
    Task<string> m_JoinCodeTask;
    Task m_InitializeTask;
    Task m_SignInTask;

    [Flags]
    enum HostStatus
    {
        Unknown,
        InitializeServices,
        Initializing,
        SigningIn,
        FailedToHost,
        Ready,
        GettingRegions,
        Allocating,
        GettingJoinCode,
        GetRelayData,
    }

    protected override void OnCreate()
    {
        RequireForUpdate<EnableRelayServer>();
        m_HostStatus = HostStatus.InitializeServices;
    }

    protected override void OnUpdate()
    {
        switch (m_HostStatus)
        {
            case HostStatus.FailedToHost:
                {

                Debug.Log($"Failed check console {HostStatus.FailedToHost.ToString()}");
                   

                    m_HostStatus = HostStatus.Unknown;
                    return;
                }
            case HostStatus.Ready:
                {

                    Debug.Log("Success, players may now connect");
                

                    m_HostStatus = HostStatus.Unknown;
                    return;
                }
            case HostStatus.InitializeServices:
                {

                    Debug.Log("Initializing services");
                    m_InitializeTask = UnityServices.InitializeAsync();
                    m_HostStatus = HostStatus.Initializing;
                    return;
                }
            case HostStatus.Initializing:
                {
                    m_HostStatus = WaitForInitialization(m_InitializeTask, out m_SignInTask);
                    return;
                }
            case HostStatus.SigningIn:
                {

                    Debug.Log("Logging in anonymously");
                    m_HostStatus = WaitForSignIn(m_SignInTask, out m_RegionsTask);
                    return;
                }
            case HostStatus.GettingRegions:
                {

                    Debug.Log("Waiting for regions");
                    m_HostStatus = WaitForRegions(m_RegionsTask, out m_AllocationTask);
                    return;
                }
            case HostStatus.Allocating:
                {

                    Debug.Log("Waiting for allocation");
                    m_HostStatus = WaitForAllocations(m_AllocationTask, out m_JoinCodeTask);
                    return;
                }
            case HostStatus.GettingJoinCode:
                {

                    Debug.Log("Waiting for join code");
                    m_HostStatus = WaitForJoin(m_JoinCodeTask, out JoinCode);
                    return;
                }
            case HostStatus.GetRelayData:
                {

                    Debug.Log("Getting relay data");
                    m_HostStatus = BindToHost(m_AllocationTask, out RelayServerData);
                    return;
                }
            case HostStatus.Unknown:
            default:
                break;
        }
    }

    static HostStatus WaitForSignIn(Task signInTask, out Task<List<Region>> regionTask)
    {
        if (!signInTask.IsCompleted)
        {
            regionTask = default;
            return HostStatus.SigningIn;
        }

        if (signInTask.IsFaulted)
        {
            Debug.LogError("Signing in failed");
            Debug.LogException(signInTask.Exception);
            regionTask = default;
            return HostStatus.FailedToHost;
        }

        // Request list of valid regions
        regionTask = RelayService.Instance.ListRegionsAsync();
        return HostStatus.GettingRegions;
    }

    static HostStatus WaitForInitialization(Task initializeTask, out Task nextTask)
    {
        if (!initializeTask.IsCompleted)
        {
            nextTask = default;
            return HostStatus.Initializing;
        }

        if (initializeTask.IsFaulted)
        {
            Debug.LogError("UnityServices Initialization failed");
            Debug.LogException(initializeTask.Exception);
            nextTask = default;
            return HostStatus.FailedToHost;
        }

        if (AuthenticationService.Instance.IsSignedIn)
        {
            nextTask = Task.CompletedTask;
            return HostStatus.SigningIn;
        }
        else
        {
            nextTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
            return HostStatus.SigningIn;
        }
    }

    // Bind and listen to the Relay server
    static HostStatus BindToHost(Task<Allocation> allocationTask, out RelayServerData relayServerData)
    {
        var allocation = allocationTask.Result;
        try
        {
            // Format the server data, based on desired connectionType
            relayServerData = HostRelayData(allocation);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            relayServerData = default;
            return HostStatus.FailedToHost;
        }
        return HostStatus.Ready;
    }

    // Get the Join Code, you can then share it with the clients so they can join
    static HostStatus WaitForJoin(Task<string> joinCodeTask, out string joinCode)
    {
        joinCode = null;
        if (!joinCodeTask.IsCompleted)
        {
            return HostStatus.GettingJoinCode;
        }

        if (joinCodeTask.IsFaulted)
        {
            Debug.LogError("Create join code request failed");
            Debug.LogException(joinCodeTask.Exception);
            return HostStatus.FailedToHost;
        }

        joinCode = joinCodeTask.Result;
        return HostStatus.GetRelayData;
    }

    static HostStatus WaitForAllocations(Task<Allocation> allocationTask, out Task<string> joinCodeTask)
    {
        if (!allocationTask.IsCompleted)
        {
            joinCodeTask = null;
            return HostStatus.Allocating;
        }

        if (allocationTask.IsFaulted)
        {
            Debug.LogError("Create allocation request failed");
            Debug.LogException(allocationTask.Exception);
            joinCodeTask = null;
            return HostStatus.FailedToHost;
        }

        // Request the join code to the Relay service
        var allocation = allocationTask.Result;
        joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        return HostStatus.GettingJoinCode;
    }

    static HostStatus WaitForRegions(Task<List<Region>> collectRegionTask, out Task<Allocation> allocationTask)
    {
        if (!collectRegionTask.IsCompleted)
        {
            allocationTask = null;
            return HostStatus.GettingRegions;
        }

        if (collectRegionTask.IsFaulted)
        {
            Debug.LogError("List regions request failed");
            Debug.LogException(collectRegionTask.Exception);
            allocationTask = null;
            return HostStatus.FailedToHost;
        }

        var regionList = collectRegionTask.Result;
        // pick a region from the list
        var targetRegion = regionList[0].Id;

        // Request an allocation to the Relay service
        // with a maximum of 5 peer connections, for a maximum of 6 players.
        allocationTask = RelayService.Instance.CreateAllocationAsync(RelayMaxConnections, targetRegion);
        return HostStatus.Allocating;
    }

    // connectionType also supports udp, but this is not recommended
    static RelayServerData HostRelayData(Allocation allocation, string connectionType = "dtls")
    {
        // Select endpoint based on desired connectionType
        var endpoint = RelayUtilities.GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
        if (endpoint == null)
        {
            throw new InvalidOperationException($"endpoint for connectionType {connectionType} not found");
        }

        // Prepare the server endpoint using the Relay server IP and port
        var serverEndpoint = NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);

        // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
        var allocationIdBytes = RelayAllocationId.FromByteArray(allocation.AllocationIdBytes);
        var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData);
        var key = RelayHMACKey.FromByteArray(allocation.Key);

        // Prepare the Relay server data and compute the nonce value
        // The host passes its connectionData twice into this function
        var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
            ref connectionData, ref key, connectionType == "dtls");

        return relayServerData;
    }
}

public static class RelayUtilities
{
    public static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
    {
        return endpoints.FirstOrDefault(endpoint => endpoint.ConnectionType == connectionType);
    }
}


// Place any established network connection in-game so ghost snapshot sync can start
[UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial class GoInGameSystem : SystemBase
{
    private EntityQuery m_NewConnections;

    protected override void OnCreate()
    {
        RequireForUpdate<EnableGoInGame>();
        RequireForUpdate(m_NewConnections);
    }

    protected override void OnUpdate()
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        FixedString32Bytes worldName = World.Name;
        // Go in game as soon as we have a connection set up (connection network ID has been set)
        Entities.WithName("NewConnectionsGoInGame").WithStoreEntityQueryInField(ref m_NewConnections).WithNone<NetworkStreamInGame>().ForEach(
            (Entity ent, in NetworkId id) =>
            {
                UnityEngine.Debug.Log($"[{worldName}] Go in game connection {id.Value}");
                commandBuffer.AddComponent<NetworkStreamInGame>(ent);
            }).Run();
        commandBuffer.Playback(EntityManager);
    }
}
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial class HelloNetcodeSystemGroup : ComponentSystemGroup
{ }

public struct EnableGoInGame : IComponentData { }

[DisallowMultipleComponent]
public class EnableGoInGameAuthoring : MonoBehaviour
{
    class Baker : Baker<EnableGoInGameAuthoring>
    {
        public override void Bake(EnableGoInGameAuthoring authoring)
        {
            EnableGoInGame component = default(EnableGoInGame);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
/// <summary>
/// Responsible for joining relay server using join code retrieved from <see cref="HostServer"/>.
/// </summary>
[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ConnectingPlayer : SystemBase
{
    Task<JoinAllocation> m_JoinTask;
    Task m_SetupTask;
    ClientStatus m_ClientStatus;
    string m_RelayJoinCode;
    NetworkEndpoint m_Endpoint;
    NetworkConnection m_ClientConnection;
    public RelayServerData RelayClientData;


    [Flags]
    enum ClientStatus
    {
        Unknown,
        FailedToConnect,
        Ready,
        GetJoinCodeFromHost,
        WaitForJoin,
        WaitForInit,
        WaitForSignIn,
    }

    protected override void OnCreate()
    {
        RequireForUpdate<EnableRelayServer>();
        m_ClientStatus = ClientStatus.Unknown;
    }

    public void GetJoinCodeFromHost()
    {
        m_ClientStatus = ClientStatus.GetJoinCodeFromHost;
    }

    public void JoinUsingCode(string joinCode)
    {

        Debug.Log("Waiting for relay response");

        m_RelayJoinCode = joinCode;
        m_SetupTask = UnityServices.InitializeAsync();
        m_ClientStatus = ClientStatus.WaitForInit;
    }

    protected override void OnUpdate()
    {
        switch (m_ClientStatus)
        {
            case ClientStatus.Ready:
                {

                    Debug.Log("Success");
                    m_ClientStatus = ClientStatus.Unknown;
                    return;
                }
            case ClientStatus.FailedToConnect:
                {

                    Debug.Log("Failed, check console");
                    m_ClientStatus = ClientStatus.Unknown;
                    return;
                }
            case ClientStatus.GetJoinCodeFromHost:
                {

                    Debug.Log("Waiting for join code from host server");
                    var hostServer = World.GetExistingSystemManaged<HostServer>();
                    m_ClientStatus = JoinUsingJoinCode(hostServer.JoinCode, out m_JoinTask);
                    return;
                }
            case ClientStatus.WaitForJoin:
                {

                    Debug.Log("Binding to relay server");
                    m_ClientStatus = WaitForJoin(m_JoinTask, out RelayClientData);
                    return;
                }
            case ClientStatus.WaitForInit:
                {
                    if (m_SetupTask.IsCompleted)
                    {
                        if (!AuthenticationService.Instance.IsSignedIn)
                        {
                            m_SetupTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
                            m_ClientStatus = ClientStatus.WaitForSignIn;
                        }
                    }
                    return;
                }
            case ClientStatus.WaitForSignIn:
                {
                    if (m_SetupTask.IsCompleted)
                        m_ClientStatus = JoinUsingJoinCode(m_RelayJoinCode, out m_JoinTask);
                    return;
                }
            case ClientStatus.Unknown:
            default:
                break;
        }
    }

    static ClientStatus WaitForJoin(Task<JoinAllocation> joinTask, out RelayServerData relayClientData)
    {
        if (!joinTask.IsCompleted)
        {
            relayClientData = default;
            return ClientStatus.WaitForJoin;
        }

        if (joinTask.IsFaulted)
        {
            relayClientData = default;
            Debug.LogError("Join Relay request failed");
            Debug.LogException(joinTask.Exception);
            return ClientStatus.FailedToConnect;
        }

        return BindToRelay(joinTask, out relayClientData);
    }

    static ClientStatus BindToRelay(Task<JoinAllocation> joinTask, out RelayServerData relayClientData)
    {
        // Collect and convert the Relay data from the join response
        var allocation = joinTask.Result;

        // Format the server data, based on desired connectionType
        try
        {
            relayClientData = PlayerRelayData(allocation);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            relayClientData = default;
            return ClientStatus.FailedToConnect;
        }

        return ClientStatus.Ready;
    }

    static ClientStatus JoinUsingJoinCode(string hostServerJoinCode, out Task<JoinAllocation> joinTask)
    {
        if (hostServerJoinCode == null)
        {
            joinTask = null;
            return ClientStatus.GetJoinCodeFromHost;
        }

        // Send the join request to the Relay service
        joinTask = RelayService.Instance.JoinAllocationAsync(hostServerJoinCode);
        return ClientStatus.WaitForJoin;
    }

    static RelayServerData PlayerRelayData(JoinAllocation allocation, string connectionType = "dtls")
    {
        // Select endpoint based on desired connectionType
        var endpoint = RelayUtilities.GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
        if (endpoint == null)
        {
            throw new Exception($"endpoint for connectionType {connectionType} not found");
        }

        // Prepare the server endpoint using the Relay server IP and port
        var serverEndpoint = NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);

        // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
        var allocationIdBytes = RelayAllocationId.FromByteArray(allocation.AllocationIdBytes);
        var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData);
        var hostConnectionData = RelayConnectionData.FromByteArray(allocation.HostConnectionData);
        var key = RelayHMACKey.FromByteArray(allocation.Key);

        // Prepare the Relay server data and compute the nonce values
        // A player joining the host passes its own connectionData as well as the host's
        var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
            ref hostConnectionData, ref key, connectionType == "dtls");

        return relayServerData;
    }
}