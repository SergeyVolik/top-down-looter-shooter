using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Rival;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
public partial class FirstPersonPlayerSystem : SystemBase
{
    public FixedUpdateTickSystem FixedUpdateTickSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        FixedUpdateTickSystem = World.GetExistingSystemManaged<FixedUpdateTickSystem>();
    }

    [BurstCompile]
    public partial struct FirstPersonPlayerJob : IJobEntity
    {

        public float2 lookInput;
        public float2 moveInput;
        public bool jumpInput;
        public ComponentLookup<FirstPersonCharacterInputs> firstPersonCharacterInputs;

        [ReadOnly]
        public ComponentLookup<LocalToWorld> localToWorld;

        [ReadOnly]
        public ComponentLookup<FirstPersonCharacterComponent> firstPersonCharacterComponent;
        public uint fixedTick;

        [BurstCompile]
        public void Execute(ref FirstPersonPlayer player)
        {
            if (firstPersonCharacterInputs.HasComponent(player.ControlledCharacter) && firstPersonCharacterComponent.HasComponent(player.ControlledCharacter))
            {
                var characterInputs = firstPersonCharacterInputs.GetRefRW(player.ControlledCharacter);
             

                var controllerLocalTransform = localToWorld.GetRefRO(player.ControlledCharacter);
             

                quaternion characterRotation = controllerLocalTransform.ValueRO.Rotation;
              

                // Look
                characterInputs.ValueRW.LookYawPitchDegrees = lookInput * player.RotationSpeed;

                // Move
                float3 characterForward = math.mul(characterRotation, math.forward());
                float3 characterRight = math.mul(characterRotation, math.right());
                characterInputs.ValueRW.MoveVector = (moveInput.y * characterForward) + (moveInput.x * characterRight);
                characterInputs.ValueRW.MoveVector = Rival.MathUtilities.ClampToMaxLength(characterInputs.ValueRO.MoveVector, 1f);

                // Jump
                // Punctual input presses need special handling when they will be used in a fixed step system.
                // We essentially need to remember if the button was pressed at any point over the last fixed update
                if (player.LastInputsProcessingTick == fixedTick)
                {
                    characterInputs.ValueRW.JumpRequested = jumpInput || characterInputs.ValueRO.JumpRequested;
                }
                else
                {
                    characterInputs.ValueRW.JumpRequested = jumpInput;
                }

                player.LastInputsProcessingTick = fixedTick;


            }
        }
    }

    protected override void OnUpdate()
    {
        uint fixedTick = FixedUpdateTickSystem.FixedTick;

        // Gather input
        float2 moveInput = float2.zero;
        moveInput.y += Input.GetKey(KeyCode.W) ? 1f : 0f;
        moveInput.y += Input.GetKey(KeyCode.S) ? -1f : 0f;
        moveInput.x += Input.GetKey(KeyCode.D) ? 1f : 0f;
        moveInput.x += Input.GetKey(KeyCode.A) ? -1f : 0f;
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);
        float2 lookInput = new float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));


        Dependency = new FirstPersonPlayerJob
        {
            fixedTick = fixedTick,
            moveInput = moveInput,
            jumpInput = jumpInput,
            lookInput = lookInput,
            firstPersonCharacterInputs = SystemAPI.GetComponentLookup<FirstPersonCharacterInputs>(),
            firstPersonCharacterComponent = SystemAPI.GetComponentLookup<FirstPersonCharacterComponent>(isReadOnly: true),
            localToWorld = SystemAPI.GetComponentLookup<LocalToWorld>(),

        }.Schedule(Dependency);

    }
}
