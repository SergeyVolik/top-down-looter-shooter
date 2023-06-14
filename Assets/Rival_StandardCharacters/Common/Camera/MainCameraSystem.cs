using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class MainCameraSystem : SystemBase
{
    public Transform CameraGameObjectTransform;

    protected override void OnUpdate()
    {
        if (CameraGameObjectTransform && SystemAPI.HasSingleton<MainEntityCamera>())
        {
            Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<MainEntityCamera>();

            LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity);
            CameraGameObjectTransform.position = targetLocalToWorld.Position;
            CameraGameObjectTransform.rotation = targetLocalToWorld.Rotation;
        }
    }
}