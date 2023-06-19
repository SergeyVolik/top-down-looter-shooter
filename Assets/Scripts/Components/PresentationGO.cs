using ProjectDawn.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    [DisallowMultipleComponent]
    public class PresentationGO : MonoBehaviour
    {


        public PresentationGOComponent.Type type = PresentationGOComponent.Type.Single;

        public GameObject prefab;

        public GameObject[] prefabs;

        public void OnEnable()
        {
            
        }
        public class Baker : Baker<PresentationGO>
        {


            public override void Bake(PresentationGO authoring)
            {
                if (!authoring.enabled)
                    return;

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var transform = GetComponent<Transform>();

                AddComponentObject(entity, new PresentationGOComponent
                {
                    type = authoring.type,
                    prefabs = authoring.prefabs,
                    prefab = authoring.prefab
                });




            }
        }
    }



    public class PresentationGOComponent : IComponentData
    {
        public enum Type
        {
            Single,
            RandomPrefab
        }


        public Type type = Type.Single;

        public GameObject prefab;

        public GameObject[] prefabs;
    }
    public struct CopyEntityTransformToGoTransform : IComponentData
    {

    }
    public class PresentationInstance : IComponentData, IDisposable, ICloneable
    {
        public GameObject Instance;

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Instance);
        }
        public object Clone()
        {
            return new PresentationInstance { Instance = UnityEngine.Object.Instantiate(Instance) };
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
            foreach (var (input, e) in SystemAPI.Query<RefRO<TopDownCharacterInputs>>().WithAll<Animator>().WithEntityAccess().WithChangeFilter<TopDownCharacterInputs>())
            {
                //Debug.Log("Update Player Animator");

                var animator = EntityManager.GetComponentObject<Animator>(e);
                animator.SetFloat(moveParam, math.length(input.ValueRO.MoveVector));
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

            foreach (var (input, e) in SystemAPI.Query<RefRO<AgentBody>>().WithAll<Animator>().WithEntityAccess())
            {
                var animator = EntityManager.GetComponentObject<Animator>(e);
                var value1 = math.length(input.ValueRO.Velocity);

                var value2 = animator.GetFloat(moveParam);

                if (value1 > value2)
                {
                    var temp = value2;
                    value2 = value1;
                    value1 = temp;
                }

                animator.SetFloat(moveParam, math.lerp(value1, value2, dletaTime));
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.BakingSystem)]

    public partial class PresentationGoSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var query = SystemAPI.QueryBuilder().WithAll<PresentationGOComponent, LocalTransform>().Build();
            var entities = query.ToEntityArray(Allocator.Temp);



            foreach (var e in entities)
            {
                var lt = EntityManager.GetComponentData<LocalTransform>(e);
                var pGO = EntityManager.GetComponentData<PresentationGOComponent>(e);

                var prefab = pGO.type == PresentationGOComponent.Type.Single ? pGO.prefab : pGO.prefabs[UnityEngine.Random.Range(0, pGO.prefabs.Length)];
                var instance = GameObject.Instantiate(prefab);

                var trans = instance.transform;
                trans.position = lt.Position;
                trans.rotation = lt.Rotation;

                EntityManager.AddComponentObject(e, instance.transform);
                EntityManager.AddComponent<CopyEntityTransformToGoTransform>(e);

                if (EntityManager.HasComponent<PresentationInstance>(e))
                {
                    EntityManager.RemoveComponent<PresentationInstance>(e);
                }

                EntityManager.AddComponentObject(e, new PresentationInstance { Instance = instance });

                instance.hideFlags |= HideFlags.HideAndDontSave;

                if (instance.TryGetComponent<VisualMessage>(out var vmComp))
                {
                    EntityManager.AddComponentObject(e, new VisualMessageGO { value = vmComp });
                }

                if (instance.TryGetComponent<Animator>(out var aniamtor))
                {

                    EntityManager.AddComponentObject(e, aniamtor);
                }

                EntityManager.RemoveComponent<PresentationGOComponent>(e);
            }
           
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class SyncPresentationWithEntity : SystemBase
    {
        protected override void OnUpdate()
        {


            foreach (var (ltw, e) in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<Transform, CopyEntityTransformToGoTransform>().WithEntityAccess())
            {
                var transData = EntityManager.GetComponentObject<Transform>(e);

                transData.position = ltw.ValueRO.Position;
                transData.rotation = ltw.ValueRO.Rotation;
            }


        }
    }


}
