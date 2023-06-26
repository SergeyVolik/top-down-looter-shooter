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


public struct ThirdPersonPlayer : IComponentData
{

   
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