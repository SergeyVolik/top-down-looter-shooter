using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class TopDownPlayerAuthoring : MonoBehaviour
{
    
}

[GhostComponent(PrefabType = GhostPrefabType.All)]
[Serializable]
public struct TopDownPlayer : IInputComponentData
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