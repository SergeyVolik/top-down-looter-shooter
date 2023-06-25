using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CameraTargetAuthoring : MonoBehaviour
{
    public GameObject TargetEntity;
}


[Serializable]
public struct CameraTarget : IComponentData
{
    public Entity TargetEntity;
}


public class CameraTargetAuthoringBaker : Baker<CameraTargetAuthoring>
{
    public override void Bake(CameraTargetAuthoring authoring)
    {
        var e = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(e, new CameraTarget
        {
            TargetEntity = GetEntity(authoring.TargetEntity, TransformUsageFlags.Dynamic)
        });
    }
}

