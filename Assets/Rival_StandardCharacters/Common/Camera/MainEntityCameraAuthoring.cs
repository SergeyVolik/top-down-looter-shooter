using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MainEntityCameraAuthoring : MonoBehaviour
{

}


[Serializable]
public struct MainEntityCamera : IComponentData
{

}

public class MyComponentAuthoringBaker : Baker<MainEntityCameraAuthoring>
{
    public override void Bake(MainEntityCameraAuthoring authoring)
    {
        AddComponent(GetEntity(TransformUsageFlags.Dynamic), new MainEntityCamera
        {

        });
    }
}
