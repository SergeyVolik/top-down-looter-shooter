using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(OrbitCameraSystem))]
public partial class MainCameraSystem : SystemBase
{

    protected override void OnCreate()
    {
        base.OnCreate();


        //RequireForUpdate<SystemAPI.ManagedAPI.UnityEngineComponent<Camera>>();
    }

    protected override void OnUpdate()
    {
        if (SystemAPI.ManagedAPI.TryGetSingleton<Camera>(out var camera))
        {
         
            if (camera && SystemAPI.HasSingleton<MainEntityCamera>())
            {
                Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<MainEntityCamera>();

                LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity);
                camera.transform.position = targetLocalToWorld.Position;
                camera.transform.rotation = targetLocalToWorld.Rotation;
            }
        }
    }
}