using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace SV.ECS
{
    public partial class PlayerInputSystem : SystemBase
    {
        private PlayerControlls input;

        protected override void OnCreate()
        {
            base.OnCreate();
            input = new PlayerControlls();



        }
        protected override void OnUpdate()
        {

            var moveInput = input.Controlls.Move.ReadValue<Vector2>();
            var y = Input.GetAxis("Vertical");
            var x = Input.GetAxis("Horizontal");

            moveInput = new Vector2(x, y);

            Entities.ForEach((ref CharacterMoveInputComponent moveInputComp) =>
            {

                moveInputComp.value = moveInput;

            }).WithAll<ReadPlayerInputComponent, PlayerComponent>().Run();
        }
    }

    public partial class ApplyCharacterMoveInputSystem : SystemBase
    {


        protected override void OnUpdate()
        {


            Entities.ForEach((ref PhysicsVelocity vel, in CharacterMoveInputComponent moveInput, in CharacterControllerComponent cc) =>
            {

                var linerVel = vel.Linear;
                var moveValue = moveInput.value.normalized * cc.speed;
                vel.Linear = new Unity.Mathematics.float3(moveValue.x, linerVel.y, moveValue.y);


               

            }).Run();
        }

    }

    public partial class LifetimeSystem : SystemBase
    {
      

        protected override void OnCreate()
        {
            base.OnCreate();

        }

        protected override void OnUpdate()
        {
            var delatTime = SystemAPI.Time.DeltaTime;

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            Entities.ForEach((ref Entity e, ref CurrentLifetimeComponent vel, in LifetimeComponent moveInput) =>
            {

                vel.value += delatTime;

                if (vel.value >= moveInput.value)
                {
                    ecb.DestroyEntity(e);
                }


            }).Schedule();

            Dependency.Complete();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

    }


    public partial class LookWithCharacterMoveInputSystem : SystemBase
    {


        protected override void OnUpdate()
        {


            Entities.ForEach((ref LocalTransform transform, in CharacterMoveInputComponent moveInput) =>
            {

             
                var moveValue = moveInput.value;
               

                if (moveValue != Vector2.zero)
                {
                    var lookPos = transform.Position;
                    var lookPos2 = lookPos;
                    lookPos2.x += moveInput.value.x;
                    lookPos2.z += moveInput.value.y;

                    Debug.DrawLine(lookPos, lookPos2, Color.red, 0.5f);

                    var vector = lookPos2 - lookPos;


                    transform.Rotation = quaternion.LookRotation(vector, math.up());
                }

            }).Run();
        }

    }

    public partial class CastAimTargetSystem : SystemBase
    {

        protected override void OnCreate()
        {
            
            base.OnCreate();
        }

        protected unsafe override void OnUpdate()
        {
            //var physWorld = GetSingleton<PhysicsWorldSingleton>();

            //var filter = new CollisionFilter()
            //{
            //    BelongsTo = ~0u,
            //    CollidesWith = ~0u, // all 1s, so all layers, collide with everything
            //    GroupIndex = 0
            //};

            //SphereGeometry sphereGeometry = new SphereGeometry() { Center = float3.zero, Radius = 1 };

            //BlobAssetReference<Unity.Physics.Collider> sphereCollider = Unity.Physics.SphereCollider.Create(sphereGeometry, filter);


            //ColliderCastInput input = new ColliderCastInput()
            //{
            //    Collider = (Unity.Physics.Collider*)sphereCollider.GetUnsafePtr(),
            //    Orientation = quaternion.identity,
            //    //Start = RayFrom,
            //    //End = RayTo
            //};

            //ColliderCastHit hit = new ColliderCastHit();
            //bool haveHit = physWorld.CastCollider(input, out hit);

            //sphereCollider.Dispose();

            ////physWorld.CastCollider();
        }

    }
}

