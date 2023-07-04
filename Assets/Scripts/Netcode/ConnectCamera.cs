using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

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