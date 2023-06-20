using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class VisualMessageManager : MonoBehaviour
{

    public GameObject prefab;


}


public struct VisualMessageData : IComponentData
{

    public Entity prefab;


}
public class VisualMessageDataBaker : Baker<VisualMessageManager>
{
    public override void Bake(VisualMessageManager authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new VisualMessageData
        {
            prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
        });



    }
}
[UpdateAfter(typeof(ApplyDamageSystem))]
[UpdateBefore(typeof(ClearDamageListSystem))]
public partial class VisualMessageSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<VisualMessageData>();

      
    }

    protected override void OnUpdate()
    {
        var vmd = SystemAPI.GetSingleton<VisualMessageData>();

        var ecbSys = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSys.CreateCommandBuffer(World.Unmanaged);

        foreach (var (damageList, visualMessage, ltw) in SystemAPI.Query<DynamicBuffer<DamageToApplyComponent>, RefRO<DamageVisualMessageComponent>, RefRO<LocalToWorld>>())
        {
            foreach (var damage in damageList)
            {
                var vmEntity = ecb.Instantiate(vmd.prefab);


                var sm = new ShowVisualMessageComponent();

                if (damage.damage > 0)
                {
                    sm.color = Color.red;
                    sm.text = $"-{damage.damage} hp";
                }
                else {
                    sm.color = Color.green;
                    sm.text = $"+{damage.damage} hp";
                }

                sm.pos = ltw.ValueRO.Position;
                ecb.SetComponent(vmEntity, sm);
                ecb.SetComponentEnabled<ShowVisualMessageComponent>(vmEntity, true);


            }


        }
        foreach (var ltw in SystemAPI
            .Query<RefRO<LocalToWorld>>()
            .WithAll<CollectedVisualMessageComponent, CollectedComponent, CollectedComponent>())
        {

            var vmEntity = ecb.Instantiate(vmd.prefab);


            var sm = new ShowVisualMessageComponent();
            sm.color = Color.green;
            sm.text = $"+1 m";
            sm.pos = ltw.ValueRO.Position;
            ecb.AddComponent(vmEntity, sm);

        }

        foreach (var (sm, visualMessage, trans, e) in SystemAPI.Query<ShowVisualMessageComponent, VisualMessageGO, RefRW<LocalTransform>>().WithEntityAccess())
        {
            visualMessage.text.color = sm.color;
            visualMessage.text.text = sm.text;

            

            var pos = sm.pos;

            pos.y += 2f;
            trans.ValueRW.Position = pos;
            ecb.SetComponentEnabled<ShowVisualMessageComponent>(e, false);
        }
    }
}