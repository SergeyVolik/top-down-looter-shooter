using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    private void Start()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entity = em.CreateEntity();
        em.SetName(entity, name);
        em.AddComponentObject(entity, this);
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
public partial class PlayerTrackerSystem : SystemBase
{
    Entity playerEntity;
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<PlayerComponent>();
    }

    protected override void OnUpdate()
    {
        if (playerEntity == Entity.Null)
        {
            if (!SystemAPI.TryGetSingletonEntity<PlayerComponent>(out playerEntity))
                return;
        }

        var localToWorld = SystemAPI.GetComponent<LocalToWorld>(playerEntity);

        Entities.ForEach((PlayerTracker tracker) =>
        {
            tracker.transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);

        }).WithoutBurst().Run();
    }
}