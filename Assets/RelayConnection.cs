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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayConnection : MonoBehaviour
{
    

    public static RelayConnection Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    protected void DestroyLocalSimulationWorld()
    {
        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
               
                world.Dispose();
                break;
            }
        }
    }
    #region Client
    void SetupClient()
    {
        var world = World.All[0];
       
        var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
       
    }

    public async void JoinAsClient(string joinCode)
    {
        SetupClient();
        var world = World.All[0];
        var enableRelayServerEntity = world.EntityManager.CreateEntity(ComponentType.ReadWrite<EnableRelayServer>());
        world.EntityManager.AddComponent<EnableRelayServer>(enableRelayServerEntity);


        await ConnectToRelayServer(joinCode);

        Debug.Log($"Cliend connected. replayCode: {joinCode}");
    }

    async Task ConnectToRelayServer(string hostServerJoinCode)
    {
        var allocation = await RelayService.Instance.JoinAllocationAsync(hostServerJoinCode);
        var relayClientData = PlayerRelayData(allocation);

        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(new RelayServerData(), relayClientData);
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;
  

        //Destroy the local simulation world to avoid the game scene to be loaded into it
        //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
        //and other issues.
        DestroyLocalSimulationWorld();
        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = client;

       

        var networkStreamEntity = client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        // For IPC this will not work and give an error in the transport layer. For this sample we force the client to connect through the relay service.
        // For a locally hosted server, the client would need to connect to NetworkEndpoint.AnyIpv4, and the relayClientData.Endpoint in all other cases.
        client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
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
    #endregion

    #region Server


    async Task SetupRelayHostedServerAndConnect(Allocation allocation, string hostServerJoinCode)
    {
        if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
        {
            UnityEngine.Debug.LogError($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
            return;
        }

      

        var  joinTask =  await RelayService.Instance.JoinAllocationAsync(hostServerJoinCode);
        var relayClientData = PlayerRelayData(joinTask);
        var relayServerData = HostRelayData(allocation);

        var world = World.All[0];
        

        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
        var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

      

        //Destroy the local simulation world to avoid the game scene to be loaded into it
        //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
        //and other issues.
        DestroyLocalSimulationWorld();
        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = server;

       

        var joinCodeEntity = server.EntityManager.CreateEntity(ComponentType.ReadOnly<JoinCode>());
        server.EntityManager.SetComponentData(joinCodeEntity, new JoinCode { Value = hostServerJoinCode });

        var networkStreamEntity = server.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
        server.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");
        server.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

        networkStreamEntity = client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        // For IPC this will not work and give an error in the transport layer. For this sample we force the client to connect through the relay service.
        // For a locally hosted server, the client would need to connect to NetworkEndpoint.AnyIpv4, and the relayClientData.Endpoint in all other cases.
        client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });

        Debug.Log($"Cliend/Server started replayCode {hostServerJoinCode}");
    }

    public async Task<string> HostServerAndClient()
    {
        var regionList = await RelayService.Instance.ListRegionsAsync();
        var targetRegion = regionList[0].Id;


        var allocation = await RelayService.Instance.CreateAllocationAsync(LobbyManager.maxPlayers, targetRegion);

        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var world = World.All[0];
      
        var enableRelayServerEntity = world.EntityManager.CreateEntity(ComponentType.ReadWrite<EnableRelayServer>());
        world.EntityManager.AddComponent<EnableRelayServer>(enableRelayServerEntity);

       
      
       
        await SetupRelayHostedServerAndConnect(allocation, joinCode);


        return joinCode;
    }

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

    #endregion

}

public struct JoinCode : IComponentData
{
    public FixedString64Bytes Value;
}

public class RelayHUD : MonoBehaviour
{
    public Text JoinCodeLabel;

    public void Awake()
    {
        var world = World.All[0];
        var joinQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<JoinCode>());
        if (joinQuery.HasSingleton<JoinCode>())
        {
            var joinCode = joinQuery.GetSingleton<JoinCode>().Value;
            JoinCodeLabel.text = $"Join code: {joinCode}";
        }
    }
}

public class RelayDriverConstructor : INetworkStreamDriverConstructor
{
    RelayServerData m_RelayClientData;
    RelayServerData m_RelayServerData;

    public RelayDriverConstructor(RelayServerData serverData, RelayServerData clientData)
    {
        m_RelayServerData = serverData;
        m_RelayClientData = clientData;
    }

    /// <summary>
    /// This method will ensure that we only register a UDP driver. This forces the client to always go through the
    /// relay service. In a setup with client-hosted servers it will make sense to allow for IPC connections and
    /// UDP both, which is what invoking
    /// <see cref="DefaultDriverBuilder.RegisterClientDriver(World, ref NetworkDriverStore, NetDebug, ref RelayServerData)"/> will do.
    /// </summary>
    public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
    {
        var settings = DefaultDriverBuilder.GetNetworkSettings();
        settings.WithRelayParameters(ref m_RelayClientData);
        DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, settings);
    }

    public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
    {
        DefaultDriverBuilder.RegisterServerDriver(world, ref driverStore, netDebug, ref m_RelayServerData);
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
