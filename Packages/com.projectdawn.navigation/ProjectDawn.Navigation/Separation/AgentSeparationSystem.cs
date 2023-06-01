using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using static Unity.Entities.SystemAPI;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// System that calculates separation direction from nearby agents.
    /// </summary>
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AgentForceSystemGroup))]
    public partial struct AgentSeparationSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var spatial = GetSingleton<AgentSpatialPartitioningSystem.Singleton>();
            new AgentSeparationJob
            {
                Spatial = spatial,
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct AgentSeparationJob : IJobEntity
        {
            [ReadOnly]
            public AgentSpatialPartitioningSystem.Singleton Spatial;

            public void Execute(Entity entity, ref AgentBody body, in AgentSeparation separation, in AgentShape shape, in LocalTransform transform)
            {
                float radius = math.max(shape.Radius, separation.Radius);

                var action = new Action
                {
                    Entity = entity,
                    Body = body,
                    Shape = shape,
                    Separation = separation,
                    Transform = transform,
                    Radius = radius,
                };

                Spatial.QuerySphere(transform.Position, radius, ref action);

                if (action.Weight > 0)
                {
                    body.Force += action.Force * separation.Weight;
                }
            }

            struct Action : ISpatialQueryEntity
            {
                public Entity Entity;
                public AgentBody Body;
                public AgentShape Shape;
                public AgentSeparation Separation;
                public LocalTransform Transform;
                public float Radius;

                public float3 Force;
                public float Weight; 

                public void Execute(Entity otherEntity, AgentBody otherBody, AgentShape otherShape, LocalTransform otherTransform)
                {
                    float3 towards = Transform.Position - otherTransform.Position;
                    float distance = math.length(towards);
                    float radiusSum = Radius + otherShape.Radius;
                    if (distance > radiusSum || Entity == otherEntity)
                        return;

                    Force += math.normalizesafe(towards) * ((radiusSum - distance) / radiusSum);
                    Weight++;
                }
            }
        }
    }
}
