using SV.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;



[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class TopDownChracterInputSystem : SystemBase
{
    public FixedUpdateTickSystem FixedUpdateTickSystem;

    private PlayerControlls input;

    protected override void OnCreate()
    {
        base.OnCreate();
        input = new PlayerControlls();
        input.Enable();
        FixedUpdateTickSystem = World.GetOrCreateSystemManaged<FixedUpdateTickSystem>();
    }


    [WithAll(typeof(PlayerComponent), typeof(ReadPlayerInputComponent), typeof(GhostOwnerIsLocal))]
    [BurstCompile]
    public partial struct TopDownPlayerSystemJob : IJobEntity
    {
        public float2 moveInput;
        public bool jumpInput;


        public uint fixedTick;


        [BurstCompile]
        public void Execute(ref TopDownPlayer player, ref TopDownCharacterInputs characterInputs)
        {

            characterInputs.moveX = moveInput.x; 
            characterInputs.moveY = moveInput.y;

            //Debug.Log($"input: {characterInputs.moveX} {characterInputs.moveY}");
            if (jumpInput)
            {
                characterInputs.JumpRequested.Set();
            }
            player.LastInputsProcessingTick = fixedTick;
        }
    }

    protected override void OnUpdate()
    {
        uint fixedTick = FixedUpdateTickSystem.FixedTick;

        // Gather raw input
        float2 moveInput = input.Controlls.Move.ReadValue<Vector2>();




        bool jumpInput = false;//input.Controlls.Jump.triggered;


        var job = new TopDownPlayerSystemJob
        {
            fixedTick = fixedTick,
            jumpInput = jumpInput,
            moveInput = moveInput
           
        };

    
        //job.Run();
        Dependency = job.Schedule(Dependency);

    }
}
