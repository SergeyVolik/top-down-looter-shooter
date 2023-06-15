using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
public partial class ThirdPersonPlayerSystem : SystemBase
{
    public FixedUpdateTickSystem FixedUpdateTickSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        FixedUpdateTickSystem = World.GetOrCreateSystemManaged<FixedUpdateTickSystem>();
    }


    [BurstCompile]
    public partial struct ThirdPersonPlayerSystemJob : IJobEntity
    {
        public float2 moveInput;
        public bool jumpInput;
        public float cameraZoomInput;
        public float2 cameraLookInput;
        public uint fixedTick;

        public ComponentLookup<ThirdPersonCharacterInputs> thirdPersonCharacterInputsLookup;

        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;
        public ComponentLookup<OrbitCameraInputs> orbitalCameraInputLookup;

        [BurstCompile]
        public void Execute(ref ThirdPersonPlayer player)
        {
            if (thirdPersonCharacterInputsLookup.HasComponent(player.ControlledCharacter))
            {
                var characterInputs = thirdPersonCharacterInputsLookup.GetRefRW(player.ControlledCharacter);

                quaternion cameraRotation = localTransformLookup.GetRefRO(player.ControlledCamera).ValueRO.Rotation;
                float3 cameraForwardOnUpPlane = math.normalizesafe(Rival.MathUtilities.ProjectOnPlane(Rival.MathUtilities.GetForwardFromRotation(cameraRotation), math.up()));
                float3 cameraRight = Rival.MathUtilities.GetRightFromRotation(cameraRotation);

                // Move
                characterInputs.ValueRW.MoveVector = (moveInput.y * cameraForwardOnUpPlane) + (moveInput.x * cameraRight);
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


            }

            // Camera control
            if (orbitalCameraInputLookup.HasComponent(player.ControlledCamera))
            {
                var cameraInputs = orbitalCameraInputLookup.GetRefRW(player.ControlledCamera);
                cameraInputs.ValueRW.Look = cameraLookInput;
                cameraInputs.ValueRW.Zoom = cameraZoomInput;


            }

            player.LastInputsProcessingTick = fixedTick;
        }
    }

    protected override void OnUpdate()
    {
        uint fixedTick = FixedUpdateTickSystem.FixedTick;

        // Gather raw input
        float2 moveInput = float2.zero;
        moveInput.y += Input.GetKey(KeyCode.W) ? 1f : 0f;
        moveInput.y += Input.GetKey(KeyCode.S) ? -1f : 0f;
        moveInput.x += Input.GetKey(KeyCode.D) ? 1f : 0f;
        moveInput.x += Input.GetKey(KeyCode.A) ? -1f : 0f;
       

        bool jumpInput = Input.GetKeyDown(KeyCode.Space);
        float2 cameraLookInput = new float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        float cameraZoomInput = -Input.mouseScrollDelta.y;

        var job = new ThirdPersonPlayerSystemJob
        {
            cameraLookInput = cameraLookInput,
            cameraZoomInput = cameraZoomInput,
            fixedTick = fixedTick,
            jumpInput = jumpInput,
            moveInput = moveInput,
            localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
            orbitalCameraInputLookup = SystemAPI.GetComponentLookup<OrbitCameraInputs>(isReadOnly: false),
            thirdPersonCharacterInputsLookup = SystemAPI.GetComponentLookup<ThirdPersonCharacterInputs>(isReadOnly: false),
        };

        Dependency = job.Schedule(Dependency);

    }
}
