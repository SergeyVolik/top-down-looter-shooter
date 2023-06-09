using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(GhostInputSystemGroup), OrderFirst = true)]
public partial class ThirdPersonPlayerInputSystem : SystemBase
{
   
    private PlayerControlls m_Input;

    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate<OrbitCamera>();
       
        m_Input = new PlayerControlls();
        m_Input.Enable();
    }


    [WithAll(typeof(ThirdPersonPlayer), typeof(GhostOwnerIsLocal))]
    [BurstCompile]
    public partial struct ThirdPersonPlayerSystemJob : IJobEntity
    {
        public float2 moveInput;
        public bool jumpInput;
        public bool sprint;
        public bool attack;
        public float cameraZoomInput;
        public float2 cameraLookInput;
       

        public Entity cameraEntity;

        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;
        public ComponentLookup<OrbitCameraInputs> orbitalCameraInputLookup;

        [BurstCompile]
        public void Execute(ref ThirdPersonCharacterInputs characterInputs)
        {



            quaternion cameraRotation = localTransformLookup.GetRefRO(cameraEntity).ValueRO.Rotation;
            float3 cameraForwardOnUpPlane = math.normalizesafe(Rival.MathUtilities.ProjectOnPlane(Rival.MathUtilities.GetForwardFromRotation(cameraRotation), math.up()));
            float3 cameraRight = Rival.MathUtilities.GetRightFromRotation(cameraRotation);

            // Move
            characterInputs.MoveVector = (moveInput.y * cameraForwardOnUpPlane) + (moveInput.x * cameraRight);
            characterInputs.MoveVector = Rival.MathUtilities.ClampToMaxLength(characterInputs.MoveVector, 1f);
            characterInputs.JumpRequested = default;
            characterInputs.AttackRequested = default;
            characterInputs.sprint = sprint;
            if (jumpInput)
            {
                characterInputs.JumpRequested.Set();
            }

            if (attack)
            {
                characterInputs.AttackRequested.Set();
            }



            // Camera control
            if (orbitalCameraInputLookup.HasComponent(cameraEntity))
            {
                var cameraInputs = orbitalCameraInputLookup.GetRefRW(cameraEntity);
                cameraInputs.ValueRW.Look = cameraLookInput;
                cameraInputs.ValueRW.Zoom = cameraZoomInput;


            }


        }
    }

    bool sprint;
    protected override void OnUpdate()
    {
     

        // Gather raw input
        float2 moveInput = m_Input.Controlls.Move.ReadValue<Vector2>();
       

        var orbitCamera = SystemAPI.GetSingletonEntity<OrbitCamera>();
        bool jumpInput = m_Input.Controlls.Jump.triggered;
        bool attckInput = m_Input.Controlls.Attack.triggered;

        if (m_Input.Controlls.Sprint.triggered)
        {
            sprint = !sprint;
        }
       
        float2 cameraLookInput = m_Input.Controlls.Camera.ReadValue<Vector2>();
        float cameraZoomInput = -m_Input.Controlls.Zoom.ReadValue<Vector2>().y;

        var job = new ThirdPersonPlayerSystemJob
        {
            cameraLookInput = cameraLookInput,
            cameraZoomInput = cameraZoomInput,           
            jumpInput = jumpInput,
            moveInput = moveInput,
            localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
            orbitalCameraInputLookup = SystemAPI.GetComponentLookup<OrbitCameraInputs>(isReadOnly: false),
            cameraEntity = orbitCamera,
            sprint = sprint,
            attack = attckInput,
        };
        //job.Run();
        Dependency = job.Schedule(Dependency);

    }
}
