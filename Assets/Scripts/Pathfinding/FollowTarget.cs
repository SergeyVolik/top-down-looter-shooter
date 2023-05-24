using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public GameObject obj;
}

public struct FollowTargetComponent : IComponentData
{
    public Entity entity;
}
public class FollowTarget_Baker : Baker<FollowTarget>
{
    public override void Bake(FollowTarget authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        var followEntity = GetEntity(authoring.obj, TransformUsageFlags.Dynamic);

        AddComponent(entity, new FollowTargetComponent
        {
             entity = followEntity
        });
    }
}