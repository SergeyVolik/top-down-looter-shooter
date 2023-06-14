using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FirstPersonPlayerAuthoring : MonoBehaviour
{
    public GameObject ControlledCharacter;
    public float RotationSpeed;
}

[Serializable]
public struct FirstPersonPlayer : IComponentData
{
    public Entity ControlledCharacter;
    public float RotationSpeed;

    [NonSerialized]
    public uint LastInputsProcessingTick;
}

public class FirstPersonPlayerBaker : Baker<FirstPersonPlayerAuthoring>
{
    public override void Bake(FirstPersonPlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new FirstPersonPlayer
        {
            RotationSpeed = authoring.RotationSpeed,
            ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic)
        });
    }
}
