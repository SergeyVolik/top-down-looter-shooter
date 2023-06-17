using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Rival;
using SV.ECS;

[UpdateInGroup(typeof(KinematicCharacterUpdateGroup), OrderFirst = true)]
[UpdateAfter(typeof(TopDownChracterInputSystem))]
[UpdateBefore(typeof(TopDownCharacterMovementSystem))]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class TopDownCharacterRotationSystem : SystemBase
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
                    typeof(TopDownCharacterComponent),
                    typeof(TopDownCharacterInputs),
                }),
        });

        RequireForUpdate(CharacterQuery);
    }


    [WithNone(typeof(DetectedTargetComponent))]
    [BurstCompile]
    public partial struct TopDownCharacterRotationJob : IJobEntity
    {
        public float deltaTime;
        public float fixedDeltaTime;

        [BurstCompile]
        public void Execute(
            ref LocalTransform localTransfrom,
            ref TopDownCharacterComponent character,
            in TopDownCharacterInputs characterInputs,
            in KinematicCharacterBody characterBody)
        {

            // Rotate towards move direction
            if (math.lengthsq(characterInputs.MoveVector) > 0f)
            {
                var rot = localTransfrom.Rotation;
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref rot, deltaTime, math.normalizesafe(characterInputs.MoveVector), MathUtilities.GetUpFromRotation(localTransfrom.Rotation), character.RotationSharpness);
                localTransfrom.Rotation = rot;
            }

            // Add rotation from parent body to the character rotation
            // (this is for allowing a rotating moving platform to rotate your character as well, and handle interpolation properly)
            KinematicCharacterUtilities.ApplyParentRotationToTargetRotation(ref localTransfrom.Rotation, in characterBody, fixedDeltaTime, deltaTime);
        }
    }

    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float fixedDeltaTime = FixedStepSimulationSystemGroup.RateManager.Timestep;

        var job = new TopDownCharacterRotationJob
        {
            deltaTime = deltaTime,
            fixedDeltaTime = fixedDeltaTime,
        };
        //job.Run();
        Dependency = job.Schedule(Dependency);


    }
}
