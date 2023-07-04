using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CameraIgnoreTargetAuthoring : MonoBehaviour
{

    public class Baker : Baker<CameraIgnoreTargetAuthoring>
    {
        public override void Bake(CameraIgnoreTargetAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<CameraIgnore>(e);
        }
    }



}



public struct CameraIgnore : IComponentData
{
    
}


