using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public class PresentationGO : MonoBehaviour
    {
        public GameObject prefab;
    }

    public class PresentationGOBaker : Baker<PresentationGO>
    {
        public override void Bake(PresentationGO authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponentObject(entity, new PresentationGOComponent
            {
                prefab = authoring.prefab
            });


        }
    }

    public class TransformGO : ICleanupComponentData
    {
        public Transform value;
    }


    public class PresentationGOComponent : IComponentData
    {
        public GameObject prefab;
    }




    public class EntityLink : MonoBehaviour
    {
        private Entity entity;
        private EntityManager manager;

        public void AssignEntity(Entity e, EntityManager m)
        {
            entity = e;
            manager = m;
        }
        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.DestroyEntity(entity);
            }
        }

    }

    //public partial class PresentationGoSystem : SystemBase
    //{
    //    protected override void OnUpdate()
    //    {
    //        var endECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
    //        var beginECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

    //        foreach (var (pGO, e) in SystemAPI.Query<PresentationGOComponent>().WithEntityAccess())
    //        {

    //            var instance = GameObject.Instantiate(pGO.prefab);
    //            beginECB.AddComponent(e, new TransformGO { value = instance.transform });
    //            instance.AddComponent<EntityLink>().AssignEntity(e, EntityManager);

    //            if (instance.TryGetComponent<VisualMessage>(out var vmComp))
    //            {
    //                beginECB.AddComponent(e, new VisualMessageGO { value = vmComp });                 
    //            }

    //            beginECB.RemoveComponent<PresentationGOComponent>(e);
    //        }

    //        foreach (var (ltw, trans) in SystemAPI.Query<RefRO<LocalToWorld>, TransformGO>())
    //        {
    //            var transData = trans.value;
    //            transData.position = ltw.ValueRO.Position;
    //            transData.rotation = ltw.ValueRO.Rotation;
    //        }


    //        foreach (var (trans, e) in SystemAPI.Query<TransformGO>().WithNone<LocalTransform>().WithEntityAccess())
    //        {
    //            GameObject.Destroy(trans.value.gameObject);
    //            endECB.RemoveComponent<TransformGO>(e);
    //        }

    //    }
    //}


}
