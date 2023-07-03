using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

namespace SV.ECS
{
    public struct PlayerSpawned : IComponentData { }

    public struct ConnectionOwner : IComponentData
    {
        [GhostField]
        public Entity Entity;
    }

    [UpdateBefore(typeof(OrbitCameraSystem))]
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ConnectCamera : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<OrbitCamera>();
            RequireForUpdate<NetworkStreamInGame>();

        }

        protected override void OnUpdate()
        {
           

          

            var camera = SystemAPI.GetSingletonEntity<OrbitCamera>();

            var orbitCamera = SystemAPI.GetComponentLookup<OrbitCamera>();
            var ignoreItems = SystemAPI.GetBufferLookup<OrbitCameraIgnoredEntityBufferElement>(isReadOnly: false);

            var commandBuffer2 = new EntityCommandBuffer(Allocator.Temp);
            //ConnectCamera
            foreach (var (tpp, e) in SystemAPI.Query<ThirdPersonPlayer>().WithAll<GhostOwnerIsLocal>().WithNone<CameraConnected>().WithEntityAccess())
            {
                Debug.Log("Camera Connected");
                if (ignoreItems.TryGetBuffer(camera, out var buffer))
                {
                    buffer.Add(new OrbitCameraIgnoredEntityBufferElement { Entity = e });


                }

                if (orbitCamera.HasComponent(camera))
                {
                    orbitCamera.GetRefRW(camera).ValueRW.FollowedCharacterEntity = e;


                }

                commandBuffer2.AddComponent<CameraConnected>(e);
            }

            commandBuffer2.Playback(EntityManager);
        }
    }

    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class SpawnPlayer1 : SystemBase
    {
        private EntityQuery m_NewPlayers;

        protected override void OnCreate()
        {
            RequireForUpdate(m_NewPlayers);
            // Must wait for the spawner entity scene to be streamed in, most likely instantaneous in
            // this sample but good to be sure
            RequireForUpdate<PlayerSpawnP>();
            RequireForUpdate<NetworkStreamInGame>();

        }

        protected override void OnUpdate()
        {

           
            var prefab = SystemAPI.GetSingleton<PlayerSpawnP>().playerPrefab;
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithName("SpawnPlayer").WithStoreEntityQueryInField(ref m_NewPlayers).WithNone<PlayerSpawned>().ForEach(
                (Entity connectionEntity, in NetworkStreamInGame req, in NetworkId networkId) =>
                {
                    Debug.Log($"Spawning player for connection {networkId.Value}");
                    var player = commandBuffer.Instantiate(prefab);

                    // The network ID owner must be set on the ghost owner component on the players
                    // this is used internally for example to set up the CommandTarget properly
                    commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
                    
                    // This is to support thin client players and you don't normally need to do this when the
                    // auto command target feature is used (enabled on the ghost authoring component on the prefab).
                    // See the ThinClients sample for more details.
                    commandBuffer.SetComponent(connectionEntity, new CommandTarget() { targetEntity = player });

                    // Mark that this connection has had a player spawned for it so we won't process it again
                    commandBuffer.AddComponent<PlayerSpawned>(connectionEntity);

                    // Add the player to the linked entity group on the connection so it is destroyed
                    // automatically on disconnect (destroyed with connection entity destruction)
                    commandBuffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = player });

                    commandBuffer.AddComponent(player, new ConnectionOwner { Entity = connectionEntity });


                }).Run();
            commandBuffer.Playback(EntityManager);
        }
    }
    

}
