using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FirstPersonPlayerSystem))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class FirstPersonCharacterRotationSystem : SystemBase
{
    public FixedStepSimulationSystemGroup FixedStepSimulationSystemGroup;
    public EntityQuery CharacterQuery;

    protected override void OnCreate()
    {
        FixedStepSimulationSystemGroup = World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();

        CharacterQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = MiscUtilities.CombineArrays(
                KinematicCharacterUtilities.GetCoreCharacterComponentTypes(),
                new ComponentType[]
                {
                    typeof(FirstPersonCharacterComponent),
                    typeof(FirstPersonCharacterInputs),
                }),
        });

        RequireForUpdate(CharacterQuery);
    }


    [BurstCompile]
    public partial struct FirstPersonCharacterRotationJob : IJobEntity
    {
        public float deltaTime;
        public float fixedDeltaTime;

        public ComponentLookup<LocalTransform> localTransform;

        [BurstCompile]
        public void Execute(Entity entity,
            ref CharacterInterpolation characterInterpolation,
            ref FirstPersonCharacterComponent character,
            in FirstPersonCharacterInputs characterInputs,
            in KinematicCharacterBody characterBody)
        {
            var characterRotation = localTransform.GetRefRW(entity);
            var localViewRotation = localTransform.GetRefRW(character.CharacterViewEntity);

            // Compute character & view rotations from rotation input
            FirstPersonCharacterUtilities.ComputeFinalRotationsFromRotationDelta(
                ref characterRotation.ValueRW,
                ref character.ViewPitchDegrees,
                characterInputs.LookYawPitchDegrees,
                0f,
                character.MinVAngle,
                character.MaxVAngle,
                out float canceledPitchDegrees,
                ref localViewRotation.ValueRW);

            // Add rotation from parent body to the character rotation
            // (this is for allowing a rotating moving platform to rotate your character as well, and handle interpolation properly)
            KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref characterRotation.ValueRW, in characterBody, fixedDeltaTime, deltaTime);


        }
    }

    protected unsafe override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float fixedDeltaTime = FixedStepSimulationSystemGroup.RateManager.Timestep;


        new FirstPersonCharacterRotationJob
        {
            deltaTime = deltaTime,
            fixedDeltaTime = fixedDeltaTime,
            localTransform = SystemAPI.GetComponentLookup<LocalTransform>()
        }.Schedule();


    }
}