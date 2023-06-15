using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ThirdPersonPlayerAuthoring : MonoBehaviour
{
    public GameObject ControlledCharacter;
    public GameObject ControlledCamera;
}

[Serializable]
public struct ThirdPersonPlayer : IComponentData
{
    public Entity ControlledCharacter;
    public Entity ControlledCamera;

    [NonSerialized]
    public uint LastInputsProcessingTick;
}

public class ThirdPersonPlayerAuthoringBaker : Baker<ThirdPersonPlayerAuthoring>
{
    public override void Bake(ThirdPersonPlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new ThirdPersonPlayer
        {
            ControlledCamera = GetEntity(authoring.ControlledCamera, TransformUsageFlags.Dynamic),
            ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic),


        });
    }
}