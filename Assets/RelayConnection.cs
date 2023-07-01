using SV.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayConnection : MonoBehaviour
{


    public static RelayConnection Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    private Allocation allocation;
    private JoinAllocation joinAllocation;


    private void OnDestroy()
    {
        DestroyClientServerWorlds();
    }

    public void LeaveGame()
    {
        DestroyClientServerWorlds();

        var handle = SceneManager.UnloadSceneAsync(2);

        handle.completed += (ers) =>
        {
            SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
        };


    }

    private static void DestroyClientServerWorlds()
    {
        var clientServerWorlds = new List<World>();
        foreach (var world in World.All)
        {
            if (world.IsClient() || world.IsServer())
                clientServerWorlds.Add(world);
        }


        foreach (var item in clientServerWorlds)
        {
            item.Dispose();
        }
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


    public async Task JoinAsClient(string joinCode)
    {
        DestroyClientServerWorlds();

        await ConnectToRelayServer(joinCode);

        Debug.Log($"Cliend connected. replayCode: {joinCode}");

    }

    async Task ConnectToRelayServer(string hostServerJoinCode)
    {
        joinAllocation = await RelayService.Instance.JoinAllocationAsync(hostServerJoinCode);
        var relayClientData = PlayerRelayData(joinAllocation);

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





    public async Task<string> HostServerAndClient()
    {
        if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
        {
            UnityEngine.Debug.LogError($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
            return null;
        }

        DestroyClientServerWorlds();


        allocation = await RelayService.Instance.CreateAllocationAsync(LobbyManager.maxPlayers);
        Debug.Log("RelayService CreateAllocationAsync executed");

        hostServerJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log($"GetJoinCodeAsync {hostServerJoinCode}");


        joinAllocation = await RelayService.Instance.JoinAllocationAsync(hostServerJoinCode);


        CreateServerClientWorlds();

        return hostServerJoinCode;
    }

    private void CreateServerClientWorlds()
    {
        var relayClientData = PlayerRelayData(joinAllocation);

        Debug.Log($"client Address {relayClientData.Endpoint.Address}");
        var relayServerData = HostRelayData(allocation);
        Debug.Log($"server Address {relayServerData.Endpoint.Address}");

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
        server.EntityManager.SetName(joinCodeEntity, "joinCode");
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



    private string hostServerJoinCode;


    #endregion



    public void StartClientServerLocal(ushort port = 7979)
    {
        if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
        {
            Debug.LogError($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
            return;
        }

        var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");



        //Destroy the local simulation world to avoid the game scene to be loaded into it
        //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
        //and other issues.
        DestroyLocalSimulationWorld();
        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = server;


        var joinCodeEntity = server.EntityManager.CreateEntity(ComponentType.ReadOnly<JoinCode>());
        server.EntityManager.SetName(joinCodeEntity, "joinCode");
        server.EntityManager.SetComponentData(joinCodeEntity, new JoinCode { Value = $"127.0.0.1:{port}" });

        NetworkEndpoint ep = NetworkEndpoint.AnyIpv4.WithPort(port);
        {
            using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(ep);
        }

        ep = NetworkEndpoint.LoopbackIpv4.WithPort(port);
        {
            using var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
        }
    }

    public void ConnectToServer(string ip = "127.0.0.1", ushort port = 7979)
    {
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        SceneManager.LoadScene("FrontendHUD");
        DestroyLocalSimulationWorld();

        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = client;


        var ep = NetworkEndpoint.Parse(ip, port);
        {
            using var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
        }
    }


}

public struct JoinCode : IComponentData
{
    public FixedString64Bytes Value;
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

