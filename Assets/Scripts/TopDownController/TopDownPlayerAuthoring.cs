using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TopDownPlayerAuthoring : MonoBehaviour
{
    
}

[Serializable]
public struct TopDownPlayer : IComponentData
{

    [NonSerialized]
    public uint LastInputsProcessingTick;
}

public class ThirdPersonPlayerAuthoringBaker : Baker<TopDownPlayerAuthoring>
{
    public override void Bake(TopDownPlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new TopDownPlayer
        {
           

        });
    }
}