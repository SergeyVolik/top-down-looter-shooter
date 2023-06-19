using SV.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
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


    [WithAll(typeof(PlayerComponent), typeof(ReadPlayerInputComponent))]
    [BurstCompile]
    public partial struct TopDownPlayerSystemJob : IJobEntity
    {
        public float2 moveInput;
        public bool jumpInput;


        public uint fixedTick;


        [BurstCompile]
        public void Execute(ref TopDownPlayer player, ref TopDownCharacterInputs characterInputs)
        {

         

          
            characterInputs.MoveVector = new float3(moveInput.x, 0, moveInput.y);
            

            // Jump
            // Punctual input presses need special handling when they will be used in a fixed step system.
            // We essentially need to remember if the button was pressed at any point over the last fixed update
            if (player.LastInputsProcessingTick == fixedTick)
            {
                characterInputs.JumpRequested = jumpInput || characterInputs.JumpRequested;
            }
            else
            {
                characterInputs.JumpRequested = jumpInput;
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
