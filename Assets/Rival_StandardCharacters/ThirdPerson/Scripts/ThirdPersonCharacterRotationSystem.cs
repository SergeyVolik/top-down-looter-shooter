using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;
using Unity.NetCode;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateAfter(typeof(ThirdPersonPlayerInputSystem))]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial class ThirdPersonCharacterRotationSystem : SystemBase
{
    public FixedStepSimulationSystemGroup FixedStepSimulationSystemGroup;
    public EntityQuery CharacterQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        
        FixedStepSimulationSystemGroup = World.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();

        CharacterQuery = this.CheckedStateRef.GetEntityQuery(new EntityQueryDesc
        {
            All = MiscUtilities.CombineArrays(
                KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                new ComponentType[]
                {
                    typeof(ThirdPersonCharacterComponent),
                    typeof(ThirdPersonCharacterInputs),
                    typeof(PredictedGhost)
                }),
        });

        RequireForUpdate(CharacterQuery);
        RequireForUpdate<NetworkStreamInGame>();
    }

    [BurstCompile]
    public partial struct ThirdPersonCharacterRotationJob : IJobEntity
    {
        public float deltaTime;
        public float fixedDeltaTime;

        [BurstCompile]
        public void Execute(
            ref LocalTransform characterRotation,
            ref ThirdPersonCharacterComponent character,
            in ThirdPersonCharacterInputs characterInputs,
            in KinematicCharacterBody characterBody)
        {
            // Rotate towards move direction
            if (math.lengthsq(characterInputs.MoveVector) > 0f)
            {
                var rot = characterRotation.Rotation;
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref rot, deltaTime, math.normalizesafe(characterInputs.MoveVector), MathUtilities.GetUpFromRotation(characterRotation.Rotation), character.RotationSharpness);
                characterRotation.Rotation = rot;
            }

            // Add rotation from parent body to the character rotation
            // (this is for allowing a rotating moving platform to rotate your character as well, and handle interpolation properly)
            KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref characterRotation, in characterBody, fixedDeltaTime, deltaTime);
        }
    }

    protected unsafe override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float fixedDeltaTime = FixedStepSimulationSystemGroup.RateManager.Timestep;

        var job = new ThirdPersonCharacterRotationJob
        {
            deltaTime = deltaTime,
            fixedDeltaTime = fixedDeltaTime,
        };

        Dependency = job.Schedule(Dependency);


    }
}
