using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.NetCode;

public class PlayerTracker : MonoBehaviour
{
    private EntityManager em;
    private Entity entity;

    private void Start()
    {
      
        em = WorldExt.GetClientWorld().EntityManager;
        entity = em.CreateEntity();
        em.SetName(entity, name);
        em.AddComponentObject(entity, this);
    }


}


[UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
public partial class PlayerTrackerSystem : SystemBase
{
    
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<PlayerTracker>();
        //Enabled = false;
    }

    protected override void OnUpdate()
    {

        if (!SystemAPI.ManagedAPI.TryGetSingleton<PlayerTracker>(out var playerEntity))
        {
            Debug.LogError("PlayerTracker not exist");
            return;
        }

        foreach (var localToWorld in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<GhostOwnerIsLocal, ThirdPersonPlayer>())
        {
            playerEntity.transform.SetPositionAndRotation(localToWorld.ValueRO.Position, localToWorld.ValueRO.Rotation);
        }

    }
}