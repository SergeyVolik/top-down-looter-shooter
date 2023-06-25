using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
public partial class ThirdPersonPlayerSystem : SystemBase
{
    public FixedUpdateTickSystem FixedUpdateTickSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate<OrbitCamera>();
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

        public Entity cameraEntity;

        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;
        public ComponentLookup<OrbitCameraInputs> orbitalCameraInputLookup;

        [BurstCompile]
        public void Execute(ref ThirdPersonPlayer player, ref ThirdPersonCharacterInputs characterInputs)
        {



            quaternion cameraRotation = localTransformLookup.GetRefRO(cameraEntity).ValueRO.Rotation;
            float3 cameraForwardOnUpPlane = math.normalizesafe(Rival.MathUtilities.ProjectOnPlane(Rival.MathUtilities.GetForwardFromRotation(cameraRotation), math.up()));
            float3 cameraRight = Rival.MathUtilities.GetRightFromRotation(cameraRotation);

            // Move
            characterInputs.MoveVector = (moveInput.y * cameraForwardOnUpPlane) + (moveInput.x * cameraRight);
            characterInputs.MoveVector = Rival.MathUtilities.ClampToMaxLength(characterInputs.MoveVector, 1f);
            characterInputs.JumpRequested = default;
            // Jump
            // Punctual input presses need special handling when they will be used in a fixed step system.
            // We essentially need to remember if the button was pressed at any point over the last fixed update
            if (player.LastInputsProcessingTick == fixedTick)
            {
                if (jumpInput || characterInputs.JumpRequested.IsSet)
                    characterInputs.JumpRequested.Set();
            }
            else if(jumpInput)
            {
                characterInputs.JumpRequested.Set();
            }




            // Camera control
            if (orbitalCameraInputLookup.HasComponent(cameraEntity))
            {
                var cameraInputs = orbitalCameraInputLookup.GetRefRW(cameraEntity);
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

        var orbitCamera = SystemAPI.GetSingletonEntity<OrbitCamera>();
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
            cameraEntity = orbitCamera
        };

        Dependency = job.Schedule(Dependency);

    }
}
