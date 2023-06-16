using ProjectDawn.Navigation;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
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

    public class AnimatorGO : IComponentData
    {
        public Animator animator;
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


    public partial class AnimatorSystem : SystemBase
    {
        private int moveParam;

        protected override void OnCreate()
        {
            base.OnCreate();
            moveParam = Animator.StringToHash("Move");

        }
        protected override void OnUpdate()
        {
            foreach (var (animator, input) in SystemAPI.Query<AnimatorGO, RefRO<TopDownCharacterInputs>>())
            {
                animator.animator.SetFloat(moveParam, math.length(input.ValueRO.MoveVector));
            }
        }
    }

    public partial class AnimatorAISystem : SystemBase
    {
        private int moveParam;

        protected override void OnCreate()
        {
            base.OnCreate();
            moveParam = Animator.StringToHash("Move");

        }
        protected override void OnUpdate()
        {
            var dletaTime = SystemAPI.Time.DeltaTime;

            foreach (var (animator, input) in SystemAPI.Query<AnimatorGO, RefRO<AgentBody>>())
            {

                var value1 = math.length(input.ValueRO.Velocity);

                var value2 = animator.animator.GetFloat(moveParam);

                if (value1 > value2)
                {
                    var temp = value2;
                    value2 = value1;
                    value1 = value2;
                }
                animator.animator.SetFloat(moveParam, math.lerp(value1, value2, dletaTime));
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class PresentationGoSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var endECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            var beginECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

            foreach (var (pGO, e) in SystemAPI.Query<PresentationGOComponent>().WithEntityAccess())
            {

                var instance = GameObject.Instantiate(pGO.prefab);
                beginECB.AddComponent(e, new TransformGO { value = instance.transform });
                instance.AddComponent<EntityLink>().AssignEntity(e, EntityManager);

                if (instance.TryGetComponent<VisualMessage>(out var vmComp))
                {
                    beginECB.AddComponent(e, new VisualMessageGO { value = vmComp });
                }

                if (instance.TryGetComponent<Animator>(out var aniamtor))
                {
                    beginECB.AddComponent(e, new AnimatorGO { animator = aniamtor });
                }

                beginECB.RemoveComponent<PresentationGOComponent>(e);
            }

            foreach (var (ltw, trans) in SystemAPI.Query<RefRO<LocalToWorld>, TransformGO>())
            {
                var transData = trans.value;
                transData.position = ltw.ValueRO.Position;
                transData.rotation = ltw.ValueRO.Rotation;
            }


            foreach (var (trans, e) in SystemAPI.Query<TransformGO>().WithNone<LocalTransform>().WithEntityAccess())
            {
                GameObject.Destroy(trans.value.gameObject);
                endECB.RemoveComponent<TransformGO>(e);
            }

        }
    }


}
