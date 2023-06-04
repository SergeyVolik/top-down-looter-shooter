using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{

    [UpdateInGroup(typeof(DamageBufferSystemGroup))]
    [UpdateAfter(typeof(AddDamageFromVisitedProjectileSystem))]
    public partial class ApplyDamageSystem : SystemBase
    {



        EntityQuery query;

       
        [BurstCompile]
        public partial struct ApplayDamageJob : IJobEntity
        {


            public float time;
            public EntityCommandBuffer buffer;
            public void Execute(Entity entity, ref HealthComponent healthComp, ref DynamicBuffer<DamageToApplyComponent> damageList)
            {
                var health = healthComp.value;
                //Debug.Log($"time {time} damageList: {damageList.Length}");
                for (int i = 0; i < damageList.Length; i++)
                {
                    var damage = damageList[i];
                    //Debug.Log(damage);
                    health -= damage.damage;

                    if (health <= 0)
                    {
                        health = 0;


                        buffer.SetComponentEnabled<DeadComponent>(entity, true);

                        buffer.SetComponent(entity, new DeadComponent
                        {
                            killDamageIfno = damage
                        });


                        break;
                    }
                }

                healthComp.value = health;

            }

        }


        protected override void OnCreate()
        {

            query = GetEntityQuery(typeof(HealthComponent), typeof(DamageToApplyComponent));
            RequireForUpdate(query);


        }


        protected override void OnUpdate()
        {
            if (query.CalculateEntityCount() == 0)
                return;


            var ecbSys = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSys.CreateCommandBuffer(World.Unmanaged);

            var job = new ApplayDamageJob
            {
                time = (float)SystemAPI.Time.ElapsedTime,
                buffer = ecb,

            };


            Dependency = job.Schedule(query, Dependency);
            //job.Run();



        }
    }

  










   
}
