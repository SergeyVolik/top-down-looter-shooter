using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public GameObject obj;
    public float updateRate = 0.1f;
}

public struct FollowTargetComponent : IComponentData
{
    public Entity entity;
    public float updateRate;
    public float nextUpdateTime;
}
public class FollowTarget_Baker : Baker<FollowTarget>
{
    public override void Bake(FollowTarget authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        var followEntity = GetEntity(authoring.obj, TransformUsageFlags.Dynamic);

        AddComponent(entity, new FollowTargetComponent
        {
            entity = followEntity,
            updateRate = authoring.updateRate,
            nextUpdateTime = Time.time + authoring.updateRate
        });
    }
}