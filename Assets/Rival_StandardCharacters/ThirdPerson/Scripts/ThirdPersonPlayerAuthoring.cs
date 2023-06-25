using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ThirdPersonPlayerAuthoring : MonoBehaviour
{

}



public struct CameraConnected : IComponentData
{

}

[Serializable]
public struct ThirdPersonPlayer : IComponentData
{

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
        


        });
    }
}